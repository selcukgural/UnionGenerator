---
sidebar_position: 1
---

# Core Package Overview

The UnionGenerator core package provides everything you need to define and use discriminated unions in C#. It leverages Roslyn source generators to create type-safe union types with zero runtime overhead.

## What's Included

The core package consists of:

- **Attributes**: Mark your types for union generation
- **Source Generators**: Compile-time code generation
- **Pattern Matching**: Exhaustive match methods
- **Type Safety**: Compiler-enforced case handling
- **Zero Dependencies**: No external runtime dependencies

## Package Information

```bash
dotnet add package UnionGenerator
```

**NuGet Package**: `UnionGenerator`  
**Target Framework**: .NET Standard 2.0+ (compatible with .NET Framework 4.7.2+, .NET Core 3.1+, .NET 5+)  
**Runtime Dependencies**: None

## Core Concepts

### Source Generation

UnionGenerator uses Roslyn source generators to create union types at compile time:

```csharp
// Your code
[GenerateUnion]
public partial record Result<T, E>
{
    public static partial Result<T, E> Ok(T value);
    public static partial Result<T, E> Error(E error);
}

// Generated code (automatically created)
public partial record Result<T, E>
{
    public sealed record OkCase(T Value) : Result<T, E>;
    public sealed record ErrorCase(E Error) : Result<T, E>;
    
    public static Result<T, E> Ok(T value) => new OkCase(value);
    public static Result<T, E> Error(E error) => new ErrorCase(error);
    
    public TResult Match<TResult>(
        Func<T, TResult> ok,
        Func<E, TResult> error) => this switch
    {
        OkCase c => ok(c.Value),
        ErrorCase c => error(c.Error),
        _ => throw new InvalidOperationException()
    };
    
    // ... more generated code
}
```

### Compile-Time Safety

The generator ensures:

- **No runtime reflection**: Everything is resolved at compile time
- **Type inference**: Generic parameters flow naturally
- **Exhaustiveness**: Compiler warns on missing cases
- **Performance**: Zero allocation overhead for matching

## Key Features

### 1. Simple Declaration

Define unions with minimal syntax:

```csharp
[GenerateUnion]
public partial record HttpResult
{
    public static partial HttpResult Success(string body, int statusCode);
    public static partial HttpResult NotFound();
    public static partial HttpResult Error(string message);
}
```

### 2. Automatic Case Generation

The generator creates nested record types for each case:

```csharp
// Generated automatically
public partial record HttpResult
{
    public sealed record SuccessCase(string Body, int StatusCode) : HttpResult;
    public sealed record NotFoundCase() : HttpResult;
    public sealed record ErrorCase(string Message) : HttpResult;
}
```

### 3. Pattern Matching Support

Multiple matching styles are generated:

```csharp
// Expression-based matching
var response = result.Match(
    success => $"OK: {success.Body}",
    notFound => "Not Found",
    error => $"Error: {error.Message}"
);

// Switch pattern matching
var status = result switch
{
    HttpResult.SuccessCase s => s.StatusCode,
    HttpResult.NotFoundCase => 404,
    HttpResult.ErrorCase => 500,
    _ => throw new InvalidOperationException()
};

// Type checking
if (result is HttpResult.SuccessCase success)
{
    Console.WriteLine(success.Body);
}

// TryGet pattern
if (result.TryGetSuccess(out var success))
{
    Console.WriteLine($"{success.Body} - Status: {success.StatusCode}");
}

// Boolean checks
if (result.IsSuccess)
{
    // Handle success case
}
```

### 4. Generic Support

Full support for generic type parameters:

```csharp
[GenerateUnion]
public partial record Option<T>
{
    public static partial Option<T> Some(T value);
    public static partial Option<T> None();
}

[GenerateUnion]
public partial record Result<TValue, TError>
{
    public static partial Result<TValue, TError> Success(TValue value);
    public static partial Result<TValue, TError> Failure(TError error);
}

// Usage with inference
var intOption = Option<int>.Some(42);
var result = Result<User, string>.Success(new User());
```

### 5. Record and Class Support

Works with both records and classes:

```csharp
// Record-based union (recommended)
[GenerateUnion]
public partial record Shape
{
    public static partial Shape Circle(double radius);
    public static partial Shape Rectangle(double width, double height);
}

// Class-based union
[GenerateUnion]
public partial class LegacyResult
{
    public static partial LegacyResult Ok();
    public static partial LegacyResult Error(string message);
}
```

### 6. Null Safety

Unions eliminate null reference errors:

```csharp
[GenerateUnion]
public partial record LoadingState<T>
{
    public static partial LoadingState<T> Loading();
    public static partial LoadingState<T> Loaded(T data);
    public static partial LoadingState<T> Failed(string error);
}

// No null checks needed!
var state = LoadingState<User>.Loaded(user);
// 'state' is guaranteed to be one of the three cases, never null
```

## Performance Characteristics

### Zero Allocation Matching

Pattern matching doesn't allocate:

```csharp
// No allocations - direct method calls
var result = option.Match(
    some => some.Value * 2,
    none => 0
);
```

### Inline Factory Methods

Factory methods are inlined by the JIT:

```csharp
// Compiles to direct constructor call
var result = Result<int, string>.Success(42);
// JIT inlines this to: new Result<int, string>.SuccessCase(42)
```

### No Virtual Dispatch

Case types are sealed, enabling devirtualization:

```csharp
// No virtual method calls
public sealed record SuccessCase(...) : Result<T, E>;
```

## IDE Support

Full IDE integration:

- **IntelliSense**: Autocomplete for generated members
- **Go to Definition**: Navigate to generated code
- **Find References**: Track usage across codebase
- **Refactoring**: Rename, extract, inline operations
- **Debugging**: Step through generated code

## Generated Code Location

Generated code can be viewed in your IDE:

- **Visual Studio**: Dependencies → Analyzers → UnionGenerator → UnionGenerator.SourceGenerator
- **Rider**: External Libraries → UnionGenerator → Generated Code
- **VS Code**: Requires C# extension

## Next Steps

Dive deeper into specific features:

- [Union Generation](./union-generation.md) - Attributes, options, and customization
- [Pattern Matching](./pattern-matching.md) - All matching techniques
- [API Reference](./api-reference.md) - Complete API documentation
- [Best Practices](./best-practices.md) - Tips and patterns for production code

## Troubleshooting

### Generator Not Running

If unions aren't being generated:

1. Check that the package is properly installed: `dotnet list package`
2. Clean and rebuild: `dotnet clean && dotnet build`
3. Verify partial keyword is present: `partial record` or `partial class`
4. Check IDE logs for generator errors

### Build Errors

Common issues:

```csharp
// ❌ Missing partial keyword
[GenerateUnion]
public record Result { } // Error: must be partial

// ❌ Missing static keyword
[GenerateUnion]
public partial record Result
{
    public partial Result Success(); // Error: must be static
}

// ✅ Correct usage
[GenerateUnion]
public partial record Result
{
    public static partial Result Success();
}
```

### Generated Code Not Visible

If IntelliSense doesn't show generated members:

1. Close and reopen the solution
2. Delete `bin/` and `obj/` folders
3. Restart IDE
4. Check that `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` is set (optional, for debugging)

## Further Reading

- [Source Generators Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
- [Records in C#](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record)
- [Pattern Matching in C#](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/pattern-matching)
