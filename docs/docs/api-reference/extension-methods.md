---
sidebar_position: 4
---

# Extension Methods Reference

Complete reference for all extension methods provided by UnionGenerator to enhance union type usage.

## 📦 Available Extensions

UnionGenerator provides two sets of extension methods:

1. **MatchVoid Extensions** - Simplified matching for void/Unit result types
2. **ResultComposition Extensions** - Monadic operations (Bind, Map, MapError)

## 🔧 MatchVoid Extensions

Simplifies pattern matching when you want to perform side effects without returning a value.

### Namespace

```csharp
using UnionGenerator.Extensions;
```

### Why MatchVoid?

Standard `Match` requires returning a value from all handlers. When performing side effects (logging, I/O, mutations), this is verbose:

```csharp
// ❌ Verbose - Need to return dummy values
result.Match(
    ok: _ => { LogSuccess(); return Unit.Value; },
    error: err => { LogError(err); return Unit.Value; }
);

// ✅ Concise - MatchVoid handles it
result.MatchVoid(
    ok: () => LogSuccess(),
    error: err => LogError(err)
);
```

---

## MatchVoid (Required Handlers)

Matches against all cases with action delegates. Both handlers are required.

### Signature

```csharp
public static void MatchVoid<TError>(
    this dynamic result,
    Action ok,
    Action<TError> error)
```

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `result` | `dynamic` | The result to match. Must have `IsSuccess`/`IsOk` property and `Error`/`ErrorValue` property |
| `ok` | `Action` | Action to execute if result is successful (no parameters) |
| `error` | `Action<TError>` | Action to execute if result contains error, receiving the error value |

### Returns

`void` - Executes the appropriate action based on the result state

### Exceptions

- `ArgumentNullException` - If `result`, `ok`, or `error` is null
- `InvalidOperationException` - If result type doesn't have recognizable success/error properties

### Example Usage

```csharp
using UnionGenerator.Extensions;

[GenerateUnion]
public partial class Result<T, TError>
{
    public static partial Result<T, TError> Ok(T value);
    public static partial Result<T, TError> Error(TError error);
}

// Using MatchVoid
Result<Unit, ValidationError> validationResult = ValidateUser(user);

validationResult.MatchVoid(
    ok: () => Console.WriteLine("Validation passed"),
    error: err => Console.WriteLine($"Validation failed: {err.Message}")
);
```

### Real-World Example

```csharp
public async Task ProcessPayment(PaymentRequest request)
{
    Result<Unit, PaymentError> result = await _paymentService.ProcessAsync(request);
    
    result.MatchVoid(
        ok: () =>
        {
            _logger.LogInformation("Payment processed successfully for request {RequestId}", request.Id);
            _metrics.IncrementCounter("payments.success");
        },
        error: err =>
        {
            _logger.LogError("Payment failed: {Error}", err);
            _metrics.IncrementCounter("payments.failed");
            _notificationService.NotifyFailure(err);
        }
    );
}
```

### Characteristics

- **Performance**: O(1) - Single property check and action invocation
- **Thread Safety**: Thread-safe if actions are thread-safe
- **Side Effects**: Designed for side effects (logging, I/O, mutations)
- **Allocations**: No allocations beyond action closures

---

## MatchVoid (Optional Handlers)

Matches with optional handlers. Allows handling only the cases you care about.

### Signature

```csharp
public static void MatchVoid<TError>(
    this dynamic result,
    Action? ok,
    Action<TError>? error)
```

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `result` | `dynamic` | The result to match |
| `ok` | `Action?` | Optional action for success case. If null, success is ignored |
| `error` | `Action<TError>?` | Optional action for error case. If null, error is ignored |

### Returns

`void` - Executes the appropriate action if provided

### Example Usage

```csharp
// Handle only errors
result.MatchVoid<ValidationError>(
    ok: null,
    error: err => Console.WriteLine($"Error: {err.Message}")
);

// Handle only success
result.MatchVoid<ValidationError>(
    ok: () => Console.WriteLine("Success!"),
    error: null
);

// No-op if neither provided
result.MatchVoid<ValidationError>(ok: null, error: null);
```

### Use Cases

✅ **Error Logging Only**
```csharp
public void LogErrors(Result<Unit, Error> result)
{
    result.MatchVoid<Error>(
        ok: null, // Don't care about success
        error: err => _logger.LogError("Operation failed: {Error}", err)
    );
}
```

✅ **Success Notifications Only**
```csharp
public void NotifySuccess(Result<Unit, Error> result)
{
    result.MatchVoid<Error>(
        ok: () => _notifications.Send("Operation completed!"),
        error: null // Don't care about errors
    );
}
```

---

## 🔗 ResultComposition Extensions

Monadic operations for composing and transforming Result types. Enables functional-style chaining of operations.

### Namespace

```csharp
using UnionGenerator.Extensions;
```

### Why Monadic Composition?

Allows chaining operations that can fail without nested error handling:

```csharp
// ❌ Without composition - nested and verbose
var userResult = GetUser(id);
if (userResult.IsOk)
{
    var validationResult = ValidateUser(userResult.Value);
    if (validationResult.IsOk)
    {
        var saveResult = SaveUser(validationResult.Value);
        return saveResult;
    }
    return validationResult;
}
return userResult;

// ✅ With composition - flat and readable
return GetUser(id)
    .Bind(user => ValidateUser(user))
    .Bind(user => SaveUser(user));
```

---

## Bind

Chains operations that return Results. Short-circuits on first error.

### Signature

```csharp
public static dynamic Bind<TSuccess, TSuccess2, TError>(
    this dynamic result,
    Func<TSuccess, dynamic> binder)
```

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `result` | `dynamic` | Source result to bind from |
| `binder` | `Func<TSuccess, dynamic>` | Function that takes success value and returns a new Result |

### Returns

- If source is **success**: Result returned by `binder`
- If source is **error**: Source error (propagated, binder not called)

### Exceptions

- `ArgumentNullException` - If `result` or `binder` is null
- `InvalidOperationException` - If result type doesn't have recognizable properties

### Example Usage

```csharp
[GenerateUnion]
public partial class Result<T, TError>
{
    public static partial Result<T, TError> Ok(T value);
    public static partial Result<T, TError> Error(TError error);
}

// Chain multiple operations
Result<User, ValidationError> GetUser(int id) { /* ... */ }
Result<User, ValidationError> ValidateUser(User user) { /* ... */ }
Result<User, ValidationError> SaveUser(User user) { /* ... */ }

var result = GetUser(userId)
    .Bind(user => ValidateUser(user))
    .Bind(user => SaveUser(user));
```

### Real-World Example

```csharp
public Result<OrderConfirmation, OrderError> ProcessOrder(OrderRequest request)
{
    return ValidateOrder(request)
        .Bind(order => CheckInventory(order))
        .Bind(order => ProcessPayment(order))
        .Bind(order => CreateShipment(order))
        .Bind(order => SendConfirmation(order));
    
    // Stops at first error - remaining steps not executed
}
```

### Monadic Laws

`Bind` satisfies the monad laws:

**Left Identity:**
```csharp
Result<T, E>.Ok(x).Bind(f) == f(x)
```

**Right Identity:**
```csharp
result.Bind(x => Result<T, E>.Ok(x)) == result
```

**Associativity:**
```csharp
result.Bind(f).Bind(g) == result.Bind(x => f(x).Bind(g))
```

### Characteristics

- **Performance**: O(1) for error case (returns immediately), O(n) for success (cost of binder)
- **Short-Circuiting**: Stops at first error, remaining operations not executed
- **Thread Safety**: Thread-safe if binder is thread-safe
- **Allocations**: No allocations except binder's return value

---

## Map

Transforms the success value without changing the Result structure.

### Signature

```csharp
public static dynamic Map<TSuccess, TSuccess2, TError>(
    this dynamic result,
    Func<TSuccess, TSuccess2> mapper)
```

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `result` | `dynamic` | Source result |
| `mapper` | `Func<TSuccess, TSuccess2>` | Function to transform success value |

### Returns

- If source is **success**: New result with mapped success value
- If source is **error**: Source error unchanged

### Example Usage

```csharp
Result<int, string> GetNumber() => Result<int, string>.Ok(10);

Result<int, string> doubled = GetNumber()
    .Map(x => x * 2);

Result<string, string> formatted = GetNumber()
    .Map(x => $"Value: {x}");

// Chain multiple maps
Result<int, string> transformed = GetNumber()
    .Map(x => x * 2)
    .Map(x => x + 10)
    .Map(x => x / 2);
```

### Real-World Example

```csharp
public Result<UserDto, DatabaseError> GetUserById(int id)
{
    return _repository.FindUser(id)
        .Map(user => new UserDto
        {
            Id = user.Id,
            Name = user.FullName,
            Email = user.EmailAddress
        });
}
```

### Characteristics

- **Pure Transformation**: Does not perform I/O or side effects
- **Performance**: O(n) where n is cost of mapper function
- **Error Preservation**: Errors pass through unchanged
- **Type Safety**: Return type changes based on mapper

---

## MapError

Transforms the error value without changing the success value.

### Signature

```csharp
public static dynamic MapError<TSuccess, TError, TError2>(
    this dynamic result,
    Func<TError, TError2> mapper)
```

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `result` | `dynamic` | Source result |
| `mapper` | `Func<TError, TError2>` | Function to transform error value |

### Returns

- If source is **success**: Source success unchanged
- If source is **error**: New result with mapped error value

### Example Usage

```csharp
public Result<User, ApiError> GetUser(int id)
{
    Result<User, DatabaseError> dbResult = _repository.FindUser(id);
    
    // Convert database errors to API errors
    return dbResult.MapError(dbErr => new ApiError
    {
        Code = "DATABASE_ERROR",
        Message = dbErr.Message,
        StatusCode = 500
    });
}
```

### Real-World Example

```csharp
public async Task<Result<User, ProblemDetails>> GetUserForApi(int id)
{
    Result<User, DomainError> domainResult = await _userService.GetUserAsync(id);
    
    // Convert domain errors to HTTP problem details
    return domainResult.MapError(err => err switch
    {
        NotFoundError nf => new ProblemDetails
        {
            Status = 404,
            Title = "User Not Found",
            Detail = nf.Message
        },
        ValidationError ve => new ProblemDetails
        {
            Status = 400,
            Title = "Validation Failed",
            Detail = ve.Message
        },
        _ => new ProblemDetails
        {
            Status = 500,
            Title = "Internal Server Error",
            Detail = "An unexpected error occurred"
        }
    });
}
```

### Use Cases

✅ **Layer Translation**
```csharp
// Domain → API
domainResult.MapError(err => err.ToApiError());

// Database → Domain
dbResult.MapError(err => err.ToDomainError());
```

✅ **Error Enrichment**
```csharp
result.MapError(err => new EnrichedError
{
    Original = err,
    Timestamp = DateTime.UtcNow,
    UserId = _currentUser.Id
});
```

✅ **Localization**
```csharp
result.MapError(err => new LocalizedError
{
    Code = err.Code,
    Message = _localizer[err.MessageKey]
});
```

### Characteristics

- **Success Preservation**: Success values pass through unchanged
- **Performance**: O(n) where n is cost of mapper function
- **Type Conversion**: Allows changing error type in result chain

---

## 🔗 Combining Extensions

These extensions work together for powerful compositions:

### Example: Full Pipeline

```csharp
public async Task<Result<OrderConfirmation, ApiError>> ProcessOrderRequest(OrderRequest request)
{
    return ValidateOrderRequest(request)
        .Bind(req => CheckInventory(req))
        .Bind(req => CalculatePricing(req))
        .Map(priced => ApplyDiscounts(priced))
        .Bind(order => ProcessPayment(order))
        .Bind(paid => CreateShipment(paid))
        .Map(shipped => new OrderConfirmation(shipped))
        .MapError(err => ConvertToApiError(err))
        .MatchVoid<ApiError>(
            ok: () => _logger.LogInformation("Order processed successfully"),
            error: err => _logger.LogError("Order processing failed: {Error}", err)
        );
}
```

### Example: Validation Pipeline

```csharp
public Result<User, ValidationError> ValidateAndCreate(UserCreateRequest request)
{
    return ValidateEmail(request.Email)
        .Bind(_ => ValidatePassword(request.Password))
        .Bind(_ => ValidateUsername(request.Username))
        .Bind(_ => CheckUsernameTaken(request.Username))
        .Map(_ => new User
        {
            Email = request.Email,
            Username = request.Username,
            PasswordHash = HashPassword(request.Password)
        });
}
```

## ⚡ Performance Considerations

| Operation | Allocations | Complexity | Notes |
|-----------|-------------|-----------|-------|
| `MatchVoid` | Closure only | O(1) | Very lightweight |
| `Bind` | Result object | O(1) + binder cost | Short-circuits on error |
| `Map` | Result object | O(1) + mapper cost | Pure transformation |
| `MapError` | Result object | O(1) + mapper cost | Errors only |

### Performance Tips

1. **Prefer Bind chains** - More efficient than nested pattern matching
2. **Avoid allocations in mappers** - Keep mapper functions lightweight
3. **Short-circuit early** - Put cheap validations first
4. **Use MatchVoid for side effects** - Don't return dummy values

## 🔒 Thread Safety

All extension methods are thread-safe with these guarantees:

- ✅ Methods don't modify shared state
- ✅ Safe to call from multiple threads
- ⚠️ Thread safety of delegates is caller's responsibility

## 🧩 Integration with LINQ

These extensions enable LINQ query syntax:

```csharp
// Query expression syntax (if SelectMany is implemented)
var result = from user in GetUser(id)
             from validated in ValidateUser(user)
             from saved in SaveUser(validated)
             select saved;

// Equivalent to:
var result = GetUser(id)
    .Bind(user => ValidateUser(user))
    .Bind(user => SaveUser(user));
```

## 🚀 Next Steps

- Review [Pattern Matching](../core-package/pattern-matching.md) for more matching patterns
- See [Common Patterns](../getting-started/common-patterns.md) for real-world examples
- Check [Best Practices](../core-package/best-practices.md) for production guidance

## 📚 Additional Resources

- [Generated API Reference](./generated-api.md) - Complete API documentation
- [Attributes Reference](./attributes.md) - Configuration options
- [Result Pattern Guide](../getting-started/common-patterns) - Result type patterns
