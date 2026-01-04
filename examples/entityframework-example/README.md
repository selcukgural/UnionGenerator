# Entity Framework Core Integration Example

This example demonstrates how to use UnionGenerator with Entity Framework Core to store and query discriminated union types in databases using JSON columns.

## Features Demonstrated

1. **Union Type JSON Storage**: Storing union types as JSON in database columns
2. **Value Converters**: Automatic conversion between union types and JSON
3. **EF Core Integration**: Using `HasResultConversion()` fluent API
4. **CRUD Operations**: Creating, reading, updating, and deleting union-based entities
5. **Pattern Matching**: Using Match() with database-loaded results
6. **Type Safety**: Compile-time safe access to union cases from databases

## Running the Example

```bash
cd examples/entityframework-example
dotnet run
```

## What This Does

### 1. Define Union Type (Result Model)

```csharp
[GenerateUnion]
public partial class ProcessingResult
{
    public static ProcessingResult Success(ProcessedData data) => new SuccessCase(data);
    public static ProcessingResult Failed(ErrorInfo error) => new FailedCase(error);
}

public record ProcessedData(Guid ProcessedId, string Message, DateTime Timestamp);
public record ErrorInfo(string Code, string Message, string? Details = null);
```

### 2. Define Entity with Union Property

```csharp
public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    
    // This property is stored as JSON in the database
    public ProcessingResult ProcessingResult { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### 3. Configure Value Converter in DbContext

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Order>()
        .HasResultConversion<Order, ProcessingResult, ProcessedData, ErrorInfo>(
            o => o.ProcessingResult
        );
}
```

### 4. Create and Save Orders

```csharp
// Create order with successful result
var order = new Order
{
    CustomerName = "Alice Johnson",
    OrderNumber = "ORD-001",
    TotalAmount = 299.99m,
    ProcessingResult = ProcessingResult.Success(new ProcessedData(
        ProcessedId: Guid.NewGuid(),
        Message: "Order processed successfully",
        Timestamp: DateTime.UtcNow
    )),
    CreatedAt = DateTime.UtcNow
};

dbContext.Orders.Add(order);
await dbContext.SaveChangesAsync();
```

### 5. Query and Pattern Match

```csharp
// Load from database
var orders = await dbContext.Orders.ToListAsync();

// Pattern match on results
foreach (var order in orders)
{
    order.ProcessingResult.Match(
        success: data => Console.WriteLine($"Processed: {data.Message}"),
        failed: error => Console.WriteLine($"Error: {error.Code}")
    );
}
```

## Database Schema

### Orders Table (with JSON column)

```sql
CREATE TABLE Orders (
    Id INT PRIMARY KEY IDENTITY,
    CustomerName NVARCHAR(200) NOT NULL,
    OrderNumber NVARCHAR(50) NOT NULL UNIQUE,
    TotalAmount DECIMAL(18, 2),
    ProcessingResult NVARCHAR(MAX),  -- Stored as JSON
    CreatedAt DATETIME2,
    UpdatedAt DATETIME2
);
```

### Sample JSON Content

When saved to the database, the ProcessingResult is stored as JSON:

**Success case:**
```json
{
  "case": "Success",
  "value": {
    "processedId": "550e8400-e29b-41d4-a716-446655440000",
    "message": "Order processed successfully",
    "timestamp": "2024-01-04T10:30:00Z"
  }
}
```

**Failed case:**
```json
{
  "case": "Failed",
  "value": {
    "code": "PAYMENT_DECLINED",
    "message": "Payment method was declined",
    "details": "Card expired"
  }
}
```

## Use Cases

### 1. Order Processing

Store order processing results with full error context:

```csharp
var order = new Order 
{
    OrderNumber = "ORD-123",
    ProcessingResult = ProcessingResult.Failed(
        new ErrorInfo("INVENTORY_ERROR", "Item out of stock", "SKU-456")
    )
};
```

### 2. Audit Logging

Track operation results in audit logs:

```csharp
var auditLog = new AuditLog
{
    Action = "UpdateUser",
    Result = Result<SuccessData, ErrorInfo>.Ok(
        new SuccessData { ItemsAffected = 1 }
    )
};
```

### 3. Payment Processing

Store payment outcomes with detailed failure reasons:

```csharp
var payment = new Payment
{
    Amount = 99.99m,
    Result = PaymentResult.Failed(
        new PaymentError("3DS_REQUIRED", "3D Secure authentication required")
    )
};
```

### 4. Event Sourcing

Store event processing results for event-driven architecture:

```csharp
var eventRecord = new EventRecord
{
    EventId = Guid.NewGuid(),
    ProcessingResult = EventProcessingResult.Success(
        new ProcessedEventInfo { HandledAt = DateTime.UtcNow }
    )
};
```

## Common Patterns

### Pattern 1: Bulk Query with Result Filtering

```csharp
// Load all orders
var allOrders = await dbContext.Orders.ToListAsync();

// Filter by result type (client-side evaluation)
var failedOrders = allOrders
    .Where(o => o.ProcessingResult is ProcessingResult.FailedCase)
    .ToList();

// Extract error codes
var errorCodes = failedOrders
    .Select(o => ((ProcessingResult.FailedCase)o.ProcessingResult).Value.Code)
    .Distinct()
    .ToList();
```

### Pattern 2: Update Results

```csharp
var order = await dbContext.Orders.FindAsync(id);
if (order != null)
{
    // Update with new result
    order.ProcessingResult = ProcessingResult.Success(
        new ProcessedData(Guid.NewGuid(), "Reprocessed", DateTime.UtcNow)
    );
    order.UpdatedAt = DateTime.UtcNow;
    
    await dbContext.SaveChangesAsync();
}
```

### Pattern 3: Create with Conditional Result

```csharp
var order = new Order
{
    CustomerName = customer.Name,
    OrderNumber = GenerateOrderNumber(),
    TotalAmount = cart.Total,
    ProcessingResult = await ValidateAndProcess(cart),
    CreatedAt = DateTime.UtcNow
};

private async Task<ProcessingResult> ValidateAndProcess(ShoppingCart cart)
{
    if (cart.Items.Count == 0)
        return ProcessingResult.Failed(
            new ErrorInfo("EMPTY_CART", "Cart is empty")
        );

    var result = await _paymentService.ProcessAsync(cart.Total);
    return result.IsSuccessful
        ? ProcessingResult.Success(
            new ProcessedData(result.TransactionId, "Payment accepted", DateTime.UtcNow)
        )
        : ProcessingResult.Failed(
            new ErrorInfo("PAYMENT_FAILED", result.ErrorMessage)
        );
}
```

## Performance Notes

- **Storage**: JSON columns are compact; typical success case < 200 bytes, failure case < 300 bytes
- **Querying**: Client-side filtering required for pattern matching; pre-materialize if querying large datasets
- **Serialization**: Fast O(n) where n is object size; minimal overhead
- **Indexing**: Create indexes on frequently queried columns (CustomerName, OrderNumber), not on JSON values
- **Null Handling**: Properly handles nullable Result properties

## Best Practices

### ✅ DO

- Use value converters for automatic serialization
- Keep union payloads simple and serializable
- Use records for error/success types
- Create indexes on entity columns, not JSON properties
- Validate entities before saving
- Include timestamps in processed data for auditing
- Handle result updates atomically with SaveChangesAsync

### ❌ DON'T

- Don't query deeply nested JSON properties with EF Core LINQ
- Don't store circular references in union payloads
- Don't use DateTime.Now (use DateTime.UtcNow)
- Don't forget to configure value converters
- Don't assume case names won't change in schema
- Don't store sensitive data in error messages
- Don't use string comparisons on JSON; materialize and pattern match

## Testing

The example uses in-memory database for easy testing:

```csharp
var options = new DbContextOptionsBuilder<OrderDbContext>()
    .UseInMemoryDatabase("TestDb")
    .Options;

var dbContext = new OrderDbContext(options);
```

For production, use SQL Server, PostgreSQL, or other providers supported by EF Core.

## Related Documentation

- [UnionGenerator README](../../src/UnionGenerator/README.md)
- [UnionGenerator.EntityFrameworkCore README](../../src/UnionGenerator.EntityFrameworkCore/README.md)
- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [JSON Support in EF Core](https://learn.microsoft.com/en-us/ef/core/modeling/value-conversions)

---

**Ready to store unions?** Run `dotnet run` and see results persisted to database! 🚀

