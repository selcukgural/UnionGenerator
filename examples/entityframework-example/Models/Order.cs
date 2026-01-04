namespace EntityFrameworkExample.Models;

/// <summary>
/// Represents a customer order with processing result stored in the database.
/// The ProcessingResult is persisted as JSON in a database column.
/// </summary>
public class Order
{
    /// <summary>
    /// Unique identifier for the order.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the customer who placed the order.
    /// </summary>
    public required string CustomerName { get; set; }

    /// <summary>
    /// Order number for reference and tracking.
    /// </summary>
    public required string OrderNumber { get; set; }

    /// <summary>
    /// Total amount for this order.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Result of processing this order.
    /// Stored as JSON in the database and automatically converted to/from ProcessingResult union type.
    /// </summary>
    public required ProcessingResult ProcessingResult { get; set; }

    /// <summary>
    /// When this order was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this order was last modified.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

