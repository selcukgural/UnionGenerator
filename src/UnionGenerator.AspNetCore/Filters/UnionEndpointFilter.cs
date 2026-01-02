using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace UnionGenerator.AspNetCore.Filters;

/// <summary>
/// Endpoint filter for Minimal API that automatically converts union return values to appropriate HTTP responses.
/// </summary>
/// <remarks>
/// <para>
/// This filter enables automatic conversion of union types (especially Result patterns) returned from
/// Minimal API endpoints into proper HTTP responses with ProblemDetails formatting for errors.
/// </para>
/// <para>
/// Unlike the <see cref="UnionResultFilter"/> for controllers, this filter is designed for use with
/// Minimal API endpoints and implements <see cref="IEndpointFilter"/>.
/// </para>
/// <para>
/// Thread-safety: This filter is stateless and safe for concurrent use across multiple requests.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Apply globally to all endpoints
/// var app = builder.Build();
/// app.MapGet("/api/users/{id}", GetUser)
///    .AddEndpointFilter&lt;UnionEndpointFilter&gt;();
/// 
/// Result&lt;User, ProblemDetailsError&gt; GetUser(int id)
/// {
///     return userService.GetUser(id);
/// }
/// 
/// // Or create an extension method for convenience
/// public static class RouteHandlerBuilderExtensions
/// {
///     public static RouteHandlerBuilder WithUnionSupport(this RouteHandlerBuilder builder)
///     {
///         return builder.AddEndpointFilter&lt;UnionEndpointFilter&gt;();
///     }
/// }
/// 
/// app.MapGet("/api/users/{id}", GetUser).WithUnionSupport();
/// </code>
/// </example>
public sealed class UnionEndpointFilter : IEndpointFilter
{
    private static readonly string[] SuccessPropertyNames = ["IsOk", "IsSuccess", "IsSome"];
    private static readonly string[] ErrorPropertyNames = ["ErrorValue", "Error", "FailureValue", "Failure", "NoneValue"];

    /// <summary>
    /// Invokes the endpoint filter to process the request and convert union results.
    /// </summary>
    /// <param name="context">The endpoint filter invocation context.</param>
    /// <param name="next">The next filter or endpoint handler in the pipeline.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the HTTP response value.
    /// </returns>
    /// <remarks>
    /// This method examines the endpoint result after execution. If it's a union type,
    /// it converts it to the appropriate HTTP response format. Non-union results are passed through unchanged.
    /// </remarks>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);

        if (result == null)
        {
            return result;
        }

        var resultType = result.GetType();

        // Check if this looks like a union type
        if (!IsUnionType(resultType))
        {
            return result;
        }

        try
        {
            return ConvertUnionToHttpResult(result, resultType);
        }
        catch
        {
            // If conversion fails, return the original result
            return result;
        }
    }

    /// <summary>
    /// Checks if a type appears to be a union type based on its properties.
    /// </summary>
    private static bool IsUnionType(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var propertyNames = properties.Where(p => p.PropertyType == typeof(bool))
                                      .Select(p => p.Name)
                                      .ToHashSet();

        return SuccessPropertyNames.Any(name => propertyNames.Contains(name));
    }

    /// <summary>
    /// Converts a union instance to an appropriate HTTP result for Minimal API.
    /// </summary>
    private static IResult ConvertUnionToHttpResult(object union, Type unionType)
    {
        var successProperty = FindProperty(unionType, SuccessPropertyNames);
        if (successProperty == null)
        {
            throw new InvalidOperationException("Union type does not have a recognizable success property.");
        }

        var isSuccess = (bool)(successProperty.GetValue(union) ?? false);

        if (isSuccess)
        {
            var valueProperty = unionType.GetProperty("Value");
            if (valueProperty == null)
            {
                throw new InvalidOperationException("Union type does not have a 'Value' property.");
            }

            var value = valueProperty.GetValue(union);
            return Results.Ok(value);
        }

        var errorProperty = FindProperty(unionType, ErrorPropertyNames);
        if (errorProperty == null)
        {
            throw new InvalidOperationException("Union type does not have a recognizable error property.");
        }

        var errorValue = errorProperty.GetValue(union);

        if (errorValue is ProblemDetailsError problemDetailsError)
        {
            return CreateProblemDetailsResult(problemDetailsError);
        }

        // Fallback for non-ProblemDetailsError types
        return Results.BadRequest(errorValue);
    }

    /// <summary>
    /// Finds a property by trying multiple possible names.
    /// </summary>
    private static PropertyInfo? FindProperty(Type type, string[] candidateNames)
    {
        foreach (var name in candidateNames)
        {
            var property = type.GetProperty(name);
            if (property != null)
            {
                return property;
            }
        }

        return null;
    }

    /// <summary>
    /// Creates an IResult from a ProblemDetailsError for Minimal API.
    /// </summary>
    private static IResult CreateProblemDetailsResult(ProblemDetailsError error)
    {
        if (error.Errors != null)
        {
            // Validation error case
            return Results.ValidationProblem(
                errors: error.Errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                detail: error.Detail,
                instance: error.Instance,
                statusCode: error.Status,
                title: error.Title,
                type: error.Type,
                extensions: error.Extensions?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            );
        }

        // Standard problem details
        return Results.Problem(
            detail: error.Detail,
            instance: error.Instance,
            statusCode: error.Status,
            title: error.Title,
            type: error.Type,
            extensions: error.Extensions?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        );
    }
}

