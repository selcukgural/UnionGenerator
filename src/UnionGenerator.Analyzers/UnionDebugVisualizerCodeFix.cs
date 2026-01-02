using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace UnionGenerator.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnionDebugVisualizerCodeFix)), Shared]
public class UnionDebugVisualizerCodeFix : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UnionDebugVisualizerAnalyzer.DiagnosticId);
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;
        var diag = context.Diagnostics.FirstOrDefault();
        if (diag == null) return;
        var token = root.FindToken(diag.Location.SourceSpan.Start);
        var typeDecl = token.Parent?.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        if (typeDecl == null) return;

        context.RegisterCodeFix(
            Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
                "Add DebuggerDisplay and DebuggerTypeProxy attributes",
                c => AddAttributesAsync(context.Document, typeDecl, c),
                "AddDebuggerVisualizers"),
            diag);
    }

    /// <summary>
    /// Add DebuggerDisplay and DebuggerTypeProxy attributes to the given type declaration.
    /// This method is resilient to the caller providing a <see cref="TypeDeclarationSyntax"/> that belongs to a different
    /// syntax tree than the target <see cref="Document"/> (the unit tests do this). In that case the corresponding node
    /// from the document's syntax root is located and used for semantic operations and replacement.
    /// </summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="typeDecl">A type declaration syntax node (may come from another syntax tree).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated document with attributes added, or the original document if the operation cannot be performed.</returns>
    public async Task<Document> AddAttributesAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
    {
        // Obtain semantic model and document root up front
        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var docRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (model == null || docRoot == null)
        {
            return document;
        }

        // Ensure we operate on the node instance that belongs to the document's syntax tree.
        var nodeInDoc = docRoot.FindToken(typeDecl.SpanStart).Parent?.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        if (nodeInDoc == null)
        {
            // Unable to locate corresponding node in the document; nothing to do.
            return document;
        }

        var hasDebuggerDisplay = nodeInDoc.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString().Contains("DebuggerDisplay"));
        var hasDebuggerTypeProxy = nodeInDoc.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString().Contains("DebuggerTypeProxy"));

        var newDecl = nodeInDoc;
        if (!hasDebuggerDisplay)
        {
            var attr = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.ParseName("System.Diagnostics.DebuggerDisplay"), SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("{ToString()}"))))))));
            newDecl = newDecl.AddAttributeLists(attr);
        }

        if (!hasDebuggerTypeProxy)
        {
            // Attempt to resolve the declared type symbol to build a closed-generic proxy type expression
            string proxyCode;

            if (model.GetDeclaredSymbol(nodeInDoc) is { } sym)
            {
                var fullType = sym.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                proxyCode = $"typeof(UnionGenerator.Internal.DebuggerTypeProxies.GenericUnionDebuggerProxy<{fullType}>)";
            }
            else
            {
                // Fallback to the previous open-generic expression
                proxyCode = "typeof(UnionGenerator.Internal.DebuggerTypeProxies.GenericUnionDebuggerProxy<>)";
            }

            var attr = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.ParseName("System.Diagnostics.DebuggerTypeProxy"), SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression(proxyCode)))))));
            newDecl = newDecl.AddAttributeLists(attr);
        }

        var newRoot = docRoot.ReplaceNode(nodeInDoc, newDecl);
        return document.WithSyntaxRoot(newRoot);
    }
}