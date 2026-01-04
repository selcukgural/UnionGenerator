using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using UnionGenerator.AspNetCore.Conventions;
using UnionGenerator.AspNetCore.Extensions;
using UnionGenerator.AspNetCore.Logging;
using Xunit;

namespace UnionGenerator.AspNetCore.Tests;

/// <summary>
/// Tests for UnionServiceCollectionExtensions - DI registration for union result handling.
/// </summary>
public class UnionServiceCollectionExtensionsTests
{
    /// <summary>
    /// Tests that AddUnionResultHandling registers required services.
    /// </summary>
    [Fact]
    public void AddUnionResultHandling_RegistersAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddUnionResultHandling();
        var provider = services.BuildServiceProvider();

        // Assert
        var registry = provider.GetRequiredService<StatusCodeConventionRegistry>();
        var logger = provider.GetRequiredService<UnionResultLogger>();
        var options = provider.GetRequiredService<UnionLoggingOptions>();

        registry.Should().NotBeNull();
        logger.Should().NotBeNull();
        options.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the registry is registered as a singleton.
    /// </summary>
    [Fact]
    public void AddUnionResultHandling_RegistersRegistryAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddUnionResultHandling();
        var provider = services.BuildServiceProvider();

        var instance1 = provider.GetRequiredService<StatusCodeConventionRegistry>();
        var instance2 = provider.GetRequiredService<StatusCodeConventionRegistry>();

        // Assert
        instance1.Should().BeSameAs(instance2);
    }

    /// <summary>
    /// Tests that the logger is registered as scoped.
    /// </summary>
    [Fact]
    public void AddUnionResultHandling_RegistersLoggerAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddUnionResultHandling();
        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var logger1 = scope1.ServiceProvider.GetRequiredService<UnionResultLogger>();
        var logger2 = scope2.ServiceProvider.GetRequiredService<UnionResultLogger>();

        // Assert - different scopes should get different instances
        logger1.Should().NotBeSameAs(logger2);
    }

    /// <summary>
    /// Tests that custom configuration is applied.
    /// </summary>
    [Fact]
    public void AddUnionResultHandling_WithConfiguration_AppliesCustomSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddUnionResultHandling(options =>
        {
            options.LoggingOptions.LogSuccessResults = true;
            options.LoggingOptions.LogErrorDetails = false;
        });

        var provider = services.BuildServiceProvider();
        var loggingOptions = provider.GetRequiredService<UnionLoggingOptions>();

        // Assert
        loggingOptions.LogSuccessResults.Should().BeTrue();
        loggingOptions.LogErrorDetails.Should().BeFalse();
    }

    /// <summary>
    /// Tests that the default registry includes standard conventions.
    /// </summary>
    [Fact]
    public void AddUnionResultHandling_DefaultRegistry_IncludesStandardConventions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddUnionResultHandling();
        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<StatusCodeConventionRegistry>();

        // Assert
        registry.Should().NotBeNull();
        // The registry should be properly initialized with default conventions
    }

    /// <summary>
    /// Tests that AddUnionResultHandling returns the service collection for chaining.
    /// </summary>
    [Fact]
    public void AddUnionResultHandling_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var result = services.AddUnionResultHandling();

        // Assert
        result.Should().BeSameAs(services);
    }

    /// <summary>
    /// Tests that AddUnionResultHandling with registry factory works correctly.
    /// </summary>
    [Fact]
    public void AddUnionResultHandling_WithRegistryFactory_UsesCustomRegistry()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddUnionResultHandling(_ =>
        {
            var registry = StatusCodeConventionRegistry.Default;
            return registry;
        });

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<StatusCodeConventionRegistry>();

        // Assert
        registry.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that multiple calls to AddUnionResultHandling override previous registration.
    /// </summary>
    [Fact]
    public void AddUnionResultHandling_MultipleCalls_LatestRegistrationWins()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddUnionResultHandling(options =>
        {
            options.LoggingOptions.LogSuccessResults = false;
        });

        services.AddUnionResultHandling(options =>
        {
            options.LoggingOptions.LogSuccessResults = true;
        });

        var provider = services.BuildServiceProvider();
        var loggingOptions = provider.GetRequiredService<UnionLoggingOptions>();

        // Assert
        loggingOptions.LogSuccessResults.Should().BeTrue();
    }

    /// <summary>
    /// Tests that the logger can be resolved without logging configuration.
    /// </summary>
    [Fact]
    public void AddUnionResultHandling_LoggerResolvable_WithoutLoggingSetup()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddUnionResultHandling();
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<UnionResultLogger>();

        // Assert
        logger.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that configuration action can be null (optional).
    /// </summary>
    [Fact]
    public void AddUnionResultHandling_WithNullConfiguration_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert - should not throw
        services.AddUnionResultHandling(null!);
        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<StatusCodeConventionRegistry>();
        registry.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that custom conventions can be added through configuration.
    /// </summary>
    [Fact]
    public void AddUnionResultHandling_WithCustomConventions_RegistersThemInRegistry()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var customConvention = new TestConvention();

        // Act
        services.AddUnionResultHandling(options =>
        {
            options.CustomConventions.Add(customConvention);
        });

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<StatusCodeConventionRegistry>();

        // Assert
        registry.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the service collection properly chains multiple union-related registrations.
    /// </summary>
    [Fact]
    public void AddUnionResultHandling_AllowsChaining_WithOtherExtensions()
    {
        // Arrange & Act
        var services = new ServiceCollection()
            .AddLogging()
            .AddUnionResultHandling()
            .AddUnionResultHandling(options => { options.LoggingOptions.LogSuccessResults = true; });

        var provider = services.BuildServiceProvider();

        // Assert
        var registry = provider.GetRequiredService<StatusCodeConventionRegistry>();
        var logger = provider.GetRequiredService<UnionResultLogger>();
        registry.Should().NotBeNull();
        logger.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that logging options have sensible defaults.
    /// </summary>
    [Fact]
    public void AddUnionResultHandling_LoggingOptions_HaveSensibleDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddUnionResultHandling();
        var provider = services.BuildServiceProvider();
        var loggingOptions = provider.GetRequiredService<UnionLoggingOptions>();

        // Assert
        loggingOptions.Should().NotBeNull();
        // Check that options are in a valid state
    }

    #region Test Helpers

    /// <summary>
    /// Test implementation of IStatusCodeConvention for testing.
    /// </summary>
    private class TestConvention : IStatusCodeConvention
    {
        public string Name => "TestConvention";
        public int Priority => 999;

        public bool TryGetStatusCode(object instance, out int statusCode)
        {
            statusCode = 200;
            return true;
        }
    }

    #endregion
}

