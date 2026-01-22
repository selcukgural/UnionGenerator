---
sidebar_position: 2
---

# Why Discriminated Unions?

Discriminated unions (also called **tagged unions** or **sum types**) are one of the most powerful tools in modern type systems. They allow you to model data that can be **one of several variants**, each with its own shape and data.

## The Core Idea

A discriminated union says: **"This value is exactly one of these types, and you must handle all possibilities."**

```csharp
// Instead of this mess:
public class Response
{
    public string? SuccessData { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsSuccess { get; set; }
}

// You get this clarity:
[GenerateUnion]
public partial class Response
{
    public static Response Success(string data) => new SuccessCase(data);
    public static Response Error(string message) => new ErrorCase(message);
}
```

## Benefits Over Traditional Approaches

### 1. **Impossible States Are Impossible**

Traditional C# allows you to create invalid states:

```csharp
// ❌ Nothing prevents this nonsense:
var response = new Response 
{ 
    IsSuccess = true,
    SuccessData = "Hello",
    ErrorMessage = "Also an error??" // Wait, what?
};
```

With discriminated unions, **the type system prevents invalid states**:

```csharp
// ✅ Can only be one thing at a time:
var response = Response.Success("Hello");
// The compiler guarantees no error message exists
```

### 2. **Exhaustive Pattern Matching**

The compiler forces you to handle every case:

```csharp
var response = await CallApiAsync();

// ✅ Compiler error if you forget a case
var result = response.Match(
    success: data => ProcessData(data),
    error: msg => LogError(msg)
);

// With traditional approaches, easy to forget:
if (response.IsSuccess) // ❌ Forgot to handle failure!
{
    ProcessData(response.SuccessData!); // Null reference warning!
}
```

### 3. **Self-Documenting Code**

Union types communicate intent clearly:

```csharp
// Traditional - what states exist?
public class PaymentStatus
{
    public bool IsPending { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsFailed { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
}

// Union - states are crystal clear
[GenerateUnion]
public partial class PaymentStatus
{
    public static PaymentStatus Pending() => new PendingCase();
    public static PaymentStatus Completed(DateTime completedAt) => new CompletedCase(completedAt);
    public static PaymentStatus Failed(string reason) => new FailedCase(reason);
}
```

### 4. **No Exceptions as Control Flow**

Exceptions are expensive and hide control flow:

```csharp
// ❌ Exception-based error handling
public User GetUser(int id)
{
    var user = _db.Users.Find(id);
    if (user == null)
        throw new NotFoundException($"User {id} not found");
    return user;
}

// Caller has no idea this throws!
var user = GetUser(123); // 💥 Hidden failure path

// ✅ Union-based error handling
public Result<User, NotFoundError> GetUser(int id)
{
    var user = _db.Users.Find(id);
    return user != null 
        ? Result.Ok(user)
        : Result.Error(new NotFoundError(id));
}

// Explicit, type-safe, and visible
var result = GetUser(123);
return result.Match(
    ok: user => Ok(user),
    error: err => NotFound(err.Message)
);
```

## Real-World Examples

### API Response Handling

```csharp
[GenerateUnion]
public partial class ApiResult<T>
{
    public static ApiResult<T> Success(T data) => new SuccessCase(data);
    public static ApiResult<T> ValidationError(ValidationErrors errors) => new ValidationErrorCase(errors);
    public static ApiResult<T> NotFound() => new NotFoundCase();
    public static ApiResult<T> Unauthorized() => new UnauthorizedCase();
    public static ApiResult<T> ServerError(string message) => new ServerErrorCase(message);
}

// Usage in ASP.NET Core
public IActionResult CreateUser(CreateUserRequest request)
{
    var result = _service.CreateUser(request);
    
    return result.Match(
        success: user => Created($"/users/{user.Id}", user),
        validationError: errors => BadRequest(errors),
        notFound: () => NotFound(),
        unauthorized: () => Unauthorized(),
        serverError: msg => StatusCode(500, msg)
    );
}
```

### State Machine

```csharp
[GenerateUnion]
public partial class OrderState
{
    public static OrderState Created(Order order) => new CreatedCase(order);
    public static OrderState PaymentPending(Order order, PaymentId paymentId) => new PaymentPendingCase(order, paymentId);
    public static OrderState Confirmed(Order order, DateTime confirmedAt) => new ConfirmedCase(order, confirmedAt);
    public static OrderState Shipped(Order order, TrackingNumber tracking) => new ShippedCase(order, tracking);
    public static OrderState Delivered(Order order, DateTime deliveredAt) => new DeliveredCase(order, deliveredAt);
    public static OrderState Cancelled(Order order, string reason) => new CancelledCase(order, reason);
}

// Each state carries exactly the data it needs
```

### Option Type (Better Than Nullable)

```csharp
[GenerateUnion]
public partial class Option<T>
{
    public static Option<T> Some(T value) => new SomeCase(value);
    public static Option<T> None() => new NoneCase();
}

// No more null reference exceptions
public Option<User> FindUser(string email)
{
    var user = _db.Users.FirstOrDefault(u => u.Email == email);
    return user != null ? Option.Some(user) : Option.None<User>();
}

// Force explicit handling
var maybeUser = FindUser("test@example.com");
var name = maybeUser.Match(
    some: user => user.Name,
    none: () => "Unknown"
);
```

## Comparison with Other Languages

Discriminated unions are first-class citizens in many languages:

### F# (Native Support)

```fsharp
type Result<'T, 'E> =
    | Ok of 'T
    | Error of 'E
```

### Rust (enum)

```rust
enum Result<T, E> {
    Ok(T),
    Err(E),
}
```

### TypeScript (Union Types)

```typescript
type Result<T, E> = 
    | { kind: 'ok', value: T }
    | { kind: 'error', error: E };
```

### C# (with UnionGenerator)

```csharp
[GenerateUnion]
public partial class Result<T, E>
{
    public static Result<T, E> Ok(T value) => new OkCase(value);
    public static Result<T, E> Error(E error) => new ErrorCase(error);
}
```

UnionGenerator brings this powerful pattern to C# with **zero runtime overhead** and **full IDE support**.

## Why Now?

Modern C# has the tools needed for great discriminated unions:

- ✅ **Source Generators**: Zero-overhead code generation
- ✅ **Pattern Matching**: Switch expressions, property patterns
- ✅ **Records**: Immutable data types with structural equality
- ✅ **Nullable Reference Types**: Explicit null handling
- ✅ **File-Scoped Types**: Clean generated code organization

UnionGenerator leverages all of these features to provide a first-class discriminated union experience in C#.

## Next Steps

- [Explore key features](./key-features)
- [Install UnionGenerator](./installation)
- [Build your first union](../getting-started/your-first-union)
