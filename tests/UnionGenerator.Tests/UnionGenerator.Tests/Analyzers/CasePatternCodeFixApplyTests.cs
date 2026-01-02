using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;

namespace UnionGenerator.Tests.Analyzers;

public class CasePatternCodeFixApplyTests
{
    [Fact]
    public async Task CodeFixRewritesIfElseToFromOneOf_Return()
    {
        var source = @"
using OneOf;
using System;

class Result
{
    public static Result Ok<T>(T v) => null;
}

class C
{
    Result M(OneOf.OneOf<int,string> o)
    {
        if (o.IsT0)
        {
            return Result.Ok(o.AsT0);
        }
        else if (o.IsT1)
        {
            return Result.Ok(o.AsT1);
        }
        return null;
    }
}
";

        var tree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                                  .Select(a => MetadataReference.CreateFromFile(a.Location))
                                  .ToList();

        // Create workspace/project/doc and add the source
        var assemblies = MefHostServices.DefaultAssemblies.ToList();
        var csharpWorkspaces = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Microsoft.CodeAnalysis.CSharp.Workspaces");
        if (csharpWorkspaces != null)
        {
            assemblies.Add(csharpWorkspaces);
        }
        var host = MefHostServices.Create(assemblies);
        var workspace = new AdhocWorkspace(host);
        var projId = ProjectId.CreateNewId();
        var version = VersionStamp.Create();
        var projectInfo = ProjectInfo.Create(projId, version, "p", "p", LanguageNames.CSharp).WithMetadataReferences(references);
        workspace.AddProject(projectInfo);
        var doc = workspace.CurrentSolution.GetProject(projId)!.AddDocument("d.cs", await tree.GetTextAsync());

        var project = doc.Project;
        var compilation = await project.GetCompilationAsync();
        Assert.NotNull(compilation);

        var analyzer = new global::UnionGenerator.Analyzers.CasePatternAnalyzer();
        var diags = await compilation.WithAnalyzers([analyzer]).GetAllDiagnosticsAsync();
        var diag = diags.FirstOrDefault(d => d.Id == "UG3001");
        Assert.NotNull(diag);

        // Find the if-node in the document's syntax tree
        var docRoot = await doc.GetSyntaxRootAsync();
        var token = docRoot!.FindToken(diag.Location.SourceSpan.Start);
        var ifNode = token.Parent?.AncestorsAndSelf().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax>().FirstOrDefault();
        Assert.NotNull(ifNode);

        var provider = new global::UnionGenerator.Analyzers.CasePatternCodeFixProvider();
        var newDoc = await provider.ReplaceWithAdapterAsync(doc, ifNode, default);
        var newText = await newDoc.GetTextAsync();
        Assert.Contains("FromOneOf<", newText.ToString());
    }
}