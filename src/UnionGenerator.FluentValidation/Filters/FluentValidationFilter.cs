using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using UnionGenerator.AspNetCore;
using UnionGenerator.FluentValidation.Extensions;

namespace UnionGenerator.FluentValidation.Filters;

/// <summary>
/// Action filter that automatically validates action parameters using FluentValidation validators
/// and converts validation failures to <see cref="ProblemDetailsError"/> responses.
/// </summary>
/// <remarks>
/// <para>
/// This filter runs before action execution and validates all action parameters that have
/// registered FluentValidation validators in the DI container.
/// </para>
/// <para>
/// If validation fails, the filter short-circuits the request pipeline and returns
/// a 400 Bad Request response with structured validation errors.
/// </para>
/// <para>
/// Thread-safety: This filter is instantiated per request and is not required to be thread-safe.
/// </para>
/// <para>
/// Performance: O(n) where n is the number of action parameters. Uses DI service resolution
/// to find validators, which may have overhead for types without registered validators.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register globally
/// builder.Services.AddControllers(options =>
/// {
///     options.Filters.Add&lt;FluentValidationFilter&gt;();
/// });
/// 
/// // Or use attribute-based registration
/// [ServiceFilter(typeof(FluentValidationFilter))]
/// public class UsersController : ControllerBase
/// {
///     [HttpPost]
///     public IActionResult CreateUser(CreateUserDto dto)
///     {
///         // Validation is already done by the filter
///         // This code only runs if validation succeeds
///     }
/// }
/// </code>
/// </example>
public sealed class FluentValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentValidationFilter"/> class.
    /// </summary>
    /// <param name="serviceProvider">
    /// The service provider used to resolve validators from the DI container.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when serviceProvider is null.</exception>
    public FluentValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Executes the filter logic to validate action parameters.
    /// </summary>
    /// <param name="context">The action executing context.</param>
    /// <param name="next">The delegate to execute the next filter or action.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// <para>
    /// This method iterates through all action parameters and attempts to resolve
    /// an IValidator for each parameter's type. If a validator is found and validation fails,
    /// the request is short-circuited with a validation error response.
    /// </para>
    /// <para>
    /// If no validators are found or all validations pass, the request continues normally.
    /// </para>
    /// <para>
    /// Validation is performed synchronously. For async validation, use FluentValidationAsyncFilter.
    /// </para>
    /// </remarks>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var parameter in context.ActionDescriptor.Parameters)
        {
            if (!context.ActionArguments.TryGetValue(parameter.Name, out var argumentValue) || argumentValue is null)
            {
                continue;
            }

            var argumentType = argumentValue.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
            var validator = _serviceProvider.GetService(validatorType) as IValidator;

            if (validator is null)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argumentValue);
            var validationResult = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted).ConfigureAwait(false);

            if (!validationResult.IsValid)
            {
                var instance = context.HttpContext.Request.Path.Value;
                if (string.IsNullOrWhiteSpace(instance))
                {
                    instance = "/";
                }

                var error = validationResult.ToProblemDetailsError(
                    instance,
                    context.HttpContext.RequestAborted);
                context.Result = new ObjectResult(error)
                {
                    StatusCode = error.Status
                };
                return;
            }
        }

        await next().ConfigureAwait(false);
    }
}

