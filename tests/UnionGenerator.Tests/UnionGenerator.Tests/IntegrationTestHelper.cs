using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace UnionGenerator.Tests;

/// <summary>
/// Helper class for integration tests that compile and execute generated code.
/// </summary>
internal static class IntegrationTestHelper
{
    /// <summary>
    /// Compiles source code with the generator and returns the compiled assembly.
    /// </summary>
    public static Assembly? CompileAndLoadAssembly(string source, out Compilation compilation)
    {
        // Add necessary using statements to source
        var sourceWithUsings = @"
using System;
using System.Collections.Generic;
using System.Linq;
" + source;
            
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceWithUsings);

        // Get System.Runtime assembly for Func<>, InvalidOperationException, etc.
        var systemRuntime = typeof(Func<>).Assembly;
            
        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(EqualityComparer<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(HashCode).Assembly.Location),
            MetadataReference.CreateFromFile(systemRuntime.Location)
        };

        compilation = CSharpCompilation.Create(
            "TestAssembly", [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Run the generator
        var generator = new UnionGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedTrees = runResult.GeneratedTrees.ToList();

        // Create a new compilation with all syntax trees to ensure proper type resolution
        var allSyntaxTrees = compilation.SyntaxTrees.Concat(generatedTrees).ToList();
        var csharpOptions = compilation.Options as CSharpCompilationOptions ?? new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        compilation = CSharpCompilation.Create(
            compilation.AssemblyName,
            allSyntaxTrees,
            compilation.References,
            csharpOptions);

        // Emit to memory
        using var ms = new MemoryStream();
        var emitResult = compilation.Emit(ms);

        if (!emitResult.Success)
        {
            var errors = string.Join("\n", emitResult.Diagnostics
                                                     .Where(d => d.Severity == DiagnosticSeverity.Error)
                                                     .Select(d => d.GetMessage()));
            throw new InvalidOperationException($"Compilation failed:\n{errors}");
        }

        ms.Seek(0, SeekOrigin.Begin);
        return Assembly.Load(ms.ToArray());
    }

    /// <summary>
    /// Creates an instance of a union type using reflection.
    /// </summary>
    public static object? CreateUnionInstance(Assembly assembly, string typeName, string methodName, params object?[] parameters)
    {
        var type = assembly.GetType(typeName);
        if (type == null)
        {
            throw new InvalidOperationException($"Type {typeName} not found in assembly");
        }

        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        if (method == null)
        {
            throw new InvalidOperationException($"Method {methodName} not found in type {typeName}");
        }

        return method.Invoke(null, parameters);
    }

    /// <summary>
    /// Gets a property value from an object using reflection.
    /// </summary>
    public static object? GetPropertyValue(object instance, string propertyName)
    {
        var type = instance.GetType();
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property == null)
        {
            throw new InvalidOperationException($"Property {propertyName} not found in type {type.Name}");
        }

        return property.GetValue(instance);
    }

    /// <summary>
    /// Invokes a method on an object using reflection.
    /// </summary>
    public static object? InvokeMethod(object instance, string methodName, params object?[] parameters)
    {
        var type = instance.GetType();
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        if (method == null)
        {
            throw new InvalidOperationException($"Method {methodName} not found in type {type.Name}");
        }

        return method.Invoke(instance, parameters);
    }
}