namespace FluentValidationExample.Models;

/// <summary>
/// DTO for creating a new product.
/// </summary>
public class CreateProductDto
{
    /// <summary>
    /// Product name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Product description.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Product price (must be positive).
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Stock quantity (must be non-negative).
    /// </summary>
    public int Stock { get; set; }

    /// <summary>
    /// Product SKU (unique identifier, required).
    /// </summary>
    public required string Sku { get; set; }
}

/// <summary>
/// Represents a product in the system.
/// </summary>
public class Product
{
    /// <summary>
    /// Unique identifier for the product.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Product name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Product description.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Product price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Current stock quantity.
    /// </summary>
    public int Stock { get; set; }

    /// <summary>
    /// Product SKU.
    /// </summary>
    public required string Sku { get; set; }

    /// <summary>
    /// When this product was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

