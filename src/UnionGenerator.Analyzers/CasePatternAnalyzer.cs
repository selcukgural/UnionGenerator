using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UnionGenerator.Analyzers;
#pragma warning disable RS1038
[DiagnosticAnalyzer(LanguageNames.CSharp)]
#pragma warning restore RS1038
public class CasePatternAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "UG3001";
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Consider using generated adapter for OneOf conversions",
        "This if/else chain can be simplified by using the generated FromOneOf adapter or TryFromOneOf helper",
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Detects repetitive if/else patterns over OneOf.IsTn properties and suggests using generated adapter methods for clarity and maintainability.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
    }

    private void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
    {
        var ifStmt = (IfStatementSyntax)context.Node;
        // Simple heuristic: look for 'if (x.IsT0) { ... } else if (x.IsT1) { ... }' chains
        var chain = ifStmt;
        int count = 0;
        while (chain != null!)
        {
            var cond = chain.Condition as MemberAccessExpressionSyntax;
            if (cond == null)
            {
                break;
            }

            // Ensure receiver type is OneOf<...>
            var receiver = cond.Expression;
            var typeInfo = context.SemanticModel.GetTypeInfo(receiver);
            var named = typeInfo.Type as INamedTypeSymbol;
            if (named == null)
            {
                break;
            }

            var isOneOf = named.Name == "OneOf" || named.ConstructedFrom?.Name == "OneOf" || named.ContainingNamespace?.ToDisplayString() == "OneOf";
            if (!isOneOf)
            {
                break;
            }

            if (cond.Name.Identifier.Text.StartsWith("IsT", StringComparison.Ordinal))
            {
                count++;
            }
            else
            {
                break;
            }

            if (chain.Else?.Statement is IfStatementSyntax elseIf)
            {
                chain = elseIf;
            }
            else
            {
                break;
            }
        }

        if (count < 2)
        {
            return;
        }

        var diag = Diagnostic.Create(Rule, ifStmt.IfKeyword.GetLocation());
        context.ReportDiagnostic(diag);
    }
}