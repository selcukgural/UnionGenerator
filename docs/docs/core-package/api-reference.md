---
sidebar_position: 4
---

# API Reference

Complete reference for all generated members and their usage.

## Generated Types

For each union, UnionGenerator creates several types and members.

### Case Classes

Each static partial method generates a corresponding sealed record case class:

```csharp
[GenerateUnion]
public partial record Result<T, E>
{
    public static partial Result<T, E> Ok(T value);
    public static partial Result<T, E> Error(E error);
}

// Generated:
public sealed record OkCase(T Value) : Result<T, E>;
public sealed record ErrorCase(E Error) : Result<T, E>;
```

**Naming convention**: `{MethodName}Case`

**Properties:**
- Parameters become public properties
- Properties are `init-only` for records
- Property names match parameter names (PascalCase)

### Factory Methods

Static partial methods are implemented to create instances:

```csharp
public static Result<T, E> Ok(T value) => new OkCase(value);
public static Result<T, E> Error(E error) => new ErrorCase(error);
```

**Signature**: Matches your partial method declaration  
**Return type**: Union type  
**Implementation**: Direct instantiation of case class

## Match Methods

### Match&lt;TResult&gt; (Expression)

Exhaustive pattern matching with a return value.

**Signature:**
```csharp
public TResult Match<TResult>(
    Func<{Case1Parameters}, TResult> case1,
    Func<{Case2Parameters}, TResult> case2,
    // ... one parameter per case
)
```

**Parameters:**
- One `Func` delegate per union case
- Delegate parameters match case class properties
- Delegates must return `TResult`

**Returns:** `TResult` - result of invoked handler

**Example:**
```csharp
var message = result.Match(
    ok => $"Success: {ok.Value}",
    error => $"Error: {error.Error}"
);
```

### Match (Action)

Exhaustive pattern matching for side effects.

**Signature:**
```csharp
public void Match(
    Action<{Case1Parameters}> case1,
    Action<{Case2Parameters}> case2,
    // ... one parameter per case
)
```

**Parameters:**
- One `Action` delegate per union case
- Delegate parameters match case class properties
- Delegates return `void`

**Returns:** `void`

**Example:**
```csharp
result.Match(
    ok => Console.WriteLine($"Success: {ok.Value}"),
    error => Console.WriteLine($"Error: {error.Error}")
);
```

## TryGet Methods

### TryGet{CaseName}

Attempts to extract data if the union matches a specific case.

**Signature:**
```csharp
public bool TryGet{CaseName}(out {CaseName}Case value)
```

**Parameters:**
- `out {CaseName}Case value` - receives the case instance if match succeeds

**Returns:** 
- `true` if union is the specified case
- `false` otherwise

**Side effects:** 
- Sets `value` to the case instance if `true`
- Sets `value` to `null` if `false`

**Example:**
```csharp
if (result.TryGetOk(out var ok))
{
    Console.WriteLine($"Value: {ok.Value}");
}
```

### Generated for Each Case

One `TryGet` method is generated per union case:

```csharp
[GenerateUnion]
public partial record Status
{
    public static partial Status Active();
    public static partial Status Inactive(DateTime since);
    public static partial Status Pending(int priority);
}

// Generated:
public bool TryGetActive(out ActiveCase value);
public bool TryGetInactive(out InactiveCase value);
public bool TryGetPending(out PendingCase value);
```

## Is Properties

### Is{CaseName}

Boolean property indicating if the union is a specific case.

**Signature:**
```csharp
public bool Is{CaseName} { get; }
```

**Returns:** 
- `true` if union is the specified case
- `false` otherwise

**Example:**
```csharp
if (result.IsOk)
{
    // Handle OK case
}
```

### Generated for Each Case

One `Is` property is generated per union case:

```csharp
[GenerateUnion]
public partial record Result
{
    public static partial Result Success();
    public static partial Result Failure(string error);
}

// Generated:
public bool IsSuccess { get; }
public bool IsFailure { get; }
```

## Complete Example

Given this union definition:

```csharp
[GenerateUnion]
public partial record HttpResult
{
    public static partial HttpResult Success(string body, int statusCode);
    public static partial HttpResult NotFound();
    public static partial HttpResult Error(string message);
}
```

### Generated API

```csharp
// Case classes
public sealed record SuccessCase(string Body, int StatusCode) : HttpResult;
public sealed record NotFoundCase() : HttpResult;
public sealed record ErrorCase(string Message) : HttpResult;

// Factory methods
public static HttpResult Success(string body, int statusCode) 
    => new SuccessCase(body, statusCode);
public static HttpResult NotFound() 
    => new NotFoundCase();
public static HttpResult Error(string message) 
    => new ErrorCase(message);

// Match (expression)
public TResult Match<TResult>(
    Func<SuccessCase, TResult> success,
    Func<NotFoundCase, TResult> notFound,
    Func<ErrorCase, TResult> error
)

// Match (action)
public void Match(
    Action<SuccessCase> success,
    Action<NotFoundCase> notFound,
    Action<ErrorCase> error
)

// TryGet methods
public bool TryGetSuccess(out SuccessCase value)
public bool TryGetNotFound(out NotFoundCase value)
public bool TryGetError(out ErrorCase value)

// Is properties
public bool IsSuccess { get; }
public bool IsNotFound { get; }
public bool IsError { get; }
```

### Usage Examples

```csharp
// Factory methods
var result = HttpResult.Success("{ \"data\": 42 }", 200);

// Match expression
var message = result.Match(
    success => $"{success.StatusCode}: {success.Body}",
    notFound => "404: Not Found",
    error => $"Error: {error.Message}"
);

// Match action
result.Match(
    success => Console.WriteLine($"OK: {success.Body}"),
    notFound => Console.WriteLine("Not Found"),
    error => Console.WriteLine($"Error: {error.Message}")
);

// TryGet
if (result.TryGetSuccess(out var success))
{
    Console.WriteLine($"Status: {success.StatusCode}");
    Console.WriteLine($"Body: {success.Body}");
}

// Is properties
if (result.IsSuccess)
{
    Console.WriteLine("Request succeeded");
}

if (result.IsError)
{
    Console.WriteLine("Request failed");
}

// Switch expression
var statusCode = result switch
{
    HttpResult.SuccessCase s => s.StatusCode,
    HttpResult.NotFoundCase => 404,
    HttpResult.ErrorCase => 500,
    _ => 0
};

// Type pattern
if (result is HttpResult.SuccessCase { StatusCode: 200 } success)
{
    Console.WriteLine($"OK: {success.Body}");
}
```

## Generic Unions

Generic type parameters flow through all generated members:

```csharp
[GenerateUnion]
public partial record Option<T>
{
    public static partial Option<T> Some(T value);
    public static partial Option<T> None();
}

// Generated:
public sealed record SomeCase(T Value) : Option<T>;
public sealed record NoneCase() : Option<T>;

public static Option<T> Some(T value) => new SomeCase(value);
public static Option<T> None() => new NoneCase();

public TResult Match<TResult>(
    Func<SomeCase, TResult> some,
    Func<NoneCase, TResult> none
)

// Usage preserves type parameters
Option<int> intOption = Option<int>.Some(42);
Option<string> strOption = Option<string>.None();

var doubled = intOption.Match(
    some => some.Value * 2,  // some.Value is int
    none => 0
);
```

## Inheritance

Generated case classes inherit from the union type:

```csharp
[GenerateUnion]
public partial record Shape
{
    public static partial Shape Circle(double radius);
    public static partial Shape Rectangle(double width, double height);
}

// All cases inherit from Shape
public sealed record CircleCase(double Radius) : Shape;
public sealed record RectangleCase(double Width, double Height) : Shape;

// Type hierarchy
Shape shape = Shape.Circle(5.0);
bool isShape = shape is Shape;        // true
bool isCircle = shape is CircleCase;  // true
```

## Record Features

Case classes are records and support record features:

### With Expressions

```csharp
var original = Result<int, string>.Ok(42);

// Create modified copy (if using record)
var modified = original with { /* properties */ };

// Case-specific with
if (original is Result<int, string>.OkCase ok)
{
    var newOk = ok with { Value = 100 };
}
```

### Deconstruction

```csharp
var success = HttpResult.Success("data", 200);

if (success is HttpResult.SuccessCase(var body, var status))
{
    Console.WriteLine($"Status: {status}, Body: {body}");
}
```

### Value Equality

```csharp
var result1 = Result<int, string>.Ok(42);
var result2 = Result<int, string>.Ok(42);

Console.WriteLine(result1 == result2);  // true (value equality)
Console.WriteLine(result1.Equals(result2));  // true
```

## Naming Rules

### Case Class Names

Format: `{FactoryMethodName}Case`

```csharp
public static partial Result Success();   // → SuccessCase
public static partial Result NotFound();   // → NotFoundCase
public static partial Result ServerError(); // → ServerErrorCase
```

### TryGet Method Names

Format: `TryGet{FactoryMethodName}`

```csharp
public static partial Status Active();     // → TryGetActive
public static partial Status Inactive();   // → TryGetInactive
```

### Is Property Names

Format: `Is{FactoryMethodName}`

```csharp
public static partial Toggle On();   // → IsOn
public static partial Toggle Off();  // → IsOff
```

## Thread Safety

Generated types are thread-safe when used correctly:

- **Immutable records**: Thread-safe by design
- **Factory methods**: Thread-safe (no shared state)
- **Match methods**: Thread-safe (no shared state)
- **Is properties**: Thread-safe (read-only)
- **TryGet methods**: Thread-safe (no shared state)

**Classes with mutable state**: Your responsibility to synchronize

## Performance Notes

- **Factory methods**: Inline by JIT, zero overhead
- **Match**: Zero allocation, direct delegate invocation
- **TryGet**: Zero allocation, simple type check
- **Is properties**: Zero allocation, inline type check
- **Case classes**: Sealed for devirtualization

## Limitations

### Current Limitations

1. **No default parameters** in case definitions
2. **No optional parameters** in case definitions
3. **No params arrays** in case definitions
4. **No ref/out parameters** in case definitions

```csharp
// ❌ Not supported
[GenerateUnion]
public partial record Invalid
{
    public static partial Invalid WithDefault(int x = 42);
    public static partial Invalid WithOptional(string? x);
    public static partial Invalid WithParams(params int[] values);
    public static partial Invalid WithRef(ref int value);
}
```

### Workarounds

Use overloads or builder patterns:

```csharp
[GenerateUnion]
public partial record Result
{
    public static partial Result Success(string message);
    
    // Overload for default case
    public static Result Success() => Success("OK");
}
```

## Next Steps

- [Best Practices](./best-practices.md) - Production-ready patterns
- [Pattern Matching](./pattern-matching.md) - All matching techniques

## Quick Reference Card

```csharp
// Definition
[GenerateUnion]
public partial record MyUnion
{
    public static partial MyUnion CaseA(int x);
    public static partial MyUnion CaseB(string y);
}

// Usage
var instance = MyUnion.CaseA(42);

// Match (expression)
var result = instance.Match(
    caseA => caseA.X * 2,
    caseB => caseB.Y.Length
);

// Match (action)
instance.Match(
    caseA => Console.WriteLine(caseA.X),
    caseB => Console.WriteLine(caseB.Y)
);

// TryGet
if (instance.TryGetCaseA(out var caseA))
{
    Console.WriteLine(caseA.X);
}

// Is property
if (instance.IsCaseA)
{
    // Handle CaseA
}

// Switch
var value = instance switch
{
    MyUnion.CaseACase a => a.X,
    MyUnion.CaseBCase b => b.Y.Length,
    _ => 0
};
```
