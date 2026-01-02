using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UnionGenerator.Tests.Analyzers;

public class CasePatternCodeFixTests
{
    [Fact]
    public async Task AnalyzerFlagsIfElseChainOnOneOf()
    {
        var source = @"
using OneOf;
using System;

class Result
{
    public static T Ok<T>(T v) => v;
    public static U Error<U>(U e) => e;
}

class C
{
    void M(OneOf.OneOf<int,string> o)
    {
        if (o.IsT0)
        {
            return;
        }
        else if (o.IsT1)
        {
            return;
        }
    }
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

        var analyzer = new global::UnionGenerator.Analyzers.CasePatternAnalyzer();
        var diags = await compilation.WithAnalyzers([analyzer]).GetAllDiagnosticsAsync();

        Assert.Contains(diags, d => d.Id == "UG3001");
    }
}