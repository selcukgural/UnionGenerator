using UnionGenerator.Attributes;

namespace OneOfExample.Models;

/// <summary>
/// Discriminated union representing a Result that can be Ok or Error.
/// Used for comparison with OneOf in this example.
/// </summary>
[GenerateUnion]
public partial class Result<T, E>
{
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="value">The success value.</param>
    /// <returns>A Result representing success.</returns>
    public static Result<T, E> Ok(T value) => new OkCase(value);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The error value.</param>
    /// <returns>A Result representing failure.</returns>
    public static Result<T, E> Error(E error) => new ErrorCase(error);
}

