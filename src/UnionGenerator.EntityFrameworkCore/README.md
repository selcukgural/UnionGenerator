# UnionGenerator.EntityFrameworkCore

Entity Framework Core integration for UnionGenerator, providing value converters and model configuration extensions for storing Result union types in databases.

## Features

- 🎯 **Value Converters**: Store Result<T, E> as JSON columns with automatic serialization
- 🔄 **JSON Format**: Compact JSON representation with case discrimination
- 🎨 **Fluent API**: Easy configuration with `HasResultConversion()` extension methods
- 📋 **Null Handling**: Proper support for nullable Result properties
- 🚀 **Auto Configuration**: Scan and configure all Result properties automatically
- 🔍 **Query Support**: LINQ queries with client-side evaluation for complex Result operations

## Installation

```bash
dotnet add package UnionGenerator.EntityFrameworkCore
```

## Quick Start

### 1. Define Your Result Union

```csharp
using UnionGenerator.Attributes;

[GenerateUnion]
public partial class Result<T, E>
{
    public static Result<T, E> Ok(T value) => new OkCase(value);
    public static Result<T, E> Error(E error) => new ErrorCase(error);
}

public record OrderData(string OrderNumber, decimal Total);
public record ErrorInfo(string Code, string Message);
```

### 2. Define Your Entity

```csharp
public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Result<OrderData, ErrorInfo> ProcessingResult { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
```

### 3. Configure DbContext

```csharp
using Microsoft.EntityFrameworkCore;
using UnionGenerator.EntityFrameworkCore.Extensions;

public class AppDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Option 1: Configure individual properties
        modelBuilder.Entity<Order>()
            .HasResultConversion<Order, Result<OrderData, ErrorInfo>, OrderData, ErrorInfo>(
                o => o.ProcessingResult
            );

        // Option 2: Auto-configure all Result properties (basic column type only)
        // modelBuilder.ConfigureResultConversions();
    }
}
```

### 4. Use in Your Application

```csharp
// Create and save
var order = new Order
{
    CustomerName = "John Doe",
    ProcessingResult = Result<OrderData, ErrorInfo>.Ok(
        new OrderData("ORD-001", 99.99m)
    ),
    CreatedAt = DateTime.UtcNow
};

await dbContext.Orders.AddAsync(order);
await dbContext.SaveChangesAsync();

// Query
var orders = await dbContext.Orders
    .Where(o => o.CustomerName == "John Doe")
    .ToListAsync();

foreach (var o in orders)
{
    o.ProcessingResult.Match(
        ok: data => Console.WriteLine($"Order {data.OrderNumber}: ${data.Total}"),
        error: err => Console.WriteLine($"Error {err.Code}: {err.Message}")
    );
}
```

## Database Schema

Result properties are stored as JSON strings in the database:

```sql
CREATE TABLE Orders (
    Id INT PRIMARY KEY,
    CustomerName NVARCHAR(255),
    ProcessingResult NVARCHAR(MAX), -- JSON: {"case":"Ok","value":{...}}
    CreatedAt DATETIME2
);
```

### JSON Format

**Success case:**
```json
{
  "case": "Ok",
  "value": {
    "orderNumber": "ORD-001",
    "total": 99.99
  }
}
```

**Error case:**
```json
{
  "case": "Error",
  "value": {
    "code": "PAYMENT_FAILED",
    "message": "Payment processing failed"
  }
}
```

## Advanced Usage

### Nullable Result Properties

```csharp
public class Order
{
    public int Id { get; set; }
    public Result<OrderData, ErrorInfo>? OptionalResult { get; set; }
}

// Configure
modelBuilder.Entity<Order>()
    .HasNullableResultConversion<Order, Result<OrderData, ErrorInfo>, OrderData, ErrorInfo>(
        o => o.OptionalResult
    );
```

### Custom JSON Options

```csharp
using System.Text.Json;
using UnionGenerator.EntityFrameworkCore.Converters;

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false,
    Converters = { new ResultJsonConverter<Result<OrderData, ErrorInfo>, OrderData, ErrorInfo>() }
};

modelBuilder.Entity<Order>()
    .HasResultConversionWithOptions<Order, Result<OrderData, ErrorInfo>, OrderData, ErrorInfo>(
        o => o.ProcessingResult,
        jsonOptions
    );
```

### Querying Result Properties

```csharp
// Basic queries work as expected
var orders = await dbContext.Orders
    .Where(o => o.CreatedAt > DateTime.UtcNow.AddDays(-7))
    .ToListAsync();

// Filter by Result state (client-side evaluation)
var successfulOrders = orders
    .Where(o => o.ProcessingResult.Match(ok: _ => true, error: _ => false))
    .ToList();

// Note: Querying nested JSON properties requires database-specific functions
// For SQL Server with JSON support:
// var query = dbContext.Orders
//     .FromSqlRaw("SELECT * FROM Orders WHERE JSON_VALUE(ProcessingResult, '$.case') = 'Ok'");
```

### Migrations

When adding Result properties to existing entities, EF Core will generate appropriate migrations:

```csharp
// dotnet ef migrations add AddProcessingResult

public partial class AddProcessingResult : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ProcessingResult",
            table: "Orders",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ProcessingResult",
            table: "Orders");
    }
}
```

### Handling Existing Data

If you're adding Result properties to entities with existing data, you may need a data migration:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<string>(
        name: "ProcessingResult",
        table: "Orders",
        type: "nvarchar(max)",
        nullable: true); // Make nullable initially

    // Migrate existing data to default Result state
    migrationBuilder.Sql(@"
        UPDATE Orders 
        SET ProcessingResult = '{""case"":""Ok"",""value"":{""orderNumber"":"""",""total"":0}}'
        WHERE ProcessingResult IS NULL
    ");

    // Make non-nullable after migration
    migrationBuilder.AlterColumn<string>(
        name: "ProcessingResult",
        table: "Orders",
        type: "nvarchar(max)",
        nullable: false);
}
```

## Best Practices

1. **Use Value Objects**: Keep TData and TError types lightweight (records or simple classes)
2. **Index Strategy**: Create computed columns or JSON indexes for frequently queried fields
3. **Column Size**: Use `nvarchar(max)` for variable-size JSON; consider fixed size for small Results
4. **Client Evaluation**: Complex LINQ queries on Result properties will use client-side evaluation
5. **Null Handling**: Use nullable Result properties when the value is truly optional
6. **Migration Testing**: Always test migrations on a copy of production data

## Performance Considerations

- **Serialization Overhead**: JSON serialization adds ~10-50μs per Result depending on size
- **Column Storage**: JSON columns use more storage than normalized tables
- **Query Performance**: Filtering by JSON properties is slower than indexed columns
- **Recommendation**: Use Result for entities where:
  - The data is read more often than queried
  - The structure is flexible and may change
  - The business logic benefits from Result pattern safety

## Database Provider Support

| Provider | Status | Notes |
|----------|--------|-------|
| SQL Server | ✅ Full | nvarchar(max) columns, JSON functions available |
| PostgreSQL | ✅ Full | text/jsonb columns, excellent JSON support |
| SQLite | ✅ Full | text columns, limited JSON functions |
| MySQL | ✅ Full | longtext columns, JSON functions available |
| In-Memory | ✅ Full | No persistence, useful for testing |

## Limitations

- **Query Translation**: Complex LINQ expressions on Result properties require client evaluation
- **JSON Functions**: Database-specific JSON functions (JSON_VALUE, etc.) require raw SQL
- **Index Performance**: JSON columns cannot be efficiently indexed (use computed columns)
- **Change Tracking**: Full Result replacement required; partial updates not supported

## Thread Safety

- Value converters are stateless and thread-safe
- JSON converters use System.Text.Json which is thread-safe
- DbContext is not thread-safe (standard EF Core behavior)

## Examples

### Audit Trail with Results

```csharp
public class AuditEntry
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public Result<AuditData, AuditError> Result { get; set; } = null!;
    public DateTime Timestamp { get; set; }
}

// Track success and failure
var auditEntry = new AuditEntry
{
    Action = "UpdateUser",
    Result = userUpdateResult, // Result<AuditData, AuditError>
    Timestamp = DateTime.UtcNow
};

await dbContext.AuditEntries.AddAsync(auditEntry);
await dbContext.SaveChangesAsync();
```

### Saga State Management

```csharp
public class SagaInstance
{
    public Guid Id { get; set; }
    public string SagaType { get; set; } = string.Empty;
    public Result<SagaState, SagaError>? CurrentState { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

// Query incomplete sagas
var pendingSagas = await dbContext.Sagas
    .Where(s => s.CompletedAt == null)
    .ToListAsync();
```

## API Reference

### ResultValueConverter<TResult, TData, TError>

- Constructor: `ResultValueConverter()`
- Constructor: `ResultValueConverter(JsonSerializerOptions)`
- Converts Result<T, E> ↔ JSON string

### ResultJsonConverter<TResult, TData, TError>

- `Read(ref Utf8JsonReader, Type, JsonSerializerOptions)`: Deserialize from JSON
- `Write(Utf8JsonWriter, TResult, JsonSerializerOptions)`: Serialize to JSON

### ModelBuilderExtensions

- `HasResultConversion<TEntity, TResult, TData, TError>()`: Configure Result property
- `HasNullableResultConversion<TEntity, TResult, TData, TError>()`: Configure nullable Result
- `HasResultConversionWithOptions<TEntity, TResult, TData, TError>()`: Configure with custom JSON options
- `ConfigureResultConversions()`: Auto-configure all Result properties

## License

This project is part of UnionGenerator and uses the same license.

