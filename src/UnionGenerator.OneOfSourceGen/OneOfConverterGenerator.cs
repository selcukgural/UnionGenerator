using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace UnionGenerator.OneOfSourceGen;

[Generator]
public class OneOfConverterGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor MissingFactoryDescriptor = new(
        id: "UG2001", title: "Generated union type missing expected factory method",
        messageFormat:
        "Union type '{0}' must expose a public static factory method '{1}({2})', for example: public static {3} {1}({2} value) => new /*Case*/({2} value)",
        category: "Usage", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true,
        description: "The generated union type must provide public static factory methods for each case.");

    private static readonly DiagnosticDescriptor FactoryParameterMismatchDescriptor = new(
        id: "UG2002", title: "Factory method parameter type mismatch",
        messageFormat:
        "Factory method '{0}' on '{1}' has parameter type '{2}' but expected '{3}' - expected signature: public static {1} {0}({3} value)",
        category: "Usage", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true,
        description: "The factory method's parameter type must match the corresponding union case type parameter.");

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Minimal POC: emit a small helper class during post-initialization
        context.RegisterPostInitializationOutput(ctx =>
        {
            const string src = "namespace UnionGenerator.OneOfSourceGen { public static class GeneratedOneOfHelpers { public static string Info() => \"OneOf POC\"; } }";
            ctx.AddSource("GeneratedOneOfHelpers.g.cs", SourceText.From(src, Encoding.UTF8));
        });

        // Find class declarations that use the [GenerateUnion] attribute
        var candidates = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 }, transform: (ctx, _) =>
            {
                var classDecl = (ClassDeclarationSyntax)ctx.Node;
                var model = ctx.SemanticModel;
                var symbol = model.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;

                if (symbol == null)
                {
                    return null;
                }

                // Check attributes by name to avoid referencing attribute type directly
                if (symbol.GetAttributes().Any(a => a.AttributeClass?.Name == "GenerateUnionAttribute" || a.AttributeClass?.Name == "GenerateUnion"))
                {
                    return symbol;
                }

                return null;
            }).Where(s => s is not null).Select((s, _) => s!);

        context.RegisterSourceOutput(candidates, (spc, typeSymbol) =>
        {
            // Determine arity and type parameter names
            var arity = typeSymbol.Arity;

            if (arity == 0)
            {
                // currently skip non-generic unions
                return;
            }

            // Check if OneOf library is available in the compilation
            var oneOfSymbol = typeSymbol.ContainingModule.ReferencedAssemblySymbols
                                        .FirstOrDefault(a => a.Name == "OneOf");
            
            if (oneOfSymbol == null)
            {
                // OneOf library not referenced; skip adapter generation
                return;
            }

            // Collect candidate factory methods: public static methods with one parameter returning the union type
            var candidateFactories = typeSymbol.GetMembers().OfType<IMethodSymbol>()
                                               .Where(m => m.IsStatic && m is { DeclaredAccessibility: Accessibility.Public, Parameters.Length: 1 })
                                               .ToList();

            // Map from type parameter index to factory method name (for generic unions)
            var factoryMap = new Dictionary<int, string>();

            foreach (var f in candidateFactories)
            {
                var pType = f.Parameters[0].Type;

                // If parameter is the actual type-parameter symbol, map by index
                if (pType is ITypeParameterSymbol tp)
                {
                    for (int idx = 0; idx < typeSymbol.TypeParameters.Length; idx++)
                    {
                        if (SymbolEqualityComparer.Default.Equals(typeSymbol.TypeParameters[idx], tp))
                        {
                            if (!factoryMap.ContainsKey(idx))
                            {
                                factoryMap[idx] = f.Name;
                            }

                            break;
                        }
                    }

                    continue;
                }

                // Try to infer the expected index by conventional factory name
                int? expectedIndex = null;
                var name = f.Name;
                if (arity == 2 && name == "Ok") expectedIndex = 0;
                if (arity == 2 && name == "Error") expectedIndex = 1;

                if (name.StartsWith("Case", StringComparison.Ordinal) && name.Length > 4 && int.TryParse(name.Substring(4), out var parsed))
                {
                    expectedIndex = parsed;
                }

                if (expectedIndex is >= 0 && expectedIndex.Value < arity)
                {
                    // Check if parameter type matches the corresponding type parameter
                    var expectedTypeParam = typeSymbol.TypeParameters[expectedIndex.Value];

                    if (!SymbolEqualityComparer.Default.Equals(pType, expectedTypeParam))
                    {
                        var diag = Diagnostic.Create(FactoryParameterMismatchDescriptor,
                                                     f.Locations.FirstOrDefault() ?? typeSymbol.Locations.FirstOrDefault() ?? Location.None, f.Name,
                                                     typeSymbol.Name, pType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                                                     expectedTypeParam.Name);
                        spc.ReportDiagnostic(diag);
                        // Do not map this factory because parameter type mismatches
                    }
                    else
                    {
                        if (!factoryMap.ContainsKey(expectedIndex.Value))
                        {
                            factoryMap[expectedIndex.Value] = f.Name;
                        }
                    }
                }
            }

            // Resolve the most helpful diagnostic location: prefer the [GenerateUnion] attribute location if available
            Location diagLocation = typeSymbol.Locations.FirstOrDefault() ?? Location.None;

            var genAttr = typeSymbol.GetAttributes()
                                    .FirstOrDefault(a => a.AttributeClass?.Name == "GenerateUnionAttribute" ||
                                                         a.AttributeClass?.Name == "GenerateUnion");

            if (genAttr?.ApplicationSyntaxReference != null)
            {
                var syntax = genAttr.ApplicationSyntaxReference.GetSyntax();
                var loc = syntax.GetLocation();

                diagLocation = loc;
            }

            // Report diagnostics for missing factories with actionable suggestions
            for (int i = 0; i < arity; i++)
            {
                if (factoryMap.ContainsKey(i))
                {
                    continue;
                }

                // Suggest conventional names: Ok/Error for 2-case unions, otherwise Case{index}
                var suggestedName = (arity == 2) ? (i == 0 ? "Ok" : "Error") : $"Case{i}";
                var suggestedParam = "T" + i;

                var constructedTypeName = typeSymbol.Name +
                                          (arity > 0
                                               ? "<" + string.Join(",", Enumerable.Range(0, arity).Select(j => "T" + j)) + ">"
                                               : string.Empty);

                var diag = Diagnostic.Create(MissingFactoryDescriptor, diagLocation, typeSymbol.Name, suggestedName, suggestedParam,
                                             constructedTypeName);
                spc.ReportDiagnostic(diag);
            }

            // Build type name strings for generation
            var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace ? null : ("global::" + typeSymbol.ContainingNamespace.ToDisplayString());
            var typeBase = ns is null ? typeSymbol.Name : ns + "." + typeSymbol.Name;
            var genericParams = Enumerable.Range(0, arity).Select(i => "T" + i).ToArray();

            var adapterName = typeSymbol.Name + "OneOfAdapter";
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("using System;");
            sb.AppendLine("using OneOf;");
            sb.AppendLine();

            if (ns is not null)
            {
                var namespaceName = typeSymbol.ContainingNamespace.ToDisplayString();
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Adapter helpers to convert OneOf<{string.Join(",", genericParams)}> to {typeSymbol.Name} unions.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    /// <example>");
            sb.AppendLine("    /// <code>");
            sb.AppendLine("    /// // Example usage - shows how to convert a OneOf into the generated union.");
            sb.AppendLine($"    /// var one = global::OneOf.OneOf<{string.Join(",", genericParams)}>.FromT0(default); ");
            sb.AppendLine($"    /// var union = one.FromOneOf<{string.Join(",", genericParams)}>();");
            sb.AppendLine("    /// </code>");
            sb.AppendLine("    /// </example>");
            sb.AppendLine("    /// <remarks>");

            sb.AppendLine(
                "    /// This adapter is generated at compile-time and calls the generated union's static factory methods directly (no reflection).");

            sb.AppendLine(
                "    /// It improves runtime performance and provides IntelliSense via XML documentation. Ensure the generated union exposes the expected static factories (e.g. Ok/Error).");
            sb.AppendLine("    /// </remarks>");
            sb.AppendLine($"    public static class {adapterName}");
            sb.AppendLine("    {");

            // Generate generic signature
            sb.AppendLine($"        /// <summary>Converts a OneOf<{string.Join(",", genericParams)}> to the generated union type.</summary>");

            for (int i = 0; i < genericParams.Length; i++)
            {
                sb.AppendLine($"        /// <typeparam name=\"{genericParams[i]}\">Case type {i}</typeparam>");
            }

            sb.AppendLine("        /// <param name=\"one\">The OneOf instance</param>");
            sb.AppendLine("        /// <returns>The generated union instance.</returns>");

            sb.AppendLine(
                "        /// <exception cref=\"System.InvalidOperationException\">Thrown when the OneOf value cannot be mapped to a factory on the generated union type. See diagnostics UG2001/UG2002 for actionable fixes.</exception>");

            sb.AppendLine(
                "        /// <seealso cref=\"global::UnionGenerator.OneOfCompat.OneOfCompat\">Use OneOfCompat helpers if you prefer a reflection-based conversion.</seealso>");

            var genericDecl = "<" + string.Join(",", genericParams) + ">";

            sb.AppendLine(
                $"        public static {typeBase}{genericDecl} FromOneOf{genericDecl}(this global::OneOf.OneOf<{string.Join(",", genericParams)}> one)");
            sb.AppendLine("        {");

            // Generate branch checks using OneOf's IsTn and AsTn
            for (int i = 0; i < arity; i++)
            {
                var isProp = $"one.IsT{i}";
                var asProp = $"one.AsT{i}";

                if (i == 0)
                {
                    sb.AppendLine($"            if ({isProp})");
                    sb.AppendLine("            {");
                }
                else
                {
                    sb.AppendLine($"            else if ({isProp})");
                    sb.AppendLine("            {");
                }

                sb.AppendLine($"                var v = {asProp};");
                // attempt to find factory by parameter type name
                var keyIndex = i;

                sb.AppendLine(factoryMap.TryGetValue(keyIndex, out var factoryName)
                                  ? $"                return {typeBase}{genericDecl}.{factoryName}(v);"
                                  // fallback: try common names by index (Case0/Case1) or default to throw with diagnostic
                                  : $"                throw new InvalidOperationException(\"No factory found for case {i} on type {typeSymbol.Name}\");");

                sb.AppendLine("            }");
            }

            sb.AppendLine("            throw new InvalidOperationException(\"Unsupported OneOf variant.\");");
            sb.AppendLine("        }");

            // Generate a TryFromOneOf helper that does not throw but returns bool + out param
            sb.AppendLine();

            sb.AppendLine(
                $"        /// <summary>Attempts to convert a OneOf<{string.Join(",", genericParams)}> to the generated union type without throwing.</summary>");

            for (int i = 0; i < genericParams.Length; i++)
            {
                sb.AppendLine($"        /// <typeparam name=\"{genericParams[i]}\">Case type {i}</typeparam>");
            }

            sb.AppendLine("        /// <param name=\"one\">The OneOf instance</param>");

            sb.AppendLine(
                "        /// <param name=\"result\">When this method returns, contains the converted union if successful; otherwise default.</param>");
            sb.AppendLine("        /// <returns>True if conversion succeeded; otherwise false.</returns>");

            sb.AppendLine(
                $"        public static bool TryFromOneOf{genericDecl}(this global::OneOf.OneOf<{string.Join(",", genericParams)}> one, out {typeBase}{genericDecl} result)");
            sb.AppendLine("        {");

            for (int i = 0; i < arity; i++)
            {
                var isProp = $"one.IsT{i}";
                var asProp = $"one.AsT{i}";

                if (i == 0)
                {
                    sb.AppendLine($"            if ({isProp})");
                    sb.AppendLine("            {");
                }
                else
                {
                    sb.AppendLine($"            else if ({isProp})");
                    sb.AppendLine("            {");
                }

                sb.AppendLine($"                var v = {asProp};");
                var keyIndex = i;
                sb.AppendLine("                try");
                sb.AppendLine("                {");

                if (factoryMap.TryGetValue(keyIndex, out var factoryName))
                {
                    sb.AppendLine($"                    result = {typeBase}{genericDecl}.{factoryName}(v);");
                    sb.AppendLine("                    return true;");
                }
                else
                {
                    sb.AppendLine("                    result = default!;");
                    sb.AppendLine("                    return false;");
                }

                sb.AppendLine("                }");
                sb.AppendLine("                catch");
                sb.AppendLine("                {");
                sb.AppendLine("                    result = default!;");
                sb.AppendLine("                    return false;");
                sb.AppendLine("                }");

                sb.AppendLine("            }");
            }

            sb.AppendLine("            result = default!;");
            sb.AppendLine("            return false;");
            sb.AppendLine("        }");

            sb.AppendLine("    }");

            if (ns is not null)
            {
                sb.AppendLine("}");
            }

            var hintName = typeSymbol.Name + ".OneOfAdapter.g.cs";
            spc.AddSource(hintName, SourceText.From(sb.ToString(), Encoding.UTF8));
        });
    }
}