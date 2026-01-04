# FluentValidation Integration Example

This example demonstrates how to use UnionGenerator with FluentValidation to implement validation-driven workflows with automatic error-to-result mapping.

## Features Demonstrated

1. **Fluent Validators**: Define validation rules declaratively
2. **Validation Results**: Convert FluentValidation results to ProblemDetailsError
3. **Error Mapping**: Automatic field → error message transformation
4. **Type-Safe Validation**: Compile-time checking of validators
5. **RFC 7807 Compliance**: Errors map to standard ProblemDetails format
6. **Batch Processing**: Validate multiple items with consistent error handling

## Running the Example

```bash
cd examples/fluentvalidation-example
dotnet run
```

## What This Does

### 1. Define DTOs (Input Models)

```csharp
public class CreateUserDto
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public int Age { get; set; }
    public required string Username { get; set; }
}
```

### 2. Create Validators

```csharp
public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .Length(2, 50).WithMessage("First name must be 2-50 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be valid.");

        RuleFor(x => x.Age)
            .GreaterThanOrEqualTo(18).WithMessage("Age must be 18 or older.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .Length(3, 20).WithMessage("Username must be 3-20 characters.");
    }
}
```

### 3. Validate and Convert

```csharp
var validator = new CreateUserDtoValidator();
var dto = new CreateUserDto { Email = "invalid" };

var result = validator.Validate(dto);

// Convert to RFC 7807 compliant error
var error = result.ToProblemDetailsError("/api/users");

// Now use in Result pattern
var userResult = result.IsValid
    ? UserCreationResult.Success(new User { /* ... */ })
    : UserCreationResult.Failed(error.Errors ?? new());
```

### 4. Pattern Match on Results

```csharp
var result = CreateUser(dto);

result.Match(
    success: user => Console.WriteLine($"Created: {user.FullName}"),
    failed: errors => Console.WriteLine($"Validation failed: {errors.Count} fields")
);
```

## Error Response Structure

When validation fails, errors are structured as:

```json
{
  "type": "https://api.example.com/errors/validation-failed",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "The request contains invalid data.",
  "instance": "/api/users",
  "errors": {
    "FirstName": [
      "First name is required."
    ],
    "Email": [
      "Email is required.",
      "Email must be a valid email address."
    ],
    "Age": [
      "Age must be at least 18 years old."
    ]
  }
}
```

## Use Cases

### 1. Web API Validation (Controller)

```csharp
[HttpPost]
public IActionResult CreateUser([FromBody] CreateUserDto dto)
{
    var validator = new CreateUserDtoValidator();
    var result = validator.Validate(dto);

    if (!result.IsValid)
    {
        var error = result.ToProblemDetailsError(Request.Path);
        return BadRequest(error);
    }

    var user = new User { /* map from dto */ };
    return Created($"/api/users/{user.Id}", user);
}
```

### 2. Service Layer Validation

```csharp
public class UserService
{
    private readonly IValidator<CreateUserDto> _validator;

    public UserService(IValidator<CreateUserDto> validator)
    {
        _validator = validator;
    }

    public async Task<UserCreationResult> CreateAsync(CreateUserDto dto)
    {
        var result = _validator.Validate(dto);

        if (!result.IsValid)
        {
            var problemError = result.ToProblemDetailsError($"/users/{Guid.NewGuid()}");
            return UserCreationResult.Failed(problemError.Errors ?? new());
        }

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Age = dto.Age,
            Username = dto.Username
        };

        // Save to database
        await _userRepository.AddAsync(user);
        return UserCreationResult.Success(user);
    }
}
```

### 3. Batch Validation

```csharp
public IEnumerable<(CreateUserDto dto, ValidationResult validation)> ValidateMany(
    IEnumerable<CreateUserDto> dtos)
{
    var validator = new CreateUserDtoValidator();

    return dtos
        .Select(dto => (dto, validation: validator.Validate(dto)))
        .ToList();
}

// Usage
var dtos = GetUserDtos();
var results = ValidateMany(dtos);

var validDtos = results
    .Where(x => x.validation.IsValid)
    .Select(x => x.dto)
    .ToList();

var invalidResults = results
    .Where(x => !x.validation.IsValid)
    .Select(x => x.validation.ToProblemDetailsError("/api/users/batch"))
    .ToList();
```

### 4. Async Validation

```csharp
public class UniqueEmailValidator : AbstractValidator<CreateUserDto>
{
    private readonly IUserRepository _userRepository;

    public UniqueEmailValidator(IUserRepository userRepository)
    {
        _userRepository = userRepository;

        RuleFor(x => x.Email)
            .MustAsync(async (email, ct) =>
                !(await _userRepository.ExistsAsync(email, ct)),
                "Email is already in use.")
            .When(x => x.Email != null);
    }
}
```

### 5. Conditional Validation

```csharp
public class ProductValidator : AbstractValidator<CreateProductDto>
{
    public ProductValidator()
    {
        RuleFor(x => x.Stock)
            .GreaterThan(0)
            .WithMessage("Stock must be positive.")
            .When(x => x.Price > 1000);  // Only for expensive items
    }
}
```

## Common Patterns

### Pattern 1: Validate Before Processing

```csharp
public async Task<Result<ProcessedOrder, ProblemDetailsError>> ProcessOrderAsync(
    CreateOrderDto dto,
    CancellationToken ct)
{
    var validator = new CreateOrderValidator();
    var result = await validator.ValidateAsync(dto, ct);

    if (!result.IsValid)
    {
        var error = result.ToProblemDetailsError("/orders");
        return Result<ProcessedOrder, ProblemDetailsError>.Error(error);
    }

    // Process validated order...
}
```

### Pattern 2: Composite Validators

```csharp
public class CompositeUserValidator : AbstractValidator<CreateUserDto>
{
    public CompositeUserValidator(
        IValidator<CreateUserDto> basicValidator,
        IValidator<CreateUserDto> businessRuleValidator)
    {
        Include(basicValidator);
        Include(businessRuleValidator);
    }
}
```

### Pattern 3: Custom Error Messages

```csharp
public class LocalizedUserValidator : AbstractValidator<CreateUserDto>
{
    public LocalizedUserValidator(IStringLocalizer<UserMessages> localizer)
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage(localizer["EmailRequired"])
            .EmailAddress()
            .WithMessage(localizer["EmailInvalid"]);
    }
}
```

### Pattern 4: Transform Valid DTO to Domain Entity

```csharp
public async Task<UserCreationResult> CreateUserAsync(CreateUserDto dto)
{
    var validator = new CreateUserDtoValidator();
    var validationResult = await validator.ValidateAsync(dto);

    if (!validationResult.IsValid)
    {
        return UserCreationResult.Failed(
            validationResult.ToProblemDetailsError("/users").Errors ?? new());
    }

    var user = new User
    {
        FirstName = dto.FirstName.Trim(),
        LastName = dto.LastName.Trim(),
        Email = dto.Email.ToLowerInvariant(),
        Age = dto.Age,
        Username = dto.Username.ToLowerInvariant()
    };

    return UserCreationResult.Success(user);
}
```

## Best Practices

### ✅ DO

- Define validators as separate classes (single responsibility)
- Use dependency injection for validators
- Validate as early as possible (at API boundary)
- Include clear, actionable error messages
- Use FluentValidation's built-in validators when possible
- Implement custom validators for business logic
- Test validators thoroughly with valid/invalid inputs
- Return structured ProblemDetailsError consistently
- Use async validators for database/external checks

### ❌ DON'T

- Don't validate in multiple layers (choose one)
- Don't use generic error messages ("Invalid")
- Don't mix validation logic across controllers/services
- Don't forget to handle async validation
- Don't ignore validation errors in silent failures
- Don't create validators for every DTO (reuse common patterns)
- Don't perform expensive operations during validation
- Don't assume client-side validation is sufficient
- Don't store sensitive data in error messages

## Testing Validators

```csharp
[Fact]
public void Validate_InvalidEmail_ReturnsFalse()
{
    // Arrange
    var validator = new CreateUserDtoValidator();
    var dto = new CreateUserDto
    {
        FirstName = "John",
        LastName = "Doe",
        Email = "invalid-email",
        Age = 25,
        Username = "johndoe"
    };

    // Act
    var result = validator.Validate(dto);

    // Assert
    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, x => x.PropertyName == "Email");
}
```

## Related Documentation

- [UnionGenerator README](../../src/UnionGenerator/README.md)
- [UnionGenerator.FluentValidation README](../../src/UnionGenerator.FluentValidation/README.md)
- [UnionGenerator.AspNetCore README](../../src/UnionGenerator.AspNetCore/README.md)
- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [RFC 7807 - Problem Details for HTTP APIs](https://tools.ietf.org/html/rfc7807)

---

**Ready to validate?** Run `dotnet run` and see validation in action! 🚀

