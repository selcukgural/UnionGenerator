namespace UnionGenerator.AspNetCore.Conventions;

/// <summary>
/// Represents a convention for inferring HTTP status codes from error types.
/// </summary>
/// <remarks>
/// Conventions are evaluated in priority order (highest first) when determining
/// the appropriate HTTP status code for an error value. Multiple conventions can
/// be registered, and the first one that successfully infers a status code wins.
/// </remarks>
public interface IStatusCodeConvention
{
    /// <summary>
    /// Attempts to infer the HTTP status code from the given error value.
    /// </summary>
    /// <param name="error">The error value to analyze. Must not be null.</param>
    /// <param name="statusCode">
    /// When this method returns true, contains the inferred HTTP status code (100-599).
    /// When this method returns false, contains 0.
    /// </param>
    /// <returns>
    /// <c>true</c> if a status code could be inferred; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Implementations should be fast and avoid expensive operations (reflection, allocations).
    /// This method may be called frequently in hot paths.
    /// </remarks>
    bool TryGetStatusCode(object error, out int statusCode);

    /// <summary>
    /// Gets the priority of this convention. Higher values are evaluated first.
    /// </summary>
    /// <value>
    /// Priority value. Default priorities:
    /// <list type="bullet">
    /// <item><description>100: Attribute-based conventions</description></item>
    /// <item><description>75: Property-based conventions</description></item>
    /// <item><description>50: Name-based conventions</description></item>
    /// <item><description>25: ProblemDetails-based conventions</description></item>
    /// <item><description>0: Fallback/default conventions</description></item>
    /// </list>
    /// </value>
    int Priority { get; }
}

