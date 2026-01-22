---
sidebar_position: 4
---

# Common Patterns

Discriminated unions excel at modeling common programming patterns. Let's explore battle-tested patterns you'll use daily with UnionGenerator.

## Result&lt;T, TError&gt; Pattern

The Result pattern represents an operation that can either succeed with a value or fail with an error. This eliminates exception-driven control flow.

### Basic Implementation

```csharp
[GenerateUnion]
public partial record Result<T, TError>
{
    public static partial Result<T, TError> Ok(T value);
    public static partial Result<T, TError> Error(TError error);
}
```

### Real-World Example: User Registration

```csharp
public class UserService
{
    public Result<User, RegistrationError> RegisterUser(string email, string password)
    {
        // Validate email
        if (!IsValidEmail(email))
        {
            return Result<User, RegistrationError>.Error(
                new RegistrationError("Invalid email format")
            );
        }

        // Check if user exists
        if (_repository.EmailExists(email))
        {
            return Result<User, RegistrationError>.Error(
                new RegistrationError("Email already registered")
            );
        }

        // Validate password strength
        if (password.Length < 8)
        {
            return Result<User, RegistrationError>.Error(
                new RegistrationError("Password must be at least 8 characters")
            );
        }

        // Create user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = HashPassword(password),
            CreatedAt = DateTime.UtcNow
        };

        _repository.Save(user);

        return Result<User, RegistrationError>.Ok(user);
    }
}

public record RegistrationError(string Message);

// Usage
var result = userService.RegisterUser("user@example.com", "securepass123");

var message = result.Match(
    ok => $"Welcome! Your user ID is {ok.Value.Id}",
    error => $"Registration failed: {error.Error.Message}"
);
```

### Chaining Results

```csharp
public static class ResultExtensions
{
    // Map transforms the success value
    public static Result<TNew, TError> Map<T, TNew, TError>(
        this Result<T, TError> result,
        Func<T, TNew> mapper)
    {
        return result.Match(
            ok => Result<TNew, TError>.Ok(mapper(ok.Value)),
            error => Result<TNew, TError>.Error(error.Error)
        );
    }

    // Bind chains operations that return Results
    public static Result<TNew, TError> Bind<T, TNew, TError>(
        this Result<T, TError> result,
        Func<T, Result<TNew, TError>> binder)
    {
        return result.Match(
            ok => binder(ok.Value),
            error => Result<TNew, TError>.Error(error.Error)
        );
    }
}

// Usage: Chain multiple operations
var result = GetUser(userId)
    .Bind(user => ValidateUser(user))
    .Bind(user => UpdatePermissions(user))
    .Map(user => new UserDto(user));

return result.Match(
    ok => Ok(ok.Value),
    error => BadRequest(error.Error)
);
```

## Option&lt;T&gt; Pattern

The Option pattern represents a value that may or may not be present, eliminating null reference errors.

### Basic Implementation

```csharp
[GenerateUnion]
public partial record Option<T>
{
    public static partial Option<T> Some(T value);
    public static partial Option<T> None();
}
```

### Real-World Example: Configuration Service

```csharp
public class ConfigurationService
{
    private readonly Dictionary<string, string> _config;

    public Option<string> GetConfig(string key)
    {
        return _config.TryGetValue(key, out var value)
            ? Option<string>.Some(value)
            : Option<string>.None();
    }

    public Option<int> GetIntConfig(string key)
    {
        return GetConfig(key)
            .Map(value => int.TryParse(value, out var num) ? Option<int>.Some(num) : Option<int>.None())
            .Match(
                some => some,
                none => Option<int>.None()
            );
    }
}

// Usage
var config = new ConfigurationService();

var maxConnections = config.GetIntConfig("MaxConnections")
    .Match(
        some => some.Value,
        none => 10 // Default value
    );

// Or with TryGet
if (config.GetConfig("ApiKey").TryGetSome(out var apiKey))
{
    httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey.Value);
}
```

### Option Extensions

```csharp
public static class OptionExtensions
{
    // Map transforms the contained value if present
    public static Option<TNew> Map<T, TNew>(
        this Option<T> option,
        Func<T, TNew> mapper)
    {
        return option.Match(
            some => Option<TNew>.Some(mapper(some.Value)),
            none => Option<TNew>.None()
        );
    }

    // Bind chains operations that return Options
    public static Option<TNew> Bind<T, TNew>(
        this Option<T> option,
        Func<T, Option<TNew>> binder)
    {
        return option.Match(
            some => binder(some.Value),
            none => Option<TNew>.None()
        );
    }

    // GetOrDefault provides a fallback value
    public static T GetOrDefault<T>(this Option<T> option, T defaultValue)
    {
        return option.Match(
            some => some.Value,
            none => defaultValue
        );
    }

    // Filter returns None if predicate fails
    public static Option<T> Filter<T>(
        this Option<T> option,
        Func<T, bool> predicate)
    {
        return option.Match(
            some => predicate(some.Value) ? option : Option<T>.None(),
            none => option
        );
    }
}

// Usage: Chain operations
var result = FindUser(userId)
    .Filter(user => user.IsActive)
    .Map(user => user.Email)
    .GetOrDefault("no-reply@example.com");
```

### Combining Multiple Options

```csharp
public record Address(string Street, string City, string ZipCode);

public Option<Address> BuildAddress(
    Option<string> street,
    Option<string> city,
    Option<string> zipCode)
{
    // All must be present to create an address
    if (street.TryGetSome(out var s) &&
        city.TryGetSome(out var c) &&
        zipCode.TryGetSome(out var z))
    {
        return Option<Address>.Some(new Address(s.Value, c.Value, z.Value));
    }

    return Option<Address>.None();
}
```

## State Machine Pattern

Model complex state transitions with compile-time guarantees.

### Real-World Example: Order Processing

```csharp
[GenerateUnion]
public partial record OrderState
{
    // Draft: customer is building the order
    public static partial OrderState Draft(List<OrderItem> items, decimal total);
    
    // Submitted: order placed, awaiting payment
    public static partial OrderState Submitted(Guid orderId, DateTime submittedAt);
    
    // PaymentPending: awaiting payment confirmation
    public static partial OrderState PaymentPending(Guid orderId, string paymentId);
    
    // Paid: payment confirmed, ready for fulfillment
    public static partial OrderState Paid(Guid orderId, string transactionId, DateTime paidAt);
    
    // Processing: being prepared/packaged
    public static partial OrderState Processing(Guid orderId, string assignedTo);
    
    // Shipped: on the way to customer
    public static partial OrderState Shipped(Guid orderId, string trackingNumber, string carrier);
    
    // Delivered: successfully delivered
    public static partial OrderState Delivered(Guid orderId, DateTime deliveredAt, string signature);
    
    // Cancelled: order cancelled
    public static partial OrderState Cancelled(Guid orderId, string reason, DateTime cancelledAt);
    
    // Refunded: payment refunded
    public static partial OrderState Refunded(Guid orderId, string refundId, decimal amount);
}

public class OrderStateMachine
{
    // Type-safe state transitions
    public Result<OrderState, string> SubmitOrder(OrderState state)
    {
        return state.Match(
            draft =>
            {
                if (draft.Items.Count == 0)
                    return Result<OrderState, string>.Error("Cannot submit empty order");

                var orderId = Guid.NewGuid();
                return Result<OrderState, string>.Ok(
                    OrderState.Submitted(orderId, DateTime.UtcNow)
                );
            },
            submitted => Result<OrderState, string>.Error("Order already submitted"),
            paymentPending => Result<OrderState, string>.Error("Order already submitted"),
            paid => Result<OrderState, string>.Error("Order already submitted"),
            processing => Result<OrderState, string>.Error("Order already submitted"),
            shipped => Result<OrderState, string>.Error("Order already submitted"),
            delivered => Result<OrderState, string>.Error("Order already submitted"),
            cancelled => Result<OrderState, string>.Error("Cannot submit cancelled order"),
            refunded => Result<OrderState, string>.Error("Cannot submit refunded order")
        );
    }

    public Result<OrderState, string> ProcessPayment(OrderState state, string paymentId)
    {
        return state.Match(
            draft => Result<OrderState, string>.Error("Order must be submitted first"),
            submitted => Result<OrderState, string>.Ok(
                OrderState.PaymentPending(submitted.OrderId, paymentId)
            ),
            paymentPending => Result<OrderState, string>.Error("Payment already pending"),
            paid => Result<OrderState, string>.Error("Order already paid"),
            processing => Result<OrderState, string>.Error("Order already paid"),
            shipped => Result<OrderState, string>.Error("Order already paid"),
            delivered => Result<OrderState, string>.Error("Order already paid"),
            cancelled => Result<OrderState, string>.Error("Cannot pay cancelled order"),
            refunded => Result<OrderState, string>.Error("Cannot pay refunded order")
        );
    }

    public Result<OrderState, string> ConfirmPayment(OrderState state, string transactionId)
    {
        return state.Match(
            draft => Result<OrderState, string>.Error("Order must be submitted first"),
            submitted => Result<OrderState, string>.Error("Payment not initiated"),
            paymentPending => Result<OrderState, string>.Ok(
                OrderState.Paid(paymentPending.OrderId, transactionId, DateTime.UtcNow)
            ),
            paid => Result<OrderState, string>.Error("Order already paid"),
            processing => Result<OrderState, string>.Error("Order already paid"),
            shipped => Result<OrderState, string>.Error("Order already paid"),
            delivered => Result<OrderState, string>.Error("Order already paid"),
            cancelled => Result<OrderState, string>.Error("Cannot confirm payment for cancelled order"),
            refunded => Result<OrderState, string>.Error("Cannot confirm payment for refunded order")
        );
    }

    public Result<OrderState, string> ShipOrder(OrderState state, string trackingNumber, string carrier)
    {
        return state.Match(
            draft => Result<OrderState, string>.Error("Order must be submitted and paid first"),
            submitted => Result<OrderState, string>.Error("Order must be paid first"),
            paymentPending => Result<OrderState, string>.Error("Order must be paid first"),
            paid => Result<OrderState, string>.Ok(
                OrderState.Shipped(paid.OrderId, trackingNumber, carrier)
            ),
            processing => Result<OrderState, string>.Ok(
                OrderState.Shipped(processing.OrderId, trackingNumber, carrier)
            ),
            shipped => Result<OrderState, string>.Error("Order already shipped"),
            delivered => Result<OrderState, string>.Error("Order already delivered"),
            cancelled => Result<OrderState, string>.Error("Cannot ship cancelled order"),
            refunded => Result<OrderState, string>.Error("Cannot ship refunded order")
        );
    }
}
```

### Usage Example

```csharp
public class OrderController
{
    private readonly OrderStateMachine _stateMachine = new();

    [HttpPost("orders/{orderId}/submit")]
    public IActionResult SubmitOrder(Guid orderId)
    {
        var currentState = GetOrderState(orderId);
        
        var result = _stateMachine.SubmitOrder(currentState);

        return result.Match(
            ok =>
            {
                SaveOrderState(orderId, ok.Value);
                return Ok(new { Message = "Order submitted successfully" });
            },
            error => BadRequest(new { Error = error.Error })
        );
    }
}
```

## Validation&lt;T&gt; Pattern

Collect multiple validation errors before failing.

```csharp
[GenerateUnion]
public partial record Validation<T>
{
    public static partial Validation<T> Valid(T value);
    public static partial Validation<T> Invalid(List<string> errors);
}

public class UserValidator
{
    public Validation<UserRegistration> ValidateRegistration(UserRegistration registration)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(registration.Email))
            errors.Add("Email is required");
        else if (!IsValidEmail(registration.Email))
            errors.Add("Email format is invalid");

        if (string.IsNullOrWhiteSpace(registration.Username))
            errors.Add("Username is required");
        else if (registration.Username.Length < 3)
            errors.Add("Username must be at least 3 characters");

        if (string.IsNullOrWhiteSpace(registration.Password))
            errors.Add("Password is required");
        else if (registration.Password.Length < 8)
            errors.Add("Password must be at least 8 characters");

        if (registration.Age < 18)
            errors.Add("Must be 18 or older");

        return errors.Count > 0
            ? Validation<UserRegistration>.Invalid(errors)
            : Validation<UserRegistration>.Valid(registration);
    }
}

// Usage
var validation = validator.ValidateRegistration(registration);

return validation.Match(
    valid => Ok(CreateUser(valid.Value)),
    invalid => BadRequest(new { Errors = invalid.Errors })
);
```

## RemoteData&lt;T&gt; Pattern

Represent data loading states in UI applications.

```csharp
[GenerateUnion]
public partial record RemoteData<T>
{
    public static partial RemoteData<T> NotAsked();
    public static partial RemoteData<T> Loading();
    public static partial RemoteData<T> Success(T data);
    public static partial RemoteData<T> Failure(string error);
}

// Usage in a UI component
public class UserProfileViewModel
{
    public RemoteData<User> UserData { get; private set; } = RemoteData<User>.NotAsked();

    public async Task LoadUser(int userId)
    {
        UserData = RemoteData<User>.Loading();
        OnPropertyChanged(nameof(UserData));

        try
        {
            var user = await _api.GetUser(userId);
            UserData = RemoteData<User>.Success(user);
        }
        catch (Exception ex)
        {
            UserData = RemoteData<User>.Failure(ex.Message);
        }

        OnPropertyChanged(nameof(UserData));
    }

    public string GetDisplayContent()
    {
        return UserData.Match(
            notAsked => "Click to load user data",
            loading => "Loading...",
            success => $"User: {success.Data.Name}",
            failure => $"Error: {failure.Error}"
        );
    }
}
```

## Next Steps

Now that you've learned common patterns:

<!-- Coming soon:
- [Explore Core Package](../core-package/overview.md) - Deep dive into UnionGenerator features
- [ASP.NET Core Integration](../integration-packages/aspnetcore/overview.md) - Use unions in web APIs
- [FluentValidation Integration](../integration-packages/fluentvalidation/overview.md) - Combine with validation
-->

## Key Takeaways

✅ **Result&lt;T, TError&gt;** replaces exceptions for expected errors  
✅ **Option&lt;T&gt;** eliminates null reference errors  
✅ **State machines** provide type-safe state transitions  
✅ **Validation** collects multiple errors before failing  
✅ **RemoteData** models async loading states clearly
