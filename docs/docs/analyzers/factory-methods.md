---
sidebar_position: 3
---

# Factory Method Analyzers

Factory method analyzers validate union type definitions, ensuring factory methods follow supported patterns and detecting common mistakes during union declaration.

## 🎯 Overview

These analyzers run when you define unions with `[GenerateUnion]`, checking factory method signatures and catching definition errors early.

| Diagnostic ID | Name | Severity |
|--------------|------|----------|
| **UG9002** | No union cases found | Warning |
| **UG9003** | Multiple parameters in factory | Warning |
| **UG9004** | Duplicate case signature | Warning |

## UG9002: No Union Cases Found

### What It Detects

Union types with `[GenerateUnion]` attribute but no valid factory methods.

### Example Problem

```csharp
// ❌ UG9002: No factory methods defined
[GenerateUnion]
public partial record EmptyUnion
{
    // No static partial methods!
}
```

### Diagnostic Message

> No union cases (static factory methods) were found for 'EmptyUnion'. The generator will not produce any union code.

### Why This Happens

Common causes:
1. Forgot to add factory methods
2. Factory methods not marked `static partial`
3. Factory methods don't return the union type
4. Methods not inside the union type declaration

### How to Fix

Add at least one static partial factory method:

```csharp
// ✅ Fixed: Added factory methods
[GenerateUnion]
public partial record Result<T, TError>
{
    public static partial Result<T, TError> Ok(T value);
    public static partial Result<T, TError> Error(TError error);
}
```

### Valid Factory Method Signatures

The generator recognizes these patterns:

```csharp
[GenerateUnion]
public partial record MyUnion
{
    // ✅ Single parameter
    public static partial MyUnion Case1(string value);
    
    // ✅ No parameters
    public static partial MyUnion Case2();
    
    // ✅ Generic parameter
    public static partial MyUnion Case3(int count);
    
    // ✅ Any type parameter
    public static partial MyUnion Case4(ComplexType data);
}
```

### Invalid Patterns

```csharp
[GenerateUnion]
public partial record MyUnion
{
    // ❌ Not static
    public partial MyUnion InvalidCase1(string value);
    
    // ❌ Not partial
    public static MyUnion InvalidCase2(string value) => default!;
    
    // ❌ Wrong return type
    public static partial string InvalidCase3(string value);
    
    // ❌ Instance method
    public void InvalidCase4() { }
}
```

## UG9003: Multiple Parameters

### What It Detects

Factory methods with more than one parameter. Currently, only single-parameter or parameterless factories are supported.

### Example Problem

```csharp
[GenerateUnion]
public partial record Coordinate
{
    // ❌ UG9003: Multiple parameters not supported
    public static partial Coordinate Point(double x, double y);
}
```

### Diagnostic Message

> Factory method 'Point' has multiple parameters. Only single-parameter or parameterless factory methods are currently supported for code generation.

### How to Fix

**Option 1: Use a wrapper type**

```csharp
// ✅ Wrapper record for multiple values
public record Point2D(double X, double Y);

[GenerateUnion]
public partial record Coordinate
{
    public static partial Coordinate Point(Point2D point);
}

// Usage:
var coord = Coordinate.Point(new Point2D(10.5, 20.3));
```

**Option 2: Use tuples**

```csharp
// ✅ Tuple parameter
[GenerateUnion]
public partial record Coordinate
{
    public static partial Coordinate Point((double X, double Y) point);
}

// Usage:
var coord = Coordinate.Point((10.5, 20.3));
```

**Option 3: Separate cases**

```csharp
// ✅ Separate cases if semantically different
[GenerateUnion]
public partial record Position
{
    public static partial Position TwoD(Point2D point);
    public static partial Position ThreeD(Point3D point);
}
```

### Why This Limitation Exists

Multi-parameter factory methods complicate:
- Pattern matching syntax
- Deconstruction patterns
- JSON serialization
- Code generation complexity

Future versions may support multi-parameter cases with explicit opt-in.

### Workaround Pattern

Create a dedicated case type:

```csharp
// Define case-specific types
public record UserCreated(Guid UserId, string Email, DateTime CreatedAt);
public record UserDeleted(Guid UserId, DateTime DeletedAt);

[GenerateUnion]
public partial record UserEvent
{
    public static partial UserEvent Created(UserCreated data);
    public static partial UserEvent Deleted(UserDeleted data);
}

// Usage:
var evt = UserEvent.Created(new UserCreated(
    UserId: Guid.NewGuid(),
    Email: "user@example.com",
    CreatedAt: DateTime.UtcNow
));

// Pattern matching:
evt.Match(
    created: data => $"User {data.Email} created at {data.CreatedAt}",
    deleted: data => $"User {data.UserId} deleted at {data.DeletedAt}"
);
```

## UG9004: Duplicate Case Signature

### What It Detects

Multiple factory methods with identical signatures, which would cause ambiguity in pattern matching.

### Example Problem

```csharp
[GenerateUnion]
public partial record ApiError
{
    // ❌ UG9004: Both methods have same signature
    public static partial ApiError ValidationError(string message);
    public static partial ApiError ServerError(string message);
}
```

### Diagnostic Message

> Multiple factory methods with the signature 'ApiError(string)' were found on 'ApiError'. Case factory signatures should be unique.

### Why This Is a Problem

Duplicate signatures create ambiguity:

```csharp
var error = ApiError.ValidationError("Invalid input");

// ❌ Ambiguous: Which case is this?
if (error.IsValidationError) // true
if (error.IsServerError)     // also true?!

// Pattern matching becomes impossible
var msg = error.Match(
    validationError: m => ...,
    serverError: m => ...  // Same type - how to distinguish?
);
```

### How to Fix

**Option 1: Use distinct wrapper types**

```csharp
// ✅ Distinct types for each case
public record ValidationError(string Message, string[] Fields);
public record ServerError(string Message, int Code);

[GenerateUnion]
public partial record ApiError
{
    public static partial ApiError Validation(ValidationError error);
    public static partial ApiError Server(ServerError error);
}
```

**Option 2: Use more specific parameters**

```csharp
// ✅ Different parameter types
[GenerateUnion]
public partial record Notification
{
    public static partial Notification Email(EmailMessage email);
    public static partial Notification Sms(SmsMessage sms);
    public static partial Notification Push(PushNotification push);
}
```

**Option 3: Parameterless vs. parameterized**

```csharp
// ✅ Different parameter counts
[GenerateUnion]
public partial record LoadingState
{
    public static partial LoadingState Idle();
    public static partial LoadingState Loading();
    public static partial LoadingState Loaded(Data data);
    public static partial LoadingState Failed(string error);
}
```

### When Signatures Are Considered Duplicate

Signatures match when they have:
- Same number of parameters
- Same parameter types (including generics)
- Same type parameter constraints (for generic unions)

```csharp
// These are NOT duplicates (different param types):
public static partial Result Success(string message);
public static partial Result Success(int count);

// These ARE duplicates (generic unions):
public static partial Result<T> Ok(T value);
public static partial Result<T> Success(T value); // ❌ Duplicate!
```

## 🔧 Configuration

### Change Severity

```ini
# .editorconfig

# Treat as errors
dotnet_diagnostic.UG9002.severity = error
dotnet_diagnostic.UG9003.severity = error
dotnet_diagnostic.UG9004.severity = error

# Disable (not recommended)
dotnet_diagnostic.UG9002.severity = none
```

### Suppress for Legacy Code

```csharp
// Suppress specific diagnostic
#pragma warning disable UG9003
[GenerateUnion]
public partial record LegacyUnion
{
    public static partial LegacyUnion MultiParam(int x, int y);
}
#pragma warning restore UG9003
```

:::caution
These suppressions prevent code generation. The factory method will be ignored.
:::

## 💡 Best Practices

### 1. Design Case Types Thoughtfully

```csharp
// ✅ Good: Semantic wrapper types
public record ValidationFailure(string Message, Dictionary<string, string[]> Errors);
public record NotFoundError(string ResourceType, string ResourceId);
public record UnauthorizedError(string Reason);

[GenerateUnion]
public partial record ApiError
{
    public static partial ApiError Validation(ValidationFailure failure);
    public static partial ApiError NotFound(NotFoundError error);
    public static partial ApiError Unauthorized(UnauthorizedError error);
}

// ❌ Avoid: Generic string parameters
[GenerateUnion]
public partial record ApiError
{
    public static partial ApiError Validation(string message);
    public static partial ApiError NotFound(string message);
    public static partial ApiError Unauthorized(string message);
}
```

### 2. Use Records for Case Data

```csharp
// ✅ Immutable record types
public record OrderPlaced(Guid OrderId, decimal Total, DateTime PlacedAt);
public record OrderShipped(Guid OrderId, string TrackingNumber);
public record OrderDelivered(Guid OrderId, DateTime DeliveredAt);

[GenerateUnion]
public partial record OrderEvent
{
    public static partial OrderEvent Placed(OrderPlaced data);
    public static partial OrderEvent Shipped(OrderShipped data);
    public static partial OrderEvent Delivered(OrderDelivered data);
}
```

### 3. Avoid Overloading Case Names

```csharp
// ❌ Don't do this
[GenerateUnion]
public partial record Result
{
    public static partial Result Success();
    public static partial Result Success(string message);  // UG9004 if same type
    public static partial Result Success(int code);
}

// ✅ Use distinct names
[GenerateUnion]
public partial record Result
{
    public static partial Result SuccessNoData();
    public static partial Result SuccessWithMessage(string message);
    public static partial Result SuccessWithCode(int code);
}
```

### 4. Group Related Cases

```csharp
// ✅ Semantic grouping with shared data
public record NetworkError(string Message, string Endpoint);
public record DatabaseError(string Message, string Query);
public record FileSystemError(string Message, string FilePath);

[GenerateUnion]
public partial record InfrastructureError
{
    public static partial InfrastructureError Network(NetworkError error);
    public static partial InfrastructureError Database(DatabaseError error);
    public static partial InfrastructureError FileSystem(FileSystemError error);
}
```

## 📊 Analyzer Performance

Factory method analyzers run during:
- Initial project load
- After editing union type declarations
- On explicit build

**Performance characteristics**:
- Analyzes only types with `[GenerateUnion]`
- Checks factory methods on single symbol
- Runs concurrently per compilation
- Typical overhead: &lt;10ms per union type

## 🐛 Troubleshooting

### UG9002 but Factory Methods Exist

**Check**:
1. Methods are `static partial`
2. Return type matches the union type exactly
3. Methods are inside the union type declaration
4. Methods are `public` or `internal`

```csharp
// ❌ Won't be detected
[GenerateUnion]
public partial class MyUnion
{
    // Missing 'static'
    public partial MyUnion Case1() => default!;
}

// ✅ Correct
[GenerateUnion]
public partial record MyUnion
{
    public static partial MyUnion Case1();
}
```

### UG9003 for Single Parameter

**Ensure** parameter is not a `params` array:

```csharp
// ❌ Treated as multiple parameters
public static partial MyUnion Case1(params string[] items);

// ✅ Single array parameter
public static partial MyUnion Case1(string[] items);
```

### UG9004 False Positive

**Check** for subtle type differences:

```csharp
// These are duplicates (int and Int32 are same):
public static partial Result Case1(int value);
public static partial Result Case2(Int32 value); // ❌ Duplicate

// These are different:
public static partial Result Case1(int value);
public static partial Result Case2(string value); // ✅ Unique
```

## 🔍 Technical Details

### How Factory Methods Are Discovered

1. Find types with `[GenerateUnion]` attribute
2. Enumerate all members
3. Filter for:
   - Static methods
   - Partial methods
   - Return type matches union type
   - Public or internal visibility
4. Validate parameter count
5. Check for signature collisions
6. Report diagnostics

### Signature Comparison Algorithm

Signatures are compared using:
- Full parameter type names (including namespace)
- Generic arity
- Parameter order
- Does NOT include parameter names

```csharp
// Same signature:
public static partial Result Create(string message);
public static partial Result Build(string value); // Different name, same signature

// Different signatures:
public static partial Result Create(string message);
public static partial Result Create(int count); // Different type
```

## 📚 Related Topics

- [Pattern Matching Analyzers](./pattern-matching) - Exhaustive case handling
- [Union Generation](../core-package/union-generation) - How factories work
- [Best Practices](../core-package/best-practices) - Union design guidelines

## 🎓 Complete Example

```csharp
// Define case-specific data types
public record ProductCreatedData(
    Guid ProductId,
    string Name,
    decimal Price,
    DateTime CreatedAt
);

public record ProductUpdatedData(
    Guid ProductId,
    string? NewName,
    decimal? NewPrice,
    DateTime UpdatedAt
);

public record ProductDeletedData(
    Guid ProductId,
    DateTime DeletedAt,
    string Reason
);

// ✅ Well-designed union with unique, single-parameter factories
[GenerateUnion]
public partial record ProductEvent
{
    public static partial ProductEvent Created(ProductCreatedData data);
    public static partial ProductEvent Updated(ProductUpdatedData data);
    public static partial ProductEvent Deleted(ProductDeletedData data);
}

// Usage example
public class ProductEventHandler
{
    public void Handle(ProductEvent evt)
    {
        // ✅ Type-safe, exhaustive pattern matching
        evt.Match(
            created: data => 
            {
                Console.WriteLine($"Product created: {data.Name} at ${data.Price}");
                SaveToDatabase(data);
            },
            updated: data =>
            {
                Console.WriteLine($"Product {data.ProductId} updated");
                UpdateDatabase(data);
            },
            deleted: data =>
            {
                Console.WriteLine($"Product {data.ProductId} deleted: {data.Reason}");
                MarkAsDeleted(data);
            }
        );
    }
    
    private void SaveToDatabase(ProductCreatedData data) { /* ... */ }
    private void UpdateDatabase(ProductUpdatedData data) { /* ... */ }
    private void MarkAsDeleted(ProductDeletedData data) { /* ... */ }
}
```
