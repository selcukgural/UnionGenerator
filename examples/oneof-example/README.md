# OneOf Compatibility Example

This example demonstrates how to migrate from the OneOf library to UnionGenerator, and how to interop between both approaches during gradual migration.

## Features Demonstrated

1. **OneOf Library Usage**: Using existing OneOf<T0, T1> types
2. **OneOfCompat Helpers**: Runtime conversion with zero dependencies
3. **OneOfExtensions**: Fluent API for modern conversions
4. **OneOfSourceGen**: Compile-time adapters for high performance
5. **Gradual Migration**: Converting code from OneOf to UnionGenerator
6. **Performance Comparison**: Reflection vs generated code

## Running the Example

```bash
cd examples/oneof-example
dotnet run
```

## What This Does

### 1. Using the OneOf Library

```csharp
using OneOf;

public OneOf<User, Error> GetUserOneOf(int id)
{
    var user = _repository.FindById(id);
    return user != null 
        ? user 
        : new Error { Message = "User not found" };
}
```

### 2. Three Ways to Migrate

#### Option A: Runtime Helpers (Minimal Dependencies)

```csharp
using UnionGenerator.OneOfCompat;

OneOf<User, Error> oneOfResult = GetUserOneOf(1);

// Convert using static helper
var unionResult = OneOfCompat.FromT0<Result<User, Error>, User, Error>(
    oneOfResult.AsT0
);
```

**Pros**: No external dependencies, simple
**Cons**: Reflection overhead (~15-65 µs)

#### Option B: Fluent Extensions (Standard Approach)

```csharp
using UnionGenerator.OneOfExtensions;

OneOf<User, Error> oneOfResult = GetUserOneOf(1);

// Convert using fluent extension method
var unionResult = oneOfResult.ToGeneratedResult<Result<User, Error>, User, Error>();
```

**Pros**: Natural fluent API, JSON helpers included
**Cons**: Newtonsoft.Json dependency

#### Option C: Compile-Time Adapters (High Performance)

```csharp
using OneOf;

OneOf<User, Error> oneOfResult = GetUserOneOf(1);

// Generated adapter method (zero reflection)
var unionResult = oneOfResult.FromOneOf<Result<User, Error>, User, Error>();
```

**Pros**: Ultra-fast (~10-50 ns), no reflection
**Cons**: Requires more setup

### 3. Complete Migration Example

**Before (OneOf)**:
```csharp
public class UserService
{
    public OneOf<User, Error> GetUser(int id)
    {
        var user = _repo.FindById(id);
        return user != null ? user : new Error { Message = "Not found" };
    }
    
    public OneOf<User[], Error> GetAllUsers()
    {
        try
        {
            var users = _repo.GetAll();
            return users;
        }
        catch (Exception ex)
        {
            return new Error { Message = ex.Message };
        }
    }
}
```

**After (UnionGenerator)**:
```csharp
[GenerateUnion]
public partial class Result<T, E>
{
    public static Result<T, E> Ok(T value) => new OkCase(value);
    public static Result<T, E> Error(E error) => new ErrorCase(error);
}

public class UserService
{
    public Result<User, Error> GetUser(int id)
    {
        var user = _repo.FindById(id);
        return user != null 
            ? Result<User, Error>.Ok(user)
            : Result<User, Error>.Error(new Error { Message = "Not found" });
    }
    
    public Result<User[], Error> GetAllUsers()
    {
        try
        {
            var users = _repo.GetAll();
            return Result<User[], Error>.Ok(users);
        }
        catch (Exception ex)
        {
            return Result<User[], Error>.Error(new Error { Message = ex.Message });
        }
    }
}
```

## Gradual Migration Path

### Step 1: Install UnionGenerator Alongside OneOf

```bash
dotnet add package UnionGenerator
dotnet add package UnionGenerator.AspNetCore
dotnet add package UnionGenerator.OneOfCompat  # For helpers
```

### Step 2: Create UnionGenerator Types (Parallel to OneOf)

```csharp
[GenerateUnion]
public partial class Result<T, E>
{
    public static Result<T, E> Ok(T value) => new OkCase(value);
    public static Result<T, E> Error(E error) => new ErrorCase(error);
}
```

### Step 3: Create Adapter Functions

```csharp
// Helper to convert OneOf to Result
public static Result<T, E> ToResult<T, E>(OneOf<T, E> oneOf)
    where T : class
    where E : class
{
    if (oneOf.IsT0)
        return OneOfCompat.FromT0<Result<T, E>, T, E>(oneOf.AsT0);
    else
        return OneOfCompat.FromT1<Result<T, E>, T, E>(oneOf.AsT1);
}
```

### Step 4: Gradually Convert Existing Code

```csharp
// Before: Returns OneOf
public OneOf<User, Error> GetUser(int id) { ... }

// During: Returns Result but uses OneOf internally
public Result<User, Error> GetUser(int id)
{
    var oneOf = _legacyService.GetUserOneOf(id);
    return ToResult(oneOf);
}

// After: Full UnionGenerator
public Result<User, Error> GetUser(int id)
{
    var user = _repo.FindById(id);
    return user != null ? Result<User, Error>.Ok(user) : Result<User, Error>.Error(...);
}
```

### Step 5: Retire OneOf

Once all code is migrated:
```bash
dotnet remove package OneOf
# Remove OneOfCompat if using OneOfSourceGen instead
```

## Comparison: OneOf vs UnionGenerator

| Feature | OneOf | UnionGenerator |
|---------|-------|---|
| **Pattern Matching** | ✅ Yes | ✅ Yes |
| **Type Safety** | ✅ Good | ✅ Excellent (code-gen) |
| **Reflection Overhead** | ❌ None | ✅ None (generated) |
| **ASP.NET Integration** | ❌ No | ✅ Yes (AspNetCore) |
| **Database Storage** | ❌ No | ✅ Yes (EF Core) |
| **Validation Integration** | ❌ No | ✅ Yes (FluentValidation) |
| **IntelliSense** | ✅ Good | ✅ Excellent |
| **Learning Curve** | ✅ Easy | ✅ Easy |
| **Setup Required** | ✅ Simple | ✅ Simple |
| **Active Development** | ⚠️ Stable | ✅ Active |

## Performance Comparison

```
Conversion: OneOf<User, Error> → Result<User, Error>

Direct (no conversion):     N/A
OneOfCompat:                ~15-65 µs (reflection)
OneOfExtensions:            ~10-35 µs (cached reflection)
OneOfSourceGen:             ~10-50 ns (generated)

Pattern matching:           Similar performance
Union creation:             OneOf ~5-15 ns, UnionGenerator ~5-15 ns
```

## Use Cases for Coexistence

You don't need to remove OneOf immediately:

### Use OneOf When:
- Working with external APIs that return OneOf types
- Maintaining legacy code during migration
- Implementing middleware that bridges both ecosystems

### Use UnionGenerator When:
- New code and new projects
- Need ASP.NET Core integration
- Need EF Core database storage
- Performance is critical (high-frequency calls)

## Testing

```bash
dotnet test
```

Tests validate:
- OneOf types convert correctly
- All three adapter approaches work
- Semantics preserved during conversion
- Performance assertions for OneOfSourceGen

## Examples in This Folder

- `Program.cs` - Demonstrates all three conversion approaches
- Shows performance comparison between methods
- Includes real-world usage patterns

## Migration Checklist

- [ ] Analyze OneOf usage in codebase
- [ ] Identify hot paths (performance-critical code)
- [ ] Create UnionGenerator equivalents
- [ ] Add adapter functions
- [ ] Convert services one-by-one
- [ ] Update unit tests
- [ ] Verify performance improvements
- [ ] Remove OneOf dependency

## Best Practices During Migration

### ✅ DO

- Migrate incrementally (one service at a time)
- Keep adapters isolated in utility classes
- Test each conversion thoroughly
- Document why you're migrating
- Measure performance before/after
- Use OneOfSourceGen for hot paths
- Keep error types identical during transition

### ❌ DON'T

- Try to migrate entire codebase at once
- Mix conversion styles in same module
- Forget to update unit tests
- Remove OneOf too early
- Assume no performance change
- Modify OneOf library code
- Create circular dependencies between approaches

## Related Documentation

- [UnionGenerator README](../../src/UnionGenerator/README.md)
- [UnionGenerator.OneOfCompat README](../../src/UnionGenerator.OneOfCompat/README.md)
- [UnionGenerator.OneOfExtensions README](../../src/UnionGenerator.OneOfExtensions/README.md)
- [UnionGenerator.OneOfSourceGen README](../../src/UnionGenerator.OneOfSourceGen/README.md)
- [OneOf Library](https://github.com/McCreary/OneOf)

---

**Ready to migrate?** Run `dotnet run` to see all three approaches in action! 🚀

