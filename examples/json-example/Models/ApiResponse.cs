using UnionGenerator.Attributes;

namespace JsonExample.Models;

/// <summary>
/// Discriminated union representing an API response that can either succeed or fail.
/// </summary>
/// <typeparam name="T">The type of data on success.</typeparam>
[GenerateUnion]
public partial class ApiResponse<T>
{
    /// <summary>
    /// Creates a successful API response.
    /// </summary>
    /// <param name="data">The response data.</param>
    /// <returns>An ApiResponse representing successful operation.</returns>
    public static ApiResponse<T> Success(T data) => new SuccessCase(data);

    /// <summary>
    /// Creates a failed API response.
    /// </summary>
    /// <param name="error">The error information.</param>
    /// <returns>An ApiResponse representing failed operation.</returns>
    public static ApiResponse<T> Failed(ErrorInfo error) => new FailedCase(error);
}

/// <summary>
/// Represents error information in an API response.
/// </summary>
/// <param name="Code">Machine-readable error code.</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Details">Optional detailed information about the error.</param>
public record ErrorInfo(string Code, string Message, string? Details = null);

