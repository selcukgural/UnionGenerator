# UnionGenerator.FluentValidation

FluentValidation integration for UnionGenerator, providing seamless conversion of FluentValidation validation results to Result unions with ProblemDetails-compatible errors.

## Features

- 🎯 **Automatic Conversion**: Convert `ValidationResult` to `ProblemDetailsError` with a single extension method
- 🔄 **Async Support**: Full async/await support with `CancellationToken` propagation
- 🎨 **Action Filter**: Automatic model validation in ASP.NET Core with `FluentValidationFilter`
- 📋 **Structured Errors**: Validation errors mapped to field → string[] format (RFC 7807 compatible)
- 🚀 **Easy Setup**: Single method registration with `AddUnionFluentValidation()`

## Installation

```bash
dotnet add package UnionGenerator.FluentValidation
```

## Quick Start

### 1. Define Your Validator

```csharp
using FluentValidation;

public class CreateUserDto
{
    public string Email { get; set; }
    public int Age { get; set; }
    public string Username { get; set; }
}

public class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be valid.");
        
        RuleFor(x => x.Age)
            .GreaterThanOrEqualTo(18).WithMessage("Age must be at least 18.");
        
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .Length(3, 20).WithMessage("Username must be between 3 and 20 characters.");
    }
}
```

### 2. Register Services

```csharp
using UnionGenerator.FluentValidation.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register FluentValidation with UnionGenerator integration
builder.Services.AddUnionFluentValidation<CreateUserValidator>();

// Add controllers with the validation filter
builder.Services.AddControllers(options =>
{
    options.Filters.Add<FluentValidationFilter>();
});

var app = builder.Build();
app.MapControllers();
app.Run();
```

### 3. Use in Controllers

```csharp
using Microsoft.AspNetCore.Mvc;
using UnionGenerator.AspNetCore;
using UnionGenerator.FluentValidation.Extensions;
using UnionGenerator.Attributes;

[GenerateUnion]
public partial class Result<T, E>
{
    public static Result<T, E> Ok(T value) => new OkCase(value);
    public static Result<T, E> Error(E error) => new ErrorCase(error);
}

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IValidator<CreateUserDto> _validator;
    
    public UsersController(IValidator<CreateUserDto> validator)
    {
        _validator = validator;
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserDto dto,
        CancellationToken cancellationToken)
    {
        // Option 1: Manual validation with extension method
        var validationResult = await _validator.ValidateAsync(dto, cancellationToken);
        
        if (!validationResult.IsValid)
        {
            var error = validationResult.ToProblemDetailsError(HttpContext.Request.Path);
            return Result<User, ProblemDetailsError>.Error(error).ToActionResult();
        }
        
        var user = new User { Id = 1, Email = dto.Email, Age = dto.Age };
        return Result<User, ProblemDetailsError>.Ok(user).ToActionResult();
    }
    
    [HttpPost("auto")]
    [ServiceFilter(typeof(FluentValidationFilter))] // Option 2: Automatic validation
    public IActionResult CreateUserAuto([FromBody] CreateUserDto dto)
    {
        // If we reach here, validation has already passed
        var user = new User { Id = 1, Email = dto.Email, Age = dto.Age };
        return Result<User, ProblemDetailsError>.Ok(user).ToActionResult();
    }
}
```

## Advanced Usage

### Async Validation with Error Check

```csharp
[HttpPost]
public async Task<IActionResult> CreateUser(
    [FromBody] CreateUserDto dto,
    CancellationToken cancellationToken)
{
    var error = await _validator
        .ValidateAsync(dto, cancellationToken)
        .ToProblemDetailsErrorIfInvalidAsync(HttpContext.Request.Path, cancellationToken);
    
    if (error is not null)
    {
        return Result<User, ProblemDetailsError>.Error(error).ToActionResult();
    }
    
    // Process valid model
    var user = await _userService.CreateUserAsync(dto, cancellationToken);
    return Result<User, ProblemDetailsError>.Ok(user).ToActionResult();
}
```

### Custom Detail Message

```csharp
var validationResult = await _validator.ValidateAsync(dto, cancellationToken);

if (!validationResult.IsValid)
{
    var error = validationResult.ToProblemDetailsError(
        HttpContext.Request.Path,
        "The user creation request failed validation. Please correct the errors and try again."
    );
    return Result<User, ProblemDetailsError>.Error(error).ToActionResult();
}
```

### Global Filter Registration

```csharp
builder.Services.AddControllers(options =>
{
    // Apply validation filter globally to all controllers
    options.Filters.Add<FluentValidationFilter>();
});
```


### Custom Validator Lifetime

```csharp
// Register validators as singletons (for stateless validators)
builder.Services.AddUnionFluentValidationWithLifetime<CreateUserValidator>(
    ServiceLifetime.Singleton
);
```

## Error Response Format

Validation errors are returned as RFC 7807 ProblemDetails with structured validation errors:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "See the errors property for details.",
  "instance": "/api/users",
  "errors": {
    "Email": [
      "Email is required.",
      "Email must be valid."
    ],
    "Age": [
      "Age must be at least 18."
    ],
    "Username": [
      "Username is required."
    ]
  }
}
```

## Best Practices

1. **Use Async Validation**: Always use `ValidateAsync` for async validators and pass `CancellationToken`
2. **Scoped Validators**: Use scoped lifetime for validators that depend on scoped services (e.g., DbContext)
3. **Singleton Validators**: Use singleton lifetime only for completely stateless validators
4. **Meaningful Messages**: Provide clear, actionable error messages in your validators
5. **Group Related Rules**: Use RuleSets for different validation scenarios (Create vs Update)

## Performance Considerations

- **Validator Resolution**: The filter resolves validators from DI per request, which has minimal overhead
- **Validation Overhead**: FluentValidation validation is O(n) where n is the number of rules
- **Async Validation**: Use async validation for I/O-bound rules (database checks, API calls)
- **Property Name Resolution**: Default property name resolution is fast; custom resolvers may add overhead

## Thread Safety

- Extension methods are stateless and thread-safe
- `FluentValidationFilter` is instantiated per request and not required to be thread-safe
- Validators should be thread-safe if registered as singletons

## API Reference

### ValidationResultExtensions

- `ToProblemDetailsError(ValidationResult, string)`: Convert validation result to ProblemDetailsError
- `ToProblemDetailsError(ValidationResult, string, string)`: Convert with custom detail message
- `ToProblemDetailsErrorIfInvalidAsync(Task<ValidationResult>, string, CancellationToken)`: Async conversion with null return on success

### ServiceCollectionExtensions

- `AddUnionFluentValidation(IServiceCollection, Action<ValidatorOptions>?)`: Register with default settings
- `AddUnionFluentValidation<TAssemblyMarker>(IServiceCollection, Action<ValidatorOptions>?)`: Register with assembly scanning
- `AddUnionFluentValidationWithLifetime<TAssemblyMarker>(IServiceCollection, ServiceLifetime, Action<ValidatorOptions>?)`: Register with custom lifetime

### FluentValidationFilter

- Automatic action filter for ASP.NET Core
- Validates all action parameters with registered validators
- Short-circuits on validation failure with 400 response

## License

This project is part of UnionGenerator and uses the same license.

