using System.Collections.Concurrent;
using System.Reflection;

namespace UnionGenerator.AspNetCore.Conventions;

/// <summary>
/// Convention that infers HTTP status codes from properties on error types.
/// </summary>
/// <remarks>
/// <para>
/// This convention looks for properties named "StatusCode", "Status", or "HttpStatusCode"
/// on the error type and reads their value. Property access is cached per type for performance.
/// </para>
/// <para>
/// Supported property signatures:
/// <list type="bullet">
/// <item><description><c>int StatusCode { get; }</c></description></item>
/// <item><description><c>int Status { get; }</c></description></item>
/// <item><description><c>int HttpStatusCode { get; }</c></description></item>
/// </list>
/// </para>
/// <para>
/// Performance: First access per type uses reflection (cached). Subsequent accesses use compiled accessor.
/// Thread-safe caching via ConcurrentDictionary.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CustomError
/// {
///     public int StatusCode => 418; // I'm a teapot
/// }
/// 
/// public class DomainError
/// {
///     public int Status { get; init; } = 422;
/// }
/// </code>
/// </example>
public sealed class PropertyBasedConvention : IStatusCodeConvention
{
    private static readonly ConcurrentDictionary<Type, Func<object, int>?> PropertyAccessorCache = new();

    private static readonly string[] PropertyNames = ["StatusCode", "Status", "HttpStatusCode"];

    /// <inheritdoc />
    public int Priority => 75;

    /// <inheritdoc />
    public bool TryGetStatusCode(object error, out int statusCode)
    {
        if (error == null!)
        {
            statusCode = 0;
            return false;
        }

        var accessor = PropertyAccessorCache.GetOrAdd(error.GetType(), CreateAccessor);

        if (accessor == null)
        {
            statusCode = 0;
            return false;
        }

        statusCode = accessor(error);

        // Validate range (100-599 is valid HTTP status code range)
        return statusCode is >= 100 and < 600;
    }

/// <summary>
    /// Creates a compiled property accessor for the given type.
    /// </summary>
    /// <param name="errorType">The error type to analyze. Must not be null.</param>
    /// <returns>
    /// A function that reads the status code property, or null if no suitable property exists.
    /// The returned delegate takes an object instance of <paramref name="errorType"/> and returns the integer status code.
    /// </returns>
    /// <remarks>
    /// This implementation lists public instance properties once and builds a compiled
    /// expression delegate for fast later access. This avoids repeated calls to
    /// <see cref="Type.GetProperty(string,BindingFlags)"/> on the hot path.
    /// The caller (concurrent cache) is expected to handle thread-safety for reuse.
    /// </remarks>
    private static Func<object, int>? CreateAccessor(Type errorType)
    { 
        var properties = errorType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
    
        foreach (var property in properties)
        {
            if (property.PropertyType != typeof(int))
            {
                continue;
            }
    
            if (!property.CanRead)
            {
                continue;
            }
    
            var found = false;
            foreach (var propertyName in PropertyNames)
            {
                if (!string.Equals(property.Name, propertyName, StringComparison.Ordinal))
                {
                    continue;
                }

                found = true;
                break;
            }
    
            if (!found)
            {
                continue;
            }
    
            var parameter = System.Linq.Expressions.Expression.Parameter(typeof(object), "obj");
            var cast = System.Linq.Expressions.Expression.Convert(parameter, errorType);
            var propertyAccess = System.Linq.Expressions.Expression.Property(cast, property);
            var convertToInt = System.Linq.Expressions.Expression.Convert(propertyAccess, typeof(int));
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<object, int>>(convertToInt, parameter);
            return lambda.Compile();
        }
    
        return null;
    }
}

