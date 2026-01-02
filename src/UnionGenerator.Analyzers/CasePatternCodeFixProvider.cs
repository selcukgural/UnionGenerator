using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnionGenerator.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CasePatternCodeFixProvider)), Shared]
public class CasePatternCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CasePatternAnalyzer.DiagnosticId);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        var diag = context.Diagnostics.FirstOrDefault();
        if (diag == null) return;

        var token = root.FindToken(diag.Location.SourceSpan.Start);
        var ifNode = token.Parent?.AncestorsAndSelf().OfType<IfStatementSyntax>().FirstOrDefault();
        if (ifNode == null) return;

        context.RegisterCodeFix(
            Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(title: "Replace with generated FromOneOf/TryFromOneOf adapter",
                                                                 createChangedDocument: c => ReplaceWithAdapterAsync(context.Document, ifNode, c),
                                                                 equivalenceKey: "ReplaceWithOneOfAdapter"), diag);
    }

    public async Task<Document> ReplaceWithAdapterAsync(Document document, IfStatementSyntax ifNode, CancellationToken cancellationToken)
    {
        var semantic = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        // Heuristic: find the OneOf variable identifier from the first condition (x.IsT0)
        var chain = ifNode;
        var conditions = new System.Collections.Generic.List<MemberAccessExpressionSyntax>();
        var statements = new System.Collections.Generic.List<StatementSyntax>();
        StatementSyntax? finalElse = null;

        while (chain != null!)
        {
            if (chain.Condition is MemberAccessExpressionSyntax m)
            {
                conditions.Add(m);
                statements.Add(chain.Statement);
            }
            else
            {
                // unsupported pattern - bail out
                break;
            }

            if (chain.Else?.Statement is IfStatementSyntax elseIf)
            {
                chain = elseIf;
            }
            else
            {
                if (chain.Else?.Statement != null)
                {
                    finalElse = chain.Else.Statement;
                }

                break;
            }
        }

        if (conditions.Count == 0)
        {
            return document;
        }

        // Try to infer a FromOneOf replacement from factory calls inside branches
        var paramTypes = new System.Collections.Generic.List<ITypeSymbol>();

        if (paramTypes == null)
        {
            throw new ArgumentNullException(nameof(paramTypes));
        }

        const bool allBranchesReturn = true;
        string? assignedTarget = null;

        // Prefer to infer type arguments directly from the OneOf receiver's declared type
        INamedTypeSymbol? oneOfNamed = null;
        var firstCond = conditions.FirstOrDefault();

        if (firstCond != null)
        {
            if (semantic.GetTypeInfo(firstCond.Expression).Type is INamedTypeSymbol recvType)
            {
                // If it's a OneOf<> constructed type, capture it
                if (recvType.Name == "OneOf" || recvType.ConstructedFrom.Name == "OneOf" ||
                    recvType.ContainingNamespace?.ToDisplayString() == "OneOf")
                {
                    oneOfNamed = recvType;
                }
            }
        }

        // Determine the receiver expression string
        var receiverExpr = conditions[0].Expression.ToString();

        // Get syntax root once and reuse to avoid duplicate local names
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException();

        // If the receiver is OneOf<T...>, prefer those type args for FromOneOf
        if (oneOfNamed != null)
        {
            var typeArgs = string.Join(",", oneOfNamed.TypeArguments.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

            // If the arity matches number of conditions, we can safely emit FromOneOf<T...>()
            if (oneOfNamed.TypeArguments.Length >= conditions.Count)
            {
                // If original branches returned, emit return pattern
                if (allBranchesReturn)
                {
                    var newStmt = SyntaxFactory.ParseStatement($"return {receiverExpr}.FromOneOf<{typeArgs}>();")
                                               .WithLeadingTrivia(ifNode.GetLeadingTrivia()).WithTrailingTrivia(ifNode.GetTrailingTrivia());
                    return document.WithSyntaxRoot(root.ReplaceNode(ifNode, newStmt));
                }
            }
        }

        // Fallback to previous param-type based heuristics
        if (paramTypes.Count == conditions.Count)
        {
            var typeArgs = string.Join(",", paramTypes.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

            var newStmt = SyntaxFactory.ParseStatement($"return {receiverExpr}.FromOneOf<{typeArgs}>();")
                                       .WithLeadingTrivia(ifNode.GetLeadingTrivia()).WithTrailingTrivia(ifNode.GetTrailingTrivia());
            return document.WithSyntaxRoot(root.ReplaceNode(ifNode, newStmt));
        }

        if (paramTypes.Count == conditions.Count && assignedTarget != null)
        {
            var typeArgs = string.Join(",", paramTypes.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

            var newStmt = SyntaxFactory.ParseStatement($"{assignedTarget} = {receiverExpr}.FromOneOf<{typeArgs}>();")
                                       .WithLeadingTrivia(ifNode.GetLeadingTrivia()).WithTrailingTrivia(ifNode.GetTrailingTrivia());
            return document.WithSyntaxRoot(root.ReplaceNode(ifNode, newStmt));
        }

        var caseLines = new System.Text.StringBuilder();

        for (var i = 0; i < conditions.Count; i++)
        {
            var name = conditions[i].Name.Identifier.Text; // e.g., IsT0
            int idx;

            if (name.Length > 3 && name.StartsWith("IsT", StringComparison.Ordinal) && int.TryParse(name.Substring(3), out var parsed))
            {
                idx = parsed;
            }
            else
            {
                // fallback: sequential mapping
                idx = i;
            }

            var body = statements[i].ToString();
            // ensure body ends with a break or return - we will add break
            caseLines.AppendLine($"case {idx}:");
            caseLines.AppendLine(body);

            // if body doesn't contain a terminal statement, add break
            if (!body.Trim().EndsWith("return;", StringComparison.Ordinal) && !body.Trim().EndsWith("return;\r", StringComparison.Ordinal) && !body.Trim().EndsWith("break;", StringComparison.Ordinal))
            {
                caseLines.AppendLine("    break;");
            }
        }

        var defaultBody = finalElse != null ? finalElse.ToString() : "// no default case";

        // build the index expression chain: var __ug_idx = receiver.IsT0 ? 0 : receiver.IsT1 ? 1 : -1;
        var idxExprSb = new System.Text.StringBuilder();

        for (var i = 0; i < conditions.Count; i++)
        {
            var cond = conditions[i];
            var name = cond.Name.Identifier.Text;
            var idx = (name.Length > 3 && name.StartsWith("IsT", StringComparison.Ordinal) && int.TryParse(name.Substring(3), out var p)) ? p : i;
            if (i > 0) idxExprSb.Append(" : ");
            idxExprSb.AppendFormat("{0}.{1} ? {2}", receiverExpr, name, idx);
        }

        idxExprSb.Append(" : -1");

        var switchCode =
            $"{{\n    var __ug_idx = {idxExprSb};\n    switch(__ug_idx)\n    {{\n{caseLines}    default:\n{defaultBody}\n        break;\n    }}\n}}";

        var newNode = SyntaxFactory.ParseStatement(switchCode).WithLeadingTrivia(ifNode.GetLeadingTrivia())
                                   .WithTrailingTrivia(ifNode.GetTrailingTrivia());
        return document.WithSyntaxRoot(root.ReplaceNode(ifNode, newNode));
    }
}