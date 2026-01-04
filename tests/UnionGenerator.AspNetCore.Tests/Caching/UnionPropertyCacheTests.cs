using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using UnionGenerator.AspNetCore.Caching;
using Xunit;

namespace UnionGenerator.AspNetCore.Tests.Caching;

/// <summary>
/// Tests for UnionPropertyCache thread-safety, performance, and correctness.
/// </summary>
public class UnionPropertyCacheTests
{
    /// <summary>
    /// Test union type for cache testing.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private sealed class TestResult
    {
        public bool IsSuccess { get; set; }
        public string? Value { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Alternative test union with different property names.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private sealed class TestOutcome
    {
        public bool IsOk { get; set; }
        public int Data { get; set; }
        public string? ErrorValue { get; set; }
    }

    /// <summary>
    /// Non-union type for testing detection.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private sealed class NonUnionType
    {
        public string Name { get; set; } = "";
    }

    [Fact]
    public void GetMetadata_WithValidUnionType_ReturnsMetadata()
    {
        // Arrange
        var cache = new UnionPropertyCache();

        // Act
        var metadata = cache.GetMetadata(typeof(TestResult));

        // Assert
        Assert.NotNull(metadata);
        Assert.True(metadata.IsValid);
        Assert.NotNull(metadata.SuccessProperty);
        Assert.NotNull(metadata.ValueProperty);
        Assert.NotNull(metadata.ErrorValueProperty);
        Assert.Equal("IsSuccess", metadata.SuccessProperty!.Name);
        Assert.Equal("Value", metadata.ValueProperty!.Name);
        Assert.Equal("Error", metadata.ErrorValueProperty!.Name);
    }

    [Fact]
    public void GetMetadata_WithAlternativePropertyNames_ReturnsCorrectMetadata()
    {
        // Arrange
        var cache = new UnionPropertyCache();

        // Act
        var metadata = cache.GetMetadata(typeof(TestOutcome));

        // Assert
        Assert.NotNull(metadata);
        Assert.True(metadata.IsValid);
        Assert.Equal("IsOk", metadata.SuccessProperty!.Name);
        Assert.Equal("Data", metadata.ValueProperty!.Name);
        Assert.Equal("ErrorValue", metadata.ErrorValueProperty!.Name);
    }

    [Fact]
    public void GetMetadata_WithNonUnionType_ReturnsNull()
    {
        // Arrange
        var cache = new UnionPropertyCache();

        // Act
        var metadata = cache.GetMetadata(typeof(NonUnionType));

        // Assert
        Assert.Null(metadata);
    }

    [Fact]
    public void GetMetadata_WithNullType_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = new UnionPropertyCache();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache.GetMetadata(null!));
    }

    [Fact]
    public void GetMetadata_FirstCallReflects_SecondCallCaches()
    {
        // Arrange
        var cache = new UnionPropertyCache();
        var type = typeof(TestResult);

        // Act - first call
        var metadata1 = cache.GetMetadata(type);
        var sizeAfterFirst = cache.CacheSize;

        // Act - second call
        var metadata2 = cache.GetMetadata(type);
        var sizeAfterSecond = cache.CacheSize;

        // Assert
        Assert.Same(metadata1, metadata2); // Same object reference (cached)
        Assert.Equal(1, sizeAfterFirst);
        Assert.Equal(1, sizeAfterSecond); // No growth
    }

    [Fact]
    public void GetMetadata_MultipleTypes_CachesIndependently()
    {
        // Arrange
        var cache = new UnionPropertyCache();

        // Act
        var metadata1 = cache.GetMetadata(typeof(TestResult));
        var metadata2 = cache.GetMetadata(typeof(TestOutcome));
        var metadata3 = cache.GetMetadata(typeof(NonUnionType));

        // Assert
        Assert.Equal(3, cache.CacheSize);
        Assert.NotNull(metadata1);
        Assert.NotNull(metadata2);
        Assert.Null(metadata3);
    }

    [Fact]
    public void GetMetadata_ThreadSafe_ConcurrentAccess()
    {
        // Arrange
        var cache = new UnionPropertyCache();
        var types = new[] { typeof(TestResult), typeof(TestOutcome), typeof(NonUnionType) };
        var results = new List<UnionPropertyCache.UnionTypeMetadata?>();
        var lockObj = new object();

        // Act - simulate concurrent access
        Parallel.ForEach(
            Enumerable.Range(0, 100),
            i =>
            {
                var type = types[i % types.Length];
                var metadata = cache.GetMetadata(type);
                lock (lockObj)
                {
                    results.Add(metadata);
                }
            }
        );

        // Assert
        Assert.Equal(100, results.Count);
        Assert.Equal(3, cache.CacheSize); // Only 3 unique types cached
        
        // Verify metadata validity
        var testResultMetadata = results
                                 .OfType<UnionPropertyCache.UnionTypeMetadata>()
                                 .FirstOrDefault(m => m.SuccessProperty?.Name == "IsSuccess");
        Assert.NotNull(testResultMetadata);
    }

    [Fact]
    public void Clear_EmptiesCache()
    {
        // Arrange
        var cache = new UnionPropertyCache();
        cache.GetMetadata(typeof(TestResult));
        cache.GetMetadata(typeof(TestOutcome));
        Assert.Equal(2, cache.CacheSize);

        // Act
        cache.Clear();

        // Assert
        Assert.Equal(0, cache.CacheSize);
        
        // Verify repopulation works
        var metadata = cache.GetMetadata(typeof(TestResult));
        Assert.NotNull(metadata);
        Assert.Equal(1, cache.CacheSize);
    }

    [Fact]
    public void Default_ReturnsSingletonInstance()
    {
        // Act
        var instance1 = UnionPropertyCache.Default;
        var instance2 = UnionPropertyCache.Default;

        // Assert
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void Metadata_IsValid_OnlyWhenRequiredPropertiesPresent()
    {
        // Arrange
        var cache = new UnionPropertyCache();

        // Act - valid union
        var validMetadata = cache.GetMetadata(typeof(TestResult));

        // Act - non-union
        var invalidMetadata = cache.GetMetadata(typeof(NonUnionType));

        // Assert
        Assert.NotNull(validMetadata);
        Assert.True(validMetadata.IsValid);
        Assert.Null(invalidMetadata);
    }

    [Fact]
    public void GetMetadata_PropertyAccessWorks_AfterCaching()
    {
        // Arrange
        var cache = new UnionPropertyCache();
        var testInstance = new TestResult { IsSuccess = true, Value = "test", Error = null };

        // Act
        var metadata = cache.GetMetadata(typeof(TestResult));

        // Assert - verify properties can actually access values
        Assert.NotNull(metadata);
        Assert.NotNull(metadata.SuccessProperty);
        Assert.NotNull(metadata.ValueProperty);
        Assert.NotNull(metadata.ErrorValueProperty);

        var successProperty = metadata.SuccessProperty;
        var valueProperty = metadata.ValueProperty;
        var errorValueProperty = metadata.ErrorValueProperty;

        var isSuccessValue = successProperty.GetValue(testInstance);
        var valueValue = valueProperty.GetValue(testInstance);
        var errorValue = errorValueProperty.GetValue(testInstance);

        Assert.Equal(true, isSuccessValue);
        Assert.Equal("test", valueValue);
        Assert.Null(errorValue);
    }

    [Fact]
    public void CacheSize_Grows_AsUniqueTypesAreAdded()
    {
        // Arrange
        var cache = new UnionPropertyCache();

        // Act & Assert
        Assert.Equal(0, cache.CacheSize);

        cache.GetMetadata(typeof(TestResult));
        Assert.Equal(1, cache.CacheSize);

        cache.GetMetadata(typeof(TestOutcome));
        Assert.Equal(2, cache.CacheSize);

        cache.GetMetadata(typeof(TestResult)); // Duplicate
        Assert.Equal(2, cache.CacheSize); // No growth
    }
}

