using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace UnionGenerator.AspNetCore.Tests;

/// <summary>
/// Tests for ProblemDetailsError record.
/// </summary>
public class ProblemDetailsErrorTests
{
    /// <summary>
    /// Tests that ProblemDetailsError can be created with required fields.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_CreatesProblemDetailsError()
    {
        // Arrange & Act
        var error = new ProblemDetailsError(
            type: "https://example.com/errors/not-found",
            title: "Not Found",
            status: 404,
            detail: "The requested resource was not found.",
            instance: "/api/users/123"
        );

        // Assert
        error.Type.Should().Be("https://example.com/errors/not-found");
        error.Title.Should().Be("Not Found");
        error.Status.Should().Be(404);
        error.Detail.Should().Be("The requested resource was not found.");
        error.Instance.Should().Be("/api/users/123");
        error.Errors.Should().BeNull();
        error.Extensions.Should().BeNull();
    }

    /// <summary>
    /// Tests that ProblemDetailsError validation errors property works correctly.
    /// </summary>
    [Fact]
    public void ErrorsProperty_WhenSet_IsAccessible()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            ["Email"] = ["Email is required.", "Email must be valid."],
            ["Age"] = ["Age must be at least 18."]
        };

        // Act
        var error = new ProblemDetailsError(
            type: "https://example.com/errors/validation",
            title: "Validation Failed",
            status: 400,
            detail: "One or more validation errors occurred.",
            instance: "/api/users"
        )
        {
            Errors = errors
        };

        // Assert
        error.Errors.Should().NotBeNull();
        error.Errors.Should().HaveCount(2);
        error.Errors!["Email"].Should().BeEquivalentTo("Email is required.", "Email must be valid.");
        error.Errors["Age"].Should().BeEquivalentTo("Age must be at least 18.");
    }

    /// <summary>
    /// Tests that ProblemDetailsError throws when type is null or whitespace.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidType_ThrowsArgumentException(string? invalidType)
    {
        // Act
        var act = () => new ProblemDetailsError(
            type: invalidType!,
            title: "Title",
            status: 400,
            detail: "Detail",
            instance: "/api/test"
        );

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Type*");
    }

    /// <summary>
    /// Tests that ProblemDetailsError throws when title is null or whitespace.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidTitle_ThrowsArgumentException(string? invalidTitle)
    {
        // Act
        var act = () => new ProblemDetailsError(
            type: "https://example.com/error",
            title: invalidTitle!,
            status: 400,
            detail: "Detail",
            instance: "/api/test"
        );

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Title*");
    }

    /// <summary>
    /// Tests that ProblemDetailsError throws when status code is invalid.
    /// </summary>
    [Theory]
    [InlineData(99)]
    [InlineData(600)]
    [InlineData(-1)]
    public void Constructor_WithInvalidStatusCode_ThrowsArgumentOutOfRangeException(int invalidStatus)
    {
        // Act
        var act = () => new ProblemDetailsError(
            type: "https://example.com/error",
            title: "Title",
            status: invalidStatus,
            detail: "Detail",
            instance: "/api/test"
        );

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithMessage("*status*");
    }

    /// <summary>
    /// Tests that ProblemDetailsError throws when instance is null or whitespace.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidInstance_ThrowsArgumentException(string? invalidInstance)
    {
        // Act
        var act = () => new ProblemDetailsError(
            type: "https://example.com/error",
            title: "Title",
            status: 400,
            detail: "Detail",
            instance: invalidInstance!
        );

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Instance*");
    }

    /// <summary>
    /// Tests that ProblemDetailsError extensions property works correctly.
    /// </summary>
    [Fact]
    public void ExtensionsProperty_WhenSet_IsAccessible()
    {
        // Arrange
        var extensions = new Dictionary<string, object?>
        {
            ["traceId"] = "12345",
            ["timestamp"] = DateTime.UtcNow
        };

        // Act
        var error = new ProblemDetailsError(
            type: "https://example.com/error",
            title: "Error",
            status: 500,
            detail: "An error occurred.",
            instance: "/api/test"
        )
        {
            Extensions = extensions
        };

        // Assert
        error.Extensions.Should().NotBeNull();
        error.Extensions.Should().HaveCount(2);
        error.Extensions!["traceId"].Should().Be("12345");
    }

    /// <summary>
    /// Tests that ProblemDetailsError supports record equality.
    /// </summary>
    [Fact]
    public void RecordEquality_WithSameValues_AreEqual()
    {
        // Arrange
        var error1 = new ProblemDetailsError(
            type: "https://example.com/error",
            title: "Error",
            status: 400,
            detail: "Detail",
            instance: "/api/test"
        );

        var error2 = new ProblemDetailsError(
            type: "https://example.com/error",
            title: "Error",
            status: 400,
            detail: "Detail",
            instance: "/api/test"
        );

        // Assert
        error1.Should().Be(error2);
    }

    /// <summary>
    /// Tests that ProblemDetailsError with expression allows modification.
    /// </summary>
    [Fact]
    public void WithExpression_ModifiesProperty_CreatesNewInstance()
    {
        // Arrange
        var original = new ProblemDetailsError(
            type: "https://example.com/error",
            title: "Error",
            status: 400,
            detail: "Detail",
            instance: "/api/test"
        );

        // Act
        var modified = original with { Status = 404, Title = "Not Found" };

        // Assert
        modified.Status.Should().Be(404);
        modified.Title.Should().Be("Not Found");
        modified.Type.Should().Be(original.Type);
        modified.Detail.Should().Be(original.Detail);
        modified.Instance.Should().Be(original.Instance);
        
        // Original should be unchanged
        original.Status.Should().Be(400);
        original.Title.Should().Be("Error");
    }
}

