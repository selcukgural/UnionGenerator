using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace UnionGenerator.AspNetCore.Filters;

/// <summary>
/// Action filter that automatically converts union return values to appropriate <see cref="IActionResult"/> responses.
/// </summary>
/// <remarks>
/// <para>
/// This filter intercepts controller actions that return union types and automatically converts them
/// to proper HTTP responses with ProblemDetails formatting for error cases.
/// </para>
/// <para>
/// The filter works with any two-case union type that follows the pattern of having boolean properties
/// indicating the active case (e.g., IsOk/IsError) and Value properties for accessing case data.
/// </para>
/// <para>
/// Thread-safety: This filter is stateless and safe for concurrent use across multiple requests.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Apply globally in Program.cs / Startup.cs
/// builder.Services.AddControllers(options =>
/// {
///     options.Filters.Add&lt;UnionResultFilter&gt;();
/// });
/// 
/// // Or apply per-controller
/// [ServiceFilter(typeof(UnionResultFilter))]
/// public class UsersController : ControllerBase
/// {
///     [HttpGet("{id}")]
///     public Result&lt;User, ProblemDetailsError&gt; GetUser(int id)
///     {
///         // Return union directly - filter handles conversion
///         return _userService.GetUser(id);
///     }
/// }
/// </code>
/// </example>
public sealed class UnionResultFilter : IActionFilter
{
    private static readonly string[] SuccessPropertyNames = ["IsOk", "IsSuccess", "IsSome"];
    private static readonly string[] ErrorPropertyNames = ["ErrorValue", "Error", "FailureValue", "Failure", "NoneValue"];

    /// <summary>
    /// Called before the action executes. No operation performed in this phase.
    /// </summary>
    /// <param name="context">The action executing context.</param>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // No pre-execution logic needed
    }

    /// <summary>
    /// Called after the action executes. Converts union results to appropriate HTTP responses.
    /// </summary>
    /// <param name="context">The action executed context containing the action result.</param>
    /// <remarks>
    /// <para>
    /// This method examines the action result. If it's an <see cref="ObjectResult"/> containing
    /// a union type, it converts it to the appropriate HTTP response format.
    /// </para>
    /// <para>
    /// Non-union results and null results are passed through unchanged.
    /// </para>
    /// </remarks>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Result is not ObjectResult objectResult)
        {
            return;
        }

        var value = objectResult.Value;
        if (value == null)
        {
            return;
        }

        var valueType = value.GetType();

        // Check if this looks like a union type (has success/error case properties)
        if (!IsUnionType(valueType))
        {
            return;
        }

        try
        {
            var actionResult = ConvertUnionToActionResult(value, valueType);
            context.Result = actionResult;
        }
        catch
        {
            // If conversion fails, leave the original result unchanged
            // This prevents the filter from breaking non-union types
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

        // Must have at least one success indicator property
        return SuccessPropertyNames.Any(name => propertyNames.Contains(name));
    }

    /// <summary>
    /// Converts a union instance to an appropriate <see cref="IActionResult"/>.
    /// </summary>
    private static IActionResult ConvertUnionToActionResult(object union, Type unionType)
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
            return new OkObjectResult(value);
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
        return new ObjectResult(errorValue)
        {
            StatusCode = 400
        };
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
    /// Creates an ObjectResult from a ProblemDetailsError.
    /// </summary>
    private static ObjectResult CreateProblemDetailsResult(ProblemDetailsError error)
    {
        var problemDetails = error.Errors != null
            ? new ValidationProblemDetails(error.Errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
            {
                Type = error.Type,
                Title = error.Title,
                Status = error.Status,
                Detail = error.Detail,
                Instance = error.Instance
            }
            : new ProblemDetails
            {
                Type = error.Type,
                Title = error.Title,
                Status = error.Status,
                Detail = error.Detail,
                Instance = error.Instance
            };

        if (error.Extensions != null)
        {
            foreach (var kvp in error.Extensions)
            {
                problemDetails.Extensions[kvp.Key] = kvp.Value;
            }
        }

        return new ObjectResult(problemDetails)
        {
            StatusCode = error.Status
        };
    }
}

