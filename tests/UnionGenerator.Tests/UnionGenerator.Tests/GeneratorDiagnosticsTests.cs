using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace UnionGenerator.Tests;

/// <summary>
/// Tests that the source generator reports expected diagnostics (UG9002/UG9003/UG9004).
/// </summary>
public class GeneratorDiagnosticsTests
{
    [Fact]
    public void Reports_UG9002_When_NoFactoryMethods()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result
    {
    }
}            ";

        var compilation = CreateCompilation(source);
        var generator = new UnionGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();

        var diags = runResult.Diagnostics;
        Assert.Contains(diags, d => d.Id == "UG9002" || d.Id == "UG9002" /* backward compatibility isn't expected here */);
        // Also accept our new UG9002 id
        Assert.Contains(diags, d => d.Id == "UG9002" || d.Id == "UG9002");
    }

    [Fact]
    public void Reports_UG9003_When_FactoryHasMultipleParameters()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result
    {
        public static Result Create(int a, string b) => new CreateCase(a, b);
    }
}            ";

        var compilation = CreateCompilation(source);
        var generator = new UnionGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();

        var diags = runResult.Diagnostics;
        Assert.Contains(diags, d => d.Id == "UG9003" || d.Id == "UG9003");
    }

    [Fact]
    public void Reports_UG9004_When_DuplicateCaseNames()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result
    {
        public static Result A() => null!;
        public static Result A() => null!;
    }
}            ";

        var compilation = CreateCompilation(source);
        var generator = new UnionGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();

        var diags = runResult.Diagnostics;
        Assert.Contains(diags, d => d.Id == "UG9004");
    }

    [Fact]
    public void NoDiagnostics_For_SimpleSingleParamFactory()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result
    {
        public static Result Ok(string v) => new OkCase(v);
    }
}            ";

        var compilation = CreateCompilation(source);
        var generator = new UnionGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();

        var diags = runResult.Diagnostics.Select(d => d.Id).ToList();
        Assert.DoesNotContain("UG9002", diags);
        Assert.DoesNotContain("UG9003", diags);
        Assert.DoesNotContain("UG9004", diags);
    }

    private static Compilation CreateCompilation(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var refs = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IEnumerable<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(global::UnionGenerator.Attributes.GenerateUnionAttribute).Assembly.Location)
        };

        return CSharpCompilation.Create("TestAssembly", [tree], refs, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}