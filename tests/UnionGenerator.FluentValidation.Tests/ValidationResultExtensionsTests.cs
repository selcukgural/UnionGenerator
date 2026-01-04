using FluentValidation;
using FluentValidation.Results;
using UnionGenerator.AspNetCore;
using UnionGenerator.FluentValidation.Extensions;

namespace UnionGenerator.FluentValidation.Tests;

/// <summary>
/// Tests for ValidationResultExtensions.
/// </summary>
public class ValidationResultExtensionsTests
{
    /// <summary>
    /// Test DTO for validation.
    /// </summary>
    private sealed record TestDto(string Email, int Age, string Username);

    /// <summary>
    /// Test validator for TestDto.
    /// </summary>
    private sealed class TestDtoValidator : AbstractValidator<TestDto>
    {
        public TestDtoValidator()
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

    [Fact]
    public void ToProblemDetailsError_WithInvalidResult_ReturnsProblemDetailsError()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "", Age: 15, Username: "ab");
        var validationResult = validator.Validate(dto);
        var instance = "/api/test";

        // Act
        var error = validationResult.ToProblemDetailsError(instance);

        // Assert
        Assert.NotNull(error);
        Assert.Equal(400, error.Status);
        Assert.Equal(instance, error.Instance);
        Assert.NotNull(error.Errors);
        Assert.Equal(3, error.Errors.Count);
        Assert.Contains("Email", error.Errors.Keys);
        Assert.Contains("Age", error.Errors.Keys);
        Assert.Contains("Username", error.Errors.Keys);
    }

    [Fact]
    public void ToProblemDetailsError_WithValidResult_ThrowsArgumentException()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "test@example.com", Age: 25, Username: "testuser");
        var validationResult = validator.Validate(dto);
        var instance = "/api/test";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            validationResult.ToProblemDetailsError(instance));
        
        Assert.Contains("ValidationResult is valid", exception.Message);
    }

    [Fact]
    public void ToProblemDetailsError_WithNullValidationResult_ThrowsArgumentNullException()
    {
        // Arrange
        ValidationResult validationResult = null!;
        var instance = "/api/test";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            validationResult.ToProblemDetailsError(instance));
    }

    [Fact]
    public void ToProblemDetailsError_WithNullInstance_ThrowsArgumentException()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "", Age: 15, Username: "");
        var validationResult = validator.Validate(dto);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            validationResult.ToProblemDetailsError(null!));
        
        Assert.Contains("Instance cannot be null", exception.Message);
    }

    [Fact]
    public void ToProblemDetailsError_WithWhitespaceInstance_ThrowsArgumentException()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "", Age: 15, Username: "");
        var validationResult = validator.Validate(dto);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            validationResult.ToProblemDetailsError("   "));
        
        Assert.Contains("Instance cannot be null", exception.Message);
    }

    [Fact]
    public void ToProblemDetailsError_GroupsErrorsByPropertyName()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "", Age: 25, Username: "testuser");
        var validationResult = validator.Validate(dto);
        var instance = "/api/test";

        // Act
        var error = validationResult.ToProblemDetailsError(instance);

        // Assert
        Assert.NotNull(error.Errors);
        Assert.Single(error.Errors);
        Assert.Contains("Email", error.Errors.Keys);
        Assert.Equal(2, error.Errors["Email"].Length);
        Assert.Contains("Email is required.", error.Errors["Email"]);
        Assert.Contains("Email must be valid.", error.Errors["Email"]);
    }

    [Fact]
    public void ToProblemDetailsError_WithCustomDetail_ReturnsCustomDetail()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "", Age: 15, Username: "ab");
        var validationResult = validator.Validate(dto);
        var instance = "/api/test";
        var customDetail = "Custom validation error message.";

        // Act
        var error = validationResult.ToProblemDetailsError(instance, customDetail);

        // Assert
        Assert.Equal(customDetail, error.Detail);
    }

    [Fact]
    public void ToProblemDetailsError_WithNullCustomDetail_ThrowsArgumentNullException()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "", Age: 15, Username: "ab");
        var validationResult = validator.Validate(dto);
        var instance = "/api/test";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            validationResult.ToProblemDetailsError(instance, null!));
    }

    [Fact]
    public async Task ToProblemDetailsErrorIfInvalidAsync_WithInvalidResult_ReturnsError()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "", Age: 15, Username: "ab");
        var instance = "/api/test";

        // Act
        var error = await validator.ValidateAsync(dto)
            .ToProblemDetailsErrorIfInvalidAsync(instance);

        // Assert
        Assert.NotNull(error);
        Assert.Equal(400, error.Status);
        Assert.Equal(instance, error.Instance);
    }

    [Fact]
    public async Task ToProblemDetailsErrorIfInvalidAsync_WithValidResult_ReturnsNull()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "test@example.com", Age: 25, Username: "testuser");
        var instance = "/api/test";

        // Act
        var error = await validator.ValidateAsync(dto)
            .ToProblemDetailsErrorIfInvalidAsync(instance);

        // Assert
        Assert.Null(error);
    }

    [Fact]
    public async Task ToProblemDetailsErrorIfInvalidAsync_WithCancellationToken_PropagatesCancellation()
    {
        // Arrange
        var validator = new TestDtoValidator();
        var dto = new TestDto(Email: "", Age: 15, Username: "ab");
        var instance = "/api/test";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await validator.ValidateAsync(dto, cts.Token)
                .ToProblemDetailsErrorIfInvalidAsync(instance, cts.Token));
    }

    [Fact]
    public async Task ToProblemDetailsErrorIfInvalidAsync_WithNullTask_ThrowsArgumentNullException()
    {
        // Arrange
        Task<ValidationResult> task = null!;
        var instance = "/api/test";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await task.ToProblemDetailsErrorIfInvalidAsync(instance));
    }

    [Fact]
    public void ToProblemDetailsError_FiltersEmptyErrorMessages()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new ValidationFailure("Email", "Email is required."),
            new ValidationFailure("Email", ""), // Empty message
            new ValidationFailure("Username", "   ") // Whitespace message
        };
        var validationResult = new ValidationResult(failures);
        var instance = "/api/test";

        // Act
        var error = validationResult.ToProblemDetailsError(instance);

        // Assert
        Assert.NotNull(error.Errors);
        Assert.Single(error.Errors);
        Assert.Contains("Email", error.Errors.Keys);
        Assert.Single(error.Errors["Email"]);
        Assert.Equal("Email is required.", error.Errors["Email"][0]);
    }

    [Fact]
    public void ToProblemDetailsError_HandlesMultipleErrorsForSameProperty()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new ValidationFailure("Email", "Email is required."),
            new ValidationFailure("Email", "Email must be valid."),
            new ValidationFailure("Email", "Email must not exceed 100 characters.")
        };
        var validationResult = new ValidationResult(failures);
        var instance = "/api/test";

        // Act
        var error = validationResult.ToProblemDetailsError(instance);

        // Assert
        Assert.NotNull(error.Errors);
        Assert.Single(error.Errors);
        Assert.Equal(3, error.Errors["Email"].Length);
    }
}

