using AspNetCoreExample.Models;
using UnionGenerator.AspNetCore;

namespace AspNetCoreExample.Services;

/// <summary>
/// Service interface for user management operations.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets a user by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <returns>
    /// A result containing the user if found, or a ProblemDetailsError if not found or an error occurred.
    /// </returns>
    Result<User, ProblemDetailsError> GetUser(int id);

    /// <summary>
    /// Gets all users.
    /// </summary>
    /// <returns>A result containing the list of all users.</returns>
    Result<IReadOnlyList<User>, ProblemDetailsError> GetAllUsers();

    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="dto">The user creation data.</param>
    /// <param name="requestPath">The request path for error reporting.</param>
    /// <returns>
    /// A result containing the created user, or a ProblemDetailsError if creation failed.
    /// </returns>
    Result<User, ProblemDetailsError> CreateUser(CreateUserDto dto, string requestPath);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="id">The unique identifier of the user to update.</param>
    /// <param name="dto">The user update data.</param>
    /// <param name="requestPath">The request path for error reporting.</param>
    /// <returns>
    /// A result containing the updated user, or a ProblemDetailsError if update failed.
    /// </returns>
    Result<User, ProblemDetailsError> UpdateUser(int id, UpdateUserDto dto, string requestPath);

    /// <summary>
    /// Deletes a user.
    /// </summary>
    /// <param name="id">The unique identifier of the user to delete.</param>
    /// <param name="requestPath">The request path for error reporting.</param>
    /// <returns>
    /// A result indicating success, or a ProblemDetailsError if deletion failed.
    /// </returns>
    Result<bool, ProblemDetailsError> DeleteUser(int id, string requestPath);
}

/// <summary>
/// In-memory implementation of a user service for demonstration purposes.
/// </summary>
/// <remarks>
/// This is a simple in-memory implementation for demonstration. In production,
/// this would interact with a database through a repository pattern.
/// </remarks>
public sealed class UserService : IUserService
{
    private readonly List<User> _users =
    [
        new()
            { Id = 1, Name = "John Doe", Email = "john@example.com", Age = 30 },
        new()
            { Id = 2, Name = "Jane Smith", Email = "jane@example.com", Age = 25 },
        new()
            { Id = 3, Name = "Bob Johnson", Email = "bob@example.com", Age = 35 }
    ];

    private int _nextId = 4;

    /// <inheritdoc/>
    public Result<User, ProblemDetailsError> GetUser(int id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);

        if (user == null)
        {
            return Result<User, ProblemDetailsError>.Error(
                ProblemDetailsErrorFactory.NotFound(
                    instance: $"/api/users/{id}",
                    detail: $"User with ID {id} was not found.",
                    resourceType: "User"
                )
            );
        }

        return Result<User, ProblemDetailsError>.Ok(user);
    }

    /// <inheritdoc/>
    public Result<IReadOnlyList<User>, ProblemDetailsError> GetAllUsers()
    {
        return Result<IReadOnlyList<User>, ProblemDetailsError>.Ok(_users.AsReadOnly());
    }

    /// <inheritdoc/>
    public Result<User, ProblemDetailsError> CreateUser(CreateUserDto dto, string requestPath)
    {
        // Check for duplicate email
        if (_users.Any(u => u.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase)))
        {
            return Result<User, ProblemDetailsError>.Error(
                ProblemDetailsErrorFactory.Conflict(
                    instance: requestPath,
                    detail: $"A user with email '{dto.Email}' already exists."
                )
            );
        }

        // Validate age
        if (dto.Age < 18)
        {
            var errors = new Dictionary<string, string[]>
            {
                [nameof(dto.Age)] = ["Age must be at least 18."]
            };

            return Result<User, ProblemDetailsError>.Error(
                ProblemDetailsErrorFactory.Validation(errors, requestPath)
            );
        }

        var user = new User
        {
            Id = _nextId++,
            Name = dto.Name,
            Email = dto.Email,
            Age = dto.Age
        };

        _users.Add(user);

        return Result<User, ProblemDetailsError>.Ok(user);
    }

    /// <inheritdoc/>
    public Result<User, ProblemDetailsError> UpdateUser(int id, UpdateUserDto dto, string requestPath)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);

        if (user == null)
        {
            return Result<User, ProblemDetailsError>.Error(
                ProblemDetailsErrorFactory.NotFound(
                    instance: requestPath,
                    detail: $"User with ID {id} was not found.",
                    resourceType: "User"
                )
            );
        }

        // Check for duplicate email if the email is being updated
        if (dto.Email != null && !dto.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (_users.Any(u => u.Id != id && u.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase)))
            {
                return Result<User, ProblemDetailsError>.Error(
                    ProblemDetailsErrorFactory.Conflict(
                        instance: requestPath,
                        detail: $"A user with email '{dto.Email}' already exists."
                    )
                );
            }
        }

        // Validate age if provided
        if (dto.Age is < 18)
        {
            var errors = new Dictionary<string, string[]>
            {
                [nameof(dto.Age)] = ["Age must be at least 18."]
            };

            return Result<User, ProblemDetailsError>.Error(
                ProblemDetailsErrorFactory.Validation(errors, requestPath)
            );
        }

        var updatedUser = user with
        {
            Name = dto.Name ?? user.Name,
            Email = dto.Email ?? user.Email,
            Age = dto.Age ?? user.Age
        };

        var index = _users.FindIndex(u => u.Id == id);
        _users[index] = updatedUser;

        return Result<User, ProblemDetailsError>.Ok(updatedUser);
    }

    /// <inheritdoc/>
    public Result<bool, ProblemDetailsError> DeleteUser(int id, string requestPath)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);

        if (user == null)
        {
            return Result<bool, ProblemDetailsError>.Error(
                ProblemDetailsErrorFactory.NotFound(
                    instance: requestPath,
                    detail: $"User with ID {id} was not found.",
                    resourceType: "User"
                )
            );
        }

        _users.Remove(user);

        return Result<bool, ProblemDetailsError>.Ok(true);
    }
}

