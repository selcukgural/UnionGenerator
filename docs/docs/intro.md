---
id: welcome
sidebar_position: 1
slug: /
---

# Welcome to UnionGenerator

**Powerful discriminated unions for C# with zero runtime overhead.**

UnionGenerator brings type-safe, compile-time discriminated unions to C# using Roslyn source generators. Build robust applications with exhaustive pattern matching, eliminate boilerplate, and make invalid states impossible.

## Why UnionGenerator?

```csharp
// ✅ Type-safe, explicit, and exhaustive
[GenerateUnion]
public partial class Result<T, E>
{
    public static Result<T, E> Ok(T value) => new OkCase(value);
    public static Result<T, E> Error(E error) => new ErrorCase(error);
}

var result = await FetchDataAsync();
return result.Match(
    ok: data => Ok(data),
    error: err => BadRequest(err.Message)
);
```

## Key Features

- **🔥 Zero Boilerplate**: Define unions with just an attribute
- **⚡ Zero Runtime Cost**: Pure compile-time code generation
- **🛡️ Type-Safe**: Compiler enforces exhaustive pattern matching
- **🔌 Framework Integration**: ASP.NET Core, EF Core, FluentValidation
- **📖 Full IDE Support**: IntelliSense, debugging, refactoring

## Installation

```bash
dotnet add package UnionGenerator
```

[View all packages →](./introduction/installation)

## Quick Example

```csharp
[GenerateUnion]
public partial class Result<T, E>
{
    public static Result<T, E> Ok(T value) => new OkCase(value);
    public static Result<T, E> Error(E error) => new ErrorCase(error);
}

public IActionResult GetUser(int id)
{
    var result = _service.GetUser(id);
    return result.Match(
        ok: user => Ok(user),
        error: msg => NotFound(msg)
    );
}
```

## Next Steps

1. [What is UnionGenerator?](./introduction/what-is-uniongenerator) - Understand the core concepts
2. [Why Discriminated Unions?](./introduction/why-discriminated-unions) - Learn the benefits
3. [Quick Start](./getting-started/quick-start) - Build your first union in 5 minutes

---

Ready to build safer, more expressive C# code? [Get started →](./introduction/what-is-uniongenerator)
