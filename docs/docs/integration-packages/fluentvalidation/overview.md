---
sidebar_position: 1
---

# FluentValidation Integration

The UnionGenerator.FluentValidation package provides seamless integration with FluentValidation, making it easy to map validation results to union error types.

## Overview

This integration enables:

- Automatic conversion from ValidationResult to union types
- Property path mapping to error dictionaries
- Error aggregation
- Type-safe validation error handling
- Integration with ASP.NET Core model validation

## Installation

```bash
dotnet add package UnionGenerator.FluentValidation
```

**Requirements:**
- .NET 6.0 or later
- FluentValidation 11.0 or later
- UnionGenerator package

## Quick Start

### Define Validation Result Union

```csharp
[GenerateUnion]
public partial record ValidationResult<T>
{
    public static partial ValidationResult<T> Valid(T value);
    public static partial ValidationResult<T> Invalid(Dictionary<string, string[]> errors);
}
```

### Create Validator

```csharp
using FluentValidation;

public class CreateUserDto
{
    public string Email { get; set; }
    public string Password { get; set; }
    public int Age { get; set; }
}

public class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters");

        RuleFor(x => x.Age)
            .GreaterThanOrEqualTo(18).WithMessage("Must be 18 or older");
    }
}
```

### Validate and Convert

```csharp
public class UserService
{
    private readonly IValidator<CreateUserDto> _validator;

    public ValidationResult<User> CreateUser(CreateUserDto dto)
    {
        var validationResult = _validator.Validate(dto);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return ValidationResult<User>.Invalid(errors);
        }

        var user = new User
        {
            Email = dto.Email,
            // ... create user
        };

        return ValidationResult<User>.Valid(user);
    }
}
```

## Extension Methods

Create reusable extension methods:

```csharp
using FluentValidation.Results;

public static class ValidationExtensions
{
    public static ValidationResult<T> ToUnionResult<T>(
        this FluentValidation.Results.ValidationResult result,
        T value)
    {
        if (result.IsValid)
        {
            return ValidationResult<T>.Valid(value);
        }

        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        return ValidationResult<T>.Invalid(errors);
    }

    public static ValidationResult<T> ValidateAndCreate<T>(
        this IValidator<T> validator,
        T value)
    {
        var result = validator.Validate(value);
        return result.ToUnionResult(value);
    }
}

// Usage
public ValidationResult<User> CreateUser(CreateUserDto dto)
{
    var result = _validator.Validate(dto);
    
    if (!result.IsValid)
    {
        return result.ToUnionResult<User>(null!);
    }

    var user = CreateUserFromDto(dto);
    return ValidationResult<User>.Valid(user);
}
```

## ASP.NET Core Integration

### Controller Action Filter

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class ValidateModelAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value.Errors.Any())
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors));
        }
    }
}

// Usage
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    [HttpPost]
    [ValidateModel]
    public IActionResult Create(CreateUserDto dto)
    {
        var result = _service.CreateUser(dto);

        return result.Match(
            valid => CreatedAtAction(nameof(Get), new { id = valid.Value.Id }, valid.Value),
            invalid => BadRequest(new ValidationProblemDetails(invalid.Errors))
        );
    }
}
```

### Minimal API

```csharp
app.MapPost("/api/users", async (
    CreateUserDto dto,
    IValidator<CreateUserDto> validator,
    IUserService service) =>
{
    var result = validator.ValidateAndCreate(dto);

    if (result.IsInvalid)
    {
        return Results.BadRequest(result.Match(
            valid => null,
            invalid => new { Errors = invalid.Errors }
        ));
    }

    var user = await service.CreateUserAsync(dto);
    return Results.Created($"/api/users/{user.Id}", user);
});
```

## Complex Validation Scenarios

### Async Validation

```csharp
public class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    private readonly IUserRepository _repository;

    public CreateUserValidator(IUserRepository repository)
    {
        _repository = repository;

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(BeUniqueEmail)
            .WithMessage("Email already exists");
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellation)
    {
        return !await _repository.EmailExistsAsync(email, cancellation);
    }
}

// Usage
public async Task<ValidationResult<User>> CreateUserAsync(CreateUserDto dto)
{
    var validationResult = await _validator.ValidateAsync(dto);

    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return ValidationResult<User>.Invalid(errors);
    }

    var user = await CreateUserFromDtoAsync(dto);
    return ValidationResult<User>.Valid(user);
}
```

### Conditional Validation

```csharp
public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .When(x => x.OrderType == OrderType.Retail);

        RuleFor(x => x.CompanyName)
            .NotEmpty()
            .When(x => x.OrderType == OrderType.Corporate);

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Order must contain at least one item");

        RuleForEach(x => x.Items)
            .SetValidator(new OrderItemValidator());
    }
}
```

### Custom Error Codes

```csharp
[GenerateUnion]
public partial record ValidationResult<T>
{
    public static partial ValidationResult<T> Valid(T value);
    public static partial ValidationResult<T> Invalid(
        Dictionary<string, ValidationError[]> errors
    );
}

public record ValidationError(string Code, string Message);

public class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithErrorCode("EMAIL_REQUIRED")
            .WithMessage("Email is required");

        RuleFor(x => x.Email)
            .EmailAddress()
            .WithErrorCode("EMAIL_INVALID")
            .WithMessage("Invalid email format");
    }
}

// Conversion
public static ValidationResult<T> ToUnionResult<T>(
    this FluentValidation.Results.ValidationResult result,
    T value)
{
    if (result.IsValid)
    {
        return ValidationResult<T>.Valid(value);
    }

    var errors = result.Errors
        .GroupBy(e => e.PropertyName)
        .ToDictionary(
            g => g.Key,
            g => g.Select(e => new ValidationError(e.ErrorCode, e.ErrorMessage)).ToArray()
        );

    return ValidationResult<T>.Invalid(errors);
}
```

## Testing Validation

```csharp
public class CreateUserValidatorTests
{
    private readonly CreateUserValidator _validator = new();

    [Fact]
    public void Validate_EmptyEmail_ReturnsInvalid()
    {
        var dto = new CreateUserDto { Email = "", Password = "password123", Age = 25 };
        var result = _validator.ValidateAndCreate(dto);

        Assert.True(result.IsInvalid);
        result.Match(
            valid => Assert.Fail("Expected invalid result"),
            invalid =>
            {
                Assert.Contains("Email", invalid.Errors.Keys);
                Assert.Contains("required", invalid.Errors["Email"][0], StringComparison.OrdinalIgnoreCase);
            }
        );
    }

    [Fact]
    public void Validate_ValidData_ReturnsValid()
    {
        var dto = new CreateUserDto 
        { 
            Email = "user@example.com", 
            Password = "password123", 
            Age = 25 
        };
        
        var result = _validator.ValidateAndCreate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        var dto = new CreateUserDto { Email = "", Password = "123", Age = 15 };
        var result = _validator.ValidateAndCreate(dto);

        result.Match(
            valid => Assert.Fail("Expected invalid result"),
            invalid =>
            {
                Assert.True(invalid.Errors.Count >= 3);
                Assert.Contains("Email", invalid.Errors.Keys);
                Assert.Contains("Password", invalid.Errors.Keys);
                Assert.Contains("Age", invalid.Errors.Keys);
            }
        );
    }
}
```

## Best Practices

### Separate Validation Logic

```csharp
// ✅ Keep validation rules in validators
public class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(8);
    }
}

// ✅ Keep business logic in services
public class UserService
{
    public ValidationResult<User> CreateUser(CreateUserDto dto)
    {
        var validationResult = _validator.Validate(dto);
        
        if (!validationResult.IsValid)
        {
            return ConvertToUnionResult(validationResult);
        }

        // Business logic here
        var user = CreateUserFromDto(dto);
        return ValidationResult<User>.Valid(user);
    }
}
```

### Reusable Error Mapping

```csharp
public static class ValidationResultMapper
{
    public static Dictionary<string, string[]> ToErrorDictionary(
        this FluentValidation.Results.ValidationResult result)
    {
        return result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );
    }
}
```

### Dependency Injection

```csharp
// Startup.cs or Program.cs
services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();

// Automatic validation in ASP.NET Core
services.AddFluentValidationAutoValidation();
```

## Key Takeaways

✅ **Extension methods** simplify validation result conversion  
✅ **Type safety** ensures all validation errors are handled  
✅ **ASP.NET Core** integration works seamlessly  
✅ **Testable** validation logic  
✅ **Reusable patterns** across application
