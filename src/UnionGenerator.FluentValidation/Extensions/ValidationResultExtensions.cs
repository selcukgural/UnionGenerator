using FluentValidation.Results;
using UnionGenerator.AspNetCore;

namespace UnionGenerator.FluentValidation.Extensions;

/// <summary>
/// Provides extension methods for converting FluentValidation validation results to union error types.
/// </summary>
/// <remarks>
/// <para>
/// These extensions facilitate the conversion of FluentValidation <see cref="ValidationResult"/>
/// instances into <see cref="ProblemDetailsError"/> that can be used in Result union error cases.
/// </para>
/// <para>
/// The extensions follow ASP.NET Core's ValidationProblemDetails convention where validation errors
/// are structured as a dictionary of field names to error message arrays.
/// </para>
/// <para>
/// Thread-safety: These extension methods are stateless and safe for concurrent use.
/// </para>
/// </remarks>
public static class ValidationResultExtensions
{
    /// <summary>
    /// Converts a FluentValidation <see cref="ValidationResult"/> to a <see cref="ProblemDetailsError"/>.
    /// </summary>
    /// <param name="validationResult">The FluentValidation validation result.</param>
    /// <param name="instance">
    /// The request path or identifier where the validation error occurred.
    /// Typically the current request path.
    /// </param>
    /// <returns>
    /// A <see cref="ProblemDetailsError"/> with status 400 and structured validation errors.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when validationResult or instance is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when validationResult is valid (contains no errors) or instance is whitespace.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method groups validation errors by property name (case-sensitive).
    /// Empty property names are mapped to an empty string key.
    /// </para>
    /// <para>
    /// Only errors with non-empty error messages are included.
    /// </para>
    /// <para>
    /// Performance: O(n) where n is the number of validation errors.
    /// Groups errors in a single pass using LINQ GroupBy.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var validator = new CreateUserValidator();
    /// var validationResult = validator.Validate(dto);
    /// 
    /// if (!validationResult.IsValid)
    /// {
    ///     var error = validationResult.ToProblemDetailsError(httpContext.Request.Path);
    ///     return Result&lt;User, ProblemDetailsError&gt;.Error(error);
    /// }
    /// </code>
    /// </example>
    public static ProblemDetailsError ToProblemDetailsError(
        this ValidationResult validationResult,
        string instance)
    {
        ArgumentNullException.ThrowIfNull(validationResult);

        if (string.IsNullOrWhiteSpace(instance))
        {
            throw new ArgumentException("Instance cannot be null or whitespace.", nameof(instance));
        }

        if (validationResult.IsValid)
        {
            throw new ArgumentException("ValidationResult is valid; cannot create error from valid state.", nameof(validationResult));
        }

        var errors = validationResult.Errors
            .Where(e => !string.IsNullOrWhiteSpace(e.ErrorMessage))
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        return ProblemDetailsErrorFactory.Validation(errors, instance);
    }

    /// <summary>
    /// Converts a FluentValidation <see cref="ValidationResult"/> to a <see cref="ProblemDetailsError"/> with a custom detail message.
    /// </summary>
    /// <param name="validationResult">The FluentValidation validation result.</param>
    /// <param name="instance">The request path or identifier where the validation error occurred.</param>
    /// <param name="detail">Custom detail message to include in the error.</param>
    /// <returns>
    /// A <see cref="ProblemDetailsError"/> with status 400, structured validation errors, and custom detail message.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when validationResult, instance, or detail is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when validationResult is valid, or instance/detail is whitespace.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This overload allows you to provide a custom detail message while maintaining
    /// the structured validation error format.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var error = validationResult.ToProblemDetailsError(
    ///     httpContext.Request.Path,
    ///     "The user creation request failed validation."
    /// );
    /// </code>
    /// </example>
    public static ProblemDetailsError ToProblemDetailsError(
        this ValidationResult validationResult,
        string instance,
        string detail)
    {
        ArgumentNullException.ThrowIfNull(detail);

        if (string.IsNullOrWhiteSpace(detail))
        {
            throw new ArgumentException("Detail cannot be null or whitespace.", nameof(detail));
        }

        var error = validationResult.ToProblemDetailsError(instance);
        return error with { Detail = detail };
    }

    /// <summary>
    /// Converts a FluentValidation <see cref="ValidationResult"/> to a <see cref="ProblemDetailsError"/> asynchronously.
    /// </summary>
    /// <param name="validationResultTask">The task representing the async validation operation.</param>
    /// <param name="instance">The request path or identifier where the validation error occurred.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// A task representing the async operation, containing a <see cref="ProblemDetailsError"/> if validation failed,
    /// or null if validation succeeded.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when validationResultTask or instance is null.</exception>
    /// <exception cref="ArgumentException">Thrown when instance is whitespace.</exception>
    /// <remarks>
    /// <para>
    /// This method awaits the validation result and returns null if validation succeeded,
    /// or a ProblemDetailsError if validation failed.
    /// </para>
    /// <para>
    /// This is useful when working with async validators and you want to convert
    /// the result directly in an async pipeline.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var validator = new CreateUserValidator();
    /// var error = await validator.ValidateAsync(dto, cancellationToken)
    ///     .ToProblemDetailsErrorIfInvalidAsync(httpContext.Request.Path, cancellationToken);
    /// 
    /// if (error is not null)
    /// {
    ///     return Result&lt;User, ProblemDetailsError&gt;.Error(error);
    /// }
    /// </code>
    /// </example>
    public static async Task<ProblemDetailsError?> ToProblemDetailsErrorIfInvalidAsync(
        this Task<ValidationResult> validationResultTask,
        string instance,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(validationResultTask);

        if (string.IsNullOrWhiteSpace(instance))
        {
            throw new ArgumentException("Instance cannot be null or whitespace.", nameof(instance));
        }

        var validationResult = await validationResultTask.ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        return validationResult.IsValid
            ? null
            : validationResult.ToProblemDetailsError(instance);
    }
}

