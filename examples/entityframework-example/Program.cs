using EntityFrameworkExample.Data;
using EntityFrameworkExample.Models;
using Microsoft.EntityFrameworkCore;

// Create database context with in-memory database
var options = new DbContextOptionsBuilder<OrderDbContext>()
    .UseInMemoryDatabase("OrdersExample")
    .Options;

await using var dbContext = new OrderDbContext(options);

// Ensure database is created
await dbContext.Database.EnsureCreatedAsync();

Console.WriteLine("=== UnionGenerator + Entity Framework Core Example ===\n");

// Example 1: Create orders with success results
Console.WriteLine("1. Creating orders with successful processing results...");
var successOrder = new Order
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

dbContext.Orders.Add(successOrder);
await dbContext.SaveChangesAsync();
Console.WriteLine($"   ✓ Order '{successOrder.OrderNumber}' saved with success result\n");

// Example 2: Create orders with error results
Console.WriteLine("2. Creating orders with error processing results...");
var errorOrder = new Order
{
    CustomerName = "Bob Smith",
    OrderNumber = "ORD-002",
    TotalAmount = 150.00m,
    ProcessingResult = ProcessingResult.Failed(new ErrorInfo(
        Code: "PAYMENT_DECLINED",
        Message: "Payment method was declined",
        Details: "Card expired"
    )),
    CreatedAt = DateTime.UtcNow
};

dbContext.Orders.Add(errorOrder);
await dbContext.SaveChangesAsync();
Console.WriteLine($"   ✓ Order '{errorOrder.OrderNumber}' saved with error result\n");

// Example 3: Query and display all orders
Console.WriteLine("3. Querying all orders from database...");
var allOrders = await dbContext.Orders.ToListAsync();

foreach (var order in allOrders)
{
    Console.WriteLine($"   Order: {order.OrderNumber} | Customer: {order.CustomerName} | Amount: ${order.TotalAmount:F2}");
    
    order.ProcessingResult.Match(
        success: data => Console.WriteLine($"     ✓ Status: SUCCESS | Message: {data.Message}"),
        failed: error => Console.WriteLine($"     ✗ Status: FAILED | Code: {error.Code} | Details: {error.Details}")
    );
}
Console.WriteLine();

// Example 4: Update an order result
Console.WriteLine("4. Updating an order with new result...");
var orderToUpdate = allOrders.First();
orderToUpdate.ProcessingResult = ProcessingResult.Success(new ProcessedData(
    ProcessedId: Guid.NewGuid(),
    Message: "Order reprocessed successfully after retry",
    Timestamp: DateTime.UtcNow
));

dbContext.Orders.Update(orderToUpdate);
await dbContext.SaveChangesAsync();
Console.WriteLine($"   ✓ Order '{orderToUpdate.OrderNumber}' updated with new success result\n");

// Example 5: Pattern matching with database results
Console.WriteLine("5. Pattern matching on database results...");
var refreshedOrders = await dbContext.Orders.ToListAsync();

foreach (var order in refreshedOrders)
{
    var status = order.ProcessingResult.Match(
        success: _ => "✓ Processed",
        failed: error => $"✗ Error: {error.Code}"
    );
    Console.WriteLine($"   {order.OrderNumber}: {status}");
}
Console.WriteLine();

// Example 6: Filtering orders by result type
Console.WriteLine("6. Filtering: Finding orders with successful processing...");
var successfulOrders = refreshedOrders
    .Where(o => o.ProcessingResult is ProcessingResult.SuccessCase)
    .ToList();

Console.WriteLine($"   Found {successfulOrders.Count} successful order(s):");
foreach (var order in successfulOrders)
{
    var successData = (ProcessingResult.SuccessCase)order.ProcessingResult;
    Console.WriteLine($"     - {order.OrderNumber}: {successData.Value.Message}");
}
Console.WriteLine();

// Example 7: Demonstrate result serialization
Console.WriteLine("7. JSON Serialization of union results...");
var sampleOrder = refreshedOrders.First();
var json = System.Text.Json.JsonSerializer.Serialize(
    sampleOrder.ProcessingResult,
    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
);
Console.WriteLine($"   Serialized result:\n{json}\n");

Console.WriteLine("=== Example completed successfully! ===");

