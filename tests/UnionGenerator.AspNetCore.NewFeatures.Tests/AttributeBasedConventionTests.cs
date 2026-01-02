using UnionGenerator.AspNetCore.Attributes;
}
    private class TestErrorWithoutAttribute { }

    private class TestErrorWith409 { }
    [UnionStatusCode(409)]

    private class TestErrorWith404 { }
    [UnionStatusCode(404)]
    // Test fixtures with [UnionStatusCode] on the type

    }
        Assert.Equal(409, statusCode2);
        Assert.Equal(409, statusCode1);
        Assert.True(success2);
        Assert.True(success1);
        // Assert

        var success2 = convention.TryGetStatusCode(error, out var statusCode2);
        var success1 = convention.TryGetStatusCode(error, out var statusCode1);
        // Act

        var error = new TestErrorWith409();
        var convention = new AttributeBasedConvention();
        // Arrange
    {
    public void TryGetStatusCode_CachesAttributeLookup()
    [Fact]

    }
        Assert.Equal(100, priority);
        // Assert

        var priority = convention.Priority;
        // Act

        var convention = new AttributeBasedConvention();
        // Arrange
    {
    public void Priority_IsHighest()
    [Fact]

    }
        Assert.Equal(0, statusCode);
        Assert.False(success);
        // Assert

        var success = convention.TryGetStatusCode(null!, out var statusCode);
        // Act

        var convention = new AttributeBasedConvention();
        // Arrange
    {
    public void TryGetStatusCode_WithNullError_ReturnsFalse()
    [Fact]

    }
        Assert.Equal(0, statusCode);
        Assert.False(success);
        // Assert

        var success = convention.TryGetStatusCode(error, out var statusCode);
        // Act

        var error = new TestErrorWithoutAttribute();
        var convention = new AttributeBasedConvention();
        // Arrange
    {
    public void TryGetStatusCode_WithoutAttribute_ReturnsFalse()
    [Fact]

    }
        Assert.Equal(404, statusCode);
        Assert.True(success);
        // Assert

        var success = convention.TryGetStatusCode(error, out var statusCode);
        // Act

        var error = new TestErrorWith404();
        var convention = new AttributeBasedConvention();
        // Arrange
    {
    public void TryGetStatusCode_WithUnionStatusCodeAttribute_ReturnsCorrectStatusCode()
    [Fact]
{
public sealed class AttributeBasedConventionTests
/// </summary>
/// Tests for AttributeBasedConvention - priority 100, fastest path.
/// <summary>

namespace UnionGenerator.AspNetCore.NewFeatures.Tests;

using Xunit;
using UnionGenerator.AspNetCore.Conventions;

