using EntityFrameworkExample.Models;
using Microsoft.EntityFrameworkCore;
using UnionGenerator.EntityFrameworkCore.Extensions;

namespace EntityFrameworkExample.Data;

/// <summary>
/// Entity Framework Core DbContext for managing orders with result tracking.
/// Configures automatic JSON conversion for the ProcessingResult union type.
/// </summary>
public class OrderDbContext : DbContext
{
    /// <summary>
    /// DbSet for orders.
    /// </summary>
    public DbSet<Order> Orders { get; set; } = null!;

    /// <summary>
    /// Initializes a new instance of the OrderDbContext.
    /// </summary>
    /// <param name="options">Configuration options for the DbContext.</param>
    public OrderDbContext(DbContextOptions<OrderDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Configures model mappings and value conversions.
    /// Enables automatic JSON serialization/deserialization of the ProcessingResult union type.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure entities.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure the Order entity
        modelBuilder.Entity<Order>(entity =>
        {
            // Set primary key
            entity.HasKey(o => o.Id);

            // Configure properties
            entity.Property(o => o.CustomerName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(o => o.OrderNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            entity.Property(o => o.CreatedAt);
            entity.Property(o => o.UpdatedAt);

            // Configure the ProcessingResult union type for JSON conversion
            // This automatically converts the union to/from JSON when saving/loading from database
            entity.HasResultConversion<Order, ProcessingResult, ProcessedData, ErrorInfo>(
                o => o.ProcessingResult
            );

            // Optional: Create an index on CustomerName for common queries
            entity.HasIndex(o => o.CustomerName);
            entity.HasIndex(o => o.OrderNumber).IsUnique();
        });
    }
}

