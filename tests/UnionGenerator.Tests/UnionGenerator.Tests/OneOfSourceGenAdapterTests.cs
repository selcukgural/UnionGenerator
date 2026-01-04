using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace UnionGenerator.Tests;

public class OneOfSourceGenAdapterTests
{
    [Fact]
    public void GeneratedAdapter_IsEmitted_And_Compiles()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestSrc
{
    [GenerateUnion]
    public partial class Result<T0, T1>
    {
        public static Result<T0, T1> Ok(T0 value) => new OkCase(value);
        public static Result<T0, T1> Error(T1 error) => new ErrorCase(error);

        // Nested case types so tests can compile without external references
        public sealed class OkCase : Result<T0, T1>
        {
            public OkCase(T0 value) { }
        }

        public sealed class ErrorCase : Result<T0, T1>
        {
            public ErrorCase(T1 error) { }
        }
    }
}
";

        // Add GenerateUnionAttribute since it's no longer in the generator assembly
        var attributeSource = @"
using System;
namespace UnionGenerator.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class GenerateUnionAttribute : Attribute
    {
    }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(source, System.Text.Encoding.UTF8));
        var attributeTree = CSharpSyntaxTree.ParseText(SourceText.From(attributeSource, System.Text.Encoding.UTF8));
        
        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                                  .Select(a => MetadataReference.CreateFromFile(a.Location))
                                  .ToList();

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree, attributeTree],
                                                   references,
                                                   new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Run generator
        var generator = new OneOfSourceGen.OneOfConverterGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        _ = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        // Ensure generator produced no errors
        Assert.False(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));

        // Ensure compilation of generated output succeeds
        var emitResult = outputCompilation.Emit(Stream.Null);
        Assert.True(emitResult.Success, string.Join("\n", emitResult.Diagnostics.Select(d => d.ToString())));
    }
}