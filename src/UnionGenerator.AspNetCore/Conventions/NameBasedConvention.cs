namespace UnionGenerator.AspNetCore.Conventions;

/// <summary>
/// Convention that infers HTTP status codes from error type names.
/// </summary>
/// <remarks>
/// <para>
/// This convention examines the simple name of the error type (without namespace)
/// and matches it against common HTTP error patterns.
/// </para>
/// <para>
/// Supported patterns (case-insensitive):
/// <list type="bullet">
/// <item><description>*NotFound* → 404</description></item>
/// <item><description>*BadRequest* → 400</description></item>
/// <item><description>*Validation* → 400</description></item>
/// <item><description>*Unauthorized* → 401</description></item>
/// <item><description>*Forbidden* → 403</description></item>
/// <item><description>*Conflict* → 409</description></item>
/// <item><description>*Gone* → 410</description></item>
/// <item><description>*UnprocessableEntity* → 422</description></item>
/// <item><description>*TooManyRequests* → 429</description></item>
/// <item><description>*InternalServerError* → 500</description></item>
/// <item><description>*NotImplemented* → 501</description></item>
/// <item><description>*ServiceUnavailable* → 503</description></item>
/// </list>
/// </para>
/// <para>
/// Performance: O(1) dictionary lookup after type name extraction. No reflection beyond Type.Name.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class UserNotFoundError { }  // → 404
/// public class ValidationError { }     // → 400
/// public class ConflictError { }       // → 409
/// </code>
/// </example>
public sealed class NameBasedConvention : IStatusCodeConvention
{
    private static readonly Dictionary<string, int> StatusCodeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["notfound"] = 404,
        ["badrequest"] = 400,
        ["validation"] = 400,
        ["unauthorized"] = 401,
        ["forbidden"] = 403,
        ["conflict"] = 409,
        ["gone"] = 410,
        ["unprocessableentity"] = 422,
        ["toomanyrequests"] = 429,
        ["internalservererror"] = 500,
        ["notimplemented"] = 501,
        ["serviceunavailable"] = 503
    };

    /// <inheritdoc />
    public int Priority => 50;

    /// <inheritdoc />
    public bool TryGetStatusCode(object error, out int statusCode)
    {
        if (error == null!)
        {
            statusCode = 0;
            return false;
        }

        var typeName = error.GetType().Name;

        // Check for exact matches first (fast path)
        if (StatusCodeMap.TryGetValue(typeName, out statusCode))
        {
            return true;
        }

        // Check for partial matches (e.g., "UserNotFoundError" contains "NotFound")
        var lowerTypeName = typeName.ToLowerInvariant();
        foreach (var kvp in StatusCodeMap)
        {
            if (!lowerTypeName.Contains(kvp.Key))
            {
                continue;
            }

            statusCode = kvp.Value;
            return true;
        }

        statusCode = 0;
        return false;
    }
}

