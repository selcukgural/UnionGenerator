# JSON Serialization Example

This example demonstrates how to use UnionGenerator for JSON serialization of discriminated unions.

## Features Demonstrated

1. **Union Type JSON Serialization**: Converting union types to/from JSON
2. **System.Text.Json Integration**: Using modern JSON APIs
3. **Custom JSON Converters**: Implementing type-safe converters
4. **Nested Unions**: Serializing complex union hierarchies
5. **Error Handling**: Serializing error cases with metadata
6. **RoundTrip Serialization**: Deserializing back to original type

## Running the Example

```bash
cd examples/json-example
dotnet run
```

## What This Does

### 1. Define Union Types

```csharp
[GenerateUnion]
public partial class ApiResponse<T>
{
    public static ApiResponse<T> Success(T data) => new SuccessCase(data);
    public static ApiResponse<T> Failed(ErrorInfo error) => new FailureCase(error);
}

public record ErrorInfo(string Code, string Message, string? Details = null);
```

### 2. Serialize to JSON

```csharp
var success = ApiResponse<User>.Success(new User { Id = 1, Name = "John" });
var json = JsonSerializer.Serialize(success);
// Output: {"case":"Success","value":{"id":1,"name":"John"}}

var failure = ApiResponse<User>.Failed(
    new ErrorInfo("NOT_FOUND", "User not found")
);
var json = JsonSerializer.Serialize(failure);
// Output: {"case":"Failed","value":{"code":"NOT_FOUND","message":"User not found","details":null}}
```

### 3. Deserialize from JSON

```csharp
var json = "{\"case\":\"Success\",\"value\":{\"id\":1,\"name\":\"John\"}}";
var response = JsonSerializer.Deserialize<ApiResponse<User>>(json);

response.Match(
    success: user => Console.WriteLine($"User: {user.Name}"),
    failed: error => Console.WriteLine($"Error: {error.Message}")
);
```

## JSON Format

### Success Case
```json
{
  "case": "Success",
  "value": {
    "id": 1,
    "name": "John Doe"
  }
}
```

### Error Case
```json
{
  "case": "Failed",
  "value": {
    "code": "NOT_FOUND",
    "message": "User not found",
    "details": null
  }
}
```

## Advanced: Custom Converters

For Entity Framework Core JSON columns, use:
```bash
dotnet add package UnionGenerator.EntityFrameworkCore
```

This provides automatic JSON conversion for database storage:

```csharp
public class AuditLog
{
    public int Id { get; set; }
    public Result<AuditData, AuditError> Result { get; set; } = null!;
}

// Configure in DbContext
modelBuilder.Entity<AuditLog>()
    .HasResultConversion<AuditLog, Result<AuditData, AuditError>, AuditData, AuditError>(
        a => a.Result
    );
```

## Use Cases

### 1. API Responses
Return structured success/error responses in REST APIs:
```csharp
public IActionResult GetUser(int id)
{
    var result = _service.GetUser(id);
    return new JsonResult(result); // Automatically serializes
}
```

### 2. Message Queues
Serialize union results for async message processing:
```csharp
var result = Process(input);
await messageQueue.PublishAsync(JsonSerializer.Serialize(result));
```

### 3. Configuration Files
Store configuration options as JSON with type safety:
```csharp
var config = JsonSerializer.Deserialize<Config>(jsonContent);
config.ConnectionMode.Match(
    direct: connStr => Console.WriteLine($"Direct: {connStr}"),
    pooled: settings => Console.WriteLine($"Pooled: {settings.PoolSize}")
);
```

### 4. Data Storage
Store union types in databases as JSON:
```csharp
var auditEntry = new AuditEntry
{
    Action = "UpdateUser",
    Result = updateResult // Automatically serialized to JSON
};

await dbContext.AuditEntries.AddAsync(auditEntry);
await dbContext.SaveChangesAsync();
```

## Common Patterns

### Pattern 1: Single Success/Error

```csharp
var result = CreateOrder(orderData);

result.Match(
    success: order => SendConfirmationEmail(order),
    failed: error => LogError(error)
);
```

### Pattern 2: Multiple Success Cases

```csharp
[GenerateUnion]
public partial class PaymentResult
{
    public static PaymentResult Success(TransactionId txnId) => new SuccessCase(txnId);
    public static PaymentResult Pending(string reason) => new PendingCase(reason);
    public static PaymentResult Failed(string reason) => new FailureCase(reason);
}

var result = ProcessPayment(amount);
var json = JsonSerializer.Serialize(result);

// Later, deserialize and handle
result.Match(
    success: txnId => CompleteOrder(txnId),
    pending: reason => QueueForRetry(reason),
    failed: reason => NotifyCustomer(reason)
);
```

### Pattern 3: Nested Types

```csharp
[GenerateUnion]
public partial class Result<T, E>
{
    public static Result<T, E> Ok(T value) => new OkCase(value);
    public static Result<T, E> Error(E error) => new ErrorCase(error);
}

// Nested
var result = Result<List<User>, ErrorInfo>.Ok(
    new List<User> { /* ... */ }
);

var json = JsonSerializer.Serialize(result);
// Works seamlessly with nested collections
```

## Performance Notes

- **Serialization**: Fast O(n) where n is object size
- **Deserialization**: Requires case discrimination, slightly slower
- **Memory**: Unions are compact, minimal overhead
- **String Size**: Minimal JSON (case name + value)

## Best Practices

### ✅ DO

- Use System.Text.Json (modern, performant)
- Keep union payloads simple and serializable
- Use records for error types (easy to serialize)
- Validate JSON format in integration tests
- Document expected JSON structure in API docs

### ❌ DON'T

- Don't use complex circular references
- Don't serialize internal state (sealed classes)
- Don't assume case names won't change (version your APIs)
- Don't skip round-trip serialization tests
- Don't store sensitive data in error messages

## Testing

```bash
dotnet test
```

Tests validate:
- Serialization produces valid JSON
- Deserialization recovers original data
- Nested structures work correctly
- All union cases serialize properly
- Error cases include metadata

## Related Documentation

- [UnionGenerator README](../../src/UnionGenerator/README.md)
- [UnionGenerator.EntityFrameworkCore](../../src/UnionGenerator.EntityFrameworkCore/README.md)
- [System.Text.Json Docs](https://learn.microsoft.com/en-us/dotnet/api/system.text.json)

---

**Ready to serialize unions?** Run `dotnet run` and start exploring! 🚀

