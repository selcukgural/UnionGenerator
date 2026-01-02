using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;

namespace UnionGenerator.Analyzers;
#pragma warning disable RS1038
[DiagnosticAnalyzer(LanguageNames.CSharp)]
#pragma warning restore RS1038
public class UnionDebugVisualizerAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "UG3002";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Add debugger visualization attributes",
        "Union type '{0}' should include debugging visualization attributes DebuggerDisplay and DebuggerTypeProxy",
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Adding DebuggerDisplay and DebuggerTypeProxy attributes improves debug-time visualization for generated union types.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var named = (INamedTypeSymbol)context.Symbol;

        // Only analyze types explicitly annotated with [GenerateUnion]
        var hasAttr = named.GetAttributes().Any(a =>
                                                    a.AttributeClass != null &&
                                                    (a.AttributeClass.Name == "GenerateUnionAttribute" || a.AttributeClass.Name == "GenerateUnion" ||
                                                     a.AttributeClass.ToDisplayString().EndsWith(".GenerateUnionAttribute", StringComparison.Ordinal)));
        if (!hasAttr)
        {
            return;
        }

        // Only consider named types (classes) for debugger visualization
        if (named.TypeKind != TypeKind.Class && named.TypeKind != TypeKind.Struct)
        {
            return;
        }

        // Check for existing DebuggerDisplay or DebuggerTypeProxy attributes
        var hasDebuggerDisplay = named.GetAttributes().Any(a => a.AttributeClass?.Name == "DebuggerDisplayAttribute");
        var hasDebuggerTypeProxy = named.GetAttributes().Any(a => a.AttributeClass?.Name == "DebuggerTypeProxyAttribute");

        if (hasDebuggerDisplay && hasDebuggerTypeProxy)
        {
            return;
        }

        var location = named.Locations.FirstOrDefault() ?? Location.None;
        var diag = Diagnostic.Create(Rule, location, named.Name);
        context.ReportDiagnostic(diag);
    }
}