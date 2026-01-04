using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using UnionGenerator.FluentValidation.Filters;

namespace UnionGenerator.FluentValidation.Extensions;

/// <summary>
/// Provides extension methods for registering FluentValidation with UnionGenerator integration in the DI container.
/// </summary>
/// <remarks>
/// <para>
/// These extensions simplify the registration of FluentValidation validators and the
/// <see cref="FluentValidationFilter"/> for automatic model validation in ASP.NET Core applications.
/// </para>
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Marker class for assembly scanning.
    /// </summary>
    private sealed class AssemblyMarker
    {
    }

    /// <summary>
    /// Adds FluentValidation services and UnionGenerator integration to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// <list type="bullet">
    /// <item>All IValidator implementations from the FluentValidation assembly</item>
    /// <item>The FluentValidationFilter for automatic validation</item>
    /// </list>
    /// </para>
    /// <para>
    /// Validators are registered with scoped lifetime by default.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic registration
    /// builder.Services.AddUnionFluentValidation();
    /// </code>
    /// </example>
    public static IServiceCollection AddUnionFluentValidation(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register validators from the calling assembly
        services.AddValidatorsFromAssemblyContaining<AssemblyMarker>();

        // Register the validation filter
        services.AddScoped<FluentValidationFilter>();

        return services;
    }

    /// <summary>
    /// Adds FluentValidation services and UnionGenerator integration to the service collection,
    /// scanning the specified assembly for validators.
    /// </summary>
    /// <typeparam name="TAssemblyMarker">A type from the assembly to scan for validators.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    /// <remarks>
    /// <para>
    /// This overload allows you to specify which assembly to scan for validators.
    /// Use this when your validators are in a different assembly than your startup code.
    /// </para>
    /// <para>
    /// All IValidator implementations in the specified assembly will be registered with scoped lifetime.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register validators from a specific assembly
    /// builder.Services.AddUnionFluentValidation&lt;CreateUserValidator&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddUnionFluentValidation<TAssemblyMarker>(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register validators from the specified assembly
        services.AddValidatorsFromAssemblyContaining<TAssemblyMarker>();

        // Register the validation filter
        services.AddScoped<FluentValidationFilter>();

        return services;
    }

    /// <summary>
    /// Adds FluentValidation services and UnionGenerator integration to the service collection,
    /// with a custom service lifetime.
    /// </summary>
    /// <typeparam name="TAssemblyMarker">A type from the assembly to scan for validators.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime for validators (default: Scoped).</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    /// <remarks>
    /// <para>
    /// This overload provides full control over validator registration, including
    /// the ability to specify the service lifetime.
    /// </para>
    /// <para>
    /// Common lifetime choices:
    /// <list type="bullet">
    /// <item>Scoped: Default, suitable for validators with dependencies on scoped services (e.g., DbContext)</item>
    /// <item>Transient: For stateless validators without dependencies</item>
    /// <item>Singleton: For validators that are completely stateless and can be safely shared</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register validators as singletons
    /// builder.Services.AddUnionFluentValidationWithLifetime&lt;CreateUserValidator&gt;(
    ///     ServiceLifetime.Singleton
    /// );
    /// </code>
    /// </example>
    public static IServiceCollection AddUnionFluentValidationWithLifetime<TAssemblyMarker>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register validators from the specified assembly with custom lifetime
        services.AddValidatorsFromAssemblyContaining<TAssemblyMarker>(lifetime);

        // Register the validation filter
        services.AddScoped<FluentValidationFilter>();

        return services;
    }
}

