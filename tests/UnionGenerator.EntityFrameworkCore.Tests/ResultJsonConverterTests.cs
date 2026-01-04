using System.Text.Json;
using UnionGenerator.EntityFrameworkCore.Converters;

namespace UnionGenerator.EntityFrameworkCore.Tests;

/// <summary>
/// Test Result union for JSON converter tests.
/// </summary>
public abstract class TestResult
{
    /// <summary>
    /// Success case.
    /// </summary>
    public sealed class OkCase : TestResult
    {
        public int Value { get; }

        public OkCase(int value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// Error case.
    /// </summary>
    public sealed class ErrorCase : TestResult
    {
        public string Value { get; }

        public ErrorCase(string value)
        {
            Value = value;
        }
    }

    public static TestResult Ok(int value) => new OkCase(value);
    public static TestResult Error(string error) => new ErrorCase(error);
}

/// <summary>
/// Tests for ResultJsonConverter.
/// </summary>
public class ResultJsonConverterTests
{
    private readonly JsonSerializerOptions _options;

    public ResultJsonConverterTests()
    {
        _options = new JsonSerializerOptions
        {
            Converters = { new ResultJsonConverter<TestResult, int, string>() }
        };
    }

    [Fact]
    public void Serialize_OkCase_ReturnsCorrectJson()
    {
        // Arrange
        var result = TestResult.Ok(42);

        // Act
        var json = JsonSerializer.Serialize(result, _options);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"case\":\"Ok\"", json);
        Assert.Contains("\"value\":42", json);
    }

    [Fact]
    public void Serialize_ErrorCase_ReturnsCorrectJson()
    {
        // Arrange
        var result = TestResult.Error("Something went wrong");

        // Act
        var json = JsonSerializer.Serialize(result, _options);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"case\":\"Error\"", json);
        Assert.Contains("\"value\":\"Something went wrong\"", json);
    }

    [Fact]
    public void Deserialize_OkCase_ReturnsCorrectResult()
    {
        // Arrange
        var json = "{\"case\":\"Ok\",\"value\":42}";

        // Act
        var result = JsonSerializer.Deserialize<TestResult>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<TestResult.OkCase>(result);
        var okCase = (TestResult.OkCase)result;
        Assert.Equal(42, okCase.Value);
    }

    [Fact]
    public void Deserialize_ErrorCase_ReturnsCorrectResult()
    {
        // Arrange
        var json = "{\"case\":\"Error\",\"value\":\"Something went wrong\"}";

        // Act
        var result = JsonSerializer.Deserialize<TestResult>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<TestResult.ErrorCase>(result);
        var errorCase = (TestResult.ErrorCase)result;
        Assert.Equal("Something went wrong", errorCase.Value);
    }

    [Fact]
    public void RoundTrip_OkCase_PreservesValue()
    {
        // Arrange
        var original = TestResult.Ok(123);

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<TestResult>(json, _options);

        // Assert
        Assert.NotNull(deserialized);
        Assert.IsType<TestResult.OkCase>(deserialized);
        var okCase = (TestResult.OkCase)deserialized;
        Assert.Equal(123, okCase.Value);
    }

    [Fact]
    public void RoundTrip_ErrorCase_PreservesValue()
    {
        // Arrange
        var original = TestResult.Error("Error message");

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<TestResult>(json, _options);

        // Assert
        Assert.NotNull(deserialized);
        Assert.IsType<TestResult.ErrorCase>(deserialized);
        var errorCase = (TestResult.ErrorCase)deserialized;
        Assert.Equal("Error message", errorCase.Value);
    }

    [Fact]
    public void Deserialize_NullJson_ReturnsNull()
    {
        // Arrange
        var json = "null";

        // Act
        var result = JsonSerializer.Deserialize<TestResult>(json, _options);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_MissingCase_ThrowsJsonException()
    {
        // Arrange
        var json = "{\"value\":42}";

        // Act & Assert
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<TestResult>(json, _options));
    }

    [Fact]
    public void Deserialize_MissingValue_ThrowsJsonException()
    {
        // Arrange
        var json = "{\"case\":\"Ok\"}";

        // Act & Assert
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<TestResult>(json, _options));
    }

    [Fact]
    public void Deserialize_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var json = "{invalid json}";

        // Act & Assert
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<TestResult>(json, _options));
    }

    [Fact]
    public void Serialize_Null_ReturnsNullJson()
    {
        // Arrange
        TestResult? result = null;

        // Act
        var json = JsonSerializer.Serialize(result, _options);

        // Assert
        Assert.Equal("null", json);
    }
}

