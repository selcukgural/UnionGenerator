namespace FluentValidationExample.Models;

/// <summary>
/// DTO for creating a new user.
/// </summary>
public class CreateUserDto
{
    /// <summary>
    /// User's first name.
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// User's last name.
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// User's email address (must be unique).
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// User's age (must be 18 or older).
    /// </summary>
    public int Age { get; set; }

    /// <summary>
    /// User's username (must be unique and 3-20 characters).
    /// </summary>
    public required string Username { get; set; }
}

/// <summary>
/// Represents a user in the system.
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier for the user.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User's first name.
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// User's last name.
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// User's full name (computed from first and last name).
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// User's email address.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// User's age.
    /// </summary>
    public int Age { get; set; }

    /// <summary>
    /// User's username.
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// When this user was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

