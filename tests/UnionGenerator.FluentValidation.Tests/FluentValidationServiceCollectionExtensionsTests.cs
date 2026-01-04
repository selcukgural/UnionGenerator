using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using UnionGenerator.FluentValidation.Extensions;
using UnionGenerator.FluentValidation.Filters;

namespace UnionGenerator.FluentValidation.Tests;

/// <summary>
/// Tests for ServiceCollectionExtensions - FluentValidation DI registration.
/// </summary>
public class FluentValidationServiceCollectionExtensionsTests
{
    /// <summary>
    /// Tests that AddUnionFluentValidation registers the filter.
    /// </summary>
    [Fact]
    public void AddUnionFluentValidation_RegistersFluentValidationFilter()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddUnionFluentValidation();
        var provider = services.BuildServiceProvider();

        // Assert
        var filter = provider.GetRequiredService<FluentValidationFilter>();
        filter.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that AddUnionFluentValidation throws when services is null.
    /// </summary>
    [Fact]
    public void AddUnionFluentValidation_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        ServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => services.AddUnionFluentValidation()
        );
    }

    /// <summary>
    /// Tests that AddUnionFluentValidation returns service collection for chaining.
    /// </summary>
    [Fact]
    public void AddUnionFluentValidation_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddUnionFluentValidation();

        // Assert
        result.Should().BeSameAs(services);
    }

    /// <summary>
    /// Tests that the filter is registered as scoped.
    /// </summary>
    [Fact]
    public void AddUnionFluentValidation_RegistersFilterAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddUnionFluentValidation();
        var provider = services.BuildServiceProvider();

        // Act
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var filter1 = scope1.ServiceProvider.GetRequiredService<FluentValidationFilter>();
        var filter2 = scope2.ServiceProvider.GetRequiredService<FluentValidationFilter>();

        // Assert
        filter1.Should().NotBeSameAs(filter2);
    }

    /// <summary>
    /// Tests that AddUnionFluentValidation with assembly marker scans correct assembly.
    /// </summary>
    [Fact]
    public void AddUnionFluentValidation_WithAssemblyMarker_RegistersFilter()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddUnionFluentValidation<TestValidator>();
        var provider = services.BuildServiceProvider();

        // Assert
        var filter = provider.GetRequiredService<FluentValidationFilter>();
        filter.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that AddUnionFluentValidation with assembly marker throws when null.
    /// </summary>
    [Fact]
    public void AddUnionFluentValidation_WithNullServicesAndMarker_ThrowsArgumentNullException()
    {
        // Arrange
        ServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => services.AddUnionFluentValidation<TestValidator>()
        );
    }

    /// <summary>
    /// Tests that AddUnionFluentValidation with marker returns service collection.
    /// </summary>
    [Fact]
    public void AddUnionFluentValidation_WithMarker_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddUnionFluentValidation<TestValidator>();

        // Assert
        result.Should().BeSameAs(services);
    }

    /// <summary>
    /// Tests that multiple calls to AddUnionFluentValidation can be chained.
    /// </summary>
    [Fact]
    public void AddUnionFluentValidation_CanBeChainedMultipleTimes()
    {
        // Arrange & Act
        var services = new ServiceCollection()
            .AddUnionFluentValidation()
            .AddUnionFluentValidation<TestValidator>();

        var provider = services.BuildServiceProvider();
        var filter = provider.GetRequiredService<FluentValidationFilter>();

        // Assert
        filter.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the filter is available after registration.
    /// </summary>
    [Fact]
    public void AddUnionFluentValidation_FilterIsResolvable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddUnionFluentValidation();

        // Act
        var provider = services.BuildServiceProvider();
        var filter = provider.GetService(typeof(FluentValidationFilter));

        // Assert
        filter.Should().NotBeNull();
        filter.Should().BeOfType<FluentValidationFilter>();
    }

    /// <summary>
    /// Tests that services can be registered before AddUnionFluentValidation.
    /// </summary>
    [Fact]
    public void AddUnionFluentValidation_WorksWithExistingServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<SomeService>();

        // Act
        services.AddUnionFluentValidation();
        var provider = services.BuildServiceProvider();

        // Assert
        var filter = provider.GetRequiredService<FluentValidationFilter>();
        var someService = provider.GetRequiredService<SomeService>();
        filter.Should().NotBeNull();
        someService.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that services can be registered after AddUnionFluentValidation.
    /// </summary>
    [Fact]
    public void AddUnionFluentValidation_CanHaveServicesAddedAfter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddUnionFluentValidation();
        services.AddLogging();
        services.AddSingleton<SomeService>();

        // Act
        var provider = services.BuildServiceProvider();

        // Assert
        var filter = provider.GetRequiredService<FluentValidationFilter>();
        var someService = provider.GetRequiredService<SomeService>();
        filter.Should().NotBeNull();
        someService.Should().NotBeNull();
    }

    #region Test Helpers

    /// <summary>
    /// Test validator for assembly marker testing.
    /// </summary>
    private class TestValidator : AbstractValidator<TestModel>
    {
        public TestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    /// <summary>
    /// Test model for validator testing.
    /// </summary>
    private class TestModel
    {
        public string Name { get; set; } = "";
    }

    /// <summary>
    /// Test service for DI testing.
    /// </summary>
    private class SomeService
    {
        public string Value => "Test";
    }

    #endregion
}

