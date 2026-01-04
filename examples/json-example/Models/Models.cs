namespace JsonExample.Models;

/// <summary>
/// Represents a user in the system.
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User's name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// User's email address.
    /// </summary>
    public required string Email { get; set; }
}

/// <summary>
/// Represents a product.
/// </summary>
public class Product
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Product name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Product price.
    /// </summary>
    public decimal Price { get; set; }
}

