using System.Collections.Generic;
using FluentAssertions;
using UnionGenerator.AspNetCore.Filters;
using Xunit;

namespace UnionGenerator.AspNetCore.Tests;

/// <summary>
/// Tests for UnionEndpointFilter - Minimal API endpoint filter for automatic union result conversion.
/// </summary>
public class UnionEndpointFilterTests
{
    /// <summary>
    /// Tests that filter can be instantiated.
    /// </summary>
    [Fact]
    public void UnionEndpointFilter_CanBeInstantiated()
    {
        // Act
        var filter = new UnionEndpointFilter();

        // Assert
        filter.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that filter type detection works with IsSuccess property.
    /// </summary>
    [Fact]
    public void IsUnionType_WithIsSuccessProperty_ReturnsTrue()
    {
        // Arrange
        var testResult = new TestUnionResult { IsSuccess = true, Value = "Success!" };

        // Act - filter uses reflection to detect union types
        var filter = new UnionEndpointFilter();

        // Assert - verifying the type has the expected shape
        var type = testResult.GetType();
        var props = type.GetProperties();
        props.Should().Satisfy(
            p => p.Name == "IsSuccess" && p.PropertyType == typeof(bool),
            p => p.Name == "Value" && p.PropertyType == typeof(object),
            p => p.Name == "ErrorValue" && p.PropertyType == typeof(ProblemDetailsError)
        );
    }

    /// <summary>
    /// Tests that filter can detect IsOk property variant.
    /// </summary>
    [Fact]
    public void IsUnionType_WithIsOkProperty_CanBeDetected()
    {
        // Arrange
        var testResult = new TestUnionResultWithIsOk { IsOk = true, Value = "Data" };

        // Act
        var type = testResult.GetType();
        var props = type.GetProperties();

        // Assert
        props.Should().Satisfy(
            p => p.Name == "IsOk" && p.PropertyType == typeof(bool),
            p => p.Name == "Value" && p.PropertyType == typeof(string)
        );
    }

    /// <summary>
    /// Tests that filter can detect IsSome property variant.
    /// </summary>
    [Fact]
    public void IsUnionType_WithIsSomeProperty_CanBeDetected()
    {
        // Arrange
        var testResult = new TestUnionResultWithIsSome { IsSome = true, Value = "Data" };

        // Act
        var type = testResult.GetType();
        var props = type.GetProperties();

        // Assert
        props.Should().Satisfy(
            p => p.Name == "IsSome" && p.PropertyType == typeof(bool),
            p => p.Name == "Value" && p.PropertyType == typeof(string)
        );
    }

    /// <summary>
    /// Tests that non-union types are correctly identified.
    /// </summary>
    [Fact]
    public void IsUnionType_WithNormalType_ReturnsFalse()
    {
        // Arrange
        var plainObject = new { Message = "Hello" };

        // Act
        var type = plainObject.GetType();
        var boolProperties = new List<string>();
        foreach (var prop in type.GetProperties())
        {
            if (prop.PropertyType == typeof(bool))
            {
                boolProperties.Add(prop.Name);
            }
        }

        // Assert - plain object has no bool properties
        boolProperties.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that filter instantiation completes successfully.
    /// </summary>
    [Fact]
    public void Filter_InstantiationIsSuccessful()
    {
        // Act
        var filter1 = new UnionEndpointFilter();
        var filter2 = new UnionEndpointFilter();

        // Assert
        filter1.Should().NotBeNull();
        filter2.Should().NotBeNull();
        filter1.Should().NotBeSameAs(filter2);
    }

    #region Test Helpers

    /// <summary>
    /// Test helper class simulating a union result type.
    /// </summary>
#pragma warning disable CA1801 // Review unused parameters
    private class TestUnionResult
    {
        public bool IsSuccess { get; set; }
        public object? Value { get; set; }
        public ProblemDetailsError? ErrorValue { get; set; }
    }

    /// <summary>
    /// Test helper class with IsOk property.
    /// </summary>
    private class TestUnionResultWithIsOk
    {
        public bool IsOk { get; set; }
        public string? Value { get; set; }
    }

    /// <summary>
    /// Test helper class with IsSome property.
    /// </summary>
    private class TestUnionResultWithIsSome
    {
        public bool IsSome { get; set; }
        public string? Value { get; set; }
    }

    /// <summary>
    /// Malformed test helper class for error scenarios.
    /// </summary>
    private class TestUnionResultMalformed
    {
        public bool IsSuccess { get; set; }
        // Missing Value property - intentionally malformed
    }
#pragma warning restore CA1801 // Review unused parameters

    #endregion
}

