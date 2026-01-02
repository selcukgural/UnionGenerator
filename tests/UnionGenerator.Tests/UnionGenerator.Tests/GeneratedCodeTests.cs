using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace UnionGenerator.Tests;

/// <summary>
/// Tests for generated code containing XML docs and debugger attributes.
/// </summary>
public class GeneratedCodeTests
{
    [Fact]
    public void GeneratedCode_Contains_DebuggerAttributes_And_XmlDocs()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Option<T>
    {
        public static Option<T> Some(T value) => new SomeCase(value);
        public static Option<T> None() => new NoneCase();
    }
}
";

        var compilation = CreateCompilation(source);
        var generator = new UnionGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();
        var generatedTrees = runResult.GeneratedTrees.ToList();

        Assert.NotEmpty(generatedTrees);

        var combined = string.Concat(generatedTrees.Select(t => t.GetText().ToString()));

        // Check for DebuggerDisplay and DebuggerTypeProxy presence anywhere in generated output
        Assert.Contains("Debugger", combined);

        // Check for XML summary on the generated class
        Assert.Contains("<summary>", combined);
        Assert.Contains("Represents a discriminated union type", combined);
    }

    private static Compilation CreateCompilation(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var refs = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IEnumerable<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Diagnostics.DebuggerDisplayAttribute).Assembly.Location)
        };

        return CSharpCompilation.Create("TestAssembly", [tree], refs, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}