---
sidebar_position: 3
---

# Key Features

UnionGenerator provides a comprehensive set of features that make discriminated unions powerful and ergonomic in C#.

## 🚀 Core Features

### Automatic Code Generation

Define unions with minimal code—UnionGenerator handles the rest:

```csharp
[GenerateUnion]
public partial class Result<T, E>
{
    public static Result<T, E> Ok(T value) => new OkCase(value);
    public static Result<T, E> Error(E error) => new ErrorCase(error);
}

// Generated automatically:
// - OkCase and ErrorCase classes
// - IsOk, IsError properties
// - Match() methods
// - TryGetOk(), TryGetError() methods
// - Equality, GetHashCode, ToString
// - And more...
```

### Exhaustive Pattern Matching

The compiler ensures you handle all cases:

```csharp
// ✅ Switch expressions
var message = result switch
{
    { IsOk: true, Ok: var value } => $"Success: {value}",
    { IsError: true, Error: var err } => $"Error: {err}",
    _ => throw new UnreachableException() // Never happens
};

// ✅ Match method
var output = result.Match(
    ok: value => ProcessSuccess(value),
    error: err => HandleError(err)
);

// ✅ Async Match
var output = await result.MatchAsync(
    ok: async value => await SaveAsync(value),
    error: async err => await LogErrorAsync(err)
);
```

### Type-Safe Case Access

Access union cases without casting:

```csharp
// ✅ Safe extraction with TryGet
if (result.TryGetOk(out var value))
{
    Console.WriteLine($"Got value: {value}");
}

// ✅ Direct property access (throws if wrong case)
var value = result.Ok; // Throws if IsError

// ✅ Safe property access
var message = result.IsOk 
    ? $"Success: {result.Ok}" 
    : $"Error: {result.Error}";
```

### Generic Support

Full support for generic unions:

```csharp
[GenerateUnion]
public partial class Result<T, E>
{
    public static Result<T, E> Ok(T value) => new OkCase(value);
    public static Result<T, E> Error(E error) => new ErrorCase(error);
}

// Use with any types
Result<int, string> intResult = Result.Ok(42);
Result<User, ValidationError> userResult = Result.Ok(user);
Result<List<Product>, DatabaseError> productsResult = Result.Ok(products);

// Constraints work too
[GenerateUnion]
public partial class Result<T, E> where E : Exception
{
    // ...
}
```

### Nested Unions

Unions can contain other unions:

```csharp
[GenerateUnion]
public partial class ValidationResult<T>
{
    public static ValidationResult<T> Valid(T value) => new ValidCase(value);
    public static ValidationResult<T> Invalid(ValidationErrors errors) => new InvalidCase(errors);
}

[GenerateUnion]
public partial class ApiResult<T>
{
    public static ApiResult<T> Success(T data) => new SuccessCase(data);
    public static ApiResult<T> ValidationFailed(ValidationResult<T> validation) => new ValidationFailedCase(validation);
    public static ApiResult<T> ServerError(string message) => new ServerErrorCase(message);
}

// Nested matching
return apiResult.Match(
    success: data => Ok(data),
    validationFailed: validation => validation.Match(
        valid: _ => Ok(),
        invalid: errors => BadRequest(errors)
    ),
    serverError: msg => StatusCode(500, msg)
);
```

## 🔌 Integration Features

### ASP.NET Core Integration

Automatic `ProblemDetails` mapping:

```csharp
// Install: dotnet add package UnionGenerator.AspNetCore

[GenerateUnion]
[MapToProblemDetails] // ← Automatic HTTP status mapping
public partial class ApiError
{
    [StatusCode(404)]
    public static ApiError NotFound(string resource) => new NotFoundCase(resource);
    
    [StatusCode(400)]
    public static ApiError BadRequest(string message) => new BadRequestCase(message);
    
    [StatusCode(500)]
    public static ApiError ServerError(string message) => new ServerErrorCase(message);
}

// In controller
public IActionResult GetUser(int id)
{
    var result = _service.GetUser(id);
    return result.ToActionResult(); // Automatic ProblemDetails!
}
```

### Entity Framework Core Integration

Store unions in databases:

```csharp
// Install: dotnet add package UnionGenerator.EntityFrameworkCore

public class Order
{
    public int Id { get; set; }
    
    // Union stored as JSON or discriminator
    public OrderStatus Status { get; set; }
}

// DbContext configuration
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Order>()
        .Property(o => o.Status)
        .HasUnionConversion(); // Automatic value converter!
}

// Queries work naturally
var pendingOrders = await _db.Orders
    .Where(o => o.Status.IsPending)
    .ToListAsync();
```

### FluentValidation Integration

Map validation errors to unions:

```csharp
// Install: dotnet add package UnionGenerator.FluentValidation

[GenerateUnion]
public partial class CreateUserResult
{
    public static CreateUserResult Success(User user) => new SuccessCase(user);
    public static CreateUserResult ValidationFailed(ValidationErrors errors) => new ValidationFailedCase(errors);
}

// Automatic validation to union mapping
public async Task<CreateUserResult> CreateUser(CreateUserCommand command)
{
    var validation = await _validator.ValidateAsync(command);
    if (!validation.IsValid)
        return CreateUserResult.ValidationFailed(validation.ToErrors());
    
    var user = await _repository.CreateAsync(command);
    return CreateUserResult.Success(user);
}
```

### OneOf Migration Helper

Migrate from OneOf library:

```csharp
// Install: dotnet add package UnionGenerator.OneOfCompat

// Your old OneOf code
OneOf<Success, NotFound, Error> result = new Success(data);

// UnionGenerator equivalent
[GenerateUnion]
[OneOfCompatible] // ← Generates OneOf-style API
public partial class Result<T0, T1, T2>
{
    public static Result<T0, T1, T2> FromT0(T0 value) => new T0Case(value);
    public static Result<T0, T1, T2> FromT1(T1 value) => new T1Case(value);
    public static Result<T0, T1, T2> FromT2(T2 value) => new T2Case(value);
}
```

## 🛠️ Developer Experience Features

### Full IntelliSense Support

Generated code works seamlessly with your IDE:

- ✅ Autocomplete for all cases
- ✅ Go to definition
- ✅ Find all references
- ✅ Refactoring support
- ✅ Debugging with F11 step-into

### Source Code Visibility

View generated code anytime:

```csharp
// In Visual Studio/Rider:
// Right-click on [GenerateUnion] → View Generated Code

// Or find it in:
// obj/Debug/net8.0/generated/UnionGenerator/...
```

### Compile-Time Diagnostics

Helpful error messages guide you:

```csharp
[GenerateUnion]
public partial class Result<T> // ❌ Error: Must have at least 2 factory methods
{
    public static Result<T> Ok(T value) => new OkCase(value);
    // Missing second case!
}

// Compiler error:
// UG001: Union type 'Result<T>' must define at least two static factory methods
```

### Roslyn Analyzers

Optional analyzers catch common mistakes:

```csharp
// Install: dotnet add package UnionGenerator.Analyzers

var result = GetResult();

// ⚠️ Warning: Pattern matching not exhaustive
var value = result switch
{
    { IsOk: true } => result.Ok
    // Missing Error case!
};

// ⚠️ Warning: Unnecessary null check
if (result.Ok != null) // Result.Ok is never null
{
    // ...
}
```

## ⚡ Performance Features

### Zero Runtime Overhead

Everything compiles to simple field access:

```csharp
// Your code
var result = Result.Ok(42);
if (result.IsOk)
{
    Console.WriteLine(result.Ok);
}

// Compiles to (simplified):
var result = new Result<int, string>.OkCase(42);
if (result._tag == 0) // Simple int comparison
{
    Console.WriteLine(result._value); // Direct field access
}
```

### Struct-Based Unions

Generate struct unions for stack allocation:

```csharp
[GenerateUnion(AsStruct = true)] // ← Value type union
public partial struct Option<T>
{
    public static Option<T> Some(T value) => new SomeCase(value);
    public static Option<T> None() => new NoneCase();
}

// No heap allocations!
Option<int> value = Option.Some(42);
```

### No Reflection

Source generation means zero reflection at runtime:

- ✅ AOT (Native AOT) compatible
- ✅ Trim-friendly
- ✅ Fast startup
- ✅ Small binary size

## 🎨 Code Quality Features

### Immutability by Default

Generated types are immutable:

```csharp
var result = Result.Ok(42);
// result.Ok = 100; // ❌ Compile error: no setter

// Use records for mutable data if needed
public record UserData(string Name, int Age);
```

### Equality and Comparison

Proper value equality:

```csharp
var result1 = Result.Ok(42);
var result2 = Result.Ok(42);

result1 == result2; // ✅ true (value equality)
result1.Equals(result2); // ✅ true
result1.GetHashCode() == result2.GetHashCode(); // ✅ true
```

### Meaningful ToString()

Useful debugging output:

```csharp
var result = Result.Ok(42);
Console.WriteLine(result); 
// Output: Result<Int32, String> { Ok = 42 }

var error = Result.Error("Failed");
Console.WriteLine(error);
// Output: Result<Int32, String> { Error = "Failed" }
```

## Next Steps

Ready to get started?

- [Install UnionGenerator](./installation)
- [Build your first union](../getting-started/your-first-union)
- [Learn common patterns](../getting-started/common-patterns)
