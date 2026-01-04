# UnionGenerator.OneOfCompat

Runtime helpers to convert OneOf&lt;T0,T1&gt; values into UnionGenerator-created discriminated union types. Provides lightweight, reflection-based interoperability with the OneOf library.

## 🚀 Quick Start (2 minutes)

### 1. Install

```bash
dotnet add package UnionGenerator.OneOfCompat
```

### 2. Define Your Union

```csharp
using UnionGenerator.Attributes;

[GenerateUnion]
public partial class Result<T, E>
{
    public static Result<T, E> Ok(T value) => new OkCase(value);
    public static Result<T, E> Error(E error) => new ErrorCase(error);
}
```

### 3. Convert OneOf Values

```csharp
using OneOf;
using UnionGenerator.OneOfCompat;

OneOf<User, string> oneOfResult = GetOneOfResult();

// Convert to generated union using factory methods
var result = OneOfCompat.FromT0<Result<User, string>, User, string>(oneOfResult.AsT0);

// Or use the helper directly
var generatedResult = Result<User, string>.Ok(oneOfResult.AsT0);
```

**That's it!** Your OneOf values are now UnionGenerator unions.

---

## 📚 Features

### ✅ Reflection-Based Conversion
Uses reflection to invoke generated factory methods dynamically.

### ✅ Zero External Dependencies (Core Only)
Only requires `System.*` namespaces. No additional NuGet packages.

### ✅ Supports OneOf v2 & v3
Works with both OneOf v2.x and v3.x library versions.

### ✅ Simple API
Two helper methods: `FromT0()` and `FromT1()` for binary unions.

### ✅ Safe Type Checking
Validates types at runtime before conversion.

---

## 🔧 Core Components

### OneOfCompat Class

```csharp
namespace UnionGenerator.OneOfCompat;

public static class OneOfCompat
{
    /// <summary>
    /// Create a generated union from the first case (T0).
    /// </summary>
    public static TGenerated FromT0<TGenerated, TSuccess, TError>(TSuccess value)
        where TGenerated : class;

    /// <summary>
    /// Create a generated union from the second case (T1).
    /// </summary>
    public static TGenerated FromT1<TGenerated, TSuccess, TError>(TError value)
        where TGenerated : class;
}
```

**How It Works**:
1. Takes a value of type T0 or T1
2. Uses reflection to find the factory method on the generated union
3. Invokes `Ok(value)` or `Error(value)` statically
4. Returns the generated union instance

---

## 📋 Usage Patterns

### Pattern 1: Converting OneOf to Result Union

```csharp
using OneOf;
using UnionGenerator.OneOfCompat;

public class User { public int Id { get; set; } }

// Your OneOf-based function
public OneOf<User, string> GetUserOneOf(int id)
{
    var user = _service.FindById(id);
    return user != null 
        ? user 
        : "User not found";
}

// Generated union
[GenerateUnion]
public partial class Result<T, E>
{
    public static Result<T, E> Ok(T value) => new OkCase(value);
    public static Result<T, E> Error(E error) => new ErrorCase(error);
}

// Convert OneOf to Result
public Result<User, string> GetUser(int id)
{
    var oneOfResult = GetUserOneOf(id);
    
    if (oneOfResult.IsT0)
    {
        return OneOfCompat.FromT0<Result<User, string>, User, string>(oneOfResult.AsT0);
    }
    else
    {
        return OneOfCompat.FromT1<Result<User, string>, User, string>(oneOfResult.AsT1);
    }
}
```

### Pattern 2: Legacy to Modern Migration

If you have legacy OneOf code and want to migrate to UnionGenerator:

**Before (OneOf)**:
```csharp
using OneOf;

public OneOf<Data, Error> Process(Input input)
{
    try
    {
        var data = DoWork(input);
        return data;
    }
    catch (Exception ex)
    {
        return new Error { Message = ex.Message };
    }
}

// Callers
var result = Process(input);
result.Switch(
    data => HandleSuccess(data),
    error => HandleError(error)
);
```

**After (UnionGenerator)**:
```csharp
using UnionGenerator.Attributes;

[GenerateUnion]
public partial class Result<T, E>
{
    public static Result<T, E> Ok(T value) => new OkCase(value);
    public static Result<T, E> Error(E error) => new ErrorCase(error);
}

public Result<Data, Error> Process(Input input)
{
    try
    {
        var data = DoWork(input);
        return Result<Data, Error>.Ok(data);
    }
    catch (Exception ex)
    {
        return Result<Data, Error>.Error(new Error { Message = ex.Message });
    }
}

// Callers (modern pattern matching)
var result = Process(input);
result.Match(
    ok: data => HandleSuccess(data),
    error: error => HandleError(error)
);
```

### Pattern 3: Adapter Function for Gradual Migration

```csharp
using OneOf;
using UnionGenerator.OneOfCompat;

// Helper function to convert all OneOf results
public Result<T, E> ConvertOneOf<T, E>(OneOf<T, E> oneOf)
    where T : class
    where E : class
{
    if (oneOf.IsT0)
    {
        return OneOfCompat.FromT0<Result<T, E>, T, E>(oneOf.AsT0);
    }
    else
    {
        return OneOfCompat.FromT1<Result<T, E>, T, E>(oneOf.AsT1);
    }
}

// Use in your code
var oneOfResult = LegacyFunction();
var result = ConvertOneOf(oneOfResult);
```

---

## ⚡ Performance

### Reflection Overhead

| Operation | Time | Notes |
|-----------|------|-------|
| Factory method lookup (first) | ~10-50 µs | Reflected, cached internally |
| Factory method invocation | ~5-15 µs | Delegate call |
| Total conversion | ~15-65 µs | Per conversion |

### Optimization Tips

1. **Cache factory methods** if converting many times (reflection is expensive)
2. **Use direct assignment** where possible instead of helpers:
   ```csharp
   // Better: Direct factory call (no reflection)
   var result = Result<User, string>.Ok(user);
   
   // Worse: Uses reflection
   var result = OneOfCompat.FromT0<Result<User, string>, User, string>(user);
   ```
3. **Batch conversions** to amortize reflection cost
4. **Consider OneOfSourceGen** for compile-time code generation (zero reflection)

---

## 🔗 When to Use This vs Alternatives

### OneOfCompat vs OneOfExtensions

| Feature | OneOfCompat | OneOfExtensions |
|---------|---|---|
| **Dependencies** | None (core only) | Newtonsoft.Json v13 |
| **OneOf Version** | v2, v3 | v3+ |
| **API Style** | Static helper methods | Extension methods |
| **Performance** | ~15-65 µs | ~5-15 µs (less reflection) |
| **Best For** | Lightweight, no-dependency projects | Standard OneOf v3 projects |

### OneOfCompat vs OneOfSourceGen

| Feature | OneOfCompat | OneOfSourceGen |
|---------|---|---|
| **When Code Runs** | Runtime (reflection) | Compile-time (generated) |
| **Reflection Overhead** | ~15-65 µs | None (zero) |
| **Setup** | Easy (just install) | Requires [GenerateUnion] |
| **Best For** | Quick integration, legacy code | High-performance code paths |
| **Code Size** | Small | Generates .g.cs files |

---

## 📖 Best Practices

### ✅ DO

- Use direct factory calls when you know the type at compile time
- Use helpers only for dynamic/unknown type scenarios
- Cache factory methods if converting many times per second
- Document which code uses OneOf vs UnionGenerator
- Plan migration from OneOf to UnionGenerator gradually

### ❌ DON'T

- Use reflection helpers in hot loops (performance hazard)
- Mix OneOf and UnionGenerator randomly (pick one per codebase)
- Forget that factory methods must match expected names (Ok, Error)
- Assume conversion is zero-cost (it uses reflection)
- Use this in high-frequency trading or extreme performance scenarios (use OneOfSourceGen instead)

---

## 🚨 Troubleshooting

### Factory Method Not Found

**Problem**: 
```
InvalidOperationException: Factory method 'Ok' not found on type 'Result'
```

**Solution**:
1. Verify union type has static factory method: `public static Result Ok(T value)`
2. Ensure method is `public` and `static`
3. Verify method name matches ("Ok" or "Error")
4. Check namespace matches

### Wrong Type Conversion

**Problem**: Creating Result<A, B> but passing Result<C, D> types

**Solution**:
```csharp
// ❌ Wrong - type mismatch
OneOfCompat.FromT0<Result<User, string>, int, string>(42); // User != int

// ✅ Correct
OneOfCompat.FromT0<Result<int, string>, int, string>(42);
```

### Reflection Not Finding Method

**Problem**: Factory method exists but reflection can't find it

**Solution**:
1. Ensure generated code actually compiled (check `obj/Debug/` for `.g.cs`)
2. Try `dotnet clean && dotnet build`
3. Verify union type is `partial` so code generation works
4. Check for naming conflicts or overloads

---

## 📊 Architecture

```
OneOfCompat (Static Helpers)
├── FromT0<TGenerated, T0, T1>(T0 value)
│   └── Uses Reflection to call TGenerated.Ok(value)
└── FromT1<TGenerated, T0, T1>(T1 value)
    └── Uses Reflection to call TGenerated.Error(value)

Generated Union Type
├── Static factories: Ok(), Error()
└── Runtime properties: IsSuccess, Data, Error
```

---

## 🔗 Related Packages

- **UnionGenerator**: Core source generator
- **UnionGenerator.OneOfExtensions**: Alternative with Newtonsoft.Json helpers
- **UnionGenerator.OneOfSourceGen**: Compile-time adapters (zero reflection)
- **OneOf**: The OneOf library this provides compatibility with

---

## 📄 License

MIT (same as UnionGenerator repository)

---

## ✨ Summary

| Feature | Benefit |
|---------|---------|
| **Zero Dependencies** | Lightweight integration |
| **Simple API** | Two methods (FromT0, FromT1) |
| **Reflection-Based** | Dynamic type handling |
| **Fast Setup** | Just install, no config |
| **Backwards Compatible** | Works with OneOf v2 & v3 |

**Use when**: You need to convert OneOf values to UnionGenerator types with minimal dependencies. For high-performance paths, use OneOfSourceGen instead. 🚀

