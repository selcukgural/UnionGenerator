namespace UnionGenerator.AspNetCore.Conventions;

/// <summary>
/// Convention that extracts HTTP status codes from ProblemDetailsError types.
/// </summary>
/// <remarks>
/// <para>
/// This convention specifically handles <see cref="ProblemDetailsError"/> instances
/// and reads their Status property directly. This is the most reliable convention
/// for errors that already follow RFC 7807 ProblemDetails format.
/// </para>
/// <para>
/// Performance: Direct property access, no reflection. Fastest convention.
/// </para>
/// </remarks>
public sealed class ProblemDetailsConvention : IStatusCodeConvention
{
    /// <inheritdoc />
    public int Priority => 100; // Highest priority - most explicit

    /// <inheritdoc />
    public bool TryGetStatusCode(object error, out int statusCode)
    {
        if (error is ProblemDetailsError problemDetailsError)
        {
            statusCode = problemDetailsError.Status;
            return true;
        }

        statusCode = 0;
        return false;
    }
}

