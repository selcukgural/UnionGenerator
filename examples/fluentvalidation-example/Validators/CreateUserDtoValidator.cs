using FluentValidation;
using FluentValidationExample.Models;

namespace FluentValidationExample.Validators;

/// <summary>
/// Validator for CreateUserDto.
/// Ensures user data meets all business requirements.
/// </summary>
public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    /// <summary>
    /// Initializes the validator with all validation rules.
    /// </summary>
    public CreateUserDtoValidator()
    {
        // First name validation
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.")
            .Length(2, 50)
            .WithMessage("First name must be between 2 and 50 characters.");

        // Last name validation
        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required.")
            .Length(2, 50)
            .WithMessage("Last name must be between 2 and 50 characters.");

        // Email validation
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Email must be a valid email address.")
            .MaximumLength(100)
            .WithMessage("Email must not exceed 100 characters.");

        // Age validation
        RuleFor(x => x.Age)
            .GreaterThanOrEqualTo(18)
            .WithMessage("User must be at least 18 years old.")
            .LessThanOrEqualTo(150)
            .WithMessage("Age must be a valid value.");

        // Username validation
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username is required.")
            .Length(3, 20)
            .WithMessage("Username must be between 3 and 20 characters.")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Username can only contain letters, numbers, underscores, and hyphens.");
    }
}

/// <summary>
/// Validator for CreateProductDto.
/// Ensures product data meets all business requirements.
/// </summary>
public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    /// <summary>
    /// Initializes the validator with all validation rules.
    /// </summary>
    public CreateProductDtoValidator()
    {
        // Product name validation
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Product name is required.")
            .Length(3, 200)
            .WithMessage("Product name must be between 3 and 200 characters.");

        // Description validation
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .Length(10, 2000)
            .WithMessage("Description must be between 10 and 2000 characters.");

        // Price validation
        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Price must be greater than zero.")
            .LessThanOrEqualTo(999999.99m)
            .WithMessage("Price must not exceed 999,999.99.");

        // Stock validation
        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Stock quantity cannot be negative.")
            .LessThanOrEqualTo(999999)
            .WithMessage("Stock quantity must not exceed 999,999.");

        // SKU validation
        RuleFor(x => x.Sku)
            .NotEmpty()
            .WithMessage("SKU is required.")
            .Length(3, 50)
            .WithMessage("SKU must be between 3 and 50 characters.")
            .Matches(@"^[A-Z0-9\-]+$")
            .WithMessage("SKU must contain only uppercase letters, numbers, and hyphens.");
    }
}

