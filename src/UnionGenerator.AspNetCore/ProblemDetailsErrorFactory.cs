namespace UnionGenerator.AspNetCore;

/// <summary>
/// Provides factory methods for creating common <see cref="ProblemDetailsError"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// This factory provides consistent error creation with standardized types and status codes
/// following REST API best practices and RFC 7807 Problem Details specification.
/// </para>
/// <para>
/// All error types use URIs under "https://tools.ietf.org/html/rfc7231" for standard HTTP errors,
/// and "about:blank" for generic errors without specific documentation.
/// </para>
/// </remarks>
public static class ProblemDetailsErrorFactory
{
    /// <summary>
    /// Creates a validation error with structured field-level error messages.
    /// </summary>
    /// <param name="errors">
    /// Dictionary mapping field names to arrays of validation error messages.
    /// </param>
    /// <param name="instance">
    /// The request path or identifier where the validation error occurred.
    /// </param>
    /// <param name="detail">
    /// Optional custom detail message. If null, a default message is used.
    /// </param>
    /// <returns>A <see cref="ProblemDetailsError"/> configured for validation failures (HTTP 400).</returns>
    /// <exception cref="ArgumentNullException">Thrown when errors or instance is null.</exception>
    /// <exception cref="ArgumentException">Thrown when errors dictionary is empty.</exception>
    /// <example>
    /// <code>
    /// var errors = new Dictionary&lt;string, string[]&gt;
    /// {
    ///     ["Email"] = new[] { "Email is required." },
    ///     ["Age"] = new[] { "Must be at least 18." }
    /// };
    /// var error = ProblemDetailsErrorFactory.Validation(errors, "/api/users");
    /// </code>
    /// </example>
    public static ProblemDetailsError Validation(
        IReadOnlyDictionary<string, string[]> errors,
        string instance,
        string? detail = null)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Count == 0)
        {
            throw new ArgumentException("Errors dictionary cannot be empty.", nameof(errors));
        }

        return new ProblemDetailsError(
            type: "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            title: "One or more validation errors occurred.",
            status: 400,
            detail: detail ?? "The request contains invalid data. Please check the errors and try again.",
            instance: instance)
        {
            Errors = errors
        };
    }

    /// <summary>
    /// Creates a not found error (HTTP 404).
    /// </summary>
    /// <param name="instance">The request path where the resource was not found.</param>
    /// <param name="detail">Specific detail about what was not found.</param>
    /// <param name="resourceType">Optional resource type for more context (e.g., "User", "Product").</param>
    /// <returns>A <see cref="ProblemDetailsError"/> configured for not found scenarios.</returns>
    /// <exception cref="ArgumentException">Thrown when instance or detail is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// var error = ProblemDetailsErrorFactory.NotFound(
    ///     "/api/users/123",
    ///     "User with ID 123 was not found.",
    ///     "User"
    /// );
    /// </code>
    /// </example>
    public static ProblemDetailsError NotFound(
        string instance,
        string detail,
        string? resourceType = null)
    {
        var title = resourceType != null
            ? $"{resourceType} not found."
            : "The requested resource was not found.";

        return new ProblemDetailsError(
            type: "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            title: title,
            status: 404,
            detail: detail,
            instance: instance);
    }

    /// <summary>
    /// Creates a conflict error (HTTP 409) for situations where the request conflicts with current state.
    /// </summary>
    /// <param name="instance">The request path where the conflict occurred.</param>
    /// <param name="detail">Specific detail about the conflict.</param>
    /// <returns>A <see cref="ProblemDetailsError"/> configured for conflict scenarios.</returns>
    /// <exception cref="ArgumentException">Thrown when instance or detail is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// var error = ProblemDetailsErrorFactory.Conflict(
    ///     "/api/users",
    ///     "A user with this email already exists."
    /// );
    /// </code>
    /// </example>
    public static ProblemDetailsError Conflict(string instance, string detail)
    {
        return new ProblemDetailsError(
            type: "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            title: "A conflict occurred.",
            status: 409,
            detail: detail,
            instance: instance);
    }

    /// <summary>
    /// Creates an unauthorized error (HTTP 401) for authentication failures.
    /// </summary>
    /// <param name="instance">The request path where authentication failed.</param>
    /// <param name="detail">
    /// Optional specific detail about the authentication failure.
    /// If null, a default message is used.
    /// </param>
    /// <returns>A <see cref="ProblemDetailsError"/> configured for authentication failures.</returns>
    /// <exception cref="ArgumentException">Thrown when the instance is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// var error = ProblemDetailsErrorFactory.Unauthorized(
    ///     "/api/protected-resource",
    ///     "Invalid or expired authentication token."
    /// );
    /// </code>
    /// </example>
    public static ProblemDetailsError Unauthorized(
        string instance,
        string? detail = null)
    {
        return new ProblemDetailsError(
            type: "https://tools.ietf.org/html/rfc7235#section-3.1",
            title: "Unauthorized.",
            status: 401,
            detail: detail ?? "Authentication is required to access this resource.",
            instance: instance);
    }

    /// <summary>
    /// Creates a forbidden error (HTTP 403) for authorization failures.
    /// </summary>
    /// <param name="instance">The request path where authorization failed.</param>
    /// <param name="detail">
    /// Optional specific detail about the authorization failure.
    /// If null, a default message is used.
    /// </param>
    /// <returns>A <see cref="ProblemDetailsError"/> configured for authorization failures.</returns>
    /// <exception cref="ArgumentException">Thrown when the instance is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// var error = ProblemDetailsErrorFactory.Forbidden(
    ///     "/api/admin",
    ///     "You do not have permission to access this resource."
    /// );
    /// </code>
    /// </example>
    public static ProblemDetailsError Forbidden(
        string instance,
        string? detail = null)
    {
        return new ProblemDetailsError(
            type: "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            title: "Forbidden.",
            status: 403,
            detail: detail ?? "You do not have permission to access this resource.",
            instance: instance);
    }

    /// <summary>
    /// Creates a bad request error (HTTP 400) for general invalid request scenarios.
    /// </summary>
    /// <param name="instance">The request path where the bad request occurred.</param>
    /// <param name="detail">Specific detail about what makes the request invalid.</param>
    /// <returns>A <see cref="ProblemDetailsError"/> configured for bad request scenarios.</returns>
    /// <exception cref="ArgumentException">Thrown when an instance or detail is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// var error = ProblemDetailsErrorFactory.BadRequest(
    ///     "/api/users",
    ///     "Request body is missing required fields."
    /// );
    /// </code>
    /// </example>
    public static ProblemDetailsError BadRequest(string instance, string detail)
    {
        return new ProblemDetailsError(
            type: "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            title: "Bad Request.",
            status: 400,
            detail: detail,
            instance: instance);
    }

    /// <summary>
    /// Creates an internal server error (HTTP 500) for unexpected failures.
    /// </summary>
    /// <param name="instance">The request path where the error occurred.</param>
    /// <param name="detail">
    /// Optional specific detail about the error. Should not expose sensitive information.
    /// If null, a default generic message is used.
    /// </param>
    /// <returns>A <see cref="ProblemDetailsError"/> configured for server errors.</returns>
    /// <exception cref="ArgumentException">Thrown when the instance is null or whitespace.</exception>
    /// <remarks>
    /// Be cautious not to expose sensitive system information in the detail message.
    /// Use logging for detailed error information instead.
    /// </remarks>
    /// <example>
    /// <code>
    /// var error = ProblemDetailsErrorFactory.InternalServerError(
    ///     "/api/users/123"
    /// );
    /// </code>
    /// </example>
    public static ProblemDetailsError InternalServerError(
        string instance,
        string? detail = null)
    {
        return new ProblemDetailsError(
            type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title: "An internal server error occurred.",
            status: 500,
            detail: detail ?? "An unexpected error occurred while processing your request. Please try again later.",
            instance: instance);
    }

    /// <summary>
    /// Creates a custom error with specified status code and messages.
    /// </summary>
    /// <param name="status">The HTTP status code for this error.</param>
    /// <param name="title">The error title (summary of the problem type).</param>
    /// <param name="detail">Specific detail about this error occurrence.</param>
    /// <param name="instance">The request path where the error occurred.</param>
    /// <param name="type">
    /// Optional URI reference identifying the problem type.
    /// If null, "about:blank" is used.
    /// </param>
    /// <returns>A <see cref="ProblemDetailsError"/> with custom configuration.</returns>
    /// <exception cref="ArgumentException">Thrown when required parameters are null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when status is not a valid HTTP status code.</exception>
    /// <example>
    /// <code>
    /// var error = ProblemDetailsErrorFactory.Custom(
    ///     status: 429,
    ///     title: "Too Many Requests",
    ///     detail: "Rate limit exceeded. Please try again in 60 seconds.",
    ///     instance: "/api/search",
    ///     type: "https://example.com/errors/rate-limit"
    /// );
    /// </code>
    /// </example>
    public static ProblemDetailsError Custom(
        int status,
        string title,
        string detail,
        string instance,
        string? type = null)
    {
        return new ProblemDetailsError(
            type: type ?? "about:blank",
            title: title,
            status: status,
            detail: detail,
            instance: instance);
    }
}

