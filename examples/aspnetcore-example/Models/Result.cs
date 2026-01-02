using UnionGenerator.Attributes;

namespace AspNetCoreExample.Models;

/// <summary>
/// Represents a result that can be either a success value or an error.
/// </summary>
/// <typeparam name="TSuccess">The type of the success value.</typeparam>
/// <typeparam name="TError">The type of the error value.</typeparam>
[GenerateUnion]
public partial class Result<TSuccess, TError>
{
    /// <summary>
    /// Creates a successful result with the given value.
    /// </summary>
    public static Result<TSuccess, TError> Ok(TSuccess value) => new OkCase(value);

    /// <summary>
    /// Creates an error result with the given error.
    /// </summary>
    public static Result<TSuccess, TError> Error(TError error) => new ErrorCase(error);
}

