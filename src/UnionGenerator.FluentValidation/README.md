# UnionGenerator.FluentValidation

Integrate FluentValidation seamlessly with UnionGenerator. Convert validation results to structured errors automatically, eliminating manual conversion logic and duplicate validation code.

## ❓ Why This Package?

### The Problem

Without integration, you write validation twice and convert results manually:

```csharp
// ❌ Manual validation + conversion boilerplate
[HttpPost]
public IActionResult CreateUser(CreateUserDto dto)
{
    var validator = new CreateUserValidator();
    var validationResult = await validator.ValidateAsync(dto);
    
    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(
                x => x.Key,
                x => x.Select(e => e.ErrorMessage).ToArray()
            );
        return UnprocessableEntity(errors); // Tedious!
    }
    
    var result = _service.CreateUser(dto);
    return result.ToActionResult();
}
```

### The Solution

```csharp
// ✅ Clean integration: automatic error handling with filter
[HttpPost]
[ServiceFilter(typeof(FluentValidationFilter))]
public IActionResult CreateUser([FromBody] CreateUserDto dto)
{
    // If we reach here, DTO is valid
    var result = _service.CreateUser(dto);
    return result.ToActionResult(); // 400 for validation errors auto-handled
}
```

---

## Features

- 🎯 **Automatic Conversion**: Convert `ValidationResult` to `ProblemDetailsError` with a single extension method
- 🔄 **Async Support**: Full async/await support with `CancellationToken` propagation
- 🎨 **Action Filter**: Automatic model validation in ASP.NET Core with `FluentValidationFilter`
- 📋 **Structured Errors**: Validation errors mapped to field → string[] format (RFC 7807 compatible)
- 🚀 **Easy Setup**: Single method registration with `AddUnionFluentValidation()`

```bash
dotnet add package UnionGenerator.FluentValidation
```

## 🎯 Integration Patterns: Choose Your Style

This package provides two ways to integrate FluentValidation. Pick based on your needs:

### Pattern 1: **Manual Validation** (Fine-grained Control)

When: You want custom validation logic per endpoint or need to control error handling flow.

```csharp
[HttpPost]
public async Task<IActionResult> CreateUser(
    [FromBody] CreateUserDto dto,
    CancellationToken cancellationToken)
{
    var validationResult = await _validator.ValidateAsync(dto, cancellationToken);
    
    if (!validationResult.IsValid)
    {
        var error = validationResult.ToProblemDetailsError(HttpContext.Request.Path);
        return Result<User, ProblemDetailsError>.Error(error).ToActionResult();
    }
    
    var result = await _service.CreateUserAsync(dto, cancellationToken);
    return Result<User, ProblemDetailsError>.Ok(result).ToActionResult();
}
```

**Pros**:
- Full control over validation flow
- Can add custom business logic after validation
- Single validator can be reused

**Cons**:
- Boilerplate repeated in each endpoint
- Easy to forget validation in one endpoint

### Pattern 2: **Action Filter** (Convention-Based, Automatic)

When: You want automatic validation on all endpoints; follow REST conventions strictly.

```csharp
[HttpPost]
[ServiceFilter(typeof(FluentValidationFilter))]
public IActionResult CreateUser([FromBody] CreateUserDto dto)
{
    // If we reach here, validation passed automatically
    var result = _service.CreateUser(dto);
    return result.ToActionResult();
}
```

**Pros**:
- Zero boilerplate in controller
- Consistent validation across all endpoints
- Automatic 400 responses
- Centralized validation rules

**Cons**:
- Less control (all-or-nothing validation)
- Not ideal for complex validation logic

### Comparison Table

| Aspect | Manual | Filter |
|--------|--------|--------|
| **Setup Effort** | Medium (register validator) | Low (one attribute per endpoint) |
| **Code per Endpoint** | 5-10 lines | 1 line + attribute |
| **Reusability** | Single validator | Filter automatically applies |
| **Custom Logic** | ✅ Full control | ❌ Limited |
| **Error Handling** | ✅ Custom responses | ✅ Auto 400 |
| **Recommended For** | Complex flows | Typical REST APIs |

**Recommendation**: Start with **Pattern 2 (Filter)** for typical REST APIs. Use **Pattern 1 (Manual)** only when you need custom business logic after validation.

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

### CancellationToken Support

All validation extension methods support `CancellationToken` for graceful shutdown and timeout handling.

#### Pattern 1: Manual Validation with Cancellation

```csharp
[HttpPost]
public async Task<IActionResult> CreateUser(
    [FromBody] CreateUserDto dto,
    CancellationToken cancellationToken)
{
    // Validate with cancellation token
    var validationResult = await _validator.ValidateAsync(dto, cancellationToken);
    
    if (!validationResult.IsValid)
    {
        // CancellationToken propagates through error conversion
        var error = validationResult.ToProblemDetailsError(
            HttpContext.Request.Path,
            cancellationToken
        );
        return Result<User, ProblemDetailsError>.Error(error).ToActionResult();
    }
    
    var user = await _userService.CreateUserAsync(dto, cancellationToken);
    return Result<User, ProblemDetailsError>.Ok(user).ToActionResult();
}
```

#### Pattern 2: Async Pipeline with Cancellation

```csharp
[HttpPost]
public async Task<IActionResult> CreateUser(
    [FromBody] CreateUserDto dto,
    CancellationToken cancellationToken)
{
    // One-liner: validate and convert error if invalid
    var error = await _validator
        .ValidateAsync(dto, cancellationToken)
        .ToProblemDetailsErrorIfInvalidAsync(HttpContext.Request.Path, cancellationToken);
    
    if (error is not null)
    {
        return Result<User, ProblemDetailsError>.Error(error).ToActionResult();
    }
    
    var user = await _userService.CreateUserAsync(dto, cancellationToken);
    return Result<User, ProblemDetailsError>.Ok(user).ToActionResult();
}
```

#### Pattern 3: FluentValidationFilter (Automatic)

The filter automatically uses `HttpContext.RequestAborted` token:

```csharp
[HttpPost]
[ServiceFilter(typeof(FluentValidationFilter))]
public async Task<IActionResult> CreateUser(
    [FromBody] CreateUserDto dto,
    CancellationToken cancellationToken)
{
    // Validation already done with cancellation support
    // RequestAborted token propagated automatically by filter
    var user = await _userService.CreateUserAsync(dto, cancellationToken);
    return Result<User, ProblemDetailsError>.Ok(user).ToActionResult();
}
```

#### CancellationToken Best Practices

1. **Always Propagate**: Pass `CancellationToken` through the entire async chain
2. **Use RequestAborted**: In ASP.NET Core, use `HttpContext.RequestAborted` for client disconnection
3. **Timeout Scenarios**: Create `CancellationTokenSource` with timeout for long-running validations
4. **Graceful Shutdown**: `OperationCanceledException` is thrown when cancelled, handle it at boundaries

```csharp
// Timeout example
using var cts = CancellationTokenSource.CreateLinkedTokenSource(
    HttpContext.RequestAborted,
    new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token
);

var validationResult = await _validator.ValidateAsync(dto, cts.Token);
```

### Additional Patterns

#### Async Validation with Error Check

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
        "The user creation request failed validation. Please correct the errors and try again.",
        cancellationToken
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
- `ToProblemDetailsError(ValidationResult, string, CancellationToken)`: Convert with cancellation support
- `ToProblemDetailsError(ValidationResult, string, string, CancellationToken)`: Convert with custom detail and cancellation support
- `ToProblemDetailsErrorIfInvalidAsync(Task<ValidationResult>, string, CancellationToken)`: Async conversion with null return on success

### ServiceCollectionExtensions

- `AddUnionFluentValidation(IServiceCollection)`: Register with default settings
- `AddUnionFluentValidation<TAssemblyMarker>(IServiceCollection)`: Register with assembly scanning
- `AddUnionFluentValidationWithLifetime<TAssemblyMarker>(IServiceCollection, ServiceLifetime)`: Register with custom lifetime

### FluentValidationFilter

- Automatic action filter for ASP.NET Core
- Validates all action parameters with registered validators
- Short-circuits on validation failure with 400 response

## License

This project is part of UnionGenerator and uses the same license.

