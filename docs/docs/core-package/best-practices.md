---
sidebar_position: 5
---

# Best Practices

Production-ready patterns and guidelines for using UnionGenerator effectively in real-world applications.

## Design Principles

### Make Invalid States Unrepresentable

Use unions to eliminate impossible states:

```csharp
// ❌ Bad: Multiple booleans create invalid states
public class PaymentState
{
    public bool IsPending { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsFailed { get; set; }
    
    // What if IsPending and IsCompleted are both true? 🤔
}

// ✅ Good: Only valid states are possible
[GenerateUnion]
public partial record PaymentState
{
    public static partial PaymentState Pending();
    public static partial PaymentState Completed(string transactionId);
    public static partial PaymentState Failed(string reason);
}
```

### Favor Explicitness Over Brevity

```csharp
// ❌ Generic, unclear names
[GenerateUnion]
public partial record Result
{
    public static partial Result Case1();
    public static partial Result Case2(string data);
}

// ✅ Descriptive, self-documenting names
[GenerateUnion]
public partial record AuthenticationResult
{
    public static partial AuthenticationResult Authenticated(User user, string token);
    public static partial AuthenticationResult InvalidCredentials();
    public static partial AuthenticationResult AccountLocked(DateTime until);
}
```

### Keep Cases Focused

Each case should represent one clear outcome:

```csharp
// ❌ Bad: Overloaded case with flags
[GenerateUnion]
public partial record Result
{
    public static partial Result Success(object data, bool isPartial);
}

// ✅ Good: Separate cases for different outcomes
[GenerateUnion]
public partial record Result
{
    public static partial Result FullSuccess(CompleteData data);
    public static partial Result PartialSuccess(PartialData data, List<string> warnings);
}
```

## Naming Conventions

### Use Domain Language

```csharp
// ✅ E-commerce domain
[GenerateUnion]
public partial record OrderStatus
{
    public static partial OrderStatus Draft();
    public static partial OrderStatus AwaitingPayment();
    public static partial OrderStatus Processing();
    public static partial OrderStatus Shipped(string trackingNumber);
    public static partial OrderStatus Delivered();
    public static partial OrderStatus Cancelled(string reason);
    public static partial OrderStatus Refunded(decimal amount);
}

// ✅ Financial domain
[GenerateUnion]
public partial record TransactionStatus
{
    public static partial TransactionStatus Pending();
    public static partial TransactionStatus Cleared();
    public static partial TransactionStatus Rejected(string rejectionCode);
    public static partial TransactionStatus Reversed();
}
```

### Standard Pattern Names

Use established names for common patterns:

```csharp
// Result pattern
[GenerateUnion]
public partial record Result<T, E>
{
    public static partial Result<T, E> Ok(T value);      // or Success
    public static partial Result<T, E> Error(E error);   // or Failure
}

// Option pattern
[GenerateUnion]
public partial record Option<T>
{
    public static partial Option<T> Some(T value);
    public static partial Option<T> None();
}

// Loading state pattern
[GenerateUnion]
public partial record AsyncData<T>
{
    public static partial AsyncData<T> NotAsked();
    public static partial AsyncData<T> Loading();
    public static partial AsyncData<T> Success(T data);
    public static partial AsyncData<T> Failure(string error);
}
```

## Error Handling

### Typed Errors Over Strings

```csharp
// ❌ String errors lose type information
[GenerateUnion]
public partial record Result<T>
{
    public static partial Result<T> Ok(T value);
    public static partial Result<T> Error(string message);
}

// ✅ Typed errors enable exhaustive handling
public abstract record AppError
{
    public sealed record ValidationError(List<string> Errors) : AppError;
    public sealed record NotFoundError(string ResourceId) : AppError;
    public sealed record UnauthorizedError() : AppError;
    public sealed record ServerError(Exception Exception) : AppError;
}

[GenerateUnion]
public partial record Result<T>
{
    public static partial Result<T> Ok(T value);
    public static partial Result<T> Error(AppError error);
}

// Usage: exhaustive error handling
var response = result.Match(
    ok => Ok(ok.Value),
    error => error.Error switch
    {
        AppError.ValidationError v => BadRequest(v.Errors),
        AppError.NotFoundError n => NotFound(n.ResourceId),
        AppError.UnauthorizedError => Unauthorized(),
        AppError.ServerError s => StatusCode(500, s.Exception.Message),
        _ => StatusCode(500)
    }
);
```

### Error Context

Include relevant context in error cases:

```csharp
[GenerateUnion]
public partial record ValidationResult<T>
{
    public static partial ValidationResult<T> Valid(T value);
    public static partial ValidationResult<T> Invalid(
        Dictionary<string, List<string>> fieldErrors,
        DateTime validatedAt
    );
}

// Usage with full context
var result = ValidationResult<User>.Invalid(
    new Dictionary<string, List<string>>
    {
        ["Email"] = new List<string> { "Invalid format", "Already exists" },
        ["Age"] = new List<string> { "Must be at least 18" }
    },
    DateTime.UtcNow
);
```

## Performance Patterns

### Avoid Allocations in Hot Paths

```csharp
// ✅ Match is allocation-free
public int ProcessResult(Result<int, string> result)
{
    return result.Match(
        ok => ok.Value * 2,
        error => 0
    );
}

// ✅ Switch expression is allocation-free
public int ProcessResult(Result<int, string> result)
{
    return result switch
    {
        Result<int, string>.OkCase ok => ok.Value * 2,
        Result<int, string>.ErrorCase => 0,
        _ => 0
    };
}
```

### Cache Common Instances

```csharp
public static class CommonResults
{
    // Cache common instances
    public static readonly Result<Unit, string> Success = Result<Unit, string>.Ok(Unit.Value);
    public static readonly Result<Unit, string> NotFound = Result<Unit, string>.Error("Not found");
    public static readonly Result<Unit, string> Unauthorized = Result<Unit, string>.Error("Unauthorized");
    
    // Factory for new instances only when needed
    public static Result<T, string> NotFoundFor<T>(string resource) =>
        Result<T, string>.Error($"{resource} not found");
}

// Usage
return CommonResults.NotFound;
```

### Use Structs for High-Frequency Unions

For performance-critical code with simple cases:

```csharp
// Consider a struct-based approach for high-frequency operations
public readonly struct OptionalInt
{
    private readonly bool _hasValue;
    private readonly int _value;
    
    public static OptionalInt Some(int value) => new OptionalInt(true, value);
    public static OptionalInt None() => new OptionalInt(false, 0);
    
    private OptionalInt(bool hasValue, int value)
    {
        _hasValue = hasValue;
        _value = value;
    }
    
    public T Match<T>(Func<int, T> some, Func<T> none) =>
        _hasValue ? some(_value) : none();
}
```

## Testing Strategies

### Test All Cases

```csharp
public class PaymentProcessorTests
{
    [Fact]
    public void ProcessPayment_ValidCard_ReturnsCompleted()
    {
        var result = _processor.ProcessPayment(validCard);
        
        Assert.True(result.IsCompleted);
        Assert.True(result.TryGetCompleted(out var completed));
        Assert.NotEmpty(completed.TransactionId);
    }
    
    [Fact]
    public void ProcessPayment_InvalidCard_ReturnsFailed()
    {
        var result = _processor.ProcessPayment(invalidCard);
        
        var errorMessage = result.Match(
            completed => throw new Exception("Expected failure"),
            pending => throw new Exception("Expected failure"),
            failed => failed.Reason
        );
        
        Assert.Contains("invalid", errorMessage, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public void ProcessPayment_PendingReview_ReturnsPending()
    {
        var result = _processor.ProcessPayment(suspiciousCard);
        
        Assert.True(result.IsPending);
    }
}
```

### Use Match for Assertions

```csharp
[Fact]
public void CreateUser_ValidData_ReturnsSuccess()
{
    var result = _service.CreateUser(validData);
    
    // Assert using Match - ensures exhaustive checking
    result.Match(
        success =>
        {
            Assert.NotEqual(Guid.Empty, success.User.Id);
            Assert.Equal(validData.Email, success.User.Email);
        },
        validationError => Assert.Fail($"Unexpected validation error: {validationError.Message}"),
        serverError => Assert.Fail($"Unexpected server error: {serverError.Exception}")
    );
}
```

## Serialization

### JSON Serialization

Implement custom converters for JSON:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

public class ResultJsonConverter<T, E> : JsonConverter<Result<T, E>>
{
    public override Result<T, E> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        
        var isSuccess = root.GetProperty("isSuccess").GetBoolean();
        
        if (isSuccess)
        {
            var value = JsonSerializer.Deserialize<T>(
                root.GetProperty("value").GetRawText(),
                options
            );
            return Result<T, E>.Ok(value!);
        }
        else
        {
            var error = JsonSerializer.Deserialize<E>(
                root.GetProperty("error").GetRawText(),
                options
            );
            return Result<T, E>.Error(error!);
        }
    }
    
    public override void Write(
        Utf8JsonWriter writer,
        Result<T, E> value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        
        value.Match(
            ok =>
            {
                writer.WriteBoolean("isSuccess", true);
                writer.WritePropertyName("value");
                JsonSerializer.Serialize(writer, ok.Value, options);
            },
            error =>
            {
                writer.WriteBoolean("isSuccess", false);
                writer.WritePropertyName("error");
                JsonSerializer.Serialize(writer, error.Error, options);
            }
        );
        
        writer.WriteEndObject();
    }
}

// Usage
var options = new JsonSerializerOptions();
options.Converters.Add(new ResultJsonConverter<User, string>());
```

## Async Patterns

### Async Result Operations

```csharp
public static class ResultAsyncExtensions
{
    public static async Task<Result<T, E>> BindAsync<T, E>(
        this Result<T, E> result,
        Func<T, Task<Result<T, E>>> binder)
    {
        return await result.Match(
            ok => binder(ok.Value),
            error => Task.FromResult(Result<T, E>.Error(error.Error))
        );
    }
    
    public static async Task<Result<TNew, E>> MapAsync<T, TNew, E>(
        this Result<T, E> result,
        Func<T, Task<TNew>> mapper)
    {
        return await result.Match(
            ok => mapper(ok.Value).ContinueWith(t => Result<TNew, E>.Ok(t.Result)),
            error => Task.FromResult(Result<TNew, E>.Error(error.Error))
        );
    }
}

// Usage: Chain async operations
var result = await GetUser(userId)
    .MapAsync(user => EnrichUserDataAsync(user))
    .BindAsync(user => ValidateUserAsync(user));
```

### Parallel Processing

```csharp
public async Task<List<Result<T, E>>> ProcessInParallel<T, E>(
    List<Task<Result<T, E>>> tasks)
{
    var results = await Task.WhenAll(tasks);
    return results.ToList();
}

// Usage
var tasks = userIds.Select(id => FetchUserAsync(id)).ToList();
var results = await ProcessInParallel(tasks);

var (successful, failed) = results.Partition(r => r.IsOk);
```

## Domain-Driven Design

### Value Objects

```csharp
[GenerateUnion]
public partial record Email
{
    public static partial Email Valid(string address);
    public static partial Email Invalid(string input, string reason);
    
    public static Email Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Invalid(input, "Email cannot be empty");
            
        if (!input.Contains("@"))
            return Invalid(input, "Email must contain @");
            
        return Valid(input);
    }
}

// Usage with validation
var email = Email.Create(userInput);

email.Match(
    valid => _repository.Save(new User { Email = valid.Address }),
    invalid => throw new ValidationException(invalid.Reason)
);
```

### Aggregate State

```csharp
[GenerateUnion]
public partial record OrderState
{
    public static partial OrderState Draft(List<OrderItem> items);
    public static partial OrderState Submitted(Guid orderId, DateTime at);
    public static partial OrderState Paid(Guid orderId, string paymentId);
    public static partial OrderState Shipped(Guid orderId, string tracking);
    public static partial OrderState Delivered(Guid orderId, DateTime at);
    
    // State machine enforcement
    public Result<OrderState, string> Submit(Guid orderId)
    {
        return this.Match(
            draft => Result<OrderState, string>.Ok(Submitted(orderId, DateTime.UtcNow)),
            submitted => Result<OrderState, string>.Error("Already submitted"),
            paid => Result<OrderState, string>.Error("Already paid"),
            shipped => Result<OrderState, string>.Error("Already shipped"),
            delivered => Result<OrderState, string>.Error("Already delivered")
        );
    }
}
```

## Documentation

### Document Cases

```csharp
/// <summary>
/// Represents the result of a user authentication attempt.
/// </summary>
[GenerateUnion]
public partial record AuthResult
{
    /// <summary>
    /// User successfully authenticated with valid credentials.
    /// </summary>
    /// <param name="user">The authenticated user</param>
    /// <param name="token">JWT authentication token</param>
    public static partial AuthResult Authenticated(User user, string token);
    
    /// <summary>
    /// Authentication failed due to invalid username or password.
    /// </summary>
    public static partial AuthResult InvalidCredentials();
    
    /// <summary>
    /// Account is temporarily locked due to too many failed attempts.
    /// </summary>
    /// <param name="unlockAt">Time when account will be automatically unlocked</param>
    public static partial AuthResult AccountLocked(DateTime unlockAt);
}
```

## Common Pitfalls

### Don't Mix Concerns

```csharp
// ❌ Bad: Mixing business logic with errors
[GenerateUnion]
public partial record Result
{
    public static partial Result Success();
    public static partial Result BusinessError(string message);
    public static partial Result TechnicalError(Exception ex);
    public static partial Result NetworkError(int statusCode);
}

// ✅ Good: Clear separation
public abstract record Error
{
    public record BusinessError(string Message) : Error;
    public record TechnicalError(Exception Exception) : Error;
}

[GenerateUnion]
public partial record Result<T>
{
    public static partial Result<T> Success(T value);
    public static partial Result<T> Failure(Error error);
}
```

### Don't Overuse Unions

```csharp
// ❌ Bad: Union for simple boolean
[GenerateUnion]
public partial record IsActive
{
    public static partial IsActive Yes();
    public static partial IsActive No();
}

// ✅ Good: Use boolean
public bool IsActive { get; set; }

// ✅ Union appropriate when state is complex
[GenerateUnion]
public partial record UserStatus
{
    public static partial UserStatus Active();
    public static partial UserStatus Inactive(DateTime since, string reason);
    public static partial UserStatus Banned(DateTime until, string reason);
}
```

## Key Takeaways

✅ **Make impossible states impossible** with unions  
✅ **Use descriptive names** from your domain  
✅ **Include relevant context** in error cases  
✅ **Test all cases** exhaustively  
✅ **Leverage Match** for compile-time safety  
✅ **Document your unions** for team clarity  
✅ **Avoid premature optimization** - profile first  
✅ **Keep cases focused** on single outcomes
