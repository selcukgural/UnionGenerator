using UnionGenerator.AspNetCore.Attributes;
using UnionGenerator.AspNetCore.Conventions;
using Xunit;

namespace UnionGenerator.AspNetCore.Tests;

/// <summary>
/// Tests for AttributeBasedConvention status code inference.
/// </summary>
public sealed class AttributeBasedConventionTests
{
    [Fact]
    public void TryGetStatusCode_WithUnionStatusCodeAttribute_ReturnsCorrectStatusCode()
    {
        // Arrange
        var convention = new AttributeBasedConvention();
        var error = new TestErrorWith404();

        // Act
        var success = convention.TryGetStatusCode(error, out var statusCode);

        // Assert
        Assert.True(success);
        Assert.Equal(404, statusCode);
    }

    [Fact]
    public void TryGetStatusCode_WithoutAttribute_ReturnsFalse()
    {
        // Arrange
        var convention = new AttributeBasedConvention();
        var error = new TestErrorWithoutAttribute();

        // Act
        var success = convention.TryGetStatusCode(error, out var statusCode);

        // Assert
        Assert.False(success);
        Assert.Equal(0, statusCode);
    }

    [Fact]
    public void TryGetStatusCode_WithNullError_ReturnsFalse()
    {
        // Arrange
        var convention = new AttributeBasedConvention();

        // Act
        var success = convention.TryGetStatusCode(null!, out var statusCode);

        // Assert
        Assert.False(success);
        Assert.Equal(0, statusCode);
    }

    [Fact]
    public void Priority_IsHighest()
    {
        // Arrange
        var convention = new AttributeBasedConvention();

        // Act
        var priority = convention.Priority;

        // Assert
        Assert.Equal(100, priority);
    }

    [Fact]
    public void TryGetStatusCode_CachesAttributeLookup()
    {
        // Arrange
        var convention = new AttributeBasedConvention();
        var error = new TestErrorWith409();

        // Act
        var success1 = convention.TryGetStatusCode(error, out var statusCode1);
        var success2 = convention.TryGetStatusCode(error, out var statusCode2);

        // Assert
        Assert.True(success1); // Has [UnionStatusCode(409)] attribute
        Assert.True(success2); // Should be cached and return same result
        Assert.Equal(409, statusCode1);
        Assert.Equal(409, statusCode2); // Same status code from cache
    }

    // Test fixtures with [UnionStatusCode] on the type (now supported)
    [UnionStatusCode(404)]
    private class TestErrorWith404;

    [UnionStatusCode(409)]
    private class TestErrorWith409;

    private class TestErrorWithoutAttribute;
}

