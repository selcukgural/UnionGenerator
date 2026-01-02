using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace UnionGenerator.AspNetCore.Extensions;

/// <summary>
/// Provides extension methods for converting ASP.NET Core model validation errors to union error types.
/// </summary>
/// <remarks>
/// <para>
/// These extensions facilitate the conversion of <see cref="ModelStateDictionary"/> validation errors
/// into <see cref="ProblemDetailsError"/> instances that can be used in Result union error cases.
/// </para>
/// <para>
/// The extensions follow ASP.NET Core's ValidationProblemDetails convention where validation errors
/// are structured as a dictionary of field names to error message arrays.
/// </para>
/// </remarks>
public static class ModelStateExtensions
{
    /// <summary>
    /// Converts a <see cref="ModelStateDictionary"/> to a <see cref="ProblemDetailsError"/> validation error.
    /// </summary>
    /// <param name="modelState">The model state dictionary containing validation errors.</param>
    /// <param name="instance">
    /// The request path or identifier where the validation error occurred.
    /// Typically the current request path.
    /// </param>
    /// <returns>
    /// A <see cref="ProblemDetailsError"/> with status 400 and structured validation errors.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when modelState or instance is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when modelState is valid (contains no errors) or instance is whitespace.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Only keys with errors are included in the resulting error dictionary.
    /// Empty error messages are filtered out.
    /// </para>
    /// <para>
    /// This method is useful for creating consistent validation error responses in controller actions
    /// that manually validate models or add custom validation errors to ModelState.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [HttpPost]
    /// public IActionResult CreateUser(CreateUserDto dto)
    /// {
    ///     if (!ModelState.IsValid)
    ///     {
    ///         var error = ModelState.ToProblemDetailsError(HttpContext.Request.Path);
    ///         return Result&lt;User, ProblemDetailsError&gt;.Error(error).ToActionResult();
    ///     }
    ///     
    ///     // ... process valid model
    /// }
    /// </code>
    /// </example>
    public static ProblemDetailsError ToProblemDetailsError(
        this ModelStateDictionary modelState,
        string instance)
    {
        ArgumentNullException.ThrowIfNull(modelState);

        if (string.IsNullOrWhiteSpace(instance))
        {
            throw new ArgumentException("Instance cannot be null or whitespace.", nameof(instance));
        }

        if (modelState.IsValid)
        {
            throw new ArgumentException("ModelState is valid; cannot create error from valid state.", nameof(modelState));
        }

        var errors = modelState.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value?.Errors
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message ?? "Invalid value." : e.ErrorMessage)
                .Where(msg => !string.IsNullOrWhiteSpace(msg))
                .ToArray() ?? []
        ).Where(kvp => kvp.Value.Length > 0)
         .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return ProblemDetailsErrorFactory.Validation(errors, instance);
    }

    /// <summary>
    /// Converts a <see cref="ModelStateDictionary"/> to a <see cref="ProblemDetailsError"/> with a custom detail message.
    /// </summary>
    /// <param name="modelState">The model state dictionary containing validation errors.</param>
    /// <param name="instance">The request path or identifier where the validation error occurred.</param>
    /// <param name="detail">Custom detail message to include in the error.</param>
    /// <returns>
    /// A <see cref="ProblemDetailsError"/> with status 400, structured validation errors, and custom detail message.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when modelState, instance, or detail is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when modelState is valid, or instance/detail is whitespace.
    /// </exception>
    /// <remarks>
    /// Use this overload when you want to provide a custom, context-specific detail message
    /// instead of the default validation error message.
    /// </remarks>
    /// <example>
    /// <code>
    /// if (!ModelState.IsValid)
    /// {
    ///     var error = ModelState.ToProblemDetailsError(
    ///         HttpContext.Request.Path,
    ///         "The user creation request contains invalid data. Please verify all required fields."
    ///     );
    ///     return error.ToActionResult();
    /// }
    /// </code>
    /// </example>
    public static ProblemDetailsError ToProblemDetailsError(
        this ModelStateDictionary modelState,
        string instance,
        string detail)
    {
        ArgumentNullException.ThrowIfNull(modelState);

        if (string.IsNullOrWhiteSpace(instance))
        {
            throw new ArgumentException("Instance cannot be null or whitespace.", nameof(instance));
        }

        if (string.IsNullOrWhiteSpace(detail))
        {
            throw new ArgumentException("Detail cannot be null or whitespace.", nameof(detail));
        }

        if (modelState.IsValid)
        {
            throw new ArgumentException("ModelState is valid; cannot create error from valid state.", nameof(modelState));
        }

        var errors = modelState.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value?.Errors
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message ?? "Invalid value." : e.ErrorMessage)
                .Where(msg => !string.IsNullOrWhiteSpace(msg))
                .ToArray() ?? Array.Empty<string>()
        ).Where(kvp => kvp.Value.Length > 0)
         .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return ProblemDetailsErrorFactory.Validation(errors, instance, detail);
    }

    /// <summary>
    /// Checks if the ModelState is invalid and provides the corresponding <see cref="ProblemDetailsError"/>.
    /// </summary>
    /// <param name="modelState">The model state dictionary to check.</param>
    /// <param name="instance">The request path for error reporting.</param>
    /// <param name="error">
    /// When this method returns, contains the <see cref="ProblemDetailsError"/> if ModelState is invalid;
    /// otherwise, null.
    /// </param>
    /// <returns>
    /// <c>true</c> if ModelState is invalid and <paramref name="error"/> was set;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when modelState or instance is null.</exception>
    /// <exception cref="ArgumentException">Thrown when instance is whitespace.</exception>
    /// <remarks>
    /// <para>
    /// This method follows the Try-pattern convention and is useful for early-return guard clauses
    /// in controller actions.
    /// </para>
    /// <para>
    /// Performance: This method constructs the error dictionary even when just checking validity.
    /// For read-only validation checks without error construction, use <c>ModelState.IsValid</c> directly.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [HttpPost]
    /// public IActionResult CreateUser(CreateUserDto dto)
    /// {
    ///     if (ModelState.TryGetValidationError(HttpContext.Request.Path, out var error))
    ///     {
    ///         return Result&lt;User, ProblemDetailsError&gt;.Error(error).ToActionResult();
    ///     }
    ///     
    ///     // ... process valid model
    /// }
    /// </code>
    /// </example>
    public static bool TryGetValidationError(
        this ModelStateDictionary modelState,
        string instance,
        out ProblemDetailsError? error)
    {
        ArgumentNullException.ThrowIfNull(modelState);

        if (string.IsNullOrWhiteSpace(instance))
        {
            throw new ArgumentException("Instance cannot be null or whitespace.", nameof(instance));
        }

        if (modelState.IsValid)
        {
            error = null;
            return false;
        }

        error = modelState.ToProblemDetailsError(instance);
        return true;
    }
}

