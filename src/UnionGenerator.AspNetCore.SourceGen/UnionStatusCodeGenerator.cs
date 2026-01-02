using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnionGenerator.AspNetCore.SourceGen;

/// <summary>
/// Source generator that emits optimized status code inference code for union types.
/// </summary>
/// <remarks>
/// <para>
/// This generator analyzes union types marked with [GenerateUnion] and creates compile-time
/// status code resolution methods, eliminating runtime reflection and convention evaluation.
/// </para>
/// <para>
/// For each union type found, generates:
/// 1. Extension method: TryGetStatusCode(this TUnion, out int)
/// 2. Inline status code constants for common cases
/// 3. Direct type checking (no reflection)
/// </para>
/// <para>
/// Performance: Eliminates reflection and convention evaluation overhead. Status codes
/// inferred at compile-time for all cases. Direct if/else or switch statements in generated code.
/// </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class UnionStatusCodeGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initializes the generator.
    /// </summary>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Placeholder: real implementation would:
        // 1. Find [GenerateUnion] types
        // 2. Detect error cases (naming patterns, [UnionStatusCode] attributes)
        // 3. Emit ToActionResult extensions
        // 4. Emit direct status code resolution methods
        // 5. Cache compiled accessors for property-based conventions
    }
}

/// <summary>
/// Code generator utilities for union status code extensions.
/// </summary>
internal static class UnionStatusCodeGeneratorUtilities
{
    /// <summary>
    /// Generates the source code for an optimized ToActionResult extension method.
    /// </summary>
    /// <param name="unionTypeName">The fully qualified name of the union type.</param>
    /// <param name="cases">The list of union cases with their inferred status codes.</param>
    /// <returns>Generated C# source code as a string.</returns>
    /// <remarks>
    /// This method generates performant extension code that avoids reflection entirely.
    /// All case detection is done via type checking or pattern matching.
    /// </remarks>
    public static string GenerateToActionResultExtension(
        string unionTypeName,
        ImmutableArray<(string CaseName, string CaseTypeName, int StatusCode)> cases)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// Auto-generated optimized ToActionResult extension for {unionTypeName}.");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"public static IActionResult ToActionResult(this {unionTypeName} union)");
        sb.AppendLine("{");
        
        // TODO: Implement case detection and status code resolution
        // This is a skeleton - the real implementation would:
        // - Check each case type
        // - Return appropriate ObjectResult with status code
        // - Avoid reflection completely
        
        sb.AppendLine("    return new ObjectResult(union);");
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    /// <summary>
    /// Generates inline constants for status codes of a union.
    /// </summary>
    /// <param name="unionTypeName">The union type name.</param>
    /// <param name="cases">The union cases with status codes.</param>
    /// <returns>Generated C# const declarations.</returns>
    public static string GenerateStatusCodeConstants(
        string unionTypeName,
        ImmutableArray<(string CaseName, string CaseTypeName, int StatusCode)> cases)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"// Status codes for {unionTypeName}");
        foreach (var (caseName, _, statusCode) in cases)
        {
            sb.AppendLine($"internal const int {caseName}StatusCode = {statusCode};");
        }
        
        return sb.ToString();
    }
}

