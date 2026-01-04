using Microsoft.EntityFrameworkCore;
using UnionGenerator.EntityFrameworkCore.Extensions;

namespace UnionGenerator.EntityFrameworkCore.Tests;

/// <summary>
/// Test entity with Result property.
/// </summary>
public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public TestResult ProcessingResult { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Test DbContext.
/// </summary>
public class TestDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; } = null!;

    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .HasResultConversion<Order, TestResult, int, string>(
                o => o.ProcessingResult
            );
    }
}

/// <summary>
/// Integration tests for EF Core Result value converter.
/// </summary>
public class ResultValueConverterIntegrationTests : IDisposable
{
    private readonly TestDbContext _dbContext;

    public ResultValueConverterIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task SaveAndRetrieve_OkCase_PreservesValue()
    {
        // Arrange
        var order = new Order
        {
            CustomerName = "John Doe",
            ProcessingResult = TestResult.Ok(42),
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        var retrieved = await _dbContext.Orders.FindAsync(order.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("John Doe", retrieved.CustomerName);
        Assert.IsType<TestResult.OkCase>(retrieved.ProcessingResult);
        var okCase = (TestResult.OkCase)retrieved.ProcessingResult;
        Assert.Equal(42, okCase.Value);
    }

    [Fact]
    public async Task SaveAndRetrieve_ErrorCase_PreservesValue()
    {
        // Arrange
        var order = new Order
        {
            CustomerName = "Jane Smith",
            ProcessingResult = TestResult.Error("Payment failed"),
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        var retrieved = await _dbContext.Orders.FindAsync(order.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("Jane Smith", retrieved.CustomerName);
        Assert.IsType<TestResult.ErrorCase>(retrieved.ProcessingResult);
        var errorCase = (TestResult.ErrorCase)retrieved.ProcessingResult;
        Assert.Equal("Payment failed", errorCase.Value);
    }

    [Fact]
    public async Task Query_MultipleOrders_ReturnsAll()
    {
        // Arrange
        var orders = new[]
        {
            new Order
            {
                CustomerName = "Customer 1",
                ProcessingResult = TestResult.Ok(100),
                CreatedAt = DateTime.UtcNow
            },
            new Order
            {
                CustomerName = "Customer 2",
                ProcessingResult = TestResult.Error("Error 1"),
                CreatedAt = DateTime.UtcNow
            },
            new Order
            {
                CustomerName = "Customer 3",
                ProcessingResult = TestResult.Ok(200),
                CreatedAt = DateTime.UtcNow
            }
        };

        _dbContext.Orders.AddRange(orders);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        // Act
        var retrieved = await _dbContext.Orders.ToListAsync();

        // Assert
        Assert.Equal(3, retrieved.Count);
        Assert.Equal(2, retrieved.Count(o => o.ProcessingResult is TestResult.OkCase));
        Assert.Single(retrieved.Where(o => o.ProcessingResult is TestResult.ErrorCase));
    }

    [Fact]
    public async Task Update_ResultProperty_SavesCorrectly()
    {
        // Arrange
        var order = new Order
        {
            CustomerName = "Test Customer",
            ProcessingResult = TestResult.Ok(50),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        // Act
        var retrieved = await _dbContext.Orders.FindAsync(order.Id);
        Assert.NotNull(retrieved);
        
        retrieved.ProcessingResult = TestResult.Error("Updated error");
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        var updated = await _dbContext.Orders.FindAsync(order.Id);

        // Assert
        Assert.NotNull(updated);
        Assert.IsType<TestResult.ErrorCase>(updated.ProcessingResult);
        var errorCase = (TestResult.ErrorCase)updated.ProcessingResult;
        Assert.Equal("Updated error", errorCase.Value);
    }

    [Fact]
    public async Task Query_WithWhere_WorksCorrectly()
    {
        // Arrange
        var orders = new[]
        {
            new Order
            {
                CustomerName = "Alice",
                ProcessingResult = TestResult.Ok(100),
                CreatedAt = DateTime.UtcNow
            },
            new Order
            {
                CustomerName = "Bob",
                ProcessingResult = TestResult.Error("Error"),
                CreatedAt = DateTime.UtcNow
            },
            new Order
            {
                CustomerName = "Charlie",
                ProcessingResult = TestResult.Ok(200),
                CreatedAt = DateTime.UtcNow
            }
        };

        _dbContext.Orders.AddRange(orders);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        // Act
        var aliceOrders = await _dbContext.Orders
            .Where(o => o.CustomerName == "Alice")
            .ToListAsync();

        // Assert
        Assert.Single(aliceOrders);
        Assert.Equal("Alice", aliceOrders[0].CustomerName);
    }

    [Fact]
    public async Task Delete_Order_RemovesFromDatabase()
    {
        // Arrange
        var order = new Order
        {
            CustomerName = "To Delete",
            ProcessingResult = TestResult.Ok(123),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        var orderId = order.Id;

        _dbContext.ChangeTracker.Clear();

        // Act
        var toDelete = await _dbContext.Orders.FindAsync(orderId);
        Assert.NotNull(toDelete);
        
        _dbContext.Orders.Remove(toDelete);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        var deleted = await _dbContext.Orders.FindAsync(orderId);

        // Assert
        Assert.Null(deleted);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}

