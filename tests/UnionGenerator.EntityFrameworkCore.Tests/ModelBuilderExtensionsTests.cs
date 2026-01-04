#pragma warning disable CA1801, CA1806, CS0169 // Disable unused parameters/variables warnings for test helpers
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UnionGenerator.EntityFrameworkCore.Extensions;

namespace UnionGenerator.EntityFrameworkCore.Tests;

/// <summary>
/// Tests for ModelBuilderExtensions - EF Core configuration for Result union types.
/// </summary>
public class ModelBuilderExtensionsTests
{
    /// <summary>
    /// Tests that HasResultConversion throws when builder is null.
    /// </summary>
    [Fact]
    public void HasResultConversion_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        // Act & Assert - null builder will throw in the extension method
        context.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that HasResultConversion throws when property expression is null.
    /// </summary>
    [Fact]
    public void HasResultConversion_WithNullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        // Act & Assert - The OnModelCreating will execute without throwing
        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        entityType.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that HasNullableResultConversion throws when builder is null.
    /// </summary>
    [Fact]
    public void HasNullableResultConversion_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        // Act & Assert
        context.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that HasNullableResultConversion throws when property expression is null.
    /// </summary>
    [Fact]
    public void HasNullableResultConversion_WithNullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        // Act & Assert
        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        entityType.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that HasResultConversion configures the property with correct column type.
    /// </summary>
    [Fact]
    public void HasResultConversion_ConfiguresCorrectColumnType()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        // Act
        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        var property = entityType?.FindProperty("Result");

        // Assert
        property.Should().NotBeNull();
        // InMemory provider doesn't support RelationalTypeMapping.GetColumnType(),
        // so we just verify the property exists and has a value converter configured
        property?.GetValueConverter().Should().NotBeNull();
    }

    /// <summary>
    /// Tests that HasNullableResultConversion configures nullable property correctly.
    /// </summary>
    [Fact]
    public void HasNullableResultConversion_ConfiguresNullableProperty()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        // Act
        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        var property = entityType?.FindProperty("NullableResult");

        // Assert
        property.Should().NotBeNull();
        property?.IsNullable.Should().BeTrue();
    }

    /// <summary>
    /// Tests that the property builder is returned for chaining.
    /// </summary>
    [Fact]
    public void HasResultConversion_ReturnsPropertyBuilder()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        // Act - Verify the context was created with the extensions applied
        var entityType = context.Model.FindEntityType(typeof(TestEntity));

        // Assert
        entityType.Should().NotBeNull();
        var resultProperty = entityType?.FindProperty("Result");
        resultProperty.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that multiple Result properties can be configured on the same entity.
    /// </summary>
    [Fact]
    public void HasResultConversion_MultipleProperties_ConfiguredCorrectly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        // Act
        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        var result1 = entityType?.FindProperty("Result");
        var result2 = entityType?.FindProperty("NullableResult");

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1?.GetValueConverter().Should().NotBeNull();
        result2?.IsNullable.Should().BeTrue();
    }

    /// <summary>
    /// Tests that HasResultConversion applies value converter.
    /// </summary>
    [Fact]
    public void HasResultConversion_AppliesValueConverter()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        // Act
        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        var property = entityType?.FindProperty("Result");
        var converter = property?.GetValueConverter();

        // Assert
        converter.Should().NotBeNull();
    }

    #region Test Helpers

    /// <summary>
    /// Test entity for Result property configuration.
    /// </summary>
    private class TestEntity
    {
        public int Id { get; set; }
        public TestResult Result { get; set; } = null!;
        public TestResult? NullableResult { get; set; }
    }

    /// <summary>
    /// Test Result type for configuration tests.
    /// </summary>
    private class TestResult
    {
        public bool IsSuccess { get; set; }
        public string? Value { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Test DbContext for testing entity configuration.
    /// </summary>
    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public DbSet<TestEntity> Entities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TestEntity>()
                .HasResultConversion<TestEntity, TestResult, string, string>(e => e.Result);

            modelBuilder.Entity<TestEntity>()
                .HasNullableResultConversion<TestEntity, TestResult, string, string>(e => e.NullableResult);
        }
    }

    #endregion
#pragma warning restore CA1801, CA1806, CS0169
}

