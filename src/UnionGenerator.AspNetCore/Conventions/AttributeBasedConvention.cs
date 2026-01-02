using System.Collections.Concurrent;
using System.Reflection;
using UnionGenerator.AspNetCore.Attributes;

namespace UnionGenerator.AspNetCore.Conventions;

/// <summary>
/// Convention that infers HTTP status codes from <see cref="UnionStatusCodeAttribute"/> annotations.
/// </summary>
/// <remarks>
/// <para>
/// This convention examines error types for explicit <see cref="UnionStatusCodeAttribute"/>
/// decoration. It's the fastest convention (direct attribute lookup, no reflection on properties),
/// and should be evaluated first in priority order.
/// </para>
/// <para>
/// Attribute lookups are cached per type to minimize reflection overhead.
/// </para>
/// <para>
/// Priority: 100 (highest) – explicit declarations always win.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [UnionStatusCode(404)]
/// public class NotFoundError { }
///
/// [UnionStatusCode(409)]
/// public class ConflictError { }
/// </code>
/// </example>
public sealed class AttributeBasedConvention : IStatusCodeConvention
{
    private static readonly ConcurrentDictionary<Type, int?> AttributeCache = new();

    /// <inheritdoc />
    public int Priority => 100;

    /// <inheritdoc />
    public bool TryGetStatusCode(object error, out int statusCode)
    {
        if (error == null!)
        {
            statusCode = 0;
            return false;
        }

        var cachedStatusCode = AttributeCache.GetOrAdd(
            error.GetType(),
            static type => GetStatusCodeFromAttribute(type));

        if (cachedStatusCode.HasValue)
        {
            statusCode = cachedStatusCode.Value;
            return true;
        }

        statusCode = 0;
        return false;
    }

    /// <summary>
    /// Extracts the HTTP status code from <see cref="UnionStatusCodeAttribute"/> if present.
    /// </summary>
    /// <param name="errorType">The error type to analyze.</param>
    /// <returns>The status code if the attribute is present, or null if not.</returns>
    private static int? GetStatusCodeFromAttribute(Type errorType)
    {
        var attribute = errorType.GetCustomAttribute<UnionStatusCodeAttribute>(inherit: false);
        return attribute?.StatusCode;
    }
}

