using Microsoft.Extensions.Logging;

namespace UnionGenerator.AspNetCore.Logging;

/// <summary>
/// High-performance structured logger for union result processing.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="LoggerMessage.Define"/> to create strongly typed, allocation-free logging methods.
/// All logging in this class is structured (no string interpolation), which enables efficient filtering
/// and analysis in log aggregation systems.
/// </para>
/// <para>
/// Thread-safe. Can be safely shared across multiple concurrent operations.
/// </para>
/// </remarks>
public sealed class UnionResultLogger
{
    private readonly ILogger _logger;
    private readonly UnionLoggingOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnionResultLogger"/> class.
    /// </summary>
    /// <param name="logger">The underlying ILogger instance. Must not be null.</param>
    /// <param name="options">Logging configuration options. If null, defaults are used.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public UnionResultLogger(ILogger logger, UnionLoggingOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new UnionLoggingOptions();
    }

    /// <summary>
    /// Logs the processing of a union error case and its status code inference.
    /// </summary>
    /// <param name="errorType">The fully qualified name of the error type.</param>
    /// <param name="statusCode">The inferred HTTP status code.</param>
    /// <param name="conventionUsed">The convention that inferred the status code.</param>
    /// <remarks>
    /// Respects <see cref="UnionLoggingOptions.LogErrorDetails"/> and <see cref="UnionLoggingOptions.MinimumLevel"/>.
    /// Only logs if configured to do so.
    /// </remarks>
    /// <example>
    /// <code>
    /// logger.LogErrorCase("MyApp.Errors.NotFoundError", 404, "NameBased");
    /// </code>
    /// </example>
    public void LogErrorCase(string errorType, int statusCode, string conventionUsed)
    {
        if (!_options.LogErrorDetails || !_logger.IsEnabled(_options.MinimumLevel))
        {
            return;
        }

        PrivateLogErrorCase(_logger, errorType, statusCode, conventionUsed, null);
    }

    /// <summary>
    /// Logs a successful union result processing.
    /// </summary>
    /// <param name="resultType">The fully qualified name of the result type.</param>
    /// <remarks>
    /// Respects <see cref="UnionLoggingOptions.LogSuccessResults"/> and <see cref="UnionLoggingOptions.MinimumLevel"/>.
    /// Only logs if configured to do so.
    /// </remarks>
    public void LogSuccessCase(string resultType)
    {
        if (!_options.LogSuccessResults || !_logger.IsEnabled(_options.MinimumLevel))
        {
            return;
        }

        PrivateLogSuccessCase(_logger, resultType, null);
    }

    /// <summary>
    /// Logs when a status code could not be inferred for an error.
    /// </summary>
    /// <param name="errorType">The fully qualified name of the error type.</param>
    /// <param name="fallbackStatusCode">The fallback status code being used (typically 500).</param>
    /// <remarks>
    /// Always logged if enabled, as this represents a potential configuration issue.
    /// </remarks>
    public void LogStatusCodeInferenceFailed(string errorType, int fallbackStatusCode)
    {
        if (!_logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        PrivateLogStatusCodeInferenceFailed(_logger, errorType, fallbackStatusCode, null);
    }

    // Static factory methods for allocation-free logging (LoggerMessage.Define pattern)
    private static readonly Action<ILogger, string, int, string, Exception?> PrivateLogErrorCase =
        LoggerMessage.Define<string, int, string>(
            LogLevel.Information,
            eventId: new EventId(4001, "UnionErrorCaseProcessed"),
            formatString: "Union error case processed. Type: {ErrorType}, StatusCode: {StatusCode}, Convention: {Convention}");

    private static readonly Action<ILogger, string, Exception?> PrivateLogSuccessCase =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            eventId: new EventId(4002, "UnionSuccessCaseProcessed"),
            formatString: "Union success case processed. Type: {ResultType}");

    private static readonly Action<ILogger, string, int, Exception?> PrivateLogStatusCodeInferenceFailed =
        LoggerMessage.Define<string, int>(
            LogLevel.Warning,
            eventId: new EventId(4003, "UnionStatusCodeInferenceFailed"),
            formatString: "Status code inference failed for union error. Type: {ErrorType}, Fallback: {FallbackStatusCode}");
}

