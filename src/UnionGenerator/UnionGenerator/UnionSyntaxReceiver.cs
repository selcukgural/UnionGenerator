using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnionGenerator;

/// <summary>
/// Syntax receiver that collects all classes marked with [GenerateUnion] attribute.
/// </summary>
internal sealed class UnionSyntaxReceiver : ISyntaxReceiver
{
    /// <summary>
    /// Gets the list of class declarations that are marked with [GenerateUnion] attribute.
    /// </summary>
    public List<ClassDeclarationSyntax> UnionClasses { get; } = new();

    /// <summary>
    /// Called for each syntax node in the compilation.
    /// </summary>
    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        // Only process class declarations
        if (syntaxNode is not ClassDeclarationSyntax classDecl)
        {
            return;
        }

        // Check if the class has a [GenerateUnion] attribute
        var hasGenerateUnionAttribute = classDecl.AttributeLists
                                                 .SelectMany(attrList => attrList.Attributes)
                                                 .Any(attr =>
                                                 {
                                                     var name = attr.Name.ToString();
                                                     // Match "GenerateUnion" or "GenerateUnionAttribute" or "UnionGenerator.Attributes.GenerateUnion"
                                                     return name.Contains("GenerateUnion");
                                                 });

        if (hasGenerateUnionAttribute)
        {
            UnionClasses.Add(classDecl);
        }
    }
}