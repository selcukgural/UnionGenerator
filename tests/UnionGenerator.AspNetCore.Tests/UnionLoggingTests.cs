using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UnionGenerator.AspNetCore.Extensions;
using UnionGenerator.AspNetCore.Logging;
using Xunit;

namespace UnionGenerator.AspNetCore.Tests;

/// <summary>
/// Tests for structured logging and DI integration.
/// </summary>
public sealed class UnionLoggingTests
{
    [Fact]
    public void AddUnionResultHandling_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddUnionResultHandling();
        var provider = services.BuildServiceProvider();

        // Assert
        var logger = provider.GetRequiredService<UnionResultLogger>();
        Assert.NotNull(logger);
    }

    [Fact]
    public void AddUnionResultHandling_WithCustomOptions_AppliesConfiguration()
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
        Assert.True(loggingOptions.LogSuccessResults);
        Assert.False(loggingOptions.LogErrorDetails);
    }

    [Fact]
    public void UnionResultLogger_CanBeInvoked()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddUnionResultHandling();
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<UnionResultLogger>();

        // Act & Assert (should not throw)
        logger.LogErrorCase("TestError", 404, "AttributeBased");
        logger.LogSuccessCase("TestResult");
        logger.LogStatusCodeInferenceFailed("UnknownError", 500);
    }

    [Fact]
    public void UnionLoggingOptions_DefaultValues()
    {
        // Act
        var options = new UnionLoggingOptions();

        // Assert
        Assert.False(options.LogSuccessResults);
        Assert.True(options.LogErrorDetails);
        Assert.True(options.LogConventionInference);
        Assert.Equal(LogLevel.Information, options.MinimumLevel);
    }
}

