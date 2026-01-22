---
sidebar_position: 1
---

# Entity Framework Core Integration

The UnionGenerator.EntityFrameworkCore package enables you to store and query discriminated unions in databases with automatic value converters and query translation.

## Overview

This integration provides:

- Automatic value converters for union types
- JSON column storage support
- LINQ query translation
- Migration support
- Complex type mapping

## Installation

```bash
dotnet add package UnionGenerator.EntityFrameworkCore
```

**Requirements:**
- .NET 6.0 or later
- Entity Framework Core 6.0 or later
- UnionGenerator package

## Quick Start

### Define Your Union

```csharp
[GenerateUnion]
public partial record OrderStatus
{
    public static partial OrderStatus Pending();
    public static partial OrderStatus Processing(string workerId);
    public static partial OrderStatus Shipped(string trackingNumber);
    public static partial OrderStatus Delivered(DateTime deliveredAt);
    public static partial OrderStatus Cancelled(string reason);
}
```

### Configure Entity

```csharp
public class Order
{
    public int Id { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AppDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .Property(o => o.Status)
            .HasConversion<OrderStatusConverter>();
    }
}
```

### Value Converter

```csharp
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

public class OrderStatusConverter : ValueConverter<OrderStatus, string>
{
    public OrderStatusConverter() 
        : base(
            v => Serialize(v),
            v => Deserialize(v))
    {
    }

    private static string Serialize(OrderStatus status)
    {
        return status.Match(
            pending => JsonSerializer.Serialize(new { Type = "Pending" }),
            processing => JsonSerializer.Serialize(new { Type = "Processing", WorkerId = processing.WorkerId }),
            shipped => JsonSerializer.Serialize(new { Type = "Shipped", TrackingNumber = shipped.TrackingNumber }),
            delivered => JsonSerializer.Serialize(new { Type = "Delivered", DeliveredAt = delivered.DeliveredAt }),
            cancelled => JsonSerializer.Serialize(new { Type = "Cancelled", Reason = cancelled.Reason })
        );
    }

    private static OrderStatus Deserialize(string json)
    {
        var doc = JsonDocument.Parse(json);
        var type = doc.RootElement.GetProperty("Type").GetString();

        return type switch
        {
            "Pending" => OrderStatus.Pending(),
            "Processing" => OrderStatus.Processing(
                doc.RootElement.GetProperty("WorkerId").GetString()!
            ),
            "Shipped" => OrderStatus.Shipped(
                doc.RootElement.GetProperty("TrackingNumber").GetString()!
            ),
            "Delivered" => OrderStatus.Delivered(
                doc.RootElement.GetProperty("DeliveredAt").GetDateTime()
            ),
            "Cancelled" => OrderStatus.Cancelled(
                doc.RootElement.GetProperty("Reason").GetString()!
            ),
            _ => throw new InvalidOperationException($"Unknown type: {type}")
        };
    }
}
```

## Querying Unions

### Basic Queries

```csharp
// Query by union type
var pendingOrders = await context.Orders
    .Where(o => EF.Property<string>(o.Status, "Type") == "Pending")
    .ToListAsync();

// Filter in memory after loading
var processingOrders = await context.Orders
    .ToListAsync();

var filtered = processingOrders
    .Where(o => o.Status.IsProcessing)
    .ToList();
```

### Complex Queries

```csharp
// Count by status type
var statusCounts = await context.Orders
    .GroupBy(o => EF.Property<string>(o.Status, "Type"))
    .Select(g => new { Status = g.Key, Count = g.Count() })
    .ToListAsync();

// Find orders by tracking number (requires JSON query support)
var order = await context.Orders
    .Where(o => EF.Functions.JsonValue(
        EF.Property<string>(o.Status, "Json"),
        "$.TrackingNumber") == trackingNumber)
    .FirstOrDefaultAsync();
```

## Storage Strategies

### JSON Column (Recommended)

Store entire union as JSON:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Order>()
        .Property(o => o.Status)
        .HasConversion<OrderStatusConverter>()
        .HasColumnType("jsonb"); // PostgreSQL
        // .HasColumnType("json"); // MySQL
        // .HasColumnType("nvarchar(max)"); // SQL Server
}
```

### Separate Columns

Store discriminator and data separately:

```csharp
public class Order
{
    public int Id { get; set; }
    
    // Shadow properties for storage
    private string StatusType { get; set; }
    private string StatusData { get; set; }
    
    public OrderStatus Status
    {
        get => DeserializeStatus(StatusType, StatusData);
        set
        {
            (StatusType, StatusData) = SerializeStatus(value);
        }
    }
}

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Order>()
        .Property<string>("StatusType")
        .HasMaxLength(50);
        
    modelBuilder.Entity<Order>()
        .Property<string>("StatusData")
        .HasColumnType("nvarchar(max)");
        
    modelBuilder.Entity<Order>()
        .Ignore(o => o.Status);
}
```

## Migration Support

### Adding Union Column

```csharp
public partial class AddOrderStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Status",
            table: "Orders",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "{\"Type\":\"Pending\"}");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Status",
            table: "Orders");
    }
}
```

### Changing Union Structure

```csharp
// Migration to add new case
public partial class AddCancelledStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // No schema changes needed if using JSON storage
        // Old data remains compatible
    }
}
```

## Real-World Examples

### Order Management

```csharp
public class Order
{
    public int Id { get; set; }
    public OrderStatus Status { get; set; }
    public List<OrderStatusHistory> StatusHistory { get; set; } = new();
    
    public void UpdateStatus(OrderStatus newStatus)
    {
        StatusHistory.Add(new OrderStatusHistory
        {
            Status = Status,
            ChangedAt = DateTime.UtcNow
        });
        
        Status = newStatus;
    }
}

public class OrderStatusHistory
{
    public int Id { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime ChangedAt { get; set; }
}

// Query status history
var order = await context.Orders
    .Include(o => o.StatusHistory)
    .FirstAsync(o => o.Id == orderId);

var timeline = order.StatusHistory
    .Select(h => new
    {
        Status = h.Status.Match(
            pending => "Pending",
            processing => $"Processing by {processing.WorkerId}",
            shipped => $"Shipped: {shipped.TrackingNumber}",
            delivered => $"Delivered at {delivered.DeliveredAt}",
            cancelled => $"Cancelled: {cancelled.Reason}"
        ),
        Date = h.ChangedAt
    })
    .ToList();
```

### User Authentication

```csharp
[GenerateUnion]
public partial record AuthMethod
{
    public static partial AuthMethod Password(string hashedPassword, DateTime lastChanged);
    public static partial AuthMethod OAuth(string provider, string externalId);
    public static partial AuthMethod TwoFactor(string secret, List<string> backupCodes);
}

public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public AuthMethod AuthMethod { get; set; }
}

// Query users by auth method
var oauthUsers = await context.Users
    .ToListAsync();

var googleUsers = oauthUsers
    .Where(u => u.AuthMethod is AuthMethod.OAuthCase { Provider: "Google" })
    .ToList();
```

## Best Practices

### Indexing

Create indexes on discriminator for better query performance:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Add index on JSON property (PostgreSQL)
    modelBuilder.Entity<Order>()
        .HasIndex(o => new { })
        .HasMethod("gin")
        .HasFilter("Status -> 'Type'");
        
    // Or separate discriminator column
    modelBuilder.Entity<Order>()
        .Property<string>("StatusType")
        .HasMaxLength(50);
        
    modelBuilder.Entity<Order>()
        .HasIndex("StatusType");
}
```

### Validation

Validate unions before saving:

```csharp
public override int SaveChanges()
{
    var orders = ChangeTracker.Entries<Order>()
        .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
        .Select(e => e.Entity);

    foreach (var order in orders)
    {
        ValidateOrderStatus(order);
    }

    return base.SaveChanges();
}

private void ValidateOrderStatus(Order order)
{
    order.Status.Match(
        pending => { /* Always valid */ },
        processing => ValidateProcessing(processing),
        shipped => ValidateShipped(shipped),
        delivered => ValidateDelivered(delivered),
        cancelled => ValidateCancelled(cancelled)
    );
}
```

### Versioning

Handle schema evolution gracefully:

```csharp
private static OrderStatus Deserialize(string json)
{
    var doc = JsonDocument.Parse(json);
    var type = doc.RootElement.GetProperty("Type").GetString();
    
    // Handle old schema versions
    if (type == "InTransit") // Old case name
    {
        return OrderStatus.Shipped(
            doc.RootElement.GetProperty("TrackingNumber").GetString()!
        );
    }

    return type switch
    {
        "Pending" => OrderStatus.Pending(),
        "Shipped" => OrderStatus.Shipped(
            doc.RootElement.GetProperty("TrackingNumber").GetString()!
        ),
        // ... other cases
    };
}
```

## Limitations

### Query Limitations

```csharp
// ❌ Cannot query union case properties directly in SQL
var orders = await context.Orders
    .Where(o => o.Status.IsShipped) // Not translatable to SQL
    .ToListAsync();

// ✅ Load and filter in memory
var orders = await context.Orders.ToListAsync();
var shippedOrders = orders.Where(o => o.Status.IsShipped).ToList();

// ✅ Or use JSON query functions
var orders = await context.Orders
    .Where(o => EF.Property<string>(o.Status, "Type") == "Shipped")
    .ToListAsync();
```

## Key Takeaways

✅ **Value converters** enable union storage  
✅ **JSON columns** provide flexible storage  
✅ **Query in memory** for complex union operations  
✅ **Index discriminators** for better performance  
✅ **Handle versioning** for schema evolution
