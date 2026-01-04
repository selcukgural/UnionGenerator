using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace UnionGenerator.AspNetCore.Middleware;

/// <summary>
/// Middleware for structured logging of union result types in HTTP responses.
/// </summary>
/// <remarks>
/// <para>
/// This middleware intercepts HTTP responses and logs union result information
/// with a standard template including case name, status code, and elapsed time.
/// </para>
/// <para>
/// The middleware is designed to work with responses that contain ProblemDetails,
/// which are typically produced by UnionResultFilter or ToActionResult extensions.
/// </para>
/// <para>
/// Performance: Minimal overhead (uses request stopwatch, logs only on union detection).
/// </para>
/// <para>
/// Thread-safety: This middleware is stateless and safe for concurrent use across multiple requests.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in Program.cs
/// app.UseMiddleware&lt;UnionResultLoggingMiddleware&gt;();
/// 
/// // Or with dependency injection
/// builder.Services.AddUnionResultHandling(options =>
/// {
///     options.EnableStructuredLogging = true;
/// });
/// </code>
/// </example>
public sealed class UnionResultLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UnionResultLoggingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnionResultLoggingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger for structured logging.</param>
    /// <exception cref="ArgumentNullException">Thrown when next or logger is null.</exception>
    public UnionResultLoggingMiddleware(RequestDelegate next, ILogger<UnionResultLoggingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes the middleware to process the HTTP request and log any union result responses.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <returns>A task that completes when the request processing is done.</returns>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    /// <remarks>
    /// <para>
    /// This method wraps the response stream to capture status code and log result information.
    /// If the response indicates a union error (ProblemDetails), it logs the case name and status.
    /// </para>
    /// <para>
    /// The elapsed time is measured from the start of the request to the end of response writing.
    /// </para>
    /// </remarks>
    // ReSharper disable once UnusedMember.Global
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var stopwatch = Stopwatch.StartNew();

            // Store the original response stream
            var originalResponseStream = context.Response.Body;

            try
            {
                using var memoryStream = new MemoryStream();
                context.Response.Body = memoryStream;

            // Call the next middleware
            await _next(context);

            stopwatch.Stop();

            // Reset response stream position for reading
            memoryStream.Position = 0;

            // Copy response to original stream
            await memoryStream.CopyToAsync(originalResponseStream);

            // Log if this is a union error response
            LogUnionResult(context, stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            context.Response.Body = originalResponseStream;
        }
    }

    /// <summary>
    /// Logs union result information if the response indicates an error.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="elapsedMilliseconds">The elapsed time in milliseconds.</param>
    private void LogUnionResult(HttpContext context, long elapsedMilliseconds)
    {
        // Detect if this is a ProblemDetails response (union error)
        var statusCode = context.Response.StatusCode;
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;

        // Success responses (2xx) are not logged by default
        if (statusCode >= 200 && statusCode < 300)
        {
            _logger.LogDebug(
                "UnionResult: Path={Path} Method={Method} Status={Status} Elapsed={Elapsed}ms Case=Success",
                path,
                method,
                statusCode,
                elapsedMilliseconds
            );
            return;
        }

        // Error responses (4xx, 5xx) are logged with case detection
        var caseName = DetectCaseName(statusCode);

        _logger.LogWarning(
            "UnionResult: Path={Path} Method={Method} Status={Status} Elapsed={Elapsed}ms Case={Case}",
            path,
            method,
            statusCode,
            elapsedMilliseconds,
            caseName
        );
    }

    /// <summary>
    /// Detects the union case name based on HTTP status code.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>A friendly case name for logging (e.g., "NotFound", "ValidationError").</returns>
    /// <remarks>
    /// This is a best-effort detection based on conventional HTTP status codes.
    /// It serves as a diagnostic aid for understanding error patterns.
    /// </remarks>
    private static string DetectCaseName(int statusCode)
    {
        return statusCode switch
        {
            400 => "BadRequest",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "NotFound",
            409 => "Conflict",
            422 => "ValidationError",
            429 => "TooManyRequests",
            500 => "InternalServerError",
            502 => "BadGateway",
            503 => "ServiceUnavailable",
            _ => $"Error({statusCode})"
        };
    }
}

