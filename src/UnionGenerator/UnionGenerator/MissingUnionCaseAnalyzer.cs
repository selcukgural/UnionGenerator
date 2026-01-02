using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UnionGenerator;

/// <summary>
/// Analyzer that warns when a switch expression/statement on a generated union type
/// does not handle all union cases and there is no discard/default arm.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingUnionCaseAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic id for missing union case coverage.
    /// </summary>
    public const string DiagnosticId = "UG002";

    private static readonly LocalizableString Title = "Missing union case handling";
    private static readonly LocalizableString MessageFormat = "Switch on union '{0}' does not handle all cases. Missing: {1}. Consider adding a pattern for the missing case(s) or a discard/default arm ('_').";
    private static readonly LocalizableString Description = "When switching over a generated union type, all cases should be handled or a discard/default arm should be present to avoid missing behaviour when new cases are added.";
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    /// <summary>
    /// Supported diagnostics by this analyzer.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <summary>
    /// Registers analysis actions.
    /// </summary>
    /// <param name="context">Context for registering actions.</param>
    public override void Initialize(AnalysisContext context)
    {
        // Conserve analyzer resources and avoid analyzing generated code
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Register a compilation start action to capture state and avoid repeated work
        context.RegisterCompilationStartAction(compilationContext =>
        {
            // Nothing to cache at compilation start for now; register syntax node actions
            compilationContext.RegisterSyntaxNodeAction(AnalyzeSwitchExpression, SyntaxKind.SwitchExpression);
            compilationContext.RegisterSyntaxNodeAction(AnalyzeSwitchStatement, SyntaxKind.SwitchStatement);
        });
    }

    /// <summary>
    /// Analyzes a switch expression for missing union case coverage.
    /// </summary>
    /// <param name="context">The analysis context for the syntax node.</param>
    private static void AnalyzeSwitchExpression(SyntaxNodeAnalysisContext context)
    {
        var switchExpr = (SwitchExpressionSyntax)context.Node;

        var semanticModel = context.SemanticModel;
        var governingType = semanticModel.GetTypeInfo(switchExpr.GoverningExpression, context.CancellationToken).Type as INamedTypeSymbol;

        if (governingType == null)
        {
            return;
        }

        // Only analyze types explicitly marked with [GenerateUnion]
        if (!HasGenerateUnionAttribute(governingType))
        {
            return;
        }

        var unionCases = GetNestedCaseTypes(governingType);

        if (unionCases.Length == 0)
        {
            return;
        }

        var covered = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        var hasDiscard = false;

        foreach (var arm in switchExpr.Arms)
        {
            var pattern = arm.Pattern;

            if (pattern is DiscardPatternSyntax)
            {
                hasDiscard = true;
                break;
            }

            if (pattern is DeclarationPatternSyntax declPattern)
            {
                var typeSyntax = declPattern.Type;

                if (semanticModel.GetTypeInfo(typeSyntax, context.CancellationToken).Type is INamedTypeSymbol typeSymbol)
                {
                    foreach (var caseType in unionCases)
                    {
                        if (!SymbolEqualityComparer.Default.Equals(caseType, typeSymbol.OriginalDefinition) &&
                            !SymbolEqualityComparer.Default.Equals(caseType, typeSymbol))
                        {
                            continue;
                        }

                        covered.Add(caseType.Name);
                        break;
                    }
                }
            }

            if (pattern is not VarPatternSyntax)
            {
                continue;
            }

            // var pattern acts like a catch-all
            hasDiscard = true;
            break;
        }

        if (hasDiscard)
        {
            return;
        }

        var missing = unionCases.Where(c => !covered.Contains(c.Name)).Select(c => c.Name).ToArray();

        if (missing.Length <= 0)
        {
            return;
        }

        var diag = Diagnostic.Create(Rule, switchExpr.GetLocation(), governingType.Name, string.Join(", ", missing));
        context.ReportDiagnostic(diag);
    }

    /// <summary>
    /// Analyzes a switch statement for missing union case coverage.
    /// </summary>
    /// <param name="context">The analysis context for the syntax node.</param>
    private static void AnalyzeSwitchStatement(SyntaxNodeAnalysisContext context)
    {
        var switchStmt = (SwitchStatementSyntax)context.Node;

        var semanticModel = context.SemanticModel;
        var governingType = semanticModel.GetTypeInfo(switchStmt.Expression, context.CancellationToken).Type as INamedTypeSymbol;

        if (governingType == null)
        {
            return;
        }

        // Only analyze types explicitly marked with [GenerateUnion]
        if (!HasGenerateUnionAttribute(governingType))
        {
            return;
        }

        var unionCases = GetNestedCaseTypes(governingType);

        if (unionCases.Length == 0)
        {
            return;
        }

        var covered = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        var hasDefault = false;

        foreach (var section in switchStmt.Sections)
        {
            foreach (var label in section.Labels)
            {
                if (label is DefaultSwitchLabelSyntax)
                {
                    hasDefault = true;
                    break;
                }

                if (label is not CasePatternSwitchLabelSyntax casePatternLabel)
                {
                    continue;
                }

                var pattern = casePatternLabel.Pattern;
                if (pattern is DiscardPatternSyntax)
                {
                    hasDefault = true;
                    break;
                }

                if (pattern is DeclarationPatternSyntax declPattern)
                {
                    var typeSyntax = declPattern.Type;

                    if (semanticModel.GetTypeInfo(typeSyntax, context.CancellationToken).Type is INamedTypeSymbol typeSymbol)
                    {
                        foreach (var caseType in unionCases)
                        {
                            if (!SymbolEqualityComparer.Default.Equals(caseType, typeSymbol.OriginalDefinition) &&
                                !SymbolEqualityComparer.Default.Equals(caseType, typeSymbol))
                            {
                                continue;
                            }

                            covered.Add(caseType.Name);
                            break;
                        }
                    }
                }

                if (pattern is not VarPatternSyntax)
                {
                    continue;
                }

                hasDefault = true;
                break;
            }

            if (hasDefault)
            {
                break;
            }
        }

        if (hasDefault)
        {
            return;
        }

        var missing = unionCases.Where(c => !covered.Contains(c.Name)).Select(c => c.Name).ToArray();

        if (missing.Length <= 0)
        {
            return;
        }

        var diag = Diagnostic.Create(Rule, switchStmt.GetLocation(), governingType.Name, string.Join(", ", missing));
        context.ReportDiagnostic(diag);
    }

    /// <summary>
    /// Gets nested case types of a named type by convention: nested types whose name ends with "Case".
    /// </summary>
    /// <param name="typeSymbol">The candidate type to inspect.</param>
    /// <returns>Array of nested case type symbols.</returns>
    private static INamedTypeSymbol[] GetNestedCaseTypes(INamedTypeSymbol typeSymbol)
    {
        try
        {
            var nested = typeSymbol.GetTypeMembers();
            var caseTypes = nested.Where(n => n.Name.EndsWith("Case", StringComparison.Ordinal)).ToArray();
            return caseTypes;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Determines whether the supplied type symbol is marked with GenerateUnionAttribute (either source or compiled attribute).
    /// </summary>
    /// <param name="typeSymbol">Type a symbol to inspect.</param>
    /// <returns>True if the type is marked with GenerateUnionAttribute; otherwise false.</returns>
    private static bool HasGenerateUnionAttribute(INamedTypeSymbol typeSymbol)
    {
        try
        {
            foreach (var attr in typeSymbol.GetAttributes())
            {
                var attrClass = attr.AttributeClass;
                if (attrClass == null)
                {
                    continue;
                }

                // Check the full metadata name first
                var fullName = attrClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (fullName == "global::UnionGenerator.Attributes.GenerateUnionAttribute" || fullName.EndsWith("GenerateUnionAttribute", StringComparison.Ordinal))
                {
                    return true;
                }

                // Fallback: check simple name and namespace
                if (attrClass is { Name: "GenerateUnionAttribute", ContainingNamespace: not null } &&
                    attrClass.ContainingNamespace.ToDisplayString() == "UnionGenerator.Attributes")
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}