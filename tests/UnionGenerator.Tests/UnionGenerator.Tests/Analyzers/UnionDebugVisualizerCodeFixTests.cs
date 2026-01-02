using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UnionGenerator.Tests.Analyzers;

public class UnionDebugVisualizerCodeFixTests
{
    [Fact]
    public async Task AnalyzerReportsMissingDebuggerAttributes()
    {
        var source = @"
using UnionGenerator.Attributes;

[GenerateUnion]
public partial class Result<T0,T1>
{
}
";

        var tree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                                  .Select(a => MetadataReference.CreateFromFile(a.Location))
                                  .ToList();

        var compilation = CSharpCompilation.Create("TestAssembly", [tree],
                                                   references,
                                                   new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new global::UnionGenerator.Analyzers.UnionDebugVisualizerAnalyzer();
        var diags = await compilation.WithAnalyzers([analyzer]).GetAllDiagnosticsAsync();

        Assert.Contains(diags, d => d.Id == "UG3002");
    }
}