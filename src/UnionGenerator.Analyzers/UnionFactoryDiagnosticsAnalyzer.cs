using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UnionGenerator.Analyzers;

/// <summary>
/// Analyzer that reports diagnostics about union factory methods and missing cases.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnionFactoryDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic id reported when no factory methods (cases) are found for a [GenerateUnion] type.
    /// </summary>
    public const string MissingCasesId = "UG9002";

    /// <summary>
    /// Diagnostic id reported when a factory method has multiple parameters (not yet supported).
    /// </summary>
    public const string MultipleParametersId = "UG9003";

    /// <summary>
    /// Diagnostic id reported when duplicated factory signatures are found.
    /// </summary>
    public const string DuplicateCaseId = "UG9004";

    private static readonly DiagnosticDescriptor MissingCasesRule = new(
        MissingCasesId,
        "No union cases found",
        "No union cases (static factory methods) were found for '{0}'. The generator will not produce any union code.",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MultipleParametersRule = new(
        MultipleParametersId,
        "Factory method has multiple parameters",
        "Factory method '{0}' has multiple parameters. Only single-parameter or parameterless factory methods are currently supported for code generation.",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateCaseRule = new(
        DuplicateCaseId,
        "Duplicate union case signature",
        "Multiple factory methods with the signature '{0}' were found on '{1}'. Case factory signatures should be unique.",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(MissingCasesRule, MultipleParametersRule, DuplicateCaseRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        // Register symbol action to inspect named types (classes/structs) for [GenerateUnion]
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var named = (INamedTypeSymbol)context.Symbol;

        if (named.TypeKind != TypeKind.Class && named.TypeKind != TypeKind.Struct)
        {
            return;
        }

        // Only types annotated with [GenerateUnion]
        if (!HasGenerateUnionAttribute(named))
        {
            return;
        }

        // Find static factory methods declared on this type that return the union type
        var staticMethods = named.GetMembers().OfType<IMethodSymbol>()
                                 .Where(m => m.IsStatic && SymbolEqualityComparer.Default.Equals(m.ReturnType, named) && m.DeclaredAccessibility == Accessibility.Public && SymbolEqualityComparer.Default.Equals(m.ContainingType, named))
                                 .ToList();

        // Report missing cases
        if (staticMethods.Count == 0)
        {
            var loc = named.Locations.FirstOrDefault() ?? Location.None;
            context.ReportDiagnostic(Diagnostic.Create(MissingCasesRule, loc, named.Name));
            return;
        }

        // Report multi-parameter factories
        foreach (var method in staticMethods)
        {
            if (method.Parameters.Length > 1)
            {
                var loc = method.Locations.FirstOrDefault() ?? named.Locations.FirstOrDefault() ?? Location.None;
                context.ReportDiagnostic(Diagnostic.Create(MultipleParametersRule, loc, method.Name));
            }
        }

        // Detect duplicate signatures (name|paramcount|param types)
        var signatures = staticMethods.Select(m => new
        {
            Method = m,
            Key = m.Name + "|" + m.Parameters.Length + "|" + string.Join(",", m.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
        }).ToList();

        var dupes = signatures.GroupBy(s => s.Key, StringComparer.Ordinal).Where(g => g.Count() > 1).SelectMany(g => g.Skip(1)).ToList();

        foreach (var d in dupes)
        {
            var loc = d.Method.Locations.FirstOrDefault() ?? named.Locations.FirstOrDefault() ?? Location.None;
            context.ReportDiagnostic(Diagnostic.Create(DuplicateCaseRule, loc, d.Key, named.Name));
        }
    }

    private static bool HasGenerateUnionAttribute(INamedTypeSymbol typeSymbol)
    {
        try
        {
            foreach (var attr in typeSymbol.GetAttributes())
            {
                var cls = attr.AttributeClass;
                if (cls == null)
                {
                    continue;
                }

                var full = cls.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (full == "global::UnionGenerator.Attributes.GenerateUnionAttribute" || full.EndsWith(".GenerateUnionAttribute", StringComparison.Ordinal))
                {
                    return true;
                }

                if (cls.Name == "GenerateUnionAttribute")
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