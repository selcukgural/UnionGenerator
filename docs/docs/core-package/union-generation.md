---
sidebar_position: 2
---

# Union Generation

Learn how to use the `[GenerateUnion]` attribute and customize union generation to fit your needs.

## The GenerateUnion Attribute

The `[GenerateUnion]` attribute is the entry point for creating discriminated unions:

```csharp
using UnionGenerator;

[GenerateUnion]
public partial record MyUnion
{
    public static partial MyUnion CaseA(string value);
    public static partial MyUnion CaseB(int number);
}
```

### Requirements

For the generator to work, your type must:

1. Be marked with `[GenerateUnion]`
2. Have the `partial` keyword
3. Be a `record` or `class`
4. Define at least one static partial method
5. Each method must return the union type

## Defining Cases

Each case is defined as a static partial method:

```csharp
[GenerateUnion]
public partial record PaymentStatus
{
    // Case with no parameters
    public static partial PaymentStatus Pending();
    
    // Case with single parameter
    public static partial PaymentStatus Completed(string transactionId);
    
    // Case with multiple parameters
    public static partial PaymentStatus Failed(string reason, DateTime failedAt);
}
```

### Case Naming Conventions

**Factory method names become case class names:**

```csharp
[GenerateUnion]
public partial record Result
{
    public static partial Result Success();  // → Result.SuccessCase
    public static partial Result Error();    // → Result.ErrorCase
}
```

**Best practices:**

- Use PascalCase for case names
- Choose descriptive names (not `Case1`, `Case2`)
- Consider the domain language (`Approved` vs `Case3`)

```csharp
// ❌ Poor naming
[GenerateUnion]
public partial record Status
{
    public static partial Status Case1();
    public static partial Status Case2();
}

// ✅ Good naming
[GenerateUnion]
public partial record OrderStatus
{
    public static partial OrderStatus Draft();
    public static partial OrderStatus Submitted();
    public static partial OrderStatus Shipped();
    public static partial OrderStatus Delivered();
}
```

## Case Parameters

Cases can have zero or more parameters:

### Parameterless Cases

```csharp
[GenerateUnion]
public partial record Toggle
{
    public static partial Toggle On();
    public static partial Toggle Off();
}

// Usage
var toggle = Toggle.On();
```

### Single Parameter Cases

```csharp
[GenerateUnion]
public partial record AsyncOperation<T>
{
    public static partial AsyncOperation<T> Loading();
    public static partial AsyncOperation<T> Loaded(T data);
    public static partial AsyncOperation<T> Failed(Exception error);
}

// Usage
var operation = AsyncOperation<User>.Loaded(user);
```

### Multiple Parameter Cases

```csharp
[GenerateUnion]
public partial record LogEntry
{
    public static partial LogEntry Info(string message, DateTime timestamp);
    public static partial LogEntry Warning(string message, DateTime timestamp, string source);
    public static partial LogEntry Error(string message, DateTime timestamp, Exception exception);
}

// Usage
var log = LogEntry.Error("Database connection failed", DateTime.UtcNow, exception);
```

### Complex Parameter Types

Parameters can be any valid C# type:

```csharp
[GenerateUnion]
public partial record ApiResponse
{
    // Collection types
    public static partial ApiResponse Success(List<User> users);
    
    // Tuple types
    public static partial ApiResponse PartialSuccess(
        List<User> succeeded, 
        List<(string Email, string Error)> failed
    );
    
    // Record types
    public static partial ApiResponse ValidationError(ValidationResult result);
    
    // Nullable types
    public static partial ApiResponse NotModified(DateTime? lastModified);
}
```

## Generic Unions

UnionGenerator supports full generic type parameters:

### Single Type Parameter

```csharp
[GenerateUnion]
public partial record Option<T>
{
    public static partial Option<T> Some(T value);
    public static partial Option<T> None();
}

// Usage with type inference
var someInt = Option<int>.Some(42);
var noneString = Option<string>.None();
```

### Multiple Type Parameters

```csharp
[GenerateUnion]
public partial record Result<TValue, TError>
{
    public static partial Result<TValue, TError> Ok(TValue value);
    public static partial Result<TValue, TError> Error(TError error);
}

// Usage
var success = Result<User, string>.Ok(user);
var failure = Result<User, string>.Error("User not found");
```

### Generic Constraints

Apply constraints to generic parameters:

```csharp
[GenerateUnion]
public partial record Validated<T> where T : IValidatable
{
    public static partial Validated<T> Valid(T value);
    public static partial Validated<T> Invalid(List<ValidationError> errors);
}

// Type parameter constraints are enforced
public class User : IValidatable { }
var validated = Validated<User>.Valid(user); // ✅ Works
// var invalid = Validated<string>.Valid("test"); // ❌ Error: string doesn't implement IValidatable
```

## Inheritance and Interfaces

Unions can implement interfaces:

```csharp
public interface IResult
{
    bool IsSuccess { get; }
}

[GenerateUnion]
public partial record Result : IResult
{
    public static partial Result Success();
    public static partial Result Failure(string error);
    
    // Implement interface
    public bool IsSuccess => this is SuccessCase;
}
```

### Base Classes

Unions can inherit from base classes:

```csharp
public abstract record Entity
{
    public Guid Id { get; init; } = Guid.NewGuid();
}

[GenerateUnion]
public partial record Event : Entity
{
    public static partial Event Created(DateTime timestamp);
    public static partial Event Updated(DateTime timestamp, string by);
    public static partial Event Deleted(DateTime timestamp);
}

// All cases inherit from Entity
var evt = Event.Created(DateTime.UtcNow);
Console.WriteLine(evt.Id); // Guid from base class
```

## Record vs Class

UnionGenerator works with both records and classes:

### Using Records (Recommended)

Records provide:
- Value equality by default
- Immutability
- Concise syntax
- With-expressions

```csharp
[GenerateUnion]
public partial record Status
{
    public static partial Status Active();
    public static partial Status Inactive(DateTime since);
}

// Value equality
var status1 = Status.Active();
var status2 = Status.Active();
Console.WriteLine(status1 == status2); // True

// Immutability enforced
// status1.Something = "value"; // Compile error if properties are init
```

### Using Classes

Use classes when you need:
- Reference equality
- Mutable state
- Compatibility with older codebases

```csharp
[GenerateUnion]
public partial class LegacyStatus
{
    public static partial LegacyStatus Active();
    public static partial LegacyStatus Inactive();
}

// Reference equality
var status1 = LegacyStatus.Active();
var status2 = LegacyStatus.Active();
Console.WriteLine(status1 == status2); // False (different instances)
```

## Nested Unions

Unions can be nested within other types:

```csharp
public class OrderSystem
{
    [GenerateUnion]
    public partial record OrderEvent
    {
        public static partial OrderEvent Created(Guid orderId);
        public static partial OrderEvent Cancelled(Guid orderId, string reason);
    }
    
    [GenerateUnion]
    public partial record PaymentEvent
    {
        public static partial PaymentEvent Initiated(decimal amount);
        public static partial PaymentEvent Completed(string transactionId);
    }
}

// Usage
var orderEvent = OrderSystem.OrderEvent.Created(Guid.NewGuid());
```

## Accessibility Modifiers

Control union visibility:

```csharp
// Public union (accessible everywhere)
[GenerateUnion]
public partial record PublicResult
{
    public static partial PublicResult Success();
}

// Internal union (accessible within assembly)
[GenerateUnion]
internal partial record InternalResult
{
    public static partial InternalResult Success();
}

// Private nested union
public class Service
{
    [GenerateUnion]
    private partial record PrivateState
    {
        public static partial PrivateState Idle();
        public static partial PrivateState Running();
    }
}
```

## Namespace Organization

Organize unions by domain:

```csharp
namespace MyApp.Domain.Orders
{
    [GenerateUnion]
    public partial record OrderStatus
    {
        public static partial OrderStatus Pending();
        public static partial OrderStatus Confirmed();
    }
}

namespace MyApp.Domain.Payments
{
    [GenerateUnion]
    public partial record PaymentStatus
    {
        public static partial PaymentStatus Pending();
        public static partial PaymentStatus Completed();
    }
}
```

## Common Patterns

### Builder Pattern

```csharp
[GenerateUnion]
public partial record ValidationResult
{
    public static partial ValidationResult Valid();
    public static partial ValidationResult Invalid(List<string> errors);
    
    public static ValidationResult FromErrors(params string[] errors)
    {
        return errors.Length == 0 
            ? Valid() 
            : Invalid(errors.ToList());
    }
}

// Usage
var result = ValidationResult.FromErrors("Email required", "Age must be positive");
```

### Factory Extensions

```csharp
public static class ResultExtensions
{
    public static Result<T, Exception> Try<T>(Func<T> action)
    {
        try
        {
            return Result<T, Exception>.Ok(action());
        }
        catch (Exception ex)
        {
            return Result<T, Exception>.Error(ex);
        }
    }
}

// Usage
var result = ResultExtensions.Try(() => int.Parse("42"));
```

## Next Steps

- [Pattern Matching](./pattern-matching.md) - Learn all matching techniques
- [API Reference](./api-reference.md) - Complete generated API
- [Best Practices](./best-practices.md) - Production-ready patterns

## Tips

💡 **Use records over classes** for immutability and value semantics  
💡 **Name cases descriptively** to make code self-documenting  
💡 **Keep case parameters focused** - each case should have a clear purpose  
💡 **Leverage generics** for reusable union types  
💡 **Implement interfaces** when unions need to fit into existing systems
