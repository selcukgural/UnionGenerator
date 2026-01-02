using Microsoft.Extensions.Logging;

namespace UnionGenerator.AspNetCore.Logging;

/// <summary>
/// Configuration options for union result logging behavior.
/// </summary>
/// <remarks>
/// <para>
/// Controls logging verbosity and what information gets logged when union results are processed.
/// By default, only errors and important state transitions are logged; success cases are silent.
/// </para>
/// </remarks>
public sealed class UnionLoggingOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to log successful union results.
    /// </summary>
    /// <value>
    /// <c>true</c> to log success cases; <c>false</c> (default) to log only errors/important events.
    /// </value>
    /// <remarks>
    /// Default: <c>false</c>. Setting to <c>true</c> can cause log spam in high-throughput scenarios.
    /// </remarks>
    public bool LogSuccessResults { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to log error details when an error case is handled.
    /// </summary>
    /// <value>
    /// <c>true</c> to log error details (error type, status code inference); <c>false</c> to suppress.
    /// </value>
    /// <remarks>
    /// Default: <c>true</c>. Error logging is important for diagnostics and should generally remain enabled.
    /// </remarks>
    public bool LogErrorDetails { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to log status code inference results.
    /// </summary>
    /// <value>
    /// <c>true</c> to log which convention inferred a status code; <c>false</c> to suppress.
    /// </value>
    /// <remarks>
    /// Default: <c>true</c>. Useful for debugging convention behavior and performance analysis.
    /// </remarks>
    public bool LogConventionInference { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum log level for union result events.
    /// </summary>
    /// <value>
    /// The minimum log level. Default: <see cref="LogLevel.Information"/>.
    /// </value>
    /// <remarks>
    /// Messages below this level are suppressed regardless of other settings.
    /// </remarks>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
}

