---
sidebar_position: 1
---

# OneOf Compatibility

The UnionGenerator.OneOfCompat package provides a compatibility layer for migrating from the OneOf library to UnionGenerator, enabling gradual migration with minimal code changes.

## Overview

This package helps you:

- Migrate from OneOf incrementally
- Use OneOf-compatible API
- Maintain backward compatibility during transition
- Minimize breaking changes

## Installation

```bash
dotnet add package UnionGenerator.OneOfCompat
```

**Requirements:**
- .NET 6.0 or later
- UnionGenerator package
- (Optional) OneOf package during migration

## Migration Strategy

### Phase 1: Side-by-Side

Keep both libraries during initial migration:

```csharp
// Old code with OneOf
using OneOf;

public OneOf<User, NotFound, ValidationError> GetUser(int id)
{
    // existing implementation
}

// New code with UnionGenerator
using UnionGenerator;

[GenerateUnion]
public partial record UserResult
{
    public static partial UserResult User(User user);
    public static partial UserResult NotFound();
    public static partial UserResult ValidationError(string message);
}

public UserResult GetUserV2(int id)
{
    // new implementation
}
```

### Phase 2: Adapter Pattern

Create adapters to convert between OneOf and UnionGenerator:

```csharp
public static class OneOfAdapters
{
    public static UserResult ToUnionResult(this OneOf<User, NotFound, ValidationError> oneOf)
    {
        return oneOf.Match(
            user => UserResult.User(user),
            notFound => UserResult.NotFound(),
            validationError => UserResult.ValidationError(validationError.Message)
        );
    }

    public static OneOf<User, NotFound, ValidationError> ToOneOf(this UserResult result)
    {
        return result.Match<OneOf<User, NotFound, ValidationError>>(
            user => user.User,
            notFound => new NotFound(),
            validationError => new ValidationError(validationError.Message)
        );
    }
}

// Usage during migration
public UserResult GetUser(int id)
{
    var oneOfResult = _legacyService.GetUserOld(id);
    return oneOfResult.ToUnionResult();
}
```

### Phase 3: Full Migration

Replace OneOf completely:

```csharp
// Before: OneOf
public OneOf<User, NotFound> GetUser(int id)
{
    return user != null 
        ? OneOf<User, NotFound>.FromT0(user)
        : OneOf<User, NotFound>.FromT1(new NotFound());
}

// After: UnionGenerator
[GenerateUnion]
public partial record UserResult
{
    public static partial UserResult Success(User user);
    public static partial UserResult NotFound();
}

public UserResult GetUser(int id)
{
    return user != null 
        ? UserResult.Success(user)
        : UserResult.NotFound();
}
```

## API Comparison

### Creating Instances

```csharp
// OneOf
var result = OneOf<string, int>.FromT0("success");
var result = OneOf<string, int>.FromT1(404);

// UnionGenerator
[GenerateUnion]
public partial record Result
{
    public static partial Result Success(string value);
    public static partial Result Error(int code);
}

var result = Result.Success("success");
var result = Result.Error(404);
```

### Pattern Matching

```csharp
// OneOf
var message = result.Match(
    str => $"String: {str}",
    num => $"Number: {num}"
);

// UnionGenerator (identical syntax!)
var message = result.Match(
    success => $"String: {success.Value}",
    error => $"Number: {error.Code}"
);
```

### Type Checking

```csharp
// OneOf
if (result.IsT0)
{
    var str = result.AsT0;
}

// UnionGenerator
if (result.IsSuccess)
{
    // Access via pattern matching or TryGet
    if (result.TryGetSuccess(out var success))
    {
        var str = success.Value;
    }
}
```

### Switch Expression

```csharp
// OneOf
var output = result.Value switch
{
    string s => $"String: {s}",
    int i => $"Number: {i}",
    _ => "Unknown"
};

// UnionGenerator
var output = result switch
{
    Result.SuccessCase s => $"String: {s.Value}",
    Result.ErrorCase e => $"Number: {e.Code}",
    _ => "Unknown"
};
```

## Migration Examples

### Simple Result Type

```csharp
// Before: OneOf
using OneOf;

public OneOf<Success, Error> ProcessRequest()
{
    try
    {
        var data = DoWork();
        return new Success(data);
    }
    catch (Exception ex)
    {
        return new Error(ex.Message);
    }
}

public record Success(string Data);
public record Error(string Message);

// After: UnionGenerator
using UnionGenerator;

[GenerateUnion]
public partial record ProcessResult
{
    public static partial ProcessResult Success(string data);
    public static partial ProcessResult Error(string message);
}

public ProcessResult ProcessRequest()
{
    try
    {
        var data = DoWork();
        return ProcessResult.Success(data);
    }
    catch (Exception ex)
    {
        return ProcessResult.Error(ex.Message);
    }
}
```

### HTTP Response Type

```csharp
// Before: OneOf
public OneOf<User, NotFound, BadRequest> GetUser(int id)
{
    if (id <= 0)
        return new BadRequest("Invalid ID");

    var user = _repository.Find(id);
    
    return user != null
        ? OneOf<User, NotFound, BadRequest>.FromT0(user)
        : OneOf<User, NotFound, BadRequest>.FromT1(new NotFound());
}

// After: UnionGenerator
[GenerateUnion]
public partial record UserResult
{
    public static partial UserResult Success(User user);
    public static partial UserResult NotFound();
    public static partial UserResult BadRequest(string message);
}

public UserResult GetUser(int id)
{
    if (id <= 0)
        return UserResult.BadRequest("Invalid ID");

    var user = _repository.Find(id);
    
    return user != null
        ? UserResult.Success(user)
        : UserResult.NotFound();
}
```

### Async Operations

```csharp
// Before: OneOf
public async Task<OneOf<User, Error>> GetUserAsync(int id)
{
    try
    {
        var user = await _repository.FindAsync(id);
        return user;
    }
    catch (Exception ex)
    {
        return new Error(ex.Message);
    }
}

// After: UnionGenerator
[GenerateUnion]
public partial record UserResult
{
    public static partial UserResult Success(User user);
    public static partial UserResult Error(string message);
}

public async Task<UserResult> GetUserAsync(int id)
{
    try
    {
        var user = await _repository.FindAsync(id);
        return UserResult.Success(user);
    }
    catch (Exception ex)
    {
        return UserResult.Error(ex.Message);
    }
}
```

## Key Differences

### Naming

| Feature | OneOf | UnionGenerator |
|---------|-------|----------------|
| Type Creation | `OneOf<T0, T1>` | `[GenerateUnion]` attribute |
| Creating Instance | `FromT0(value)` | Factory method |
| Type Checking | `IsT0` | `IsSuccess` (based on case name) |
| Value Access | `AsT0` | Pattern matching or `TryGet` |
| Switch Case | Match on value type | Match on case class |

### Advantages of UnionGenerator

```csharp
// ✅ Descriptive case names (not T0, T1, T2)
[GenerateUnion]
public partial record Result
{
    public static partial Result Success(User user);
    public static partial Result NotFound();
    public static partial Result ValidationError(string message);
}

// vs. OneOf
OneOf<User, NotFound, ValidationError>  // Which is T0? T1? T2?

// ✅ Case data is strongly typed with property names
result.Match(
    success => success.User.Name,  // Property is named 'User'
    notFound => "Not found",
    validationError => validationError.Message  // Property is named 'Message'
);

// vs. OneOf
result.Match(
    user => user.Name,  // Just the raw type
    notFound => "Not found",
    error => error.Message
);

// ✅ Better IDE support
if (result.TryGetSuccess(out var success))  // IntelliSense shows 'TryGetSuccess'
{
    Console.WriteLine(success.User.Name);  // IntelliSense shows 'User' property
}
```

## Migration Checklist

### Code Changes

- [ ] Replace `OneOf<T0, T1, ...>` with `[GenerateUnion]` types
- [ ] Replace `FromT0(value)` with factory methods
- [ ] Replace `IsT0` with `IsSuccess` (or appropriate case name)
- [ ] Replace `AsT0` with pattern matching or `TryGet`
- [ ] Update switch expressions to use case classes
- [ ] Update tests to use new API

### Testing

- [ ] Run all unit tests
- [ ] Run integration tests
- [ ] Verify serialization/deserialization
- [ ] Check API contracts
- [ ] Validate error handling

### Documentation

- [ ] Update API documentation
- [ ] Update code examples
- [ ] Update migration notes
- [ ] Update changelog

## Compatibility Helpers

Create helpers for gradual migration:

```csharp
public static class MigrationHelpers
{
    // Convert OneOf to UnionGenerator result
    public static Result<T, E> ToResult<T, E>(this OneOf<T, E> oneOf)
        where T : class
        where E : class
    {
        return oneOf.Match(
            success => Result<T, E>.Ok(success),
            error => Result<T, E>.Error(error)
        );
    }

    // Convert UnionGenerator result to OneOf
    public static OneOf<T, E> ToOneOf<T, E>(this Result<T, E> result)
        where T : class
        where E : class
    {
        return result.Match(
            ok => OneOf<T, E>.FromT0(ok.Value),
            error => OneOf<T, E>.FromT1(error.Error)
        );
    }
}
```

## Best Practices

### Migrate Module by Module

```csharp
// ✅ Migrate one bounded context at a time
namespace Orders
{
    // All new code uses UnionGenerator
    [GenerateUnion]
    public partial record OrderResult { }
}

namespace Payments
{
    // Still using OneOf
    public OneOf<Payment, Error> ProcessPayment() { }
}
```

### Use Adapters at Boundaries

```csharp
// ✅ Convert at module boundaries
public class OrderService
{
    private readonly IPaymentService _paymentService;  // Uses OneOf

    public OrderResult PlaceOrder(Order order)
    {
        var paymentResult = _paymentService.ProcessPayment(order.Payment);
        
        // Convert at boundary
        var convertedResult = paymentResult.ToUnionResult();
        
        return convertedResult.Match(
            success => OrderResult.Success(order),
            error => OrderResult.PaymentFailed(error.Message)
        );
    }
}
```

### Update Tests Incrementally

```csharp
// ✅ Update test assertions as you migrate
[Fact]
public void GetUser_ValidId_ReturnsSuccess()
{
    var result = _service.GetUser(validId);
    
    // New assertion style
    Assert.True(result.IsSuccess);
    result.Match(
        success => Assert.Equal(expectedUser, success.User),
        notFound => Assert.Fail("Expected success"),
        error => Assert.Fail("Expected success")
    );
}
```

## Key Takeaways

✅ **Incremental migration** reduces risk  
✅ **Adapter pattern** bridges old and new code  
✅ **Descriptive names** improve code clarity  
✅ **Type safety** maintained throughout migration  
✅ **Better IDE support** with generated code
