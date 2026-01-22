---
sidebar_position: 5
---

# Code Style Guide

Coding standards and best practices for contributing to UnionGenerator.

## 🎯 General Principles

- **Clarity over cleverness** - Write code that's easy to understand
- **Consistency** - Follow existing patterns in the codebase
- **Simplicity** - Prefer simple solutions over complex ones
- **Performance** - Be mindful of allocations and performance
- **Safety** - Use nullable reference types and avoid nulls

## 📝 C# Coding Standards

### Naming Conventions

#### PascalCase

Used for:
- Class names
- Interface names (with `I` prefix)
- Method names
- Property names
- Public fields
- Namespace names
- Enum types and values

```csharp
// ✅ Good
public class UnionGenerator
public interface ISourceGenerator
public void GenerateCode()
public string UserName { get; set; }
public enum ResultType { Success, Error }

// ❌ Bad
public class unionGenerator
public interface SourceGenerator
public void generateCode()
public string userName { get; set; }
```

#### camelCase

Used for:
- Method parameters
- Local variables
- Private fields (with `_` prefix)

```csharp
// ✅ Good
private readonly string _userName;
public void ProcessUser(int userId, string userName)
{
    var processedName = userName.Trim();
}

// ❌ Bad
private readonly string UserName;
public void ProcessUser(int UserId, string UserName)
{
    var ProcessedName = UserName.Trim();
}
```

### File Organization

```csharp
// 1. Using directives (sorted alphabetically)
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

// 2. Namespace
namespace UnionGenerator.Analyzers;

// 3. XML documentation
/// <summary>
/// Analyzer that detects missing union cases.
/// </summary>
// 4. Class declaration
public sealed class MissingUnionCaseAnalyzer : DiagnosticAnalyzer
{
    // 5. Constants
    public const string DiagnosticId = "UG002";
    
    // 6. Static fields
    private static readonly DiagnosticDescriptor Rule = ...;
    
    // 7. Instance fields
    private readonly ImmutableArray<string> _supportedTypes;
    
    // 8. Constructors
    public MissingUnionCaseAnalyzer()
    {
    }
    
    // 9. Properties
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ...;
    
    // 10. Public methods
    public override void Initialize(AnalysisContext context)
    {
    }
    
    // 11. Private methods
    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
    }
}
```

### Documentation Comments

#### XML Documentation Required

All public APIs must have XML documentation:

```csharp
/// <summary>
/// Marks a class as a discriminated union type that should have case classes
/// and pattern matching properties generated.
/// </summary>
/// <remarks>
/// Apply this attribute to a partial class. The class must define static partial methods
/// that will become factory methods for creating union instances.
/// </remarks>
/// <example>
/// <code>
/// [GenerateUnion]
/// public partial class Result&lt;T, E&gt;
/// {
///     public static partial Result&lt;T, E&gt; Ok(T value);
///     public static partial Result&lt;T, E&gt; Error(E error);
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateUnionAttribute : Attribute
{
}
```

#### XML Tags to Use

- `<summary>` - Brief description (required)
- `<remarks>` - Additional details
- `<param>` - Parameter description
- `<returns>` - Return value description
- `<exception>` - Exceptions that can be thrown
- `<example>` - Usage examples with `<code>` blocks
- `<see cref=""/>` - Cross-references to other types/members

```csharp
/// <summary>
/// Generates source code for a union type.
/// </summary>
/// <param name="context">The generation context containing syntax and semantic information.</param>
/// <param name="unionType">The union type symbol to generate code for.</param>
/// <returns>Generated C# source code as a string.</returns>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> or <paramref name="unionType"/> is null.</exception>
/// <remarks>
/// This method analyzes the union type's structure and generates:
/// <list type="bullet">
/// <item>Nested case classes</item>
/// <item>Factory methods</item>
/// <item>Pattern matching methods</item>
/// <item>Type checking properties</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var code = GenerateUnionCode(context, unionSymbol);
/// context.AddSource("MyUnion.g.cs", code);
/// </code>
/// </example>
public static string GenerateUnionCode(GeneratorExecutionContext context, INamedTypeSymbol unionType)
{
    // Implementation
}
```

### Bracing Style

Always use braces, even for single-line statements:

```csharp
// ✅ Good
if (condition)
{
    DoSomething();
}

for (int i = 0; i < count; i++)
{
    Process(i);
}

// ❌ Bad
if (condition)
    DoSomething();

for (int i = 0; i < count; i++)
    Process(i);
```

### Whitespace and Formatting

```csharp
// Space after keywords
if (condition)
for (int i = 0; i < 10; i++)
while (running)

// No space before opening parenthesis for method calls
DoSomething();
var result = Calculate(x, y);

// Space around operators
var sum = a + b;
var isEqual = x == y;
var combined = firstName + " " + lastName;

// Line breaks for readability
var query = items
    .Where(x => x.IsActive)
    .OrderBy(x => x.Name)
    .Select(x => x.Id);
```

### Nullable Reference Types

Enable and use nullable reference types:

```csharp
#nullable enable

// ✅ Good - Explicit nullability
public string? GetOptionalName(int userId)
{
    return _users.TryGetValue(userId, out var user) ? user.Name : null;
}

public string GetRequiredName(int userId)
{
    if (!_users.TryGetValue(userId, out var user))
    {
        throw new ArgumentException($"User {userId} not found");
    }
    
    return user.Name; // Never null
}

// ❌ Bad - Unclear nullability
public string GetName(int userId)
{
    return _users.TryGetValue(userId, out var user) ? user.Name : null;
}
```

### Modern C# Features

Use modern C# features when appropriate:

```csharp
// ✅ Use file-scoped namespaces (C# 10+)
namespace UnionGenerator.Analyzers;

public class MyAnalyzer { }

// ✅ Use pattern matching
if (symbol is INamedTypeSymbol { IsGenericType: true } namedType)
{
    ProcessGenericType(namedType);
}

// ✅ Use target-typed new (C# 9+)
Dictionary<string, int> map = new();

// ✅ Use collection expressions (C# 12+)
int[] numbers = [1, 2, 3, 4, 5];

// ✅ Use primary constructors (C# 12+) for simple classes
public sealed class Result(string value)
{
    public string Value { get; } = value;
}
```

## 🎨 Source Generator Specific

### Generator Structure

```csharp
[Generator]
public class UnionGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // Register syntax receiver for performance
        context.RegisterForSyntaxNotifications(() => new UnionSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // Guard against no syntax receiver
        if (context.SyntaxReceiver is not UnionSyntaxReceiver receiver)
        {
            return;
        }

        // Process candidates
        foreach (var candidate in receiver.Candidates)
        {
            ProcessCandidate(context, candidate);
        }
    }
}
```

### Performance Considerations

```csharp
// ✅ Good - Use syntax receiver for filtering
class UnionSyntaxReceiver : ISyntaxReceiver
{
    public List<ClassDeclarationSyntax> Candidates { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        // Only collect classes with attributes
        if (syntaxNode is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDecl)
        {
            Candidates.Add(classDecl);
        }
    }
}

// ✅ Good - Cache semantic information
private static bool HasGenerateUnionAttribute(INamedTypeSymbol symbol)
{
    return symbol.GetAttributes()
        .Any(attr => attr.AttributeClass?.Name == "GenerateUnionAttribute");
}

// ❌ Bad - Expensive operations in loops
foreach (var node in allNodes)
{
    var model = compilation.GetSemanticModel(node.SyntaxTree); // Expensive!
    // ...
}
```

### String Building for Code Generation

```csharp
// ✅ Good - Use StringBuilder for large strings
var builder = new StringBuilder();
builder.AppendLine("namespace MyNamespace;");
builder.AppendLine();
builder.AppendLine("public partial class MyClass");
builder.AppendLine("{");
builder.AppendLine("    // Generated code");
builder.AppendLine("}");

// ✅ Good - Use string interpolation for small strings
var methodDeclaration = $"public {returnType} {methodName}({parameters})";

// ❌ Bad - String concatenation in loops
string code = "";
foreach (var member in members)
{
    code += $"public int {member.Name};\n"; // Allocates new string each time!
}
```

## 🧪 Testing Standards

### Test Naming

```csharp
// Pattern: MethodName_Scenario_ExpectedBehavior
public class UnionGeneratorTests
{
    [Fact]
    public void Generate_BasicUnion_CreatesFactoryMethods()
    {
        // Test implementation
    }

    [Fact]
    public void Generate_GenericUnion_PreservesConstraints()
    {
        // Test implementation
    }

    [Fact]
    public void Generate_InvalidSyntax_ReportsDiagnostic()
    {
        // Test implementation
    }
}
```

### Test Structure (Arrange-Act-Assert)

```csharp
[Fact]
public void Match_SuccessCase_ReturnsOkValue()
{
    // Arrange
    var result = Result<int, string>.Ok(42);
    
    // Act
    var value = result.Match(
        ok: x => x,
        error: _ => 0
    );
    
    // Assert
    value.Should().Be(42);
}
```

### Use FluentAssertions

```csharp
// ✅ Good - Descriptive assertions
result.Should().NotBeNull();
result.Value.Should().Be(42);
result.IsSuccess.Should().BeTrue();
list.Should().HaveCount(3);
list.Should().Contain(x => x.Id == 5);

// ❌ Bad - Basic assertions
Assert.NotNull(result);
Assert.Equal(42, result.Value);
Assert.True(result.IsSuccess);
```

## 📋 Code Review Checklist

Before submitting your code:

### General
- [ ] Code follows naming conventions
- [ ] All public APIs have XML documentation
- [ ] No unused usings
- [ ] No commented-out code
- [ ] No hardcoded strings (use constants)
- [ ] No magic numbers
- [ ] Proper error handling

### Performance
- [ ] No unnecessary allocations
- [ ] Efficient string building
- [ ] Proper use of LINQ (avoid multiple enumeration)
- [ ] Syntax receivers used in generators

### Safety
- [ ] Nullable reference types used correctly
- [ ] No potential null reference exceptions
- [ ] Thread-safety considered
- [ ] Resources disposed properly

### Tests
- [ ] Tests added for new functionality
- [ ] Edge cases covered
- [ ] Tests follow naming conventions
- [ ] Tests use FluentAssertions

## 🛠️ Tools

### Recommended IDE Extensions

**Visual Studio:**
- CodeMaid - Code cleanup
- ReSharper - Code analysis (if you have license)

**Rider:**
- Built-in code analysis and cleanup

**VS Code:**
- C# Dev Kit
- EditorConfig

### Code Cleanup

Use IDE code cleanup features before committing:

**Visual Studio / Rider:**
- `Ctrl+K, Ctrl+D` - Format document
- `Ctrl+K, Ctrl+E` - Remove and sort usings

**Command Line:**
```bash
# Format code using dotnet format
dotnet format
```

## 📚 Additional Resources

- [C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [.NET Framework Design Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- [Roslyn Source Generators Cookbook](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md)

## 🤝 Questions?

If you're unsure about style decisions:
- Check existing code for patterns
- Ask in your pull request
- Refer to this guide
- When in doubt, prefer clarity
