using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UnionGenerator.AspNetCore.Extensions;

/// <summary>
/// Provides extension methods for converting union types to ASP.NET Core action results.
/// </summary>
/// <remarks>
/// <para>
/// These extensions enable seamless conversion of generated union types (especially Result-like patterns)
/// into ASP.NET Core <see cref="IActionResult"/> responses with automatic ProblemDetails formatting.
/// </para>
/// <para>
/// The extensions work with any union type that follows the two-case pattern with boolean properties
/// (e.g., IsOk/IsError, IsSuccess/IsFailure) and exposes Value properties for extracting case data.
/// </para>
/// <para>
/// Performance: These methods use reflection to detect union cases and extract values. For high-throughput
/// scenarios, consider caching property accessors or using source-generated specific extensions.
/// </para>
/// </remarks>
public static class UnionActionResultExtensions
{
    /// <summary>
    /// Converts a two-case union with <see cref="ProblemDetailsError"/> error case to an <see cref="IActionResult"/>.
    /// </summary>
    /// <typeparam name="TUnion">The type of the union.</typeparam>
    /// <param name="union">The union instance to convert.</param>
    /// <param name="successStatusCode">
    /// The HTTP status code to return on success. Defaults to 200 (OK).
    /// Common values: 200 (OK), 201 (Created), 204 (No Content).
    /// </param>
    /// <returns>
    /// An <see cref="OkObjectResult"/> with the success value if the union represents success,
    /// or an <see cref="ObjectResult"/> with <see cref="ProblemDetailsError"/> if it represents an error.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when union is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the union type does not follow the expected two-case pattern with appropriate properties.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method expects the union to have:
    /// - Two boolean properties indicating active case (e.g., IsOk/IsError, IsSuccess/IsFailure)
    /// - A Value property returning the success value
    /// - An error property returning <see cref="ProblemDetailsError"/>
    /// </para>
    /// <para>
    /// The error case is automatically converted to an appropriate <see cref="ObjectResult"/> with
    /// the status code from the <see cref="ProblemDetailsError"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In a controller action
    /// [HttpGet("{id}")]
    /// public IActionResult GetUser(int id)
    /// {
    ///     Result&lt;User, ProblemDetailsError&gt; result = userService.GetUser(id);
    ///     return result.ToActionResult();
    /// }
    /// 
    /// // With custom success status
    /// [HttpPost]
    /// public IActionResult CreateUser(CreateUserDto dto)
    /// {
    ///     Result&lt;User, ProblemDetailsError&gt; result = userService.CreateUser(dto);
    ///     return result.ToActionResult(successStatusCode: 201);
    /// }
    /// </code>
    /// </example>
    public static IActionResult ToActionResult<TUnion>(this TUnion union, int successStatusCode = StatusCodes.Status200OK) where TUnion : class
    {
        if (union == null)
        {
            throw new ArgumentNullException(nameof(union));
        }

        var unionType = union.GetType();

        // Find the success case property (IsOk, IsSuccess, etc.)
        var successProperty = FindSuccessProperty(unionType);

        if (successProperty == null)
        {
            throw new InvalidOperationException(
                $"Union type '{unionType.Name}' does not have a recognizable success case property (IsOk, IsSuccess, etc.).");
        }

        var isSuccess = (bool)(successProperty.GetValue(union) ?? false);

        if (isSuccess)
        {
            // Extract success value
            var valueProperty = unionType.GetProperty("Value");

            if (valueProperty == null)
            {
                throw new InvalidOperationException(
                    $"Union type '{unionType.Name}' does not have a 'Value' property for accessing the success value.");
            }

            var value = valueProperty.GetValue(union);

            // Handle different success status codes
            return successStatusCode switch
            {
                StatusCodes.Status204NoContent => new NoContentResult(),
                StatusCodes.Status201Created   => new ObjectResult(value) { StatusCode = StatusCodes.Status201Created },
                _                              => new OkObjectResult(value)
            };
        }

        // Extract error value
        var errorValue = ExtractErrorValue(union, unionType);

        if (errorValue is ProblemDetailsError problemDetailsError)
        {
            return CreateProblemDetailsResult(problemDetailsError);
        }

        // Fallback for non-ProblemDetailsError types
        return new ObjectResult(errorValue)
        {
            StatusCode = StatusCodes.Status400BadRequest
        };
    }

    /// <summary>
    /// Converts a two-case union to an <see cref="IActionResult"/> using a custom error mapper.
    /// </summary>
    /// <typeparam name="TUnion">The type of the union.</typeparam>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <param name="union">The union instance to convert.</param>
    /// <param name="errorMapper">
    /// Function to convert the error value to <see cref="ProblemDetailsError"/>.
    /// </param>
    /// <param name="successStatusCode">
    /// The HTTP status code to return on success. Defaults to 200 (OK).
    /// </param>
    /// <returns>
    /// An appropriate <see cref="IActionResult"/> based on the union state.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when union or errorMapper is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the union type does not follow the expected pattern.
    /// </exception>
    /// <remarks>
    /// Use this overload when the error case is not <see cref="ProblemDetailsError"/> and needs custom mapping.
    /// </remarks>
    /// <example>
    /// <code>
    /// // With custom error type
    /// Result&lt;User, DomainError&gt; result = userService.GetUser(id);
    /// 
    /// return result.ToActionResult(
    ///     errorMapper: domainError => ProblemDetailsErrorFactory.BadRequest(
    ///         HttpContext.Request.Path,
    ///         domainError.Message
    ///     )
    /// );
    /// </code>
    /// </example>
    public static IActionResult ToActionResult<TUnion, TSuccess, TError>(this TUnion union, Func<TError, ProblemDetailsError> errorMapper,
                                                                         int successStatusCode = StatusCodes.Status200OK) where TUnion : class
    {
        ArgumentNullException.ThrowIfNull(union);
        ArgumentNullException.ThrowIfNull(errorMapper);

        var unionType = union.GetType();
        var successProperty = FindSuccessProperty(unionType);

        if (successProperty == null)
        {
            throw new InvalidOperationException($"Union type '{unionType.Name}' does not have a recognizable success case property.");
        }

        var isSuccess = (bool)(successProperty.GetValue(union) ?? false);

        if (isSuccess)
        {
            var valueProperty = unionType.GetProperty("Value");

            if (valueProperty == null)
            {
                throw new InvalidOperationException($"Union type '{unionType.Name}' does not have a 'Value' property.");
            }

            var value = valueProperty.GetValue(union);

            return successStatusCode switch
            {
                StatusCodes.Status204NoContent => new NoContentResult(),
                StatusCodes.Status201Created   => new ObjectResult(value) { StatusCode = StatusCodes.Status201Created },
                _                              => new OkObjectResult(value)
            };
        }

        var errorValue = ExtractErrorValue(union, unionType);
        var problemDetails = errorMapper((TError)errorValue!);
        return CreateProblemDetailsResult(problemDetails);
    }

    /// <summary>
    /// Converts a <see cref="ProblemDetailsError"/> directly to an <see cref="IActionResult"/>.
    /// </summary>
    /// <param name="error">The error to convert.</param>
    /// <returns>
    /// An <see cref="ObjectResult"/> configured with the error's status code and ProblemDetails payload.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when error is null.</exception>
    /// <remarks>
    /// This is a convenience method for directly returning <see cref="ProblemDetailsError"/> instances
    /// from controller actions without wrapping them in a union.
    /// </remarks>
    /// <example>
    /// <code>
    /// var error = ProblemDetailsErrorFactory.NotFound(
    ///     HttpContext.Request.Path,
    ///     "User not found."
    /// );
    /// return error.ToActionResult();
    /// </code>
    /// </example>
    public static IActionResult ToActionResult(this ProblemDetailsError error)
    {
        return error == null ? throw new ArgumentNullException(nameof(error)) : CreateProblemDetailsResult(error);
    }

    /// <summary>
    /// Finds the success case property in a union type (IsOk, IsSuccess, etc.).
    /// </summary>
    private static System.Reflection.PropertyInfo? FindSuccessProperty(Type unionType)
    {
        var successPropertyNames = new[] { "IsOk", "IsSuccess", "IsSome" };

        foreach (var name in successPropertyNames)
        {
            var property = unionType.GetProperty(name);

            if (property != null && property.PropertyType == typeof(bool))
            {
                return property;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the error value from a union instance.
    /// </summary>
    private static object? ExtractErrorValue(object union, Type unionType)
    {
        // Try common error property names
        var errorPropertyNames = new[] { "ErrorValue", "Error", "FailureValue", "Failure", "NoneValue" };

        foreach (var name in errorPropertyNames)
        {
            var property = unionType.GetProperty(name);

            if (property != null)
            {
                return property.GetValue(union);
            }
        }

        throw new InvalidOperationException($"Union type '{unionType.Name}' does not have a recognizable error value property.");
    }

    /// <summary>
    /// Creates an ObjectResult from a ProblemDetailsError following ASP.NET Core conventions.
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

        // Add custom extensions if present
        if (error.Extensions == null)
        {
            return new ObjectResult(problemDetails)
            {
                StatusCode = error.Status
            };
        }


        foreach (var kvp in error.Extensions)
        {
            problemDetails.Extensions[kvp.Key] = kvp.Value;
        }


        return new ObjectResult(problemDetails)
        {
            StatusCode = error.Status
        };
    }
}