---
sidebar_position: 3
---

# Pattern Matching

UnionGenerator provides multiple ways to match and extract values from union types. Each method offers different trade-offs between safety, ergonomics, and performance.

## Match Expression

The `Match` method provides exhaustive pattern matching with a return value:

```csharp
[GenerateUnion]
public partial record Result<T, E>
{
    public static partial Result<T, E> Ok(T value);
    public static partial Result<T, E> Error(E error);
}

// Match with return value
var message = result.Match(
    ok => $"Success: {ok.Value}",
    error => $"Failed: {error.Error}"
);
```

### Type-Safe and Exhaustive

The compiler ensures all cases are handled:

```csharp
[GenerateUnion]
public partial record Status
{
    public static partial Status Active();
    public static partial Status Inactive();
    public static partial Status Pending();
}

// ❌ Compile error: missing 'pending' case
var text = status.Match(
    active => "Active",
    inactive => "Inactive"
    // Error: CS7036 - no argument for 'pending' parameter
);

// ✅ All cases handled
var text = status.Match(
    active => "Active",
    inactive => "Inactive",
    pending => "Pending"
);
```

### With Generic Return Type

Match infers the return type:

```csharp
// Returns int
var count = result.Match(
    ok => ok.Value.Length,
    error => 0
);

// Returns bool
var isSuccess = result.Match(
    ok => true,
    error => false
);

// Returns custom type
var dto = result.Match(
    ok => new ResponseDto { Success = true, Data = ok.Value },
    error => new ResponseDto { Success = false, ErrorMessage = error.Error }
);
```

### Accessing Case Data

Match parameters give you strongly-typed access to case data:

```csharp
[GenerateUnion]
public partial record Payment
{
    public static partial Payment Pending(string orderId);
    public static partial Payment Completed(string transactionId, decimal amount);
    public static partial Payment Failed(string reason, int errorCode);
}

var result = payment.Match(
    pending => $"Order {pending.OrderId} is pending",
    completed => $"Transaction {completed.TransactionId}: ${completed.Amount}",
    failed => $"Failed (code {failed.ErrorCode}): {failed.Reason}"
);
```

## Match Action

When you don't need a return value, use `Match` for side effects:

```csharp
result.Match(
    ok => Console.WriteLine($"Success: {ok.Value}"),
    error => Console.WriteLine($"Error: {error.Error}")
);

// Equivalent to
result.Match(
    ok =>
    {
        Console.WriteLine($"Success: {ok.Value}");
    },
    error =>
    {
        Console.WriteLine($"Error: {error.Error}");
    }
);
```

### Multi-Statement Handlers

```csharp
payment.Match(
    pending =>
    {
        logger.LogInformation($"Payment pending: {pending.OrderId}");
        notificationService.SendPendingEmail(pending.OrderId);
    },
    completed =>
    {
        logger.LogInformation($"Payment completed: {completed.TransactionId}");
        database.UpdatePaymentStatus(completed.TransactionId);
        notificationService.SendReceiptEmail(completed.TransactionId);
    },
    failed =>
    {
        logger.LogError($"Payment failed: {failed.Reason}");
        notificationService.SendFailureEmail(failed.Reason);
    }
);
```

## TryGet Methods

Extract specific case data only if it matches:

```csharp
[GenerateUnion]
public partial record ApiResponse
{
    public static partial ApiResponse Success(string data);
    public static partial ApiResponse NotFound();
    public static partial ApiResponse Error(string message);
}

// TryGet pattern - safe extraction
if (response.TryGetSuccess(out var success))
{
    Console.WriteLine($"Data: {success.Data}");
}

if (response.TryGetError(out var error))
{
    logger.LogError(error.Message);
}
```

### Pattern: Early Return

```csharp
public string ProcessResponse(ApiResponse response)
{
    // Early return on error
    if (response.TryGetError(out var error))
    {
        return $"Error: {error.Message}";
    }

    // Early return on not found
    if (response.TryGetNotFound(out var _))
    {
        return "Resource not found";
    }

    // Handle success
    if (response.TryGetSuccess(out var success))
    {
        return $"Success: {success.Data}";
    }

    throw new InvalidOperationException("Unknown case");
}
```

### Combining TryGet with Conditional Logic

```csharp
public void ProcessOrders(List<OrderResult> results)
{
    var successful = results.Where(r => r.TryGetSuccess(out var _)).ToList();
    var failed = results.Where(r => r.TryGetError(out var _)).ToList();

    Console.WriteLine($"Successful: {successful.Count}, Failed: {failed.Count}");

    foreach (var result in failed)
    {
        if (result.TryGetError(out var error))
        {
            logger.LogError($"Order failed: {error.Message}");
        }
    }
}
```

## Is Properties

Quick boolean checks for case type:

```csharp
if (result.IsOk)
{
    // Handle success case
}

if (result.IsError)
{
    // Handle error case
}
```

### Guard Clauses

```csharp
public void ProcessResult(Result<User, string> result)
{
    if (!result.IsOk)
    {
        logger.LogWarning("Result is not OK");
        return;
    }

    // Safe to extract - we know it's Ok case
    result.Match(
        ok => SaveUser(ok.Value),
        error => { } // Won't reach here
    );
}
```

### Multiple Conditions

```csharp
public string GetStatusMessage(PaymentStatus status)
{
    if (status.IsCompleted || status.IsPending)
    {
        return "Active payment";
    }

    if (status.IsFailed || status.IsCancelled)
    {
        return "Inactive payment";
    }

    return "Unknown status";
}
```

## Switch Expressions

C#'s native switch expressions work with union types:

```csharp
[GenerateUnion]
public partial record Shape
{
    public static partial Shape Circle(double radius);
    public static partial Shape Rectangle(double width, double height);
    public static partial Shape Triangle(double baseLength, double height);
}

// Switch expression
var area = shape switch
{
    Shape.CircleCase c => Math.PI * c.Radius * c.Radius,
    Shape.RectangleCase r => r.Width * r.Height,
    Shape.TriangleCase t => 0.5 * t.BaseLength * t.Height,
    _ => throw new ArgumentException("Unknown shape")
};
```

### With Property Patterns

```csharp
var description = shape switch
{
    Shape.CircleCase { Radius: > 10 } => "Large circle",
    Shape.CircleCase { Radius: > 5 } => "Medium circle",
    Shape.CircleCase => "Small circle",
    Shape.RectangleCase { Width: var w, Height: var h } when w == h => "Square",
    Shape.RectangleCase => "Rectangle",
    Shape.TriangleCase { Height: > 10 } => "Tall triangle",
    Shape.TriangleCase => "Triangle",
    _ => "Unknown"
};
```

### With Discard Patterns

```csharp
var category = payment switch
{
    Payment.CompletedCase => "Processed",
    Payment.PendingCase => "Awaiting",
    Payment.FailedCase or Payment.CancelledCase => "Terminal",
    _ => "Unknown"
};
```

## Switch Statements

Traditional switch statements for imperative code:

```csharp
switch (result)
{
    case Result<User, string>.OkCase ok:
        Console.WriteLine($"User: {ok.Value.Name}");
        SaveToDatabase(ok.Value);
        break;

    case Result<User, string>.ErrorCase error:
        Console.WriteLine($"Error: {error.Error}");
        LogError(error.Error);
        break;

    default:
        throw new InvalidOperationException("Unknown case");
}
```

### Multiple Statements Per Case

```csharp
switch (orderStatus)
{
    case OrderStatus.DraftCase draft:
        logger.LogInformation("Processing draft order");
        ValidateItems(draft.Items);
        CalculateTotal(draft.Items);
        break;

    case OrderStatus.SubmittedCase submitted:
        logger.LogInformation($"Order submitted at {submitted.SubmittedAt}");
        SendConfirmationEmail(submitted.OrderId);
        NotifyWarehouse(submitted.OrderId);
        break;

    case OrderStatus.ShippedCase shipped:
        logger.LogInformation($"Order shipped: {shipped.TrackingNumber}");
        UpdateInventory(shipped.OrderId);
        SendTrackingEmail(shipped.TrackingNumber);
        break;
}
```

## Type Patterns

Use C#'s `is` pattern for type checking:

```csharp
if (result is Result<User, string>.OkCase ok)
{
    Console.WriteLine($"User: {ok.Value.Name}");
}
else if (result is Result<User, string>.ErrorCase error)
{
    Console.WriteLine($"Error: {error.Error}");
}
```

### In LINQ Queries

```csharp
var successfulUsers = results
    .Where(r => r is Result<User, string>.OkCase)
    .Select(r => ((Result<User, string>.OkCase)r).Value)
    .ToList();

// Or with pattern matching
var errorMessages = results
    .OfType<Result<User, string>.ErrorCase>()
    .Select(e => e.Error)
    .ToList();
```

## Async Matching

Match works seamlessly with async code:

```csharp
// Async match with return value
var message = await result.Match(
    ok => SaveUserAsync(ok.Value),
    error => Task.FromResult($"Error: {error.Error}")
);

// Async match for side effects
await result.Match(
    ok => SaveUserAsync(ok.Value),
    error => LogErrorAsync(error.Error)
);
```

### Async Handlers

```csharp
await payment.Match(
    async pending =>
    {
        await database.UpdateStatusAsync(pending.OrderId, "Pending");
        await notificationService.SendEmailAsync(pending.OrderId);
    },
    async completed =>
    {
        await database.UpdateStatusAsync(completed.TransactionId, "Completed");
        await invoiceService.GenerateInvoiceAsync(completed.TransactionId);
    },
    async failed =>
    {
        await database.UpdateStatusAsync(failed.OrderId, "Failed");
        await alertService.NotifyAdminAsync(failed.Reason);
    }
);
```

## Performance Considerations

### Match is Zero-Allocation

```csharp
// No allocations - direct delegate invocation
var result = option.Match(
    some => some.Value * 2,
    none => 0
);
```

### Switch Expressions are Optimal

```csharp
// Compiles to efficient jump table
var value = status switch
{
    Status.ActiveCase => 1,
    Status.InactiveCase => 2,
    Status.PendingCase => 3,
    _ => 0
};
```

### Avoid Boxing

```csharp
// ❌ Boxing occurs here
object boxed = result;
var isOk = ((Result<int, string>)boxed).IsOk;

// ✅ No boxing
var isOk = result.IsOk;
```

## Pattern Comparison

| Pattern | Exhaustive | Return Value | Side Effects | Async Support |
|---------|-----------|--------------|--------------|---------------|
| Match Expression | ✅ Yes | ✅ Yes | ❌ No | ✅ Yes |
| Match Action | ✅ Yes | ❌ No | ✅ Yes | ✅ Yes |
| TryGet | ❌ No | ❌ No | ✅ Yes | ❌ No |
| Is Properties | ❌ No | ❌ No | ✅ Yes | ❌ No |
| Switch Expression | ⚠️ Manual | ✅ Yes | ❌ No | ✅ Yes |
| Switch Statement | ⚠️ Manual | ❌ No | ✅ Yes | ✅ Yes |
| Type Pattern | ❌ No | ❌ No | ✅ Yes | ❌ No |

## Best Practices

### Prefer Match for Exhaustiveness

```csharp
// ✅ Compiler ensures all cases handled
var result = payment.Match(
    pending => "Pending",
    completed => "Completed",
    failed => "Failed"
);

// ❌ Easy to forget a case
if (payment.IsPending) return "Pending";
if (payment.IsCompleted) return "Completed";
// Forgot 'failed' case!
```

### Use TryGet for Optional Handling

```csharp
// ✅ Only care about error case
if (result.TryGetError(out var error))
{
    logger.LogError(error.Message);
    return;
}

// ❌ Overkill with Match
result.Match(
    ok => { /* don't care */ },
    error => logger.LogError(error.Message)
);
```

### Use Is Properties for Guards

```csharp
// ✅ Clear guard clause
if (!result.IsOk)
{
    return BadRequest();
}

// ❌ Awkward with Match
var shouldReturn = result.Match(
    ok => false,
    error => true
);
if (shouldReturn) return BadRequest();
```

## Next Steps

- [API Reference](../api-reference/generated-api.md) - Complete generated API
- [Best Practices](./best-practices.md) - Production-ready patterns

## Key Takeaways

✅ **Match** provides exhaustive, type-safe pattern matching  
✅ **TryGet** is perfect for optional case handling  
✅ **Is properties** enable quick boolean checks  
✅ **Switch expressions** offer native C# pattern matching  
✅ **All patterns** have zero allocation overhead
