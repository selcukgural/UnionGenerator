using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UnionGenerator.Tests;

/// <summary>
/// Tests for the UnionPatternMatchingAnalyzer.
/// </summary>
public class UnionPatternMatchingAnalyzerTests
{
    /// <summary>
    /// Tests that the analyzer detects incomplete pattern matching in switch expressions.
    /// </summary>
    [Fact]
    public void AnalyzerDetectsIncompleteSwitchExpression()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }

    public class TestClass
    {
        public void Test(Result<int, string> result)
        {
            var x = result switch
            {
                Result<int, string>.OkCase ok => ok.Value,
                _ => 0
            };
        }
    }
}";

        var diagnostics = GetDiagnostics(source);
        // Should have warning about missing Error case
        Assert.Contains(diagnostics, d => d.Id == "UG1001" && d.GetMessage().Contains("Error"));
    }

    /// <summary>
    /// Tests that the analyzer does not report warnings when all cases are handled.
    /// </summary>
    [Fact]
    public void AnalyzerDoesNotReportWhenAllCasesHandled()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }

    public class TestClass
    {
        public void Test(Result<int, string> result)
        {
            var x = result switch
            {
                Result<int, string>.OkCase ok => ok.Value,
                Result<int, string>.ErrorCase err => err.Value,
                _ => 0
            };
        }
    }
}";

        var diagnostics = GetDiagnostics(source);
        // Should not have UG1001 warnings
        Assert.DoesNotContain(diagnostics, d => d.Id == "UG1001");
    }

    /// <summary>
    /// Tests that the analyzer detects incomplete pattern matching in switch statements.
    /// </summary>
    [Fact]
    public void AnalyzerDetectsIncompleteSwitchStatement()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }

    public class TestClass
    {
        public void Test(Result<int, string> result)
        {
            switch (result)
            {
                case Result<int, string>.OkCase ok:
                    var x = ok.Value;
                    break;
            }
        }
    }
}";

        var diagnostics = GetDiagnostics(source);
        // Should have warning about missing Error case
        Assert.Contains(diagnostics, d => d.Id == "UG1001" && d.GetMessage().Contains("Error"));
    }

    /// <summary>
    /// Tests that the analyzer detects incomplete pattern matching in if statements.
    /// </summary>
    [Fact]
    public void AnalyzerDetectsIncompleteIfStatement()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }

    public class TestClass
    {
        public void Test(Result<int, string> result)
        {
            if (result is Result<int, string>.OkCase ok)
            {
                var x = ok.Value;
            }
        }
    }
}";

        var diagnostics = GetDiagnostics(source);
        // Should have warning about missing Error case
        Assert.Contains(diagnostics, d => d.Id == "UG1001" && d.GetMessage().Contains("Error"));
    }

    /// <summary>
    /// Tests that the analyzer detects incomplete Match method calls.
    /// </summary>
    [Fact]
    public void AnalyzerDetectsIncompleteMatchMethod()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }

    public class TestClass
    {
        public void Test(Result<int, string> result)
        {
            var x = result.Match(
                ok: value => value
            );
        }
    }
}";

        var diagnostics = GetDiagnostics(source);
        // Should have warning about missing Error case
        Assert.Contains(diagnostics, d => d.Id == "UG1001" && d.GetMessage().Contains("Error"));
    }

    /// <summary>
    /// Tests that the analyzer does not report warnings for non-union types.
    /// </summary>
    [Fact]
    public void AnalyzerIgnoresNonUnionTypes()
    {
        var source = @"
namespace TestNamespace
{
    public class RegularClass
    {
        public int Value { get; set; }
    }

    public class TestClass
    {
        public void Test(RegularClass obj)
        {
            var x = obj switch
            {
                RegularClass r => r.Value,
                _ => 0
            };
        }
    }
}";

        var diagnostics = GetDiagnostics(source);
        // Should not have UG1001 warnings for non-union types
        Assert.DoesNotContain(diagnostics, d => d.Id == "UG1001");
    }

    /// <summary>
    /// Gets diagnostics from source code using the analyzer.
    /// </summary>
    private static Diagnostic[] GetDiagnostics(string source)
    {
        var compilation = GetCompilationWithGeneratedCode(source);
            
        // Run the analyzer on the complete compilation
        var analyzer = new UnionPatternMatchingAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);
        var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;

        return diagnostics.ToArray();
    }

    /// <summary>
    /// Gets a compilation with generated code included.
    /// </summary>
    private static Compilation GetCompilationWithGeneratedCode(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IEnumerable<>).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly", [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Run the generator first to generate union code
        var generator = new UnionGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedTrees = runResult.GeneratedTrees.ToList();

        if (generatedTrees.Count == 0)
        {
            return compilation;
        }

        // Create a new compilation with all syntax trees (original + generated)
        var allSyntaxTrees = compilation.SyntaxTrees.Concat(generatedTrees).ToList();
        var newCompilation = CSharpCompilation.Create(
            compilation.AssemblyName,
            allSyntaxTrees,
            compilation.References,
            compilation.Options);

        // Force semantic model creation to ensure types are resolved
        // This is important for the analyzer to see the union types
        foreach (var tree in allSyntaxTrees)
        {
            var semanticModel = newCompilation.GetSemanticModel(tree);
            // Force full binding by getting diagnostics
            var _ = semanticModel.GetDiagnostics();
        }

        return newCompilation;
    }
}