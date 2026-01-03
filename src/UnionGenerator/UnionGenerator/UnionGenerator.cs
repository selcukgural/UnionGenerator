using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

// No alias; use InternalUnionCase explicitly to avoid conflicts

namespace UnionGenerator;

/// <summary>
/// Source generator that creates discriminated union-like structures for classes marked with the [GenerateUnion] attribute.
/// </summary>
[Generator]
public sealed class UnionGenerator : ISourceGenerator
{
    /// <summary>
    /// Initializes the generator and registers the syntax receiver.
    /// </summary>
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new UnionSyntaxReceiver());
    }

    /// <summary>
    /// Executes the generator and produces source code for union types.
    /// </summary>
    public void Execute(GeneratorExecutionContext context)
    {
        // Embed the GenerateUnionAttribute so users don't need to reference it
        context.AddSource("GenerateUnionAttribute.g.cs", SourceText.From(@"
using System;

namespace UnionGenerator.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class GenerateUnionAttribute : Attribute
    {
    }
}", Encoding.UTF8));

        if (context.SyntaxReceiver is not UnionSyntaxReceiver receiver)
        {
            return;
        }

        // Track processed class symbols to avoid duplicates
        var processedClasses = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var classDecl in receiver.UnionClasses)
        {
            try
            {
                var semanticModel = context.Compilation.GetSemanticModel(classDecl.SyntaxTree);
                INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDecl);

                if (classSymbol == null)
                {
                    continue;
                }

                // Skip if already processed
                if (!processedClasses.Add(classSymbol))
                {
                    continue;
                }

                // Analyze union cases from static factory methods
                var cases = AnalyzeUnionCases(classSymbol, classDecl, context);

                if (cases.Count == 0)
                {
                    // No valid cases found, skip generation, but diagnostics were already reported
                    continue;
                }

                // Generate the union code
                var source = GenerateUnionCode(classSymbol, cases);

                // Add the generated source
                var fileName = $"{classSymbol.Name}.g.cs";
                context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                // Report diagnostic for generator errors
                var descriptor = new DiagnosticDescriptor("UG001", "Union Generator Error", "Error generating union code: {0}", "UnionGenerator",
                                                          DiagnosticSeverity.Error, isEnabledByDefault: true);

                context.ReportDiagnostic(Diagnostic.Create(descriptor, classDecl.GetLocation(), ex.Message));
            }
        }
    }

    /// <summary>
    /// Analyzes the union type to find all static factory methods that represent union cases.
    /// Reports diagnostics for ignored methods and duplicates.
    /// </summary>
    private List<InternalUnionCase> AnalyzeUnionCases(INamedTypeSymbol unionType,
                                                      Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax classDecl,
                                                      GeneratorExecutionContext context)
    {
        var cases = new List<InternalUnionCase>();

        // Find all static methods declared on the union type that return the union type
        var staticMethods = unionType.GetMembers().OfType<IMethodSymbol>()
                                     .Where(m => m.IsStatic && m.ReturnType.Equals(unionType, SymbolEqualityComparer.Default) &&
                                                 m.DeclaredAccessibility == Accessibility.Public &&
                                                 SymbolEqualityComparer.Default.Equals(m.ContainingType, unionType)).ToList();

        if (staticMethods.Count == 0)
        {
            var descriptor = new DiagnosticDescriptor("UG9002", "No union cases found",
                                                      "No union cases (static factory methods) were found for '{0}'. The generator will not produce any union code.",
                                                      "UnionGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);
            context.ReportDiagnostic(Diagnostic.Create(descriptor, classDecl.GetLocation(), unionType.Name));
            return cases;
        }

        foreach (var method in staticMethods)
        {
            // For iteration 1, only support methods with 0 or 1 parameters; ignore others for code generation
            if (method.Parameters.Length > 1)
            {
                var descriptor = new DiagnosticDescriptor("UG9003", "Factory method has multiple parameters",
                                                          "Factory method '{0}' has multiple parameters. Only single-parameter or parameterless factory methods are currently supported for code generation.",
                                                          "UnionGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);
                var loc = method.Locations.FirstOrDefault() ?? classDecl.GetLocation();
                context.ReportDiagnostic(Diagnostic.Create(descriptor, loc, method.Name));
                continue;
            }

            var caseName = method.Name;
            ITypeSymbol? valueType = null;
            string? valueName = null;

            if (method.Parameters.Length == 1)
            {
                valueType = method.Parameters[0].Type;
                valueName = method.Parameters[0].Name;
            }

            var unionCase = new InternalUnionCase
            {
                Name = caseName,
                ValueType = valueType,
                ValueName = valueName
            };

            // Add parameter types and names for multi-parameter cases
            foreach (var parameter in method.Parameters)
            {
                unionCase.ParameterTypes.Add(parameter.Type);
                unionCase.ParameterNames.Add(parameter.Name);
            }

            cases.Add(unionCase);
        }

        // Detect duplicate signatures (name|paramcount|param types)
        var signatures = staticMethods.Select(m => new
        {
            Method = m,
            Key = m.Name + "|" + m.Parameters.Length + "|" +
                  string.Join(",", m.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
        }).ToList();

        var dupeGroups = signatures.GroupBy(s => s.Key, StringComparer.Ordinal).Where(g => g.Count() > 1).ToList();

        foreach (var group in dupeGroups)
        {
            var descriptor = new DiagnosticDescriptor("UG9004", "Duplicate union case signature",
                                                      "Multiple factory methods with the signature '{0}' were found on '{1}'. Case factory signatures should be unique.",
                                                      "UnionGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            foreach (var d in group)
            {
                var loc = d.Method.Locations.FirstOrDefault() ?? classDecl.GetLocation();
                context.ReportDiagnostic(Diagnostic.Create(descriptor, loc, d.Key, unionType.Name));
            }
        }

        if (cases.Count == 0 && staticMethods.Count > 0)
        {
            // If we have static methods but none were valid cases (e.g., all had >1 params), 
            // we should still return something so it doesn't trigger UG9002 incorrectly if there are other errors
        }

        return cases;
    }

    /// <summary>
    /// Generates the source code for the union type with nested case classes and pattern matching properties.
    /// </summary>
    private string GenerateUnionCode(INamedTypeSymbol unionType, List<InternalUnionCase> cases)
    {
        var sb = new StringBuilder();
        var namespaceName = unionType.ContainingNamespace.ToDisplayString();
        var className = unionType.Name;
        var typeParameters = unionType.TypeParameters;

        // Check if the original source uses file-scoped namespace
        var useFileScopedNamespace = false;

        try
        {
            if (unionType.DeclaringSyntaxReferences.Length > 0)
            {
                var syntaxRef = unionType.DeclaringSyntaxReferences[0];
                var syntax = syntaxRef.GetSyntax();


                var root = syntax.SyntaxTree.GetRoot();

                // Check if there's a file-scoped namespace in the same file
                var fileScopedNs = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.FileScopedNamespaceDeclarationSyntax>()
                                       .FirstOrDefault();

                if (fileScopedNs != null)
                {
                    var nsName = fileScopedNs.Name.ToString();

                    // Match namespace name (handle both full and partial matches)
                    if (nsName == namespaceName || namespaceName.EndsWith("." + nsName, StringComparison.Ordinal) ||
                        nsName.EndsWith("." + namespaceName.Split('.').Last(), StringComparison.Ordinal))
                    {
                        useFileScopedNamespace = true;
                    }
                }
            }
        }
        catch
        {
            // If we can't determine, use traditional namespace
            useFileScopedNamespace = false;
        }

        // Add auto-generated header
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine();

        // Add nullable context for generated code
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        // Add necessary using statements for generated code
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using System.Diagnostics;");
        sb.AppendLine();

        // Namespace - use file-scoped namespace if the original uses it
        if (!string.IsNullOrEmpty(namespaceName))
        {
            if (useFileScopedNamespace)
            {
                sb.AppendLine($"namespace {namespaceName};");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }
        }

        // Class declaration with IEquatable interface
        var typeName = typeParameters.Length > 0 ? $"{className}<{string.Join(", ", typeParameters.Select(tp => tp.Name))}>" : className;

        // Add XML documentation for the union class
        GenerateUnionClassXmlDoc(sb, className, cases);

        // Add DebuggerDisplay and DebuggerTypeProxy attributes
        GenerateDebugAttributes(sb, className, cases, typeParameters);

        if (typeParameters.Length > 0)
        {
            var genParams = string.Join(", ", typeParameters.Select(tp => tp.Name));
            sb.AppendLine($"    public partial class {className}<{genParams}> : IEquatable<{typeName}>");
        }
        else
        {
            sb.AppendLine($"    public partial class {className} : IEquatable<{typeName}>");
        }

        sb.AppendLine("    {");

        // Generate nested case classes first (before the proxy class so it can reference them)
        foreach (var unionCase in cases)
        {
            GenerateCaseClass(sb, unionType, unionCase, typeParameters, cases);
        }

        // Generate pattern matching properties
        GeneratePatternMatchingProperties(sb, cases);

        // Generate value properties (Value, Error)
        GenerateValueProperties(sb, cases);

        // Generate Match method
        GenerateMatchMethod(sb, cases, typeParameters);

        // Generate equality methods (Equals, ==, !=)
        GenerateEqualityMethods(sb, className, cases, typeParameters);

        // Generate GetHashCode
        GenerateGetHashCode(sb, cases);

        // Generate ToString
        GenerateToString(sb, cases);

        // Generate Deconstruct method
        GenerateDeconstructMethod(sb, cases, typeParameters);

        // Generate TryGetValue methods
        GenerateTryGetValueMethods(sb, cases, typeParameters);

        // Generate Map/Select methods
        GenerateMapMethods(sb, className, cases, typeParameters);

        // Generate functional operators (Bind, Tap, Fold, etc.)
        GenerateFunctionalOperators(sb, className, cases, typeParameters);

        // Generate LINQ-like operators (Select, SelectMany, Where)
        GenerateLinqOperators(sb, className, cases, typeParameters);

        // Generate utility methods (OrElseThrow, Ensure, etc.)
        GenerateUtilityMethods(sb, className, cases, typeParameters);

        // Generate Async methods (BindAsync, MapAsync, MatchAsync)
        GenerateAsyncMethods(sb, className, cases, typeParameters);

        // Generate OrElse/Or methods
        GenerateOrElseMethods(sb, cases, typeParameters);

        sb.AppendLine("    }");

        // Generate debugger proxy class (outside union class, at namespace level)
        GenerateDebuggerProxyClass(sb, className, cases, typeParameters);

        // Generate async extension methods
        GenerateAsyncExtensions(sb, className, cases, typeParameters);

        if (!string.IsNullOrEmpty(namespaceName) && !useFileScopedNamespace)
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates a nested sealed class for a union case.
    /// </summary>
    private static void GenerateCaseClass(StringBuilder sb, INamedTypeSymbol unionType, InternalUnionCase unionCase,
                                          System.Collections.Immutable.ImmutableArray<ITypeParameterSymbol> typeParameters,
                                          List<InternalUnionCase> cases)
    {
        var caseClassName = $"{unionCase.Name}Case";

        // Add XML documentation for the case class
        sb.AppendLine("        /// <summary>");

        sb.AppendLine(unionCase.ValueType != null
                          ? $"        /// Represents the {unionCase.Name} case of the union, containing a value of type {GetTypeName(unionCase.ValueType)}."
                          : $"        /// Represents the {unionCase.Name} case of the union (unit-like case with no value).");

        sb.AppendLine("        /// </summary>");

        // Add the DebuggerDisplay attribute for case class
        // Use literal case name in DebuggerDisplay so the expression doesn't reference an out-of-scope identifier.
        sb.AppendLine(unionCase.ValueType != null
                          ? $"        [DebuggerDisplay(\"{unionCase.Name}({{Value}})\")] "
                          : $"        [DebuggerDisplay(\"{unionCase.Name}\")] ");

        // Nested classes inherit generic parameters from the outer class, so we don't redeclare them,
        // But we need to specify them in the base class reference
        if (typeParameters.Length > 0)
        {
            var genParams = string.Join(", ", typeParameters.Select(tp => tp.Name));
            sb.AppendLine($"        public sealed class {caseClassName} : {unionType.Name}<{genParams}>");
        }
        else
        {
            sb.AppendLine($"        public sealed class {caseClassName} : {unionType.Name}");
        }

        sb.AppendLine("        {");

        if (unionCase.ValueType != null)
        {
            // Value property - use 'new' if the base class has a Value property (for 2-case unions)
            var valueTypeName = GetTypeName(unionCase.ValueType!);
            // Check if the base union type has a Value property (for 2-case unions)
            // Both cases need 'new' if the base has Value property
            var baseHasValue = cases.Count == 2 && cases[0].ValueType != null;
            var newKeyword = baseHasValue ? "new " : "";
            sb.AppendLine("            /// <summary>");
            sb.AppendLine($"            /// Gets the value associated with the {unionCase.Name} case.");
            sb.AppendLine("            /// </summary>");
            sb.AppendLine($"            public {newKeyword}{valueTypeName} Value {{ get; }}");
            sb.AppendLine();

            // Constructor
            sb.AppendLine($"            internal {caseClassName}({valueTypeName} value)");
            sb.AppendLine("            {");
            sb.AppendLine("                Value = value;");
            sb.AppendLine("            }");
        }
        else
        {
            // Parameterless constructor for unit-like cases
            sb.AppendLine($"            internal {caseClassName}()");
            sb.AppendLine("            {");
            sb.AppendLine("            }");
        }

        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates pattern matching properties (IsOk, IsError, etc.).
    /// </summary>
    private static void GeneratePatternMatchingProperties(StringBuilder sb, List<InternalUnionCase> cases)
    {
        foreach (var unionCase in cases)
        {
            var propName = $"Is{unionCase.Name}";
            var caseClassName = $"{unionCase.Name}Case";

            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets a value indicating whether this union instance is the {unionCase.Name} case.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <returns><c>true</c> if this instance is the {unionCase.Name} case; otherwise, <c>false</c>.</returns>");
            // Case classes inherit from union base, so they need a 'new' keyword to hide base properties
            sb.AppendLine($"        public bool {propName} => this is {caseClassName};");
        }
    }

    /// <summary>
    /// Generates Value and Error properties for direct access to case values.
    /// For 2-case unions, generates Value (first case) and Error (second case) properties.
    /// </summary>
    private static void GenerateValueProperties(StringBuilder sb, List<InternalUnionCase> cases)
    {
        if (cases.Count != 2)
        {
            return;
        }

        // For 2-case unions, generate Value and Error properties
        var firstCase = cases[0];
        var secondCase = cases[1];

        string? nullableTypeName;
        string? caseClassName;

        // Value property for the first case (always named "Value")
        if (firstCase.ValueType != null)
        {
            var valueTypeName = GetTypeName(firstCase.ValueType);
            caseClassName = $"{firstCase.Name}Case";

            // Make nullable if it's a value type
            nullableTypeName = firstCase.ValueType.IsValueType ? $"{valueTypeName}?" : valueTypeName;

            sb.AppendLine("        /// <summary>");

            sb.AppendLine($"        /// Gets the value when this union is the {firstCase.Name} case, or <c>null</c>/<c>default</c> otherwise.");
            sb.AppendLine("        /// </summary>");

            sb.AppendLine(
                $"        /// <returns>The value if this is the {firstCase.Name} case; otherwise, <c>null</c> or <c>default</c>.</returns>");
            sb.AppendLine($"        public {nullableTypeName} Value");
            sb.AppendLine("        {");
            sb.AppendLine("            get");
            sb.AppendLine("            {");

            sb.AppendLine(firstCase.ValueType.IsValueType
                              ? $"                return this is {caseClassName} c ? c.Value : null;"
                              : $"                return this is {caseClassName} c ? c.Value : default!;");

            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        if (secondCase.ValueType == null)
        {
            return;
        }

        // Error property for the second case (use case name + "Value" to avoid conflicts)
        var errorTypeName = GetTypeName(secondCase.ValueType);
        caseClassName = $"{secondCase.Name}Case";

        // Make nullable if it's a value type
        nullableTypeName = secondCase.ValueType.IsValueType ? $"{errorTypeName}?" : errorTypeName;

        // Use case name + "Value" to avoid conflicts with static methods
        var propertyName = $"{secondCase.Name}Value";
        sb.AppendLine("        /// <summary>");

        sb.AppendLine($"        /// Gets the value when this union is the {secondCase.Name} case, or <c>null</c>/<c>default</c> otherwise.");
        sb.AppendLine("        /// </summary>");

        sb.AppendLine($"        /// <returns>The value if this is the {secondCase.Name} case; otherwise, <c>null</c> or <c>default</c>.</returns>");
        sb.AppendLine($"        public {nullableTypeName} {propertyName}");
        sb.AppendLine("        {");
        sb.AppendLine("            get");
        sb.AppendLine("            {");

        sb.AppendLine(secondCase.ValueType.IsValueType
                          ? $"                return this is {caseClassName} c ? c.Value : null;"
                          : $"                return this is {caseClassName} c ? c.Value : default!;");

        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates the Match method for pattern matching with func-based callbacks.
    /// </summary>
    private void GenerateMatchMethod(StringBuilder sb, List<InternalUnionCase> cases,
                                     System.Collections.Immutable.ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        _ = typeParameters;

        if (cases.Count == 0)
        {
            return;
        }

        // Generate Match method with generic return type
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Performs pattern matching on the union, calling the appropriate handler function based on the active case.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        /// <typeparam name=\"TResult\">The return type of all handler functions.</typeparam>");

        foreach (var unionCase in cases)
        {
            var docParam = NormalizeParamName(unionCase.Name);

            sb.AppendLine(unionCase.ValueType != null
                              ? $"        /// <param name=\"{docParam}\">Handler function for the {unionCase.Name} case.</param>"
                              : $"        /// <param name=\"{docParam}\">Handler function for the {unionCase.Name} case (no parameters).</param>");
        }

        sb.AppendLine("        /// <returns>The result of calling the appropriate handler function.</returns>");

        sb.AppendLine(
            "        /// <exception cref=\"InvalidOperationException\">Thrown when no case matches (should not occur in normal usage).</exception>");
        sb.AppendLine("        /// <example>");
        sb.AppendLine("        /// <code>");
        sb.AppendLine("        /// var result = union.Match(");

        foreach (var unionCase in cases)
        {
            sb.AppendLine(unionCase.ValueType != null
                              ? $"        ///     {unionCase.Name.ToLower()}: value => $\"Got {{value}}\", "
                              : $"        ///     {unionCase.Name.ToLower()}: () => \"No value\" ");
        }

        sb.AppendLine("        /// );");
        sb.AppendLine("        /// </code>");
        sb.AppendLine("        /// </example>");
        sb.Append("        public TResult Match<TResult>(");

        // Generate parameters for each case
        var parameters = new List<string>();

        foreach (var unionCase in cases)
        {
            var paramName = NormalizeParamName(unionCase.Name);

            if (unionCase.ValueType != null)
            {
                var valueTypeName = GetTypeName(unionCase.ValueType);
                parameters.Add($"Func<{valueTypeName}, TResult> {paramName}");
            }
            else
            {
                parameters.Add($"Func<TResult> {paramName}");
            }
        }

        sb.AppendLine(string.Join(", ", parameters));
        sb.AppendLine("        )");
        sb.AppendLine("        {");

        // Generate match logic for each case
        foreach (var unionCase in cases)
        {
            var caseClassName = $"{unionCase.Name}Case";
            var paramName = NormalizeParamName(unionCase.Name);

            if (unionCase.ValueType != null)
            {
                sb.AppendLine($"            if (this is {caseClassName} {paramName}Case)");
                sb.AppendLine("            {");
                sb.AppendLine($"                return {paramName}({paramName}Case.Value);");
                sb.AppendLine("            }");
            }
            else
            {
                sb.AppendLine($"            if (this is {caseClassName})");
                sb.AppendLine("            {");
                sb.AppendLine($"                return {paramName}();");
                sb.AppendLine("            }");
            }
        }

        sb.AppendLine("            throw new InvalidOperationException(\"Unmatched union case\");");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate void-returning Match overload that accepts Action/Action<T>
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Performs pattern matching on the union and executes the appropriate action for the active case.");
        sb.AppendLine("        /// </summary>");

        foreach (var unionCase in cases)
        {
            var docParam = NormalizeParamName(unionCase.Name);

            sb.AppendLine(unionCase.ValueType != null
                              ? $"        /// <param name=\"{docParam}\">Action to invoke when this is the {unionCase.Name} case.</param>"
                              : $"        /// <param name=\"{docParam}\">Action to invoke when this is the {unionCase.Name} case (no parameters).</param>");
        }

        sb.AppendLine(
            "        /// <exception cref=\"InvalidOperationException\">Thrown when no case matches (should not occur in normal usage).</exception>");
        sb.Append("        public void Match(");

        // Parameters for action overload
        var actionParams = new List<string>();

        foreach (var unionCase in cases)
        {
            var paramName = NormalizeParamName(unionCase.Name);

            if (unionCase.ValueType != null)
            {
                var valueTypeName = GetTypeName(unionCase.ValueType);
                actionParams.Add($"Action<{valueTypeName}> {paramName}");
            }
            else
            {
                actionParams.Add($"Action {paramName}");
            }
        }

        sb.AppendLine(string.Join(", ", actionParams));
        sb.AppendLine("        )");
        sb.AppendLine("        {");

        // Invoke actions
        foreach (var unionCase in cases)
        {
            var caseClassName = $"{unionCase.Name}Case";
            var paramName = NormalizeParamName(unionCase.Name);

            if (unionCase.ValueType != null)
            {
                sb.AppendLine($"            if (this is {caseClassName} {paramName}Case)");
                sb.AppendLine("            {");
                sb.AppendLine($"                {paramName}({paramName}Case.Value); ");
                sb.AppendLine("                return; ");
                sb.AppendLine("            }");
            }
            else
            {
                sb.AppendLine($"            if (this is {caseClassName})");
                sb.AppendLine("            {");
                sb.AppendLine($"                {paramName}(); ");
                sb.AppendLine("                return; ");
                sb.AppendLine("            }");
            }
        }

        sb.AppendLine("            throw new InvalidOperationException(\"Unmatched union case\");");
        sb.AppendLine("        }");

        // Generate an async Match method with a generic return type
        sb.AppendLine("        /// <summary>");

        sb.AppendLine(
            "        /// Performs asynchronous pattern matching on the union, calling the appropriate handler function based on the active case.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        /// <typeparam name=\"TResult\">The return type of all handler functions.</typeparam>");

        foreach (var unionCase in cases)
        {
            var docParam = NormalizeParamName(unionCase.Name);

            sb.AppendLine(unionCase.ValueType != null
                              ? $"        /// <param name=\"{docParam}\">Asynchronous handler function for the {unionCase.Name} case.</param>"
                              : $"        /// <param name=\"{docParam}\">Asynchronous handler function for the {unionCase.Name} case (no parameters).</param>");
        }

        sb.AppendLine(
            "        /// <returns>A task representing the asynchronous operation, with the result of calling the appropriate handler function.</returns>");

        sb.AppendLine(
            "        /// <exception cref=\"InvalidOperationException\">Thrown when no case matches (should not occur in normal usage).</exception>");
        sb.AppendLine("        /// <example>");
        sb.AppendLine("        /// <code>");
        sb.AppendLine("        /// var result = await union.MatchAsync(");

        foreach (var unionCase in cases)
        {
            sb.AppendLine(unionCase.ValueType != null
                              ? $"        ///     {unionCase.Name.ToLower()}: async value => $\"Got {{value}}\", "
                              : $"        ///     {unionCase.Name.ToLower()}: async () => \"No value\" ");
        }

        sb.AppendLine("        /// );");
        sb.AppendLine("        /// </code>");
        sb.AppendLine("        /// </example>");
        sb.Append("        public async Task<TResult> MatchAsync<TResult>(");

        // Generate parameters for each case
        parameters = [];

        foreach (var unionCase in cases)
        {
            var paramName = NormalizeParamName(unionCase.Name);

            if (unionCase.ValueType != null)
            {
                var valueTypeName = GetTypeName(unionCase.ValueType);
                parameters.Add($"Func<{valueTypeName}, Task<TResult>> {paramName}");
            }
            else
            {
                parameters.Add($"Func<Task<TResult>> {paramName}");
            }
        }

        sb.AppendLine(string.Join(", ", parameters));
        sb.AppendLine("        )");
        sb.AppendLine("        {");

        // Generate match logic for each case
        foreach (var unionCase in cases)
        {
            var caseClassName = $"{unionCase.Name}Case";
            var paramName = NormalizeParamName(unionCase.Name);

            if (unionCase.ValueType != null)
            {
                sb.AppendLine($"            if (this is {caseClassName} {paramName}Case)");
                sb.AppendLine("            {");
                sb.AppendLine($"                return await {paramName}({paramName}Case.Value);");
                sb.AppendLine("            }");
            }
            else
            {
                sb.AppendLine($"            if (this is {caseClassName})");
                sb.AppendLine("            {");
                sb.AppendLine($"                return await {paramName}();");
                sb.AppendLine("            }");
            }
        }

        sb.AppendLine("            throw new InvalidOperationException(\"Unmatched union case\");");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate a void-returning async Match overload that accepts Func<Task>/Func<T, Task>
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Performs asynchronous pattern matching on the union and executes the appropriate action for the active case.");
        sb.AppendLine("        /// </summary>");

        foreach (var unionCase in cases)
        {
            var docParam = NormalizeParamName(unionCase.Name);

            sb.AppendLine(unionCase.ValueType != null
                              ? $"        /// <param name=\"{docParam}\">Asynchronous action to invoke when this is the {unionCase.Name} case.</param>"
                              : $"        /// <param name=\"{docParam}\">Asynchronous action to invoke when this is the {unionCase.Name} case (no parameters).</param>");
        }

        sb.AppendLine(
            "        /// <exception cref=\"InvalidOperationException\">Thrown when no case matches (should not occur in normal usage).</exception>");
        sb.Append("        public async Task Match(");

        // Parameters for async action overload (Func<Task>/Func<T, Task>)
        actionParams = [];

        foreach (var unionCase in cases)
        {
            var paramName = NormalizeParamName(unionCase.Name);

            if (unionCase.ValueType != null)
            {
                var valueTypeName = GetTypeName(unionCase.ValueType);
                actionParams.Add($"Func<{valueTypeName}, Task> {paramName}");
            }
            else
            {
                actionParams.Add($"Func<Task> {paramName}");
            }
        }

        sb.AppendLine(string.Join(", ", actionParams));
        sb.AppendLine("        )");
        sb.AppendLine("        {");

        // Invoke async actions
        foreach (var unionCase in cases)
        {
            var caseClassName = $"{unionCase.Name}Case";
            var paramName = NormalizeParamName(unionCase.Name);

            if (unionCase.ValueType != null)
            {
                sb.AppendLine($"            if (this is {caseClassName} {paramName}Case)");
                sb.AppendLine("            {");
                sb.AppendLine($"                await {paramName}({paramName}Case.Value); ");
                sb.AppendLine("                return; ");
                sb.AppendLine("            }");
            }
            else
            {
                sb.AppendLine($"            if (this is {caseClassName})");
                sb.AppendLine("            {");
                sb.AppendLine($"                await {paramName}(); ");
                sb.AppendLine("                return; ");
                sb.AppendLine("            }");
            }
        }

        sb.AppendLine("            throw new InvalidOperationException(\"Unmatched union case\");");
        sb.AppendLine("        }");
    }

    /// <summary>
    /// Generates equality methods (Equals, ==, !=) for the union type.
    /// </summary>
    private void GenerateEqualityMethods(StringBuilder sb, string className, List<InternalUnionCase> cases,
                                         System.Collections.Immutable.ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        var typeName = typeParameters.Length > 0 ? $"{className}<{string.Join(", ", typeParameters.Select(tp => tp.Name))}>" : className;

        // Override Equals(object)
        sb.AppendLine("        public override bool Equals(object? obj)");
        sb.AppendLine("        {");
        sb.AppendLine($"            return obj is {typeName} other && Equals(other);");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Equals(T) method
        sb.AppendLine($"        public bool Equals({typeName}? other)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (other is null) return false;");
        sb.AppendLine("            if (ReferenceEquals(this, other)) return true;");
        sb.AppendLine();

        // Compare cases
        foreach (var unionCase in cases)
        {
            var caseClassName = $"{unionCase.Name}Case";
            var thisCaseVar = $"this{unionCase.Name}Case";
            var otherCaseVar = $"other{unionCase.Name}Case";
            sb.AppendLine($"            if (this is {caseClassName} {thisCaseVar} && other is {caseClassName} {otherCaseVar})");
            sb.AppendLine("            {");

            if (unionCase.ValueType != null)
            {
                var valueTypeName = GetTypeName(unionCase.ValueType);

                sb.AppendLine($"                return EqualityComparer<{valueTypeName}>.Default.Equals({thisCaseVar}.Value, {otherCaseVar}.Value);");
            }
            else
            {
                sb.AppendLine("                return true;");
            }

            sb.AppendLine("            }");
        }

        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine();

        // == operator
        sb.AppendLine($"        public static bool operator ==({typeName}? left, {typeName}? right)");
        sb.AppendLine("        {");
        sb.AppendLine($"            return EqualityComparer<{typeName}>.Default.Equals(left, right);");
        sb.AppendLine("        }");
        sb.AppendLine();

        // != operator
        sb.AppendLine($"        public static bool operator !=({typeName}? left, {typeName}? right)");
        sb.AppendLine("        {");
        sb.AppendLine("            return !(left == right);");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates GetHashCode method for the union type.
    /// </summary>
    private static void GenerateGetHashCode(StringBuilder sb, List<InternalUnionCase> cases)
    {
        sb.AppendLine("        public override int GetHashCode()");
        sb.AppendLine("        {");

        foreach (var unionCase in cases)
        {
            var caseClassName = $"{unionCase.Name}Case";
            var caseVarName = unionCase.Name.ToLower() + "Case";
            sb.AppendLine($"            if (this is {caseClassName} {caseVarName})");
            sb.AppendLine("            {");

            // Use literal case name to avoid referencing identifiers that may not be in scope in generated code.
            sb.AppendLine(unionCase.ValueType != null
                              ? $"                return HashCode.Combine(\"{unionCase.Name}\", {caseVarName}.Value?.GetHashCode() ?? 0);"
                              : $"                return HashCode.Combine(\"{unionCase.Name}\");");

            sb.AppendLine("            }");
        }

        sb.AppendLine("            return 0;");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates ToString method for the union type.
    /// </summary>
    private static void GenerateToString(StringBuilder sb, List<InternalUnionCase> cases)
    {
        sb.AppendLine("        public override string ToString()");
        sb.AppendLine("        {");

        foreach (var unionCase in cases)
        {
            var caseClassName = $"{unionCase.Name}Case";
            var caseVarName = unionCase.Name.ToLower() + "Case";
            sb.AppendLine($"            if (this is {caseClassName} {caseVarName})");
            sb.AppendLine("            {");

            sb.AppendLine(unionCase.ValueType != null
                              ? $"                return $\"{unionCase.Name}({{{caseVarName}.Value}})\";"
                              : $"                return $\"{unionCase.Name}\";");

            sb.AppendLine("            }");
        }

        sb.AppendLine("            return base.ToString() ?? string.Empty;");
        sb.AppendLine("        }");
    }

    /// <summary>
    /// Generates Deconstruct method for tuple deconstruction support.
    /// </summary>
    private void GenerateDeconstructMethod(StringBuilder sb, List<InternalUnionCase> cases,
                                           System.Collections.Immutable.ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        _ = typeParameters;

        if (cases.Count != 2)
        {
            return;
        }

        var firstCase = cases[0];
        var secondCase = cases[1];

        if (firstCase.ValueType == null || secondCase.ValueType == null)
        {
            return;
        }

        // For 2-case unions, generate Deconstruct method
        var firstTypeName = GetTypeName(firstCase.ValueType);
        var secondTypeName = GetTypeName(secondCase.ValueType);
        var firstCaseClassName = $"{firstCase.Name}Case";
        var secondCaseClassName = $"{secondCase.Name}Case";

        // Make nullable for out parameters
        var firstNullableType = firstCase.ValueType.IsValueType ? $"{firstTypeName}?" : firstTypeName;
        var secondNullableType = secondCase.ValueType.IsValueType ? $"{secondTypeName}?" : secondTypeName;

        sb.AppendLine(
            $"        public void Deconstruct(out {firstNullableType} {firstCase.Name.ToLower()}, out {secondNullableType} {secondCase.Name.ToLower()})");
        sb.AppendLine("        {");
        sb.AppendLine($"            if (this is {firstCaseClassName} {firstCase.Name.ToLower()}Case)");
        sb.AppendLine("            {");
        sb.AppendLine($"                {firstCase.Name.ToLower()} = {firstCase.Name.ToLower()}Case.Value;");

        sb.AppendLine(secondCase.ValueType.IsValueType
                          ? $"                {secondCase.Name.ToLower()} = null;"
                          : $"                {secondCase.Name.ToLower()} = default!;");

        sb.AppendLine("            }");
        sb.AppendLine($"            else if (this is {secondCaseClassName} {secondCase.Name.ToLower()}Case)");
        sb.AppendLine("            {");

        sb.AppendLine(firstCase.ValueType.IsValueType
                          ? $"                {firstCase.Name.ToLower()} = null;"
                          : $"                {firstCase.Name.ToLower()} = default!;");

        sb.AppendLine($"                {secondCase.Name.ToLower()} = {secondCase.Name.ToLower()}Case.Value;");
        sb.AppendLine("            }");
        sb.AppendLine("            else");
        sb.AppendLine("            {");

        sb.AppendLine(firstCase.ValueType.IsValueType
                          ? $"                {firstCase.Name.ToLower()} = null;"
                          : $"                {firstCase.Name.ToLower()} = default!;");

        sb.AppendLine(secondCase.ValueType.IsValueType
                          ? $"                {secondCase.Name.ToLower()} = null;"
                          : $"                {secondCase.Name.ToLower()} = default!;");

        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates TryGetValue methods for each case (TryGetOk, TryGetError, etc.).
    /// </summary>
    private void GenerateTryGetValueMethods(StringBuilder sb, List<InternalUnionCase> cases,
                                            System.Collections.Immutable.ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        _ = typeParameters;

        foreach (var unionCase in cases)
        {
            if (unionCase.ValueType == null)
            {
                continue;
            }

            var valueTypeName = GetTypeName(unionCase.ValueType);
            var caseClassName = $"{unionCase.Name}Case";
            var methodName = $"TryGet{unionCase.Name}";
            var paramName = unionCase.Name.ToLower();

            var caseVarName = $"{unionCase.Name.ToLower()}Case";
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Attempts to extract the value from the {unionCase.Name} case.");
            sb.AppendLine("        /// </summary>");

            sb.AppendLine(
                $"        /// <param name=\"{paramName}\">When this method returns, contains the value if this is the {unionCase.Name} case; otherwise, the default value.</param>");

            sb.AppendLine($"        /// <returns><c>true</c> if this instance is the {unionCase.Name} case; otherwise, <c>false</c>.</returns>");
            sb.AppendLine($"        public bool {methodName}(out {valueTypeName} {paramName})");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (this is {caseClassName} {caseVarName})");
            sb.AppendLine("            {");
            sb.AppendLine($"                {paramName} = {caseVarName}.Value;");
            sb.AppendLine("                return true;");
            sb.AppendLine("            }");
            sb.AppendLine($"            {paramName} = default({valueTypeName})!;");
            sb.AppendLine("            return false;");
            sb.AppendLine("        }");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates Map/Select methods for functional transformation.
    /// </summary>
    private void GenerateMapMethods(StringBuilder sb, string className, List<InternalUnionCase> cases,
                                    System.Collections.Immutable.ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        _ = typeParameters;

        if (cases.Count != 2 || typeParameters.Length < 2 || cases[0].ValueType == null || cases[1].ValueType == null)
        {
            return;
        }

        // Map method - transforms the value of the first case if it's the active case
        // Only generate for 2-case unions with generic type parameters
        var firstCase = cases[0];
        var secondCase = cases[1];
        var firstCaseClassName = $"{firstCase.Name}Case";
        var secondCaseClassName = $"{secondCase.Name}Case";
        var firstValueTypeName = GetTypeName(firstCase.ValueType!);
        _ = GetTypeName(secondCase.ValueType!);

        // MapOk method - transforms Ok value
        var firstCaseVar = $"{firstCase.Name.ToLower()}Case";
        var secondCaseVar = $"{secondCase.Name.ToLower()}Case";
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Transforms the value of the {firstCase.Name} case using the specified mapper function.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        /// <typeparam name=\"TNew\">The type of the transformed value.</typeparam>");
        sb.AppendLine($"        /// <param name=\"mapper\">The function to transform the {firstCase.Name} case value.</param>");

        sb.AppendLine(
            $"        /// <returns>A new union instance with the transformed {firstCase.Name} value, or the same {secondCase.Name} case if this is not the {firstCase.Name} case.</returns>");

        sb.AppendLine(
            $"        public {className}<TNew, {typeParameters[1].Name}> Map{firstCase.Name}<TNew>(Func<{firstValueTypeName}, TNew> mapper)");
        sb.AppendLine("        {");
        sb.AppendLine($"            if (this is {firstCaseClassName} {firstCaseVar})");
        sb.AppendLine("            {");
        sb.AppendLine($"                return {className}<TNew, {typeParameters[1].Name}>.{firstCase.Name}(mapper({firstCaseVar}.Value));");
        sb.AppendLine("            }");
        sb.AppendLine($"            if (this is {secondCaseClassName} {secondCaseVar})");
        sb.AppendLine("            {");
        sb.AppendLine($"                return {className}<TNew, {typeParameters[1].Name}>.{secondCase.Name}({secondCaseVar}.Value);");
        sb.AppendLine("            }");
        sb.AppendLine("            throw new InvalidOperationException(\"Unmatched union case\");");
        sb.AppendLine("        }");
        sb.AppendLine();

        // MapError method - transforms Error value
        {
            var secondValueTypeNameForMap = GetTypeName(secondCase.ValueType!);
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Transforms the value of the {secondCase.Name} case using the specified mapper function.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        /// <typeparam name=\"TNew\">The type of the transformed value.</typeparam>");
            sb.AppendLine($"        /// <param name=\"mapper\">The function to transform the {secondCase.Name} case value.</param>");

            sb.AppendLine(
                $"        /// <returns>A new union instance with the transformed {secondCase.Name} value, or the same {firstCase.Name} case if this is not the {secondCase.Name} case.</returns>");

            sb.AppendLine(
                $"        public {className}<{typeParameters[0].Name}, TNew> Map{secondCase.Name}<TNew>(Func<{secondValueTypeNameForMap}, TNew> mapper)");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (this is {secondCaseClassName} {secondCaseVar})");
            sb.AppendLine("            {");

            sb.AppendLine($"                return {className}<{typeParameters[0].Name}, TNew>.{secondCase.Name}(mapper({secondCaseVar}.Value));");
            sb.AppendLine("            }");
            sb.AppendLine($"            if (this is {firstCaseClassName} {firstCaseVar})");
            sb.AppendLine("            {");
            sb.AppendLine($"                return {className}<{typeParameters[0].Name}, TNew>.{firstCase.Name}({firstCaseVar}.Value);");
            sb.AppendLine("            }");
            sb.AppendLine("            throw new InvalidOperationException(\"Unmatched union case\");");
            sb.AppendLine("        }");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates OrElse/Or methods for default value handling.
    /// </summary>
    private void GenerateOrElseMethods(StringBuilder sb, List<InternalUnionCase> cases,
                                       System.Collections.Immutable.ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        _ = typeParameters;

        // For the first case, generate OrElse method
        if (cases.Count < 1 || cases[0].ValueType == null)
        {
            return;
        }

        var firstCase = cases[0];
        var firstCaseClassName = $"{firstCase.Name}Case";
        var firstValueTypeName = GetTypeName(firstCase.ValueType!);

        var firstCaseVarForOr = $"{firstCase.Name.ToLower()}Case";
        sb.AppendLine("        /// <summary>");

        sb.AppendLine(
            $"        /// Gets the value of the {firstCase.Name} case, or returns the specified default value if this is not the {firstCase.Name} case.");
        sb.AppendLine("        /// </summary>");

        sb.AppendLine($"        /// <param name=\"defaultValue\">The default value to return if this is not the {firstCase.Name} case.</param>");

        sb.AppendLine(
            $"        /// <returns>The value if this is the {firstCase.Name} case; otherwise, <paramref name=\"defaultValue\"/>.</returns>");
        sb.AppendLine($"        public {firstValueTypeName} {firstCase.Name}OrElse({firstValueTypeName} defaultValue)");
        sb.AppendLine("        {");
        sb.AppendLine($"            if (this is {firstCaseClassName} {firstCaseVarForOr})");
        sb.AppendLine("            {");
        sb.AppendLine($"                return {firstCaseVarForOr}.Value;");
        sb.AppendLine("            }");
        sb.AppendLine("            return defaultValue;");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Or method with Func
        sb.AppendLine("        /// <summary>");

        sb.AppendLine(
            $"        /// Gets the value of the {firstCase.Name} case, or invokes the specified factory function to get a default value if this is not the {firstCase.Name} case.");
        sb.AppendLine("        /// </summary>");

        sb.AppendLine(
            $"        /// <param name=\"defaultValueFactory\">The factory function to invoke if this is not the {firstCase.Name} case.</param>");

        sb.AppendLine(
            $"        /// <returns>The value if this is the {firstCase.Name} case; otherwise, the result of invoking <paramref name=\"defaultValueFactory\"/>.</returns>");
        sb.AppendLine($"        public {firstValueTypeName} {firstCase.Name}Or(Func<{firstValueTypeName}> defaultValueFactory)");
        sb.AppendLine("        {");
        sb.AppendLine($"            if (this is {firstCaseClassName} {firstCaseVarForOr})");
        sb.AppendLine("            {");
        sb.AppendLine($"                return {firstCaseVarForOr}.Value;");
        sb.AppendLine("            }");
        sb.AppendLine("            return defaultValueFactory();");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates asynchronous methods like BindAsync, MapAsync, and MatchAsync.
    /// </summary>
    private static void GenerateAsyncMethods(StringBuilder sb, string className, List<InternalUnionCase> cases,
                                             System.Collections.Immutable.ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        if (cases.Count == 0) return;

        var firstCase = cases[0];
        var firstValueType = firstCase.ValueType != null ? GetTypeName(firstCase.ValueType) : null;

        // BindAsync
        if (firstValueType != null)
        {
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Asynchronous monadic bind operation. If this is {firstCase.Name}, applies the async selector.");
            sb.AppendLine("        /// </summary>");

            sb.AppendLine(
                $"        public async global::System.Threading.Tasks.Task<TResult> BindAsync<TResult>(global::System.Func<{firstValueType}, global::System.Threading.Tasks.Task<TResult>> selector) ");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (this is {firstCase.Name}Case c) return await selector(c.Value).ConfigureAwait(false);");
            sb.AppendLine($"            throw new global::System.InvalidOperationException(\"Cannot bind from non-{firstCase.Name} case.\");");
            sb.AppendLine("        }");
            sb.AppendLine();

            // MapAsync
            if (cases.Count == 2 && typeParameters.Length >= 2)
            {
                var secondCase = cases[1];
                var restTypeArgs = typeParameters.Length > 1 ? ", " + string.Join(", ", typeParameters.Skip(1).Select(p => p.Name)) : "";

                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Asynchronously transforms the value of the {firstCase.Name} case.");
                sb.AppendLine("        /// </summary>");

                sb.AppendLine(
                    $"        public async global::System.Threading.Tasks.Task<{className}<TNew{restTypeArgs}>> Map{firstCase.Name}Async<TNew>(global::System.Func<{firstValueType}, global::System.Threading.Tasks.Task<TNew>> mapper)");
                sb.AppendLine("        {");
                sb.AppendLine($"            if (this is {firstCase.Name}Case c) ");
                sb.AppendLine("            {");
                sb.AppendLine("                var mapped = await mapper(c.Value).ConfigureAwait(false);");
                sb.AppendLine($"                return {className}<TNew{restTypeArgs}>.{firstCase.Name}(mapped);");
                sb.AppendLine("            }");

                sb.AppendLine(
                    $"            if (this is {secondCase.Name}Case c2) return {className}<TNew{restTypeArgs}>.{secondCase.Name}(c2.Value);");
                sb.AppendLine("            throw new global::System.InvalidOperationException(\"Unmatched union case\");");
                sb.AppendLine("        }");
                sb.AppendLine();
            }
        }
    }

    /// <summary>
    /// Generates asynchronous extension methods for Task of Union.
    /// </summary>
    private static void GenerateAsyncExtensions(StringBuilder sb, string className, List<InternalUnionCase> cases,
                                                System.Collections.Immutable.ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        var typeArgs = typeParameters.Length > 0 ? "<" + string.Join(", ", typeParameters.Select(p => p.Name)) + ">" : "";
        var typeName = className + typeArgs;

        switch (className)
        {
            // Wrapper types ResultAsync and OptionAsync (only if the class name matches by convention)
            case "Result" when cases.Count == 2 && typeParameters.Length == 2 && cases[0].Name == "Ok" && cases[1].Name == "Error":
                GenerateResultAsyncWrapper(sb, typeName, typeParameters);
                break;
            case "Option" when cases.Count == 2 && typeParameters.Length == 1 && cases[0].Name == "Some" && cases[1].Name == "None":
                GenerateOptionAsyncWrapper(sb, typeName, typeParameters);
                break;
        }

        sb.AppendLine($"    public static class {className}AsyncExtensions");
        sb.AppendLine("    {");

        // MatchAsync on Task
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Asynchronously performs pattern matching on a task returning {className}.");
        sb.AppendLine("        /// </summary>");

        // Note: Task extension methods need to explicitly specify type parameters of the union if it is generic.
        var extensionTypeArgs = typeParameters.Length > 0 ? "<TResult, " + string.Join(", ", typeParameters.Select(p => p.Name)) + ">" : "<TResult>";
        sb.Append($"        public static async global::System.Threading.Tasks.Task<TResult> MatchAsync{extensionTypeArgs}(");
        sb.Append($"this global::System.Threading.Tasks.Task<{typeName}> task, ");

        var matchParams = cases.Select(c => c.ValueType != null
                                                ? $"global::System.Func<{GetTypeName(c.ValueType)}, TResult> {NormalizeParamName(c.Name)}"
                                                : $"global::System.Func<TResult> {NormalizeParamName(c.Name)}");
        sb.Append(string.Join(", ", matchParams));
        sb.AppendLine(")");
        sb.AppendLine("        {");
        sb.AppendLine("            var union = await task.ConfigureAwait(false);");
        sb.AppendLine($"            return union.Match({string.Join(", ", cases.Select(c => NormalizeParamName(c.Name)))});");
        sb.AppendLine("        }");
        sb.AppendLine();

        // BindAsync on Task
        if (cases.Count > 0 && cases[0].ValueType != null)
        {
            var firstCase = cases[0];
            var firstValueType = GetTypeName(firstCase.ValueType ?? throw new InvalidOperationException());

            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Asynchronous monadic bind operation on a task returning {className}.");
            sb.AppendLine("        /// </summary>");

            var bindExtensionTypeArgs =
                typeParameters.Length > 0 ? "<TResult, " + string.Join(", ", typeParameters.Select(p => p.Name)) + ">" : "<TResult>";
            sb.Append($"        public static async Task<TResult> BindAsync{bindExtensionTypeArgs}(");
            sb.AppendLine($"this global::System.Threading.Tasks.Task<{typeName}> task, Func<{firstValueType}, Task<TResult>> selector)");
            sb.AppendLine("        {");
            sb.AppendLine("            var union = await task.ConfigureAwait(false);");
            sb.AppendLine("            return await union.BindAsync(selector).ConfigureAwait(false);");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        sb.AppendLine("    }");
    }

    private static void GenerateResultAsyncWrapper(StringBuilder sb, string resultTypeName,
                                                   System.Collections.Immutable.ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        var t = typeParameters[0].Name;
        var e = typeParameters[1].Name;

        sb.AppendLine($@"
            public readonly struct ResultAsync<{t}, {e}>
            {{
                private readonly Task<{resultTypeName}> _task;
                public ResultAsync(Task<{resultTypeName}> task) => _task = task;
                public Task<{resultTypeName}> AsTask() => _task;
                public System.Runtime.CompilerServices.TaskAwaiter<{resultTypeName}> GetAwaiter() => _task.GetAwaiter();

                public async Task<TResult> MatchAsync<TResult>(Func<{t}, TResult> ok, Func<{e}, TResult> error)
                {{
                    var res = await _task.ConfigureAwait(false);
                    return res.Match(ok, error);
                }}

                public async Task<ResultAsync<TNew, {e}>> MapAsync<TNew>(Func<{t}, Task<TNew>> mapper)
                {{
                    var res = await _task.ConfigureAwait(false);
                    var mapped = await res.MapOkAsync(mapper).ConfigureAwait(false);
                    return new ResultAsync<TNew, {e}>(Task.FromResult(mapped));
                }}

                public async Task<ResultAsync<TNew, {e}>> BindAsync<TNew>(Func<{t}, Task<Result<TNew, {e}>>> selector)
                {{
                     var res = await _task.ConfigureAwait(false);
                     if (res is {resultTypeName}.OkCase c) 
                     {{
                         return new ResultAsync<TNew, {e}>(selector(c.Value));
                     }}
                     return new ResultAsync<TNew, {e}>(Task.FromResult(Result<TNew, {e} >.Error(res.ErrorValue)));
                }}
            }}
        ");
    }

    private static void GenerateOptionAsyncWrapper(StringBuilder sb, string optionTypeName,
                                                   System.Collections.Immutable.ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        var t = typeParameters[0].Name;

        sb.AppendLine($@"
            public readonly struct OptionAsync<{t}>
            {{
                private readonly Task<{optionTypeName}> _task;
                public OptionAsync(Task<{optionTypeName}> task) => _task = task;
                public Task<{optionTypeName}> AsTask() => _task;
                public System.Runtime.CompilerServices.TaskAwaiter<{optionTypeName}> GetAwaiter() => _task.GetAwaiter();

                public async Task<TResult> MatchAsync<TResult>(Func<{t}, TResult> some, Func<TResult> none)
                {{
                    var res = await _task.ConfigureAwait(false);
                    return res.Match(some, none);
                }}
            }}
        ");
    }

    /// <summary>
    /// Gets the display name of a type, handling generics and nullable types.
    /// </summary>
    private static string GetTypeName(ITypeSymbol typeSymbol)
    {
        return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    /// <summary>
    /// Generates functional operators like Bind, Tap, Fold, etc.
    /// </summary>
    private static void GenerateFunctionalOperators(StringBuilder sb, string className, List<InternalUnionCase> cases,
                                                    System.Collections.Immutable.ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        if (cases.Count == 0) return;

        // Bind (FlatMap) - typically for the first case
        if (cases[0].ValueType != null)
        {
            var firstCase = cases[0];
            var firstValueType = GetTypeName(firstCase.ValueType ?? throw new InvalidOperationException());
            NormalizeParamName(firstCase.Name);

            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Monadic bind operation. If this is {firstCase.Name}, applies the selector and returns its result.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public TResult Bind<TResult>(Func<{firstValueType}, TResult> selector) ");
            sb.AppendLine("        {");

            sb.AppendLine(
                $"            return this is {firstCase.Name}Case c ? selector(c.Value) : throw new InvalidOperationException(\"Cannot bind from non-{firstCase.Name} case.\");");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Tap - side effect without changing the value
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Executes the action if this is {firstCase.Name} and returns the same instance.");
            sb.AppendLine("        /// </summary>");

            sb.AppendLine(
                $"        public {className}{(typeParameters.Length > 0 ? "<" + string.Join(", ", typeParameters.Select(p => p.Name)) + ">" : "")} Tap(Action<{firstValueType}> action)");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (this is {firstCase.Name}Case c) action(c.Value);");
            sb.AppendLine("            return this;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Iter - Action for each case but focuses on the first
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Executes the action if this is {firstCase.Name}.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public void Iter(Action<{firstValueType}> action)");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (this is {firstCase.Name}Case c) action(c.Value);");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        // BiMap - for 2-case unions
        if (cases.Count == 2 && cases[0].ValueType != null && cases[1].ValueType != null)
        {
            var c1 = cases[0];
            var c2 = cases[1];
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Maps both cases of the union.");
            sb.AppendLine("        /// </summary>");

            sb.AppendLine(
                $"        public {className}<T1, T2> BiMap<T1, T2>(Func<{GetTypeName(c1.ValueType!)}, T1> map1, Func<{GetTypeName(c2.ValueType!)}, T2> map2)");
            sb.AppendLine("        {");
            sb.AppendLine("            return Match(");
            sb.AppendLine($"                {NormalizeParamName(c1.Name)}: v => {className}<T1, T2>.{c1.Name}(map1(v)),");
            sb.AppendLine($"                {NormalizeParamName(c2.Name)}: v => {className}<T1, T2>.{c2.Name}(map2(v))");
            sb.AppendLine("            );");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        // Fold - similar to Match but often used in functional contexts
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Folds the union into a single value using the provided functions for each case.");
        sb.AppendLine("        /// </summary>");
        sb.Append("        public TResult Fold<TResult>(");

        var foldParams = cases.Select(c => c.ValueType != null
                                               ? $"Func<{GetTypeName(c.ValueType)}, TResult> {NormalizeParamName(c.Name)}"
                                               : $"Func<TResult> {NormalizeParamName(c.Name)}");
        sb.Append(string.Join(", ", foldParams));
        sb.AppendLine(")");
        sb.AppendLine("        {");
        sb.AppendLine("            return Match( " + string.Join(", ", cases.Select(c => NormalizeParamName(c.Name))) + " );");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates LINQ-like operators Select, SelectMany, and Where.
    /// </summary>
    private static void GenerateLinqOperators(StringBuilder sb, string className, List<InternalUnionCase> cases,
                                              System.Collections.Immutable.ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        // For simplicity and alignment with Result-like patterns, we focus on the first case for LINQ
        if (cases.Count == 0 || cases[0].ValueType == null)
        {
            return;
        }

        var firstCase = cases[0];
        var firstValueType = GetTypeName(firstCase.ValueType ?? throw new InvalidOperationException());
        var typeArgs = typeParameters.Length > 0 ? "<" + string.Join(", ", typeParameters.Select(p => p.Name)) + ">" : "";
        var restTypeArgs = typeParameters.Length > 1 ? ", " + string.Join(", ", typeParameters.Skip(1).Select(p => p.Name)) : "";

        // Select (Map)
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// LINQ Select operator. Projects the value of the first case.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public {className}<TResult{restTypeArgs}> Select<TResult>(Func<{firstValueType}, TResult> selector)");
        sb.AppendLine("        {");

        if (cases.Count == 2 && typeParameters.Length >= 2)
        {
            sb.AppendLine($"            return Map{firstCase.Name}(selector);");
        }
        else
        {
            sb.AppendLine(
                $"            if (this is {firstCase.Name}Case c) return {className}<TResult{restTypeArgs}>.{firstCase.Name}(selector(c.Value));");
            sb.AppendLine($"            throw new InvalidOperationException(\"Select can only be called on {firstCase.Name} case.\");");
        }

        sb.AppendLine("        }");
        sb.AppendLine();

        // SelectMany (Bind)
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// LINQ SelectMany operator. Flattens the nested union result.");
        sb.AppendLine("        /// </summary>");

        sb.AppendLine(
            $"        public {className}<TResult{restTypeArgs}> SelectMany<TIntermediate, TResult>(Func<{firstValueType}, {className}<TIntermediate{restTypeArgs}>> selector, Func<{firstValueType}, TIntermediate, TResult> resultSelector)");
        sb.AppendLine("        {");
        sb.AppendLine($"            if (this is {firstCase.Name}Case c)");
        sb.AppendLine("            {");
        sb.AppendLine("                var intermediate = selector(c.Value);");

        sb.AppendLine(
            $"                if (intermediate is {className}<TIntermediate{restTypeArgs}>.{firstCase.Name}Case i) return {className}<TResult{restTypeArgs}>.{firstCase.Name}(resultSelector(c.Value, i.Value));");
        sb.AppendLine("                return intermediate.Match(");

        foreach (var c in cases)
        {
            var p = NormalizeParamName(c.Name);

            if (c.Name == firstCase.Name)
            {
                continue; // Handled above for intermediate success
            }

            sb.AppendLine(c.ValueType != null
                              ? $"                    {p}: v => {className}<TResult{restTypeArgs}>.{c.Name}(v),"
                              : $"                    {p}: () => {className}<TResult{restTypeArgs}>.{c.Name}(),");
        }

        // This is a bit complex for a generic generator, simplified version:
        sb.AppendLine(
            $"                    {NormalizeParamName(firstCase.Name)}: v => {className}<TResult{restTypeArgs}>.{firstCase.Name}(resultSelector(c.Value, v))");
        sb.AppendLine("                );");
        sb.AppendLine("            }");

        // If not the first case, propagate the current case
        foreach (var c in cases.Skip(1))
        {
            sb.AppendLine(c.ValueType != null
                              ? $"            if (this is {c.Name}Case c{c.Name}) return {className}<TResult{restTypeArgs}>.{c.Name}(c{c.Name}.Value);"
                              : $"            if (this is {c.Name}Case) return {className}<TResult{restTypeArgs}>.{c.Name}();");
        }

        sb.AppendLine("            throw new InvalidOperationException(\"Invalid state\");");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Where (Filter)
        if (cases.Count != 2 || cases[1].ValueType != null)
        {
            return;
        }

        // Case like Option<T> where the second case is None
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// LINQ Where operator. Filters the value of the first case.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public {className}{typeArgs} Where(Func<{firstValueType}, bool> predicate)");
        sb.AppendLine("        {");
        sb.AppendLine($"            if (this is {firstCase.Name}Case c && predicate(c.Value)) return this;");
        sb.AppendLine($"            return {className}{typeArgs}.{cases[1].Name}();");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates extra utility methods like ToOption, Ensure, OrElseThrow.
    /// </summary>
    private static void GenerateUtilityMethods(StringBuilder sb, string className, List<InternalUnionCase> cases,
                                               System.Collections.Immutable.ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        if (cases.Count == 0 || cases[0].ValueType == null)
        {
            return;
        }

        var firstCase = cases[0];
        var firstValueType = GetTypeName(firstCase.ValueType ?? throw new InvalidOperationException());
        var typeArgs = typeParameters.Length > 0 ? "<" + string.Join(", ", typeParameters.Select(p => p.Name)) + ">" : "";

        // OrElseThrow
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Returns the value if this is {firstCase.Name}, otherwise throws an exception.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public {firstValueType} OrElseThrow(Func<Exception> exceptionFactory)");
        sb.AppendLine("        {");
        sb.AppendLine($"            if (this is {firstCase.Name}Case c) return c.Value;");
        sb.AppendLine("            throw exceptionFactory();");
        sb.AppendLine("        }");
        sb.AppendLine();

        if (cases.Count != 2 || cases[1].ValueType == null)
        {
            return;
        }

        // Ensure
        var secondCase = cases[1];
        var secondValueType = GetTypeName(secondCase.ValueType ?? throw new InvalidOperationException());
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Ensures a condition is met, otherwise returns the {secondCase.Name} case.");
        sb.AppendLine("        /// </summary>");

        sb.AppendLine(
            $"        public {className}{typeArgs} Ensure(Func<{firstValueType}, bool> predicate, Func<{firstValueType}, {secondValueType}> errorFactory)");
        sb.AppendLine("        {");
        sb.AppendLine($"            if (this is {firstCase.Name}Case c)");
        sb.AppendLine("            {");
        sb.AppendLine($"                return predicate(c.Value) ? this : {className}{typeArgs}.{secondCase.Name}(errorFactory(c.Value));");
        sb.AppendLine("            }");
        sb.AppendLine("            return this;");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates XML documentation for the union class.
    /// </summary>
    private static void GenerateUnionClassXmlDoc(StringBuilder sb, string className, List<InternalUnionCase> cases)
    {
        sb.AppendLine("    /// <summary>");

        sb.AppendLine($"    /// Represents a discriminated union type with {cases.Count} case(s): {string.Join(", ", cases.Select(c => c.Name))}.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <remarks>");

        sb.AppendLine(
            "    /// This class is generated by UnionGenerator. Use pattern matching, Match method, or property checks to handle different cases.");
        sb.AppendLine("    /// </remarks>");

        if (cases.Count <= 0)
        {
            return;
        }

        sb.AppendLine("    /// <example>");
        var exampleCase = cases[0];

        if (exampleCase.ValueType != null)
        {
            sb.AppendLine("    /// <code>");
            sb.AppendLine($"    /// var result = {className}.{exampleCase.Name}(value);");
            sb.AppendLine($"    /// if (result.Is{exampleCase.Name})");
            sb.AppendLine("    /// {");
            sb.AppendLine("    ///     var value = result.Value;");
            sb.AppendLine("    /// }");
            sb.AppendLine("    /// </code>");
        }
        else
        {
            sb.AppendLine("    /// <code>");
            sb.AppendLine($"    /// var option = {className}.{exampleCase.Name}();");
            sb.AppendLine($"    /// if (option.Is{exampleCase.Name})");
            sb.AppendLine("    /// {");
            sb.AppendLine($"    ///     // Handle {exampleCase.Name} case");
            sb.AppendLine("    /// }");
            sb.AppendLine("    /// </code>");
        }

        sb.AppendLine("    /// </example>");
    }

    /// <summary>
    /// Generates DebuggerDisplay and DebuggerTypeProxy attributes for the union class.
    /// </summary>
    private void GenerateDebugAttributes(StringBuilder sb, string className, List<InternalUnionCase> cases,
                                         System.Collections.Immutable.ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        // Generate DebuggerDisplay attribute
        // Format: "{CaseName}({Value})" or just "{CaseName}" for unit cases
        var displayFormat = BuildDebuggerDisplayFormat(cases);
        // Escape quotes in the format string
        var escapedFormat = displayFormat.Replace("\"", "\\\"");
        sb.AppendLine($"    [DebuggerDisplay(\"{escapedFormat}\")] ");

        // Generate DebuggerTypeProxy attribute
        var proxyClassName = $"{className}DebuggerProxy";

        if (typeParameters.Length == 0)
        {
            sb.AppendLine($"    [DebuggerTypeProxy(typeof({proxyClassName}))]");
        }
        else
        {
            // For generic types, use open generic type syntax: <>, <,>, <,,>, etc.
            var commas = typeParameters.Length > 1 ? new string(',', typeParameters.Length - 1) : "";
            sb.AppendLine($"    [DebuggerTypeProxy(typeof({proxyClassName}<{commas}>))]");
        }
    }

    /// <summary>
    /// Builds the DebuggerDisplay format string for the union type.
    /// </summary>
    private static string BuildDebuggerDisplayFormat(List<InternalUnionCase> cases)
    {
        // Use a simple format that works with DebuggerDisplay
        //  uses {expression} syntax where expression is C# code
        var parts = new List<string>();

        foreach (var unionCase in cases)
        {
            var caseClassName = $"{unionCase.Name}Case";
            var caseVarName = $"{unionCase.Name.ToLower()}Case";

            // Simple format: use ToString() which will call the generated ToString method
            parts.Add(unionCase.ValueType != null
                          ? $"this is {caseClassName} {caseVarName} ? {caseVarName}.ToString()"
                          // Use literal case name instead of nameof(...) to avoid referencing out-of-scope symbols in the debugger display.
                          : $"this is {caseClassName} ? \"{unionCase.Name}\"");
        }

        parts.Add("\"Unknown\"");
        return string.Join(" : ", parts);
    }

    /// <summary>
    /// Generates the debugger proxy class for better visualization in the debugger.
    /// </summary>
    private static void GenerateDebuggerProxyClass(StringBuilder sb, string className, List<InternalUnionCase> cases,
                                                   System.Collections.Immutable.ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        var proxyClassName = $"{className}DebuggerProxy";
        var typeName = typeParameters.Length > 0 ? $"{className}<{string.Join(", ", typeParameters.Select(tp => tp.Name))}>" : className;

        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Debugger proxy class for better visualization of union types in debugger.");
        sb.AppendLine("    /// </summary>");

        if (typeParameters.Length > 0)
        {
            // Use original type parameter names to match case class references
            var genParams = string.Join(", ", typeParameters.Select(tp => tp.Name));
            sb.AppendLine($"    internal sealed class {proxyClassName}<{genParams}>");
            sb.AppendLine("    {");
            sb.AppendLine($"        private readonly {className}<{genParams}> _union;");
            sb.AppendLine();
            sb.AppendLine($"        public {proxyClassName}({className}<{genParams}> union)");
            sb.AppendLine("        {");
            sb.AppendLine("            _union = union;");
            sb.AppendLine("        }");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine($"    internal sealed class {proxyClassName}");
            sb.AppendLine("    {");
            sb.AppendLine($"        private readonly {typeName} _union;");
            sb.AppendLine();
            sb.AppendLine($"        public {proxyClassName}({typeName} union)");
            sb.AppendLine("        {");
            sb.AppendLine("            _union = union;");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        // Generate properties for each case
        foreach (var unionCase in cases)
        {
            var caseClassName = $"{unionCase.Name}Case";
            var propName = $"Is{unionCase.Name}";

            // For namespace-level proxy, need to use a fully qualified case class name
            var fullCaseClassName = typeParameters.Length > 0
                                        ? $"{className}<{string.Join(", ", typeParameters.Select(tp => tp.Name))}>.{caseClassName}"
                                        : $"{className}.{caseClassName}";

            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets a value indicating whether the union is the {unionCase.Name} case.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public bool {propName} => _union is {fullCaseClassName};");

            if (unionCase.ValueType != null)
            {
                var valueTypeName = GetTypeName(unionCase.ValueType!);
                var nullableTypeName = unionCase.ValueType!.IsValueType ? $"{valueTypeName}?" : valueTypeName;
                var valuePropName = $"{unionCase.Name}Value";
                sb.AppendLine();
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Gets the value of the {unionCase.Name} case.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        public {nullableTypeName} {valuePropName}");
                sb.AppendLine("        {");
                sb.AppendLine("            get");
                sb.AppendLine("            {");
                sb.AppendLine($"                if (_union is {fullCaseClassName} c)");
                sb.AppendLine("                {");
                sb.AppendLine("                    return c.Value;");
                sb.AppendLine("                }");

                sb.AppendLine(unionCase.ValueType!.IsValueType ? "                return null;" : "                return default!;");

                sb.AppendLine("            }");
                sb.AppendLine("        }");
            }

            sb.AppendLine();
        }

        // Generate CaseName property
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Gets the name of the active case.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        public string CaseName");
        sb.AppendLine("        {");
        sb.AppendLine("            get");
        sb.AppendLine("            {");

        foreach (var unionCase in cases)
        {
            var caseClassName = $"{unionCase.Name}Case";

            var fullCaseClassName = typeParameters.Length > 0
                                        ? $"{className}<{string.Join(", ", typeParameters.Select(tp => tp.Name))}>.{caseClassName}"
                                        : $"{className}.{caseClassName}";
            // Return a string literal with the case name rather than using the nameof on a symbol that may not be in scope.
            sb.AppendLine($"                if (_union is {fullCaseClassName}) return \"{unionCase.Name}\";");
        }

        sb.AppendLine("                return \"Unknown\";");
        sb.AppendLine("            }");
        sb.AppendLine("        }");

        // Close the proxy class
        sb.AppendLine("    }");
    }

    /// <summary>
    /// Normalizes a case name to a stable camelCase parameter name.
    /// </summary>
    private static string NormalizeParamName(string caseName)
    {
        if (string.IsNullOrEmpty(caseName))
        {
            return "case";
        }

        // Handle common two-case union conventions
        if (caseName.Equals("Ok", StringComparison.OrdinalIgnoreCase)) return "ok";
        if (caseName.Equals("Error", StringComparison.OrdinalIgnoreCase)) return "error";
        if (caseName.Equals("Some", StringComparison.OrdinalIgnoreCase)) return "some";
        if (caseName.Equals("None", StringComparison.OrdinalIgnoreCase)) return "none";

        // Default: camelCase the name
        return char.ToLowerInvariant(caseName[0]) + caseName.Substring(1);
    }
}

/// <summary>
/// Represents a union case with its name and optional value type.
/// </summary>
internal sealed class InternalUnionCase
{
    /// <summary>
    /// Gets or sets the name of the case (e.g., "Ok", "Error").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the single value type (convenience property for single-parameter cases).
    /// </summary>
    public ITypeSymbol? ValueType { get; set; }

    /// <summary>
    /// Gets or sets the value parameter name for single-parameter cases.
    /// </summary>
    public string? ValueName { get; set; }

    /// <summary>
    /// Gets the parameter types for this case. Empty for unit-like cases.
    /// </summary>
    public List<ITypeSymbol> ParameterTypes { get; } = [];

    /// <summary>
    /// Gets the parameter names for this case (same order as ParameterTypes).
    /// </summary>
    public List<string> ParameterNames { get; } = [];

    /// <summary>
    /// Convenience: whether this case carries any values.
    /// </summary>
    public bool HasValue => ParameterTypes.Count > 0 || ValueType != null;
}