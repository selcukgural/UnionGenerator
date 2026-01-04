using System;
using UnionGenerator.Attributes;
using UnionGenerator.Extensions;
using Xunit;

namespace UnionGenerator.Tests.Extensions;

/// <summary>
/// Tests for MatchVoid extension methods for unit-like result matching.
/// </summary>
public class MatchVoidExtensionTests
{
    /// <summary>
    /// Simple test result type with Unit-like success.
    /// </summary>
    private sealed class ValidationResult
    {
        public bool IsSuccess { get; set; }
        public string? Error { get; set; }
    }

    [Fact]
    public void MatchVoid_WithSuccess_ExecutesOkAction()
    {
        // Arrange
        var result = new ValidationResult { IsSuccess = true, Error = null };
        var okExecuted = false;
        var errorExecuted = false;

        // Act
        result.MatchVoid(
            ok: () => { okExecuted = true; },
            error: _ => { errorExecuted = true; }
        );

        // Assert
        Assert.True(okExecuted);
        Assert.False(errorExecuted);
    }

    [Fact]
    public void MatchVoid_WithError_ExecutesErrorAction()
    {
        // Arrange
        var result = new ValidationResult { IsSuccess = false, Error = "Invalid input" };
        var okExecuted = false;
        var errorValue = "";

        // Act
        result.MatchVoid(
            ok: () => { okExecuted = true; },
            error: err => { errorValue = err; }
        );

        // Assert
        Assert.False(okExecuted);
        Assert.Equal("Invalid input", errorValue);
    }

    [Fact]
    public void MatchVoid_WithNullResult_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            ValidationResult? result = null;
            result!.MatchVoid(
                ok: () => { },
                error: _ => { }
            );
        });
    }

    [Fact]
    public void MatchVoid_WithNullOkAction_ThrowsArgumentNullException()
    {
        // Arrange
        var result = new ValidationResult { IsSuccess = true, Error = null };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            result.MatchVoid(
                ok: null!,
                error: _ => { }
            )
        );
    }

    [Fact]
    public void MatchVoid_WithNullErrorAction_ThrowsArgumentNullException()
    {
        // Arrange
        var result = new ValidationResult { IsSuccess = false, Error = "error" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            result.MatchVoid(
                ok: () => { },
                error: null!
            )
        );
    }

    [Fact]
    public void MatchVoid_Optional_WithBothNull_DoesNothing()
    {
        // Arrange
        var result = new ValidationResult { IsSuccess = true, Error = null };

        // Act - should not throw
        result.MatchVoid(ok: null, error: null);

        // Assert - passed without exception
    }

    [Fact]
    public void MatchVoid_Optional_WithOnlyOkHandler_ExecutesOkOnSuccess()
    {
        // Arrange
        var result = new ValidationResult { IsSuccess = true, Error = null };
        var okExecuted = false;

        // Act
        result.MatchVoid(
            ok: () => { okExecuted = true; },
            error: null
        );

        // Assert
        Assert.True(okExecuted);
    }

    [Fact]
    public void MatchVoid_Optional_WithOnlyErrorHandler_ExecutesErrorOnFailure()
    {
        // Arrange
        var result = new ValidationResult { IsSuccess = false, Error = "validation failed" };
        var errorMessage = "";

        // Act
        result.MatchVoid(
            ok: null,
            error: err => { errorMessage = err; }
        );

        // Assert
        Assert.Equal("validation failed", errorMessage);
    }

    [Fact]
    public void MatchVoid_Optional_WithOnlyErrorHandler_SkipsOnSuccess()
    {
        // Arrange
        var result = new ValidationResult { IsSuccess = true, Error = null };
        var errorExecuted = false;

        // Act
        result.MatchVoid(
            ok: null,
            error: _ => { errorExecuted = true; }
        );

        // Assert
        Assert.False(errorExecuted);
    }

    [Fact]
    public void MatchVoid_MultipleInvocations_WorkCorrectly()
    {
        // Arrange
        var result = new ValidationResult { IsSuccess = false, Error = "error1" };
        var callCount = 0;
        var lastError = "";

        // Act - invoke multiple times
        result.MatchVoid(
            ok: () => { callCount++; },
            error: err => { callCount++; lastError = err; }
        );

        result.MatchVoid(
            ok: () => { callCount++; },
            error: err => { callCount++; lastError = err; }
        );

        // Assert
        Assert.Equal(4, callCount); // 2 errors
        Assert.Equal("error1", lastError);
    }

    [Fact]
    public void MatchVoid_WithDifferentErrorTypes_WorksWithGenericType()
    {
        // Arrange - using dynamic to test generic type inference
        dynamic result = new ValidationResult { IsSuccess = false, Error = "test error" };

        // Act
        var capturedError = "";
        result.MatchVoid(
            ok: () => { },
            error: (string err) => { capturedError = err; }
        );

        // Assert
        Assert.Equal("test error", capturedError);
    }

    [Fact]
    public void MatchVoid_WithMissingSuccessProperty_ThrowsInvalidOperationException()
    {
        // Arrange - object without IsSuccess property
        var obj = new object();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            ((dynamic)obj).MatchVoid(
                ok: () => { },
                error: (string _) => { }
            )
        );
    }

    [Fact]
    public void MatchVoid_WithMissingErrorProperty_ThrowsInvalidOperationException()
    {
        // Arrange - object with IsSuccess but no Error property
        var obj = new { IsSuccess = false };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            ((dynamic)obj).MatchVoid(
                ok: () => { },
                error: (string _) => { }
            )
        );
    }
}

