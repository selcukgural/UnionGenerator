namespace OneOfExample.Models;

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
/// Represents an error response.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Machine-readable error code.
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public required string Message { get; set; }
}

