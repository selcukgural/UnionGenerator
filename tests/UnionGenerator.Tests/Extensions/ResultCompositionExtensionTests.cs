using System;
using UnionGenerator.Extensions;
using Xunit;

namespace UnionGenerator.Tests.Extensions;

/// <summary>
/// Tests for Result composition extensions (Bind, Map, MapError).
/// </summary>
public class ResultCompositionExtensionTests
{
    /// <summary>
    /// Simple test result type for composition testing.
    /// </summary>
    private sealed class TestResult
    {
        public bool IsSuccess { get; set; }
        public string? Value { get; set; }
        public string? Error { get; set; }

        public static TestResult Ok(string value) => new() { IsSuccess = true, Value = value };
        public static TestResult Failure(string error) => new() { IsSuccess = false, Error = error };
    }

    [Fact]
    public void Bind_WithSuccessAndValidBinder_ReturnsBoundResult()
    {
        // Arrange
        var result = TestResult.Ok("hello");
        
        // Act
        dynamic boundResult = ((dynamic)result).Bind<string, string, string>(
            value => TestResult.Ok(value.ToUpper())
        );

        // Assert
        Assert.True(boundResult.IsSuccess);
        Assert.Equal("HELLO", boundResult.Value);
    }

    [Fact]
    public void Bind_WithErrorAndBinder_ReturnsErrorWithoutCallingBinder()
    {
        // Arrange
        var result = TestResult.Failure("error");
        var binderCalled = false;

        // Act
        dynamic boundResult = ((dynamic)result).Bind<string, string, string>(
            value => { binderCalled = true; return TestResult.Ok(value); }
        );

        // Assert
        Assert.False(boundResult.IsSuccess);
        Assert.Equal("error", boundResult.Error);
        Assert.False(binderCalled); // Binder should not be called for error case
    }

    [Fact]
    public void Bind_ChainMultipleOperations_WorksCorrectly()
    {
        // Arrange
        var result = TestResult.Ok("hello");

        // Act - chain multiple Bind operations
        dynamic chained = ((dynamic)result)
            .Bind<string, string, string>(value => TestResult.Ok(value.ToUpper()))
            .Bind<string, string, string>(value => TestResult.Ok(value + "!"));

        // Assert
        Assert.True(chained.IsSuccess);
        Assert.Equal("HELLO!", chained.Value);
    }

    [Fact]
    public void Bind_WithErrorInChain_StopsProcessing()
    {
        // Arrange
        var result = TestResult.Ok("hello");
        var secondBinderCalled = false;

        // Act - chain with error in middle
        dynamic chained = ((dynamic)result)
            .Bind<string, string, string>(value => TestResult.Failure("validation error"))
            .Bind<string, string, string>(value => 
            { 
                secondBinderCalled = true; 
                return TestResult.Ok(value); 
            });

        // Assert
        Assert.False(chained.IsSuccess);
        Assert.Equal("validation error", chained.Error);
        Assert.False(secondBinderCalled); // Second binder not called due to error
    }

    [Fact]
    public void Bind_WithNullResult_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            TestResult? result = null;
            ((dynamic)result!).Bind<string, string, string>(v => TestResult.Ok(v));
        });
    }

    [Fact]
    public void Bind_WithNullBinder_ThrowsArgumentNullException()
    {
        // Arrange
        var result = TestResult.Ok("hello");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ((dynamic)result).Bind<string, string, string>(null!)
        );
    }

    [Fact]
    public void Map_WithSuccessAndMapper_ReturnsMappedResult()
    {
        // Arrange
        var result = TestResult.Ok("hello");

        // Act
        dynamic mapped = ((dynamic)result).Map<string, string, string>(value => value.ToUpper());

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Equal("HELLO", mapped.Value);
    }

    [Fact]
    public void Map_WithError_ReturnsErrorUnchanged()
    {
        // Arrange
        var result = TestResult.Failure("error");

        // Act
        dynamic mapped = ((dynamic)result).Map<string, string, string>(value => value.ToUpper());

        // Assert
        Assert.False(mapped.IsSuccess);
        Assert.Equal("error", mapped.Error);
    }

    [Fact]
    public void Map_ChainMultipleTransforms_WorksCorrectly()
    {
        // Arrange
        var result = TestResult.Ok("hello");

        // Act - chain multiple Map operations
        dynamic mapped = ((dynamic)result)
            .Map<string, string, string>(value => value.ToUpper())
            .Map<string, string, string>(value => value + "!");

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Equal("HELLO!", mapped.Value);
    }

    [Fact]
    public void Map_WithNullResult_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            TestResult? result = null;
            ((dynamic)result!).Map<string, string, string>(v => v);
        });
    }

    [Fact]
    public void Map_WithNullMapper_ThrowsArgumentNullException()
    {
        // Arrange
        var result = TestResult.Ok("hello");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ((dynamic)result).Map<string, string, string>(null!)
        );
    }

    [Fact]
    public void MapError_WithErrorAndMapper_ReturnsMappedError()
    {
        // Arrange
        var result = TestResult.Failure("validation failed");

        // Act
        dynamic mapped = ((dynamic)result).MapError<string, string, string>(
            error => $"Error: {error}"
        );

        // Assert
        Assert.False(mapped.IsSuccess);
        Assert.Equal("Error: validation failed", mapped.Error);
    }

    [Fact]
    public void MapError_WithSuccess_ReturnsSuccessUnchanged()
    {
        // Arrange
        var result = TestResult.Ok("hello");

        // Act
        dynamic mapped = ((dynamic)result).MapError<string, string, string>(
            error => $"Error: {error}"
        );

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Equal("hello", mapped.Value);
    }

    [Fact]
    public void MapError_WithNullResult_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            TestResult? result = null;
            ((dynamic)result!).MapError<string, string, string>(e => e);
        });
    }

    [Fact]
    public void MapError_WithNullMapper_ThrowsArgumentNullException()
    {
        // Arrange
        var result = TestResult.Failure("error");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ((dynamic)result).MapError<string, string, string>(null!)
        );
    }

    [Fact]
    public void Bind_LeftIdentity_HoldsMonadicLaw()
    {
        // Left identity: Bind(Return(x), f) == f(x)
        // Arrange
        var value = "hello";
        Func<string, dynamic> f = v => TestResult.Ok(v.ToUpper());

        // Act
        var result1 = TestResult.Ok(value);
        dynamic left = ((dynamic)result1).Bind<string, string, string>(f);
        dynamic right = f(value);

        // Assert
        Assert.True(left.IsSuccess);
        Assert.True(right.IsSuccess);
        Assert.Equal(left.Value, right.Value);
    }

    [Fact]
    public void Bind_RightIdentity_HoldsMonadicLaw()
    {
        // Right identity: Bind(m, Return) == m
        // Arrange
        var result = TestResult.Ok("hello");

        // Act
        dynamic bound = ((dynamic)result).Bind<string, string, string>(
            v => TestResult.Ok(v)
        );

        // Assert
        Assert.True(bound.IsSuccess);
        Assert.Equal(result.Value, bound.Value);
    }

    [Fact]
    public void Bind_Associativity_HoldsMonadicLaw()
    {
        // Associativity: Bind(Bind(m, f), g) == Bind(m, x => Bind(f(x), g))
        // Arrange
        var result = TestResult.Ok("hello");
        Func<string, dynamic> f = v => TestResult.Ok(v.ToUpper());
        Func<string, dynamic> g = v => TestResult.Ok(v + "!");

        // Act
        dynamic left = ((dynamic)result)
            .Bind<string, string, string>(f)
            .Bind<string, string, string>(g);

        dynamic right = ((dynamic)result).Bind<string, string, string>(
            v => ((dynamic)f(v)).Bind<string, string, string>(g)
        );

        // Assert
        Assert.True(left.IsSuccess);
        Assert.True(right.IsSuccess);
        Assert.Equal(left.Value, right.Value);
    }
}

