namespace UnionGenerator.AspNetCore.Conventions;

/// <summary>
/// Registry for managing and applying HTTP status code conventions.
/// </summary>
/// <remarks>
/// <para>
/// The registry maintains a collection of <see cref="IStatusCodeConvention"/> instances
/// and evaluates them in priority order when inferring status codes from error values.
/// </para>
/// <para>
/// Thread-safety: This class is thread-safe for reads after initial configuration.
/// Avoid modifying the registry (Register/Clear) after the application startup.
/// </para>
/// <para>
/// Default conventions (in order of priority):
/// <list type="number">
/// <item><description><see cref="ProblemDetailsConvention"/> (Priority: 100)</description></item>
/// <item><description><see cref="PropertyBasedConvention"/> (Priority: 75)</description></item>
/// <item><description><see cref="NameBasedConvention"/> (Priority: 50)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class StatusCodeConventionRegistry
{
    private readonly List<IStatusCodeConvention> _conventions;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusCodeConventionRegistry"/> class.
    /// </summary>
    public StatusCodeConventionRegistry()
    {
        _conventions = [];
    }

    /// <summary>
    /// Gets the default registry with built-in conventions pre-registered.
    /// </summary>
    /// <value>
    /// A singleton instance with ProblemDetails, Property-based, and Name-based conventions.
    /// </value>
    public static StatusCodeConventionRegistry Default { get; } = CreateDefault();

    /// <summary>
    /// Registers a new convention in the registry.
    /// </summary>
    /// <param name="convention">The convention to register. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="convention"/> is null.</exception>
    /// <remarks>
    /// Conventions are automatically sorted by priority after registration.
    /// Avoid calling this method after the application startup for thread-safety.
    /// </remarks>
    public void Register(IStatusCodeConvention convention)
    {
        ArgumentNullException.ThrowIfNull(convention);

        lock (_lock)
        {
            _conventions.Add(convention);
            // Sort by priority descending (highest first)
            _conventions.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }
    }

    /// <summary>
    /// Attempts to infer the HTTP status code for the given error value.
    /// </summary>
    /// <param name="error">The error value to analyze. Must not be null.</param>
    /// <param name="statusCode">
    /// When this method returns true, contains the inferred HTTP status code.
    /// When this method returns false, contains 0.
    /// </param>
    /// <returns>
    /// <c>true</c> if any convention successfully inferred a status code; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Conventions are evaluated in priority order. The first convention that returns
    /// true determines the final status code.
    /// </remarks>
    /// <example>
    /// <code>
    /// var registry = StatusCodeConventionRegistry.Default;
    /// 
    /// if (registry.TryInferStatusCode(error, out int statusCode))
    /// {
    ///     return StatusCode(statusCode, error);
    /// }
    /// else
    /// {
    ///     return StatusCode(500, error); // Fallback
    /// }
    /// </code>
    /// </example>
    public bool TryInferStatusCode(object error, out int statusCode)
    {
        if (error == null!)
        {
            statusCode = 0;
            return false;
        }

        // No lock needed for reads - List<T> is safe for concurrent reads
        // as long as no writes occur (which should only happen at startup)
        // ReSharper disable once InconsistentlySynchronizedField
        foreach (var convention in _conventions)
        {
            if (convention.TryGetStatusCode(error, out statusCode))
            {
                return true;
            }
        }

        statusCode = 0;
        return false;
    }

    /// <summary>
    /// Infers the HTTP status code for the given error value, with fallback.
    /// </summary>
    /// <param name="error">The error value to analyze.</param>
    /// <param name="defaultStatusCode">
    /// The status code to return if no convention can infer one. Default is 500.
    /// </param>
    /// <returns>
    /// The inferred status code, or <paramref name="defaultStatusCode"/> if inference fails.
    /// </returns>
    /// <remarks>
    /// This is a convenience method that always returns a valid status code.
    /// Use this when you need a guaranteed status code value.
    /// </remarks>
    public int InferStatusCode(object error, int defaultStatusCode = 500)
    {
        return TryInferStatusCode(error, out var statusCode) ? statusCode : defaultStatusCode;
    }

    /// <summary>
    /// Clears all registered conventions.
    /// </summary>
    /// <remarks>
    /// Use this method only in testing scenarios. Avoid calling after application startup.
    /// </remarks>
    public void Clear()
    {
        lock (_lock)
        {
            _conventions.Clear();
        }
    }

    /// <summary>
    /// Gets the count of registered conventions.
    /// </summary>
    /// <value>The number of conventions currently registered.</value>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _conventions.Count;
            }
        }
    }

    /// <summary>
    /// Gets a read-only snapshot of currently registered conventions.
    /// </summary>
    /// <returns>An array of conventions sorted by priority (highest first).</returns>
    public IReadOnlyList<IStatusCodeConvention> GetConventions()
    {
        lock (_lock)
        {
            return _conventions.ToArray();
        }
    }

    /// <summary>
    /// Creates the default registry with built-in conventions.
    /// </summary>
    /// <remarks>
    /// Conventions are registered in the following order (by priority):
    /// 1. AttributeBasedConvention (100) – explicit [UnionStatusCode] annotations
    /// 2. PropertyBasedConvention (75) – StatusCode/Status/HttpStatusCode properties
    /// 3. ProblemDetailsConvention (50) – ProblemDetails-based errors
    /// 4. NameBasedConvention (50) – naming patterns (NotFound, BadRequest, etc.)
    /// </remarks>
    private static StatusCodeConventionRegistry CreateDefault()
    {
        var registry = new StatusCodeConventionRegistry();
        registry.Register(new AttributeBasedConvention());
        registry.Register(new PropertyBasedConvention());
        registry.Register(new ProblemDetailsConvention());
        registry.Register(new NameBasedConvention());
        return registry;
    }
}

