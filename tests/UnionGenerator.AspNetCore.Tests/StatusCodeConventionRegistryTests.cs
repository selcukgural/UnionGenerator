using UnionGenerator.AspNetCore.Conventions;
using Xunit;

namespace UnionGenerator.AspNetCore.Tests;

/// <summary>
/// Tests for StatusCodeConventionRegistry with all conventions.
/// </summary>
public sealed class StatusCodeConventionRegistryTests
{
    [Fact]
    public void Default_ContainsAllBuiltInConventions()
    {
        // Act
        var registry = StatusCodeConventionRegistry.Default;

        // Assert
        Assert.NotNull(registry);
        Assert.Equal(4, registry.Count); // Attribute, Property, ProblemDetails, Name-based
    }

    [Fact]
    public void TryInferStatusCode_WithAttributeBasedError_UsesAttributeConvention()
    {
        // Arrange
        var registry = StatusCodeConventionRegistry.Default;
        var error = new NotFoundErrorWithAttribute();

        // Act
        var success = registry.TryInferStatusCode(error, out var statusCode);

        // Assert
        Assert.True(success);
        Assert.Equal(404, statusCode);
    }

    [Fact]
    public void TryInferStatusCode_WithNameBasedError_FallsBackToNameConvention()
    {
        // Arrange
        var registry = StatusCodeConventionRegistry.Default;
        var error = new ValidationError(); // Name-based: "Validation" → 400

        // Act
        var success = registry.TryInferStatusCode(error, out var statusCode);

        // Assert
        Assert.True(success);
        Assert.Equal(400, statusCode);
    }

    [Fact]
    public void TryInferStatusCode_WithPropertyBasedError_DetectsStatusCodeProperty()
    {
        // Arrange
        var registry = StatusCodeConventionRegistry.Default;
        var error = new ErrorWithStatusCodeProperty();

        // Act
        var success = registry.TryInferStatusCode(error, out var statusCode);

        // Assert
        Assert.True(success);
        Assert.Equal(422, statusCode);
    }

    [Fact]
    public void TryInferStatusCode_WithUnknownError_ReturnsFalse()
    {
        // Arrange
        var registry = StatusCodeConventionRegistry.Default;
        var error = new UnknownError();

        // Act
        var success = registry.TryInferStatusCode(error, out var statusCode);

        // Assert
        Assert.False(success);
        Assert.Equal(0, statusCode);
    }

    [Fact]
    public void InferStatusCode_WithUnknownError_ReturnsDefaultStatusCode()
    {
        // Arrange
        var registry = StatusCodeConventionRegistry.Default;
        var error = new UnknownError();

        // Act
        var statusCode = registry.InferStatusCode(error, defaultStatusCode: 500);

        // Assert
        Assert.Equal(500, statusCode);
    }

    [Fact]
    public void Register_CustomConvention_AddedToRegistry()
    {
        // Arrange
        var registry = new StatusCodeConventionRegistry();
        var convention = new AlwaysReturn418Convention();

        // Act
        registry.Register(convention);

        // Assert
        Assert.Equal(1, registry.Count);
        Assert.True(registry.TryInferStatusCode(new object(), out var statusCode));
        Assert.Equal(418, statusCode);
    }

    [Fact]
    public void Register_MultipleConventions_SortedByPriority()
    {
        // Arrange
        var registry = new StatusCodeConventionRegistry();
        registry.Register(new LowPriorityConvention()); // Priority 10
        registry.Register(new HighPriorityConvention()); // Priority 100

        // Act
        var conventions = registry.GetConventions();

        // Assert
        Assert.Equal(2, conventions.Count);
        Assert.Equal(100, conventions[0].Priority); // Highest first
        Assert.Equal(10, conventions[1].Priority);
    }

    // Test fixtures
    [UnionGenerator.AspNetCore.Attributes.UnionStatusCode(404)]
    private class NotFoundErrorWithAttribute;

    private class ValidationError;

    private class ErrorWithStatusCodeProperty
    {
        public int StatusCode => 422;
    }

    private class UnknownError;

    private sealed class AlwaysReturn418Convention : IStatusCodeConvention
    {
        public int Priority => 50;

        public bool TryGetStatusCode(object error, out int statusCode)
        {
            statusCode = 418;
            return true;
        }
    }

    private sealed class HighPriorityConvention : IStatusCodeConvention
    {
        public int Priority => 100;

        public bool TryGetStatusCode(object error, out int statusCode)
        {
            statusCode = 0;
            return false;
        }
    }

    private sealed class LowPriorityConvention : IStatusCodeConvention
    {
        public int Priority => 10;

        public bool TryGetStatusCode(object error, out int statusCode)
        {
            statusCode = 0;
            return false;
        }
    }
}

