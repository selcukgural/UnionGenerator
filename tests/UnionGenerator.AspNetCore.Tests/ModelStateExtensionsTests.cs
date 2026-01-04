using System;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using UnionGenerator.AspNetCore.Extensions;
using Xunit;

namespace UnionGenerator.AspNetCore.Tests;

/// <summary>
/// Tests for ModelStateExtensions - conversion of ASP.NET Core model validation errors.
/// </summary>
public class ModelStateExtensionsTests
{
    /// <summary>
    /// Tests that ToProblemDetailsError correctly converts ModelState with validation errors.
    /// </summary>
    [Fact]
    public void ToProblemDetailsError_WithValidationErrors_CreatesValidationError()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Email", "Email is required.");
        modelState.AddModelError("Age", "Age must be at least 18.");

        // Act
        var error = modelState.ToProblemDetailsError("/api/users");

        // Assert
        error.Status.Should().Be(400);
        error.Title.Should().Be("One or more validation errors occurred.");
        error.Instance.Should().Be("/api/users");
        error.Errors.Should().NotBeNull();
        error.Errors.Should().ContainKey("Email");
        error.Errors.Should().ContainKey("Age");
        error.Errors!["Email"].Should().Contain("Email is required.");
        error.Errors["Age"].Should().Contain("Age must be at least 18.");
    }

    /// <summary>
    /// Tests that ToProblemDetailsError with custom detail message includes it.
    /// </summary>
    [Fact]
    public void ToProblemDetailsErrorWithDetail_IncludesCustomDetail()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Name", "Name is invalid.");
        var customDetail = "Please check the submitted data.";

        // Act
        var error = modelState.ToProblemDetailsError("/api/users", customDetail);

        // Assert
        error.Detail.Should().Be(customDetail);
        error.Errors.Should().ContainKey("Name");
    }

    /// <summary>
    /// Tests that ToProblemDetailsError throws when modelState is null.
    /// </summary>
    [Fact]
    public void ToProblemDetailsError_WithNullModelState_ThrowsArgumentNullException()
    {
        // Arrange
        ModelStateDictionary? modelState = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => modelState.ToProblemDetailsError("/api/users")
        );
    }

    /// <summary>
    /// Tests that ToProblemDetailsError throws when instance is null.
    /// </summary>
    [Fact]
    public void ToProblemDetailsError_WithNullInstance_ThrowsArgumentException()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Field", "Error message.");

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => modelState.ToProblemDetailsError(null!)
        );
    }

    /// <summary>
    /// Tests that ToProblemDetailsError throws when instance is empty or whitespace.
    /// </summary>
    [Fact]
    public void ToProblemDetailsError_WithEmptyInstance_ThrowsArgumentException()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Field", "Error message.");

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => modelState.ToProblemDetailsError("   ")
        );
    }

    /// <summary>
    /// Tests that ToProblemDetailsError throws when modelState is valid.
    /// </summary>
    [Fact]
    public void ToProblemDetailsError_WithValidModelState_ThrowsArgumentException()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        // Don't add any errors - modelState is valid

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => modelState.ToProblemDetailsError("/api/users")
        );
    }

    /// <summary>
    /// Tests that ToProblemDetailsError with custom detail throws when modelState is valid.
    /// </summary>
    [Fact]
    public void ToProblemDetailsErrorWithDetail_WithValidModelState_ThrowsArgumentException()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        var customDetail = "Custom detail message.";

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => modelState.ToProblemDetailsError("/api/users", customDetail)
        );
    }

    /// <summary>
    /// Tests that ToProblemDetailsErrorWithDetail throws when detail is null.
    /// </summary>
    [Fact]
    public void ToProblemDetailsErrorWithDetail_WithNullDetail_ThrowsArgumentNullException()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Field", "Error message.");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => modelState.ToProblemDetailsError("/api/users", null!)
        );
    }

    /// <summary>
    /// Tests that ToProblemDetailsErrorWithDetail throws when detail is whitespace.
    /// </summary>
    [Fact]
    public void ToProblemDetailsErrorWithDetail_WithEmptyDetail_ThrowsArgumentException()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Field", "Error message.");

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => modelState.ToProblemDetailsError("/api/users", "   ")
        );
    }

    /// <summary>
    /// Tests that ToProblemDetailsError filters out empty error messages.
    /// </summary>
    [Fact]
    public void ToProblemDetailsError_FilterEmptyErrorMessages()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Field1", "Error 1");
        modelState.AddModelError("Field1", ""); // Empty message
        modelState.AddModelError("Field2", "Error 2");

        // Act
        var error = modelState.ToProblemDetailsError("/api/test");

        // Assert
        error.Errors.Should().NotBeNull();
        error.Errors!["Field1"].Should().HaveCount(1);
        error.Errors["Field1"].Should().Contain("Error 1");
        error.Errors["Field2"].Should().Contain("Error 2");
    }

    /// <summary>
    /// Tests that ToProblemDetailsError handles multiple errors on the same field.
    /// </summary>
    [Fact]
    public void ToProblemDetailsError_WithMultipleErrorsOnSameField_IncludesAll()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Email", "Email is required.");
        modelState.AddModelError("Email", "Email format is invalid.");

        // Act
        var error = modelState.ToProblemDetailsError("/api/users");

        // Assert
        error.Errors.Should().NotBeNull();
        error.Errors!["Email"].Should().HaveCount(2);
        error.Errors["Email"].Should().Contain("Email is required.");
        error.Errors["Email"].Should().Contain("Email format is invalid.");
    }

    /// <summary>
    /// Tests that ToProblemDetailsError extracts exception messages when error message is missing.
    /// </summary>
    [Fact]
    public void ToProblemDetailsError_WithExceptionInsteadOfMessage_ExtractsExceptionMessage()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        var exceptionMessage = "Custom exception message";
        modelState.AddModelError("Field", exceptionMessage);

        // Act
        var error = modelState.ToProblemDetailsError("/api/test");

        // Assert
        error.Errors.Should().NotBeNull();
        error.Errors!["Field"].Should().Contain(exceptionMessage);
    }

    /// <summary>
    /// Tests that ToProblemDetailsError filters out error messages when neither message nor exception is available.
    /// </summary>
    [Fact]
    public void ToProblemDetailsError_WithoutMessageOrException_FiltersOutField()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Field", ""); // Empty error message
        modelState.AddModelError("ValidField", "Valid error message");

        // Act
        var error = modelState.ToProblemDetailsError("/api/test");

        // Assert
        error.Errors.Should().NotBeNull();
        error.Errors!.Should().NotContainKey("Field"); // Field with empty message is filtered out
        error.Errors.Should().ContainKey("ValidField");
        error.Errors["ValidField"].Should().Contain("Valid error message");
    }

    /// <summary>
    /// Tests that ToProblemDetailsError only includes fields with errors.
    /// </summary>
    [Fact]
    public void ToProblemDetailsError_OnlyIncludesFieldsWithErrors()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.SetModelValue("ValidField", null, null);
        modelState.AddModelError("InvalidField", "This field has errors.");

        // Act
        var error = modelState.ToProblemDetailsError("/api/test");

        // Assert
        error.Errors.Should().ContainKey("InvalidField");
        error.Errors.Should().NotContainKey("ValidField");
    }

    /// <summary>
    /// Tests that ToProblemDetailsError handles special characters in field names and error messages.
    /// </summary>
    [Fact]
    public void ToProblemDetailsError_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("user.Email", "Invalid email: test@example.com is taken");
        modelState.AddModelError("addresses[0].City", "City contains special chars: São Paulo");

        // Act
        var error = modelState.ToProblemDetailsError("/api/users");

        // Assert
        error.Errors.Should().NotBeNull();
        error.Errors!.Should().ContainKey("user.Email");
        error.Errors.Should().ContainKey("addresses[0].City");
        error.Errors["user.Email"].Should().Contain("Invalid email: test@example.com is taken");
        error.Errors["addresses[0].City"].Should().Contain("City contains special chars: São Paulo");
    }

    /// <summary>
    /// Tests that the error has correct HTTP status code for validation errors.
    /// </summary>
    [Fact]
    public void ToProblemDetailsError_HasCorrectHttpStatusCode()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Field", "Error");

        // Act
        var error = modelState.ToProblemDetailsError("/api/test");

        // Assert
        error.Status.Should().Be(400);
    }

    /// <summary>
    /// Tests that ToProblemDetailsError creates proper ProblemDetails type URL.
    /// </summary>
    [Fact]
    public void ToProblemDetailsError_HasCorrectProblemDetailsType()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Field", "Error");

        // Act
        var error = modelState.ToProblemDetailsError("/api/test");

        // Assert
        error.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.1");
    }
}

