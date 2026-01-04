using UnionGenerator.Attributes;

namespace FluentValidationExample.Models;

/// <summary>
/// Represents the result of a user creation attempt.
/// Either succeeds with created User or fails with validation errors.
/// </summary>
[GenerateUnion]
public partial class UserCreationResult
{
    /// <summary>
    /// Creates a successful user creation result.
    /// </summary>
    /// <param name="user">The created user.</param>
    /// <returns>A UserCreationResult representing successful creation.</returns>
    public static UserCreationResult Success(User user) => new SuccessCase(user);

    /// <summary>
    /// Creates a failed user creation result.
    /// </summary>
    /// <param name="errors">Dictionary of field names to error messages.</param>
    /// <returns>A UserCreationResult representing failed creation.</returns>
    public static UserCreationResult Failed(Dictionary<string, string[]> errors) => new FailedCase(errors);
}

