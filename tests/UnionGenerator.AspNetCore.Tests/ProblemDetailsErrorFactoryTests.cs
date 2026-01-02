using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace UnionGenerator.AspNetCore.Tests;

/// <summary>
/// Tests for ProblemDetailsErrorFactory.
/// </summary>
public class ProblemDetailsErrorFactoryTests
{
    /// <summary>
    /// Tests that Validation factory method creates correct error.
    /// </summary>
    [Fact]
    public void Validation_WithErrors_CreatesValidationError()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            ["Email"] = ["Email is required."],
            ["Age"] = ["Age must be at least 18."]
        };

        // Act
        var error = ProblemDetailsErrorFactory.Validation(errors, "/api/users");

        // Assert
        error.Status.Should().Be(400);
        error.Title.Should().Be("One or more validation errors occurred.");
        error.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.1");
        error.Instance.Should().Be("/api/users");
        error.Errors.Should().NotBeNull();
        error.Errors.Should().HaveCount(2);
    }

    /// <summary>
    /// Tests that NotFound factory method creates correct error.
    /// </summary>
    [Fact]
    public void NotFound_WithResourceType_CreatesNotFoundError()
    {
        // Act
        var error = ProblemDetailsErrorFactory.NotFound(
            "/api/users/123",
            "User with ID 123 was not found.",
            "User"
        );

        // Assert
        error.Status.Should().Be(404);
        error.Title.Should().Be("User not found.");
        error.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.4");
        error.Instance.Should().Be("/api/users/123");
        error.Detail.Should().Be("User with ID 123 was not found.");
    }

    /// <summary>
    /// Tests that NotFound without resource type uses generic title.
    /// </summary>
    [Fact]
    public void NotFound_WithoutResourceType_UsesGenericTitle()
    {
        // Act
        var error = ProblemDetailsErrorFactory.NotFound(
            "/api/users/123",
            "Resource was not found."
        );

        // Assert
        error.Title.Should().Be("The requested resource was not found.");
    }

    /// <summary>
    /// Tests that Conflict factory method creates correct error.
    /// </summary>
    [Fact]
    public void Conflict_CreatesConflictError()
    {
        // Act
        var error = ProblemDetailsErrorFactory.Conflict(
            "/api/users",
            "A user with this email already exists."
        );

        // Assert
        error.Status.Should().Be(409);
        error.Title.Should().Be("A conflict occurred.");
        error.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.8");
        error.Instance.Should().Be("/api/users");
        error.Detail.Should().Be("A user with this email already exists.");
    }

    /// <summary>
    /// Tests that Unauthorized factory method creates correct error.
    /// </summary>
    [Fact]
    public void Unauthorized_CreatesUnauthorizedError()
    {
        // Act
        var error = ProblemDetailsErrorFactory.Unauthorized("/api/protected");

        // Assert
        error.Status.Should().Be(401);
        error.Title.Should().Be("Unauthorized.");
        error.Type.Should().Be("https://tools.ietf.org/html/rfc7235#section-3.1");
        error.Instance.Should().Be("/api/protected");
        error.Detail.Should().Be("Authentication is required to access this resource.");
    }

    /// <summary>
    /// Tests that Forbidden factory method creates correct error.
    /// </summary>
    [Fact]
    public void Forbidden_CreatesForbiddenError()
    {
        // Act
        var error = ProblemDetailsErrorFactory.Forbidden("/api/admin");

        // Assert
        error.Status.Should().Be(403);
        error.Title.Should().Be("Forbidden.");
        error.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.3");
        error.Instance.Should().Be("/api/admin");
        error.Detail.Should().Be("You do not have permission to access this resource.");
    }

    /// <summary>
    /// Tests that BadRequest factory method creates correct error.
    /// </summary>
    [Fact]
    public void BadRequest_CreatesBadRequestError()
    {
        // Act
        var error = ProblemDetailsErrorFactory.BadRequest(
            "/api/users",
            "Invalid request format."
        );

        // Assert
        error.Status.Should().Be(400);
        error.Title.Should().Be("Bad Request.");
        error.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.1");
        error.Instance.Should().Be("/api/users");
        error.Detail.Should().Be("Invalid request format.");
    }

    /// <summary>
    /// Tests that InternalServerError factory method creates correct error.
    /// </summary>
    [Fact]
    public void InternalServerError_CreatesServerError()
    {
        // Act
        var error = ProblemDetailsErrorFactory.InternalServerError("/api/users/123");

        // Assert
        error.Status.Should().Be(500);
        error.Title.Should().Be("An internal server error occurred.");
        error.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.6.1");
        error.Instance.Should().Be("/api/users/123");
        error.Detail.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Tests that Custom factory method creates error with custom values.
    /// </summary>
    [Fact]
    public void Custom_CreatesCustomError()
    {
        // Act
        var error = ProblemDetailsErrorFactory.Custom(
            status: 429,
            title: "Too Many Requests",
            detail: "Rate limit exceeded.",
            instance: "/api/search",
            type: "https://example.com/errors/rate-limit"
        );

        // Assert
        error.Status.Should().Be(429);
        error.Title.Should().Be("Too Many Requests");
        error.Type.Should().Be("https://example.com/errors/rate-limit");
        error.Instance.Should().Be("/api/search");
        error.Detail.Should().Be("Rate limit exceeded.");
    }

    /// <summary>
    /// Tests that Custom factory method uses about:blank when type is null.
    /// </summary>
    [Fact]
    public void Custom_WithoutType_UsesAboutBlank()
    {
        // Act
        var error = ProblemDetailsErrorFactory.Custom(
            status: 429,
            title: "Too Many Requests",
            detail: "Rate limit exceeded.",
            instance: "/api/search"
        );

        // Assert
        error.Type.Should().Be("about:blank");
    }

    /// <summary>
    /// Tests that Validation throws when errors is null.
    /// </summary>
    [Fact]
    public void Validation_WithNullErrors_ThrowsArgumentNullException()
    {
        // Act
        var act = () => ProblemDetailsErrorFactory.Validation(null!, "/api/users");

        // Assert
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("errors");
    }

    /// <summary>
    /// Tests that Validation throws when errors dictionary is empty.
    /// </summary>
    [Fact]
    public void Validation_WithEmptyErrors_ThrowsArgumentException()
    {
        // Arrange
        var emptyErrors = new Dictionary<string, string[]>();

        // Act
        var act = () => ProblemDetailsErrorFactory.Validation(emptyErrors, "/api/users");

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Errors dictionary cannot be empty*");
    }

    /// <summary>
    /// Tests that Validation with custom detail uses provided message.
    /// </summary>
    [Fact]
    public void Validation_WithCustomDetail_UsesProvidedDetail()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            ["Email"] = ["Email is required."]
        };

        // Act
        var error = ProblemDetailsErrorFactory.Validation(
            errors,
            "/api/users",
            "Custom validation message"
        );

        // Assert
        error.Detail.Should().Be("Custom validation message");
    }
}

