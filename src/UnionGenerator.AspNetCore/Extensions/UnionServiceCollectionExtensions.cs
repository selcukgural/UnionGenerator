using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UnionGenerator.AspNetCore.Conventions;
using UnionGenerator.AspNetCore.Logging;

namespace UnionGenerator.AspNetCore.Extensions;

/// <summary>
/// Provides extension methods for configuring union result handling in ASP.NET Core dependency injection.
/// </summary>
public static class UnionServiceCollectionExtensions
{
    /// <summary>
    /// Adds union result handling services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional action to configure logging and convention behavior.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// - <see cref="StatusCodeConventionRegistry"/> (singleton with default conventions)
    /// - <see cref="UnionLoggingOptions"/> (scoped)
    /// - <see cref="UnionResultLogger"/> (scoped)
    /// </para>
    /// <para>
    /// By default, the registry uses the built-in convention chain:
    /// 1. AttributeBasedConvention (100)
    /// 2. PropertyBasedConvention (75)
    /// 3. ProblemDetailsConvention (50)
    /// 4. NameBasedConvention (50)
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddUnionResultHandling();
    /// 
    /// // Or with custom configuration
    /// services.AddUnionResultHandling(options =>
    /// {
    ///     options.LoggingOptions.LogErrorDetails = true;
    ///     options.LoggingOptions.LogConventionInference = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddUnionResultHandling(
        this IServiceCollection services,
        Action<UnionResultHandlingOptions>? configure = null)
    {
        var options = new UnionResultHandlingOptions();
        configure?.Invoke(options);

        // Register a registry as singleton (thread-safe, immutable after config)
        services.AddSingleton(_ =>
        {
            var registry = StatusCodeConventionRegistry.Default;
            
            // Apply any custom conventions if provided
            foreach (var convention in options.CustomConventions)
            {
                registry.Register(convention);
            }

            return registry;
        });

        // Register logging options (scoped so they can be injected per request if needed)
        services.AddScoped(_ => options.LoggingOptions);

        // Register the logger
        services.AddScoped<UnionResultLogger>((sp) =>
        {
            var logger = sp.GetRequiredService<ILogger<UnionResultLogger>>();
            var loggingOptions = sp.GetRequiredService<UnionLoggingOptions>();
            return new UnionResultLogger(logger, loggingOptions);
        });

        return services;
    }

    /// <summary>
    /// Adds union result handling services with a custom convention registry.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="registryFactory">Factory function to create the convention registry. If null, uses default registry.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Use this overload when you need complete control over which conventions are registered.
    /// If registryFactory is null, the default registry is used.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddUnionResultHandling(sp =>
    /// {
    ///     var registry = new StatusCodeConventionRegistry();
    ///     registry.Register(new MyCustomConvention());
    ///     return registry;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddUnionResultHandling(
        this IServiceCollection services,
        Func<IServiceProvider, StatusCodeConventionRegistry>? registryFactory)
    {
        // If no factory provided, use default registry
        services.AddSingleton(registryFactory ?? (_ => StatusCodeConventionRegistry.Default));
        services.AddScoped(_ => new UnionLoggingOptions());
        services.AddScoped<UnionResultLogger>((sp) =>
        {
            var logger = sp.GetRequiredService<ILogger<UnionResultLogger>>();
            var loggingOptions = sp.GetRequiredService<UnionLoggingOptions>();
            return new UnionResultLogger(logger, loggingOptions);
        });

        return services;
    }
}

/// <summary>
/// Configuration options for union result handling setup.
/// </summary>
public sealed class UnionResultHandlingOptions
{
    /// <summary>
    /// Gets or sets the logging configuration.
    /// </summary>
    /// <value>A <see cref="UnionLoggingOptions"/> instance with logging preferences.</value>
    public UnionLoggingOptions LoggingOptions { get; set; } = new();

    /// <summary>
    /// Gets a list of custom conventions to register in addition to the defaults.
    /// </summary>
    /// <value>A list of custom <see cref="IStatusCodeConvention"/> implementations.</value>
    /// <remarks>
    /// Custom conventions are added to the default convention registry in the order they appear in this list.
    /// They will be evaluated according to their priority relative to built-in conventions.
    /// </remarks>
    public List<IStatusCodeConvention> CustomConventions { get; } = [];
}

