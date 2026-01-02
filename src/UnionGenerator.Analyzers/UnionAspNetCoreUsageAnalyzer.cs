using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UnionGenerator.Analyzers;

/// <summary>
/// Analyzer that detects common misuse patterns with union types in ASP.NET Core controllers.
/// </summary>
/// <remarks>
/// <para>
/// Performs static analysis on controller methods to ensure:
/// - Union result types are properly handled (not returned raw to clients)
/// - Convention-based status code inference is correctly configured
/// - All error cases have appropriate status codes
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnionAspNetCoreUsageAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID: Union type returned without mapping to IActionResult.
    /// </summary>
    public const string UnionNotMappedToActionResultId = "UG4010";

    /// <summary>
    /// Diagnostic ID: Union error case lacks status code convention.
    /// </summary>
    public const string ErrorCaseWithoutStatusCodeId = "UG4011";

    /// <summary>
    /// Diagnostic ID: Status code convention registered but override missing.
    /// </summary>
    public const string ConventionOverrideRecommendedId = "UG4012";

    private static readonly DiagnosticDescriptor UnionNotMappedToActionResultRule = new(
        UnionNotMappedToActionResultId,
        "Union result not mapped to IActionResult",
        "Method '{0}' returns a union type directly. Consider mapping it to IActionResult using ToActionResult() or equivalent.",
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Union types should be converted to IActionResult for proper HTTP responses.");

    private static readonly DiagnosticDescriptor ErrorCaseWithoutStatusCodeRule = new(
        ErrorCaseWithoutStatusCodeId,
        "Error case lacks explicit status code",
        "Error type '{0}' used in union does not have an explicit status code. Consider adding [UnionStatusCode] attribute or implementing StatusCode property.",
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Error cases should declare their HTTP status code for proper response handling.");

    private static readonly DiagnosticDescriptor ConventionOverrideRecommendedRule = new(
        ConventionOverrideRecommendedId,
        "Convention-based status code can be overridden",
        "Error type '{0}' may be inferred by name-based convention, but explicit [UnionStatusCode] attribute is recommended for clarity",
        "Usage",
        DiagnosticSeverity.Hidden,
        isEnabledByDefault: false,
        description: "While convention-based inference works, explicit attributes make intent clearer.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            UnionNotMappedToActionResultRule,
            ErrorCaseWithoutStatusCodeRule,
            ConventionOverrideRecommendedRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(AnalyzeMethodSymbol, SymbolKind.Method);
    }

    private static void AnalyzeMethodSymbol(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;

        // Only analyze public methods (likely controller actions)
        if (method.DeclaredAccessibility != Accessibility.Public)
        {
            return;
        }

        // Skip if the method doesn't have a return type or is async void
        if (method.ReturnType == null! || method.ReturnType.SpecialType == SpecialType.System_Void)
        {
            return;
        }

        var returnType = method.ReturnType;

        // Check for async return types (Task<T>, ValueTask<T>)
        if (IsTaskType(returnType, out var underlyingType))
        {
            returnType = underlyingType;
        }

        // Check if return type is a union-like type (has union static factory pattern)
        if (IsUnionType(returnType))
        {
            // Check if it's being converted to IActionResult
            if (!IsIActionResult(returnType) && !HasToActionResultCallSite(method, returnType))
            {
                var location = method.Locations.FirstOrDefault() ?? Location.None;
                context.ReportDiagnostic(Diagnostic.Create(
                    UnionNotMappedToActionResultRule,
                    location,
                    method.Name));
            }

            // Analyze error cases in the union
            AnalyzeUnionErrorCases(context, returnType, method);
        }
    }

    /// <summary>
    /// Checks if a type represents Task{T} or ValueTask{T} and extracts T.
    /// </summary>
    private static bool IsTaskType(ITypeSymbol type, out ITypeSymbol underlyingType)
    {
        underlyingType = null!;

        if (type is INamedTypeSymbol namedType)
        {
            var name = namedType.Name;
            if ((name == "Task" || name == "ValueTask") && namedType.TypeArguments.Length == 1)
            {
                underlyingType = namedType.TypeArguments[0];
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Detects if a type looks like a union (has static factory methods returning itself).
    /// </summary>
    private static bool IsUnionType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        // Look for static methods that return the same type (pattern matching)
        var staticMethods = namedType.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.IsStatic && SymbolEqualityComparer.Default.Equals(m.ReturnType, namedType))
            .ToList();

        // If there are 2+ static factory methods, it's likely a union
        return staticMethods.Count >= 2;
    }

    /// <summary>
    /// Checks if a type is IActionResult or derived from it.
    /// </summary>
    private static bool IsIActionResult(ITypeSymbol type)
    {
        if (type.Name == "IActionResult" || type.ToDisplayString().Contains("IActionResult"))
        {
            return true;
        }

        // Check base types
        if (type is INamedTypeSymbol namedType)
        {
            foreach (var baseType in namedType.AllInterfaces)
            {
                if (baseType.Name == "IActionResult")
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the method body likely calls ToActionResult() on the union.
    /// (This is a heuristic; full control-flow analysis would be more accurate.)
    /// </summary>
    private static bool HasToActionResultCallSite(IMethodSymbol method, ITypeSymbol unionType)
    {
        // This is a simplified heuristic - a full implementation would use syntax analysis
        // For now, we assume if the method returns IActionResult directly, it's fine
        return IsIActionResult(method.ReturnType);
    }

    /// <summary>
    /// Analyzes union error cases to check for status code conventions.
    /// </summary>
    private static void AnalyzeUnionErrorCases(SymbolAnalysisContext context, ITypeSymbol unionType, IMethodSymbol method)
    {
        if (unionType is not INamedTypeSymbol namedType)
        {
            return;
        }

        // Get all static factory methods (representing union cases)
        var staticMethods = namedType.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.IsStatic && SymbolEqualityComparer.Default.Equals(m.ReturnType, namedType) && m.DeclaredAccessibility == Accessibility.Public)
            .ToList();

        // Check each error-like case (those with parameter types that look like errors)
        foreach (var caseMethod in staticMethods)
        {
            if (caseMethod.Parameters.Length != 1)
            {
                continue;
            }

            var paramType = caseMethod.Parameters[0].Type;
            var paramName = paramType.Name;

            // Heuristic: if parameter name contains "Error" or "Failure", it's likely an error case
            if (paramName.IndexOf("Error", StringComparison.OrdinalIgnoreCase) < 0 &&
                paramName.IndexOf("Failure", StringComparison.OrdinalIgnoreCase) < 0 &&
                paramName.IndexOf("Exception", StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            // Check if error type has status code attribute
            if (!HasUnionStatusCodeAttribute(paramType))
            {
                var methodLocation = caseMethod.Locations.FirstOrDefault() ?? Location.None;
                context.ReportDiagnostic(Diagnostic.Create(
                    ErrorCaseWithoutStatusCodeRule,
                    methodLocation,
                    paramName));
            }
        }
    }

    /// <summary>
    /// Checks if a type has [UnionStatusCode] attribute.
    /// </summary>
    private static bool HasUnionStatusCodeAttribute(ITypeSymbol typeSymbol)
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

                if (attrClass.Name == "UnionStatusCodeAttribute" || attrClass.ToDisplayString().IndexOf("UnionStatusCodeAttribute", StringComparison.Ordinal) >= 0)
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

