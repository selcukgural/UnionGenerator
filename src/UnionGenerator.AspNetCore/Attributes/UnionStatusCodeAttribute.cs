namespace UnionGenerator.AspNetCore.Attributes;

/// <summary>
/// Specifies the HTTP status code to use when a union case is active in the response.
/// </summary>
/// <remarks>
/// <para>
/// This attribute can be applied to:
/// - Union static factory methods to indicate the HTTP status code that should be returned when that case is active.
/// - Error type classes to explicitly declare their HTTP error status code.
/// This enables automatic status code selection in filters and extensions.
/// </para>
/// <para>
/// When used with success cases, this allows customization beyond the default 200 OK
/// (e.g., 201 Created, 204 No Content). When used with error cases, this can specify
/// the appropriate HTTP error status (e.g., 404 Not Found, 409 Conflict).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // On factory methods
/// [GenerateUnion]
/// public partial class Result&lt;T, E&gt;
/// {
///     [UnionStatusCode(201)] // Created
///     public static Result&lt;T, E&gt; Created(T value) => new CreatedCase(value);
///     
///     [UnionStatusCode(404)] // Not Found
///     public static Result&lt;T, E&gt; NotFound(E error) => new NotFoundCase(error);
/// }
/// 
/// // On error types
/// [UnionStatusCode(404)]
/// public class NotFoundError { }
/// 
/// [UnionStatusCode(409)]
/// public class ConflictError { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
public sealed class UnionStatusCodeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnionStatusCodeAttribute"/> class.
    /// </summary>
    /// <param name="statusCode">
    /// The HTTP status code to use when this union case is active.
    /// Must be a valid HTTP status code (100-599).
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when statusCode is not a valid HTTP status code.
    /// </exception>
    public UnionStatusCodeAttribute(int statusCode)
    {
        if (statusCode is < 100 or > 599)
        {
            throw new ArgumentOutOfRangeException(
                nameof(statusCode),
                statusCode,
                "Status code must be a valid HTTP status code (100-599).");
        }

        StatusCode = statusCode;
    }

    /// <summary>
    /// Gets the HTTP status code for this union case.
    /// </summary>
    public int StatusCode { get; }
}

