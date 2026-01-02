namespace AspNetCoreExample.Models;

/// <summary>
/// Represents a user entity.
/// </summary>
public sealed record User
{
    /// <summary>
    /// Gets or initializes the unique identifier of the user.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets or initializes the name of the user.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or initializes the email address of the user.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets or initializes the age of the user.
    /// </summary>
    public int Age { get; init; }
}

/// <summary>
/// Data transfer object for creating a new user.
/// </summary>
public sealed record CreateUserDto
{
    /// <summary>
    /// Gets or initializes the name of the user.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or initializes the email address of the user.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets or initializes the age of the user.
    /// </summary>
    public int Age { get; init; }
}

/// <summary>
/// Data transfer object for updating an existing user.
/// </summary>
public sealed record UpdateUserDto
{
    /// <summary>
    /// Gets or initializes the name of the user.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or initializes the email address of the user.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Gets or initializes the age of the user.
    /// </summary>
    public int? Age { get; init; }
}

