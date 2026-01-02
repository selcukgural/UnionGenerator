using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;

namespace UnionGenerator.Tests.Analyzers;

public class UnionDebugVisualizerCodeFixApplyTests
{
    [Fact]
    public async Task CodeFixAddsDebuggerAttributes()
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
        var diag = diags.FirstOrDefault(d => d.Id == "UG3002");
        Assert.NotNull(diag);

        var root = await tree.GetRootAsync();
        var token = root.FindToken(diag.Location.SourceSpan.Start);
        var typeDecl = token.Parent?.AncestorsAndSelf().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>().FirstOrDefault();
        Assert.NotNull(typeDecl);

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
        var projectInfo = ProjectInfo.Create(projId, version, "p", "p", LanguageNames.CSharp).WithMetadataReferences([MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
                                                                                                                     ]);
        workspace.AddProject(projectInfo);
        var doc = workspace.CurrentSolution.GetProject(projId)!.AddDocument("d.cs", await tree.GetTextAsync());

        var provider = new global::UnionGenerator.Analyzers.UnionDebugVisualizerCodeFix();
        var newDoc = await provider.AddAttributesAsync(doc, typeDecl, default);
        var newText = await newDoc.GetTextAsync();
        var s = newText.ToString();
        Assert.Contains("DebuggerDisplay", s);
        Assert.Contains("DebuggerTypeProxy", s);
    }
}