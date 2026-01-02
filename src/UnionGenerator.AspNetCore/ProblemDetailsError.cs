namespace UnionGenerator.AspNetCore;

/// <summary>
/// Represents a standardized error structure compatible with RFC 7807 Problem Details specification.
/// This type is designed to be used as the error case in Result unions for ASP.NET Core applications.
/// </summary>
/// <remarks>
/// <para>
/// This error model follows the Problem Details for HTTP APIs specification (RFC 7807) and includes
/// all required fields plus optional extensions for validation errors.
/// </para>
/// <para>
/// Thread-safety: This type is immutable after construction and is safe for concurrent access.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var error = new ProblemDetailsError(
///     type: "https://example.com/errors/validation",
///     title: "Validation Failed",
///     status: 400,
///     detail: "One or more validation errors occurred.",
///     instance: "/api/users/123"
/// );
/// 
/// // With validation errors
/// var validationError = error with 
/// {
///     Errors = new Dictionary&lt;string, string[]&gt;
///     {
///         ["Email"] = new[] { "Email is required.", "Email must be valid." },
///         ["Age"] = new[] { "Age must be at least 18." }
///     }
/// };
/// </code>
/// </example>
public sealed record ProblemDetailsError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProblemDetailsError"/> class.
    /// </summary>
    /// <param name="type">
    /// A URI reference that identifies the problem type. Should be a stable, dereferenceable URI
    /// that provides human-readable documentation for the problem type.
    /// </param>
    /// <param name="title">
    /// A short, human-readable summary of the problem type. Should not change from occurrence to occurrence.
    /// </param>
    /// <param name="status">
    /// The HTTP status code for this occurrence of the problem.
    /// Must be a valid HTTP status code (100-599).
    /// </param>
    /// <param name="detail">
    /// A human-readable explanation specific to this occurrence of the problem.
    /// Should provide enough context for the client to understand what went wrong.
    /// </param>
    /// <param name="instance">
    /// A URI reference that identifies the specific occurrence of the problem.
    /// Typically, the request path that generated the error.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when type, title, or instance is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when status is not a valid HTTP status code.</exception>
    public ProblemDetailsError(
        string type,
        string title,
        int status,
        string? detail,
        string instance)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Type cannot be null or whitespace.", nameof(type));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be null or whitespace.", nameof(title));
        }

        if (status is < 100 or > 599)
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Status must be a valid HTTP status code (100-599).");
        }

        if (string.IsNullOrWhiteSpace(instance))
        {
            throw new ArgumentException("Instance cannot be null or whitespace.", nameof(instance));
        }

        Type = type;
        Title = title;
        Status = status;
        Detail = detail ?? string.Empty;
        Instance = instance;
    }

    /// <summary>
    /// Gets a URI reference that identifies the problem type.
    /// </summary>
    /// <value>
    /// A stable URI that identifies the problem type and ideally provides human-readable documentation.
    /// </value>
    public string Type { get; init; }

    /// <summary>
    /// Gets a short, human-readable summary of the problem type.
    /// </summary>
    /// <value>
    /// A summary that describes the class of problem. This should not change between occurrences.
    /// </value>
    public string Title { get; init; }

    /// <summary>
    /// Gets the HTTP status code for this problem.
    /// </summary>
    /// <value>
    /// A valid HTTP status code (100-599) that represents the error category.
    /// </value>
    public int Status { get; init; }

    /// <summary>
    /// Gets a human-readable explanation specific to this occurrence.
    /// </summary>
    /// <value>
    /// Detailed information about what went wrong in this specific case.
    /// </value>
    public string Detail { get; init; }

    /// <summary>
    /// Gets a URI reference identifying this specific problem occurrence.
    /// </summary>
    /// <value>
    /// Typically the request path or a unique identifier for this error occurrence.
    /// </value>
    public string Instance { get; init; }

    /// <summary>
    /// Gets the validation errors dictionary for validation failure scenarios.
    /// </summary>
    /// <value>
    /// A dictionary mapping field names to arrays of error messages for that field.
    /// Null if this is not a validation error.
    /// </value>
    /// <remarks>
    /// This property follows the ASP.NET Core ValidationProblemDetails convention where
    /// each key is a field name and each value is an array of error messages for that field.
    /// </remarks>
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    /// <summary>
    /// Gets additional extension members for the problem details.
    /// </summary>
    /// <value>
    /// Optional extension data that provides additional context. Null if no extensions are present.
    /// </value>
    /// <remarks>
    /// This allows for custom extensions to the Problem Details format while maintaining
    /// compatibility with the RFC 7807 specification.
    /// </remarks>
    public IReadOnlyDictionary<string, object?>? Extensions { get; init; }
}

