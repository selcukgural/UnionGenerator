using System.Collections.Immutable;

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
/// Thread-safety: This class is fully thread-safe. Uses <see cref="ImmutableArray{T}"/>
/// for lock-free concurrent reads and copy-on-write semantics for modifications.
/// Modifications (Register/Clear) can be called at any time, but are relatively expensive
/// due to array copying. Design for infrequent modifications (startup/configuration time).
/// </para>
/// <para>
/// Default conventions (in order of priority):
/// <list type="number">
/// <item><description><see cref="AttributeBasedConvention"/> (Priority: 100)</description></item>
/// <item><description><see cref="PropertyBasedConvention"/> (Priority: 75)</description></item>
/// <item><description><see cref="ProblemDetailsConvention"/> (Priority: 50)</description></item>
/// <item><description><see cref="NameBasedConvention"/> (Priority: 50)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class StatusCodeConventionRegistry
{
    private ImmutableArray<IStatusCodeConvention> _conventions;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusCodeConventionRegistry"/> class.
    /// </summary>
    public StatusCodeConventionRegistry()
    {
        _conventions = ImmutableArray<IStatusCodeConvention>.Empty;
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
    /// <para>
    /// Conventions are automatically sorted by priority after registration.
    /// This method is thread-safe and can be called at any time.
    /// </para>
    /// <para>
    /// Performance: O(n log n) due to sorting. Uses copy-on-write semantics,
    /// so modifications are relatively expensive. Design for infrequent updates.
    /// </para>
    /// </remarks>
    public void Register(IStatusCodeConvention convention)
    {
        ArgumentNullException.ThrowIfNull(convention);

        lock (_lock)
        {
            // Copy-on-write: create new sorted array
            var builder = _conventions.ToBuilder();
            builder.Add(convention);
            
            // Sort by priority descending (highest first)
            builder.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            
            // Atomic swap
            _conventions = builder.ToImmutable();
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
    /// <para>
    /// Conventions are evaluated in priority order. The first convention that returns
    /// true determines the final status code.
    /// </para>
    /// <para>
    /// Thread-safety: This method is lock-free and fully thread-safe.
    /// Uses a local snapshot of the immutable convention array.
    /// </para>
    /// <para>
    /// Performance: O(n) where n is the number of conventions. Lock-free read
    /// ensures no contention even under high concurrent load.
    /// </para>
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

        // Lock-free read: ImmutableArray is thread-safe for concurrent reads
        // Get local snapshot to avoid issues if registry is modified during iteration
        var conventions = _conventions;
        
        foreach (var convention in conventions)
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
    /// Thread-safe. Can be called at any time.
    /// Primarily intended for testing scenarios where registry reset is needed.
    /// </remarks>
    public void Clear()
    {
        lock (_lock)
        {
            _conventions = ImmutableArray<IStatusCodeConvention>.Empty;
        }
    }

    /// <summary>
    /// Gets the current number of registered conventions.
    /// </summary>
    /// <value>The number of conventions currently registered.</value>
    /// <remarks>
    /// Thread-safe. Lock-free read operation.
    /// </remarks>
    public int Count => _conventions.Length;

    /// <summary>
    /// Gets a read-only snapshot of currently registered conventions.
    /// </summary>
    /// <returns>An immutable array of conventions sorted by priority (highest first).</returns>
    /// <remarks>
    /// Thread-safe. Returns the current immutable snapshot without copying.
    /// The returned array cannot be modified and is safe to enumerate concurrently.
    /// </remarks>
    public IReadOnlyList<IStatusCodeConvention> GetConventions()
    {
        // No lock needed - ImmutableArray is already thread-safe
        // Return the array directly; it's immutable so no defensive copy needed
        return _conventions;
    }

    /// <summary>
    /// Creates a copy of this registry with all its conventions.
    /// </summary>
    /// <returns>A new registry instance with the same conventions.</returns>
    /// <remarks>
    /// <para>
    /// This method is useful when you need to modify a registry without affecting the original.
    /// The new registry shares the same convention instances (shallow copy) but maintains
    /// its own independent collection.
    /// </para>
    /// <para>
    /// Thread-safe. Takes a snapshot of current conventions.
    /// </para>
    /// </remarks>
    public StatusCodeConventionRegistry Clone()
    {
        lock (_lock)
        {
            // Create clone with private constructor that accepts conventions
            return new StatusCodeConventionRegistry(_conventions);
        }
    }

    /// <summary>
    /// Private constructor for cloning with existing conventions.
    /// </summary>
    private StatusCodeConventionRegistry(ImmutableArray<IStatusCodeConvention> conventions)
    {
        _conventions = conventions;
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

