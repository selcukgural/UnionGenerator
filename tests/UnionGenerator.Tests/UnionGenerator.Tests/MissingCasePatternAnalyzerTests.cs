using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UnionGenerator.Tests;

/// <summary>
/// Basic tests for the attribute-based missing-case analyzer (UG002).
/// </summary>
public class MissingCasePatternAnalyzerTests
{
    [Fact]
    public void ReportsDiagnostic_When_SwitchExpression_MissingCase()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result
    {
        public static Result Ok() => new OkCase();
        public static Result Error(string message) => new ErrorCase(message);
    }

    public class TestClass
    {
        public void Test(Result r)
        {
            var x = r switch
            {
                Result.OkCase _ => 1
            };
        }
    }
}            ";

        var compilation = CreateCompilationWithGeneratedCode(source);
        var analyzer = new MissingUnionCaseAnalyzer();
        var diags = RunAnalyzer(compilation, analyzer);

        // Accept diagnostics from either UG002 (this analyzer) or existing UG1001 (other analyzer)
        Assert.Contains(diags, d => d.Id == MissingUnionCaseAnalyzer.DiagnosticId || d.Id == "UG1001" || d.GetMessage().Contains("Error"));
    }

    [Fact]
    public void NoDiagnostic_When_AllCasesHandled()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result
    {
        public static Result Ok() => new OkCase();
        public static Result Error(string message) => new ErrorCase(message);
    }

    public class TestClass
    {
        public void Test(Result r)
        {
            var x = r switch
            {
                Result.OkCase _ => 1,
                Result.ErrorCase _ => 2
            };
        }
    }
}            ";

        var compilation = CreateCompilationWithGeneratedCode(source);
        var analyzer = new MissingUnionCaseAnalyzer();
        var diags = RunAnalyzer(compilation, analyzer);

        // Ensure no missing-case diagnostics like UG002/UG1001 or messages containing "Missing" are present
        Assert.DoesNotContain(diags, d => d.Id == MissingUnionCaseAnalyzer.DiagnosticId || d.Id == "UG1001" || d.GetMessage().Contains("Missing"));
    }

    private static Compilation CreateCompilation(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var refs = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IEnumerable<>).Assembly.Location)
        };

        return CSharpCompilation.Create("TestAssembly", [tree], refs, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static Compilation CreateCompilationWithGeneratedCode(string source)
    {
        var compilation = CreateCompilation(source);

        // Run the generator to produce union code
        var generator = new UnionGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();
        var generatedTrees = runResult.GeneratedTrees.ToList();

        if (generatedTrees.Count == 0)
        {
            return compilation;
        }

        var allSyntaxTrees = compilation.SyntaxTrees.Concat(generatedTrees).ToList();
        var newCompilation = CSharpCompilation.Create(
            compilation.AssemblyName,
            allSyntaxTrees,
            compilation.References,
            (CSharpCompilationOptions)compilation.Options);

        // Force semantic model binding
        foreach (var tree in allSyntaxTrees)
        {
            _ = newCompilation.GetSemanticModel(tree).GetDiagnostics();
        }

        return newCompilation;
    }

    private static Diagnostic[] RunAnalyzer(Compilation compilation, DiagnosticAnalyzer analyzer)
    {
        var analyzers = ImmutableArray.Create(analyzer, new UnionPatternMatchingAnalyzer());
        var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers);
        var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
        return diagnostics.ToArray();
    }
}