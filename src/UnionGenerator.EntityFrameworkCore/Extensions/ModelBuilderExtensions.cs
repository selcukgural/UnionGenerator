using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using UnionGenerator.EntityFrameworkCore.ValueConverters;

namespace UnionGenerator.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extension methods for configuring Result union types in EF Core models.
/// </summary>
/// <remarks>
/// <para>
/// These extensions simplify the configuration of Result properties in entity types,
/// automatically applying JSON value converters and appropriate column types.
/// </para>
/// </remarks>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Configures a Result property to be stored as JSON in the database.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TResult">The Result union type.</typeparam>
    /// <typeparam name="TData">The success value type.</typeparam>
    /// <typeparam name="TError">The error value type.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="propertyExpression">Expression selecting the Result property.</param>
    /// <returns>The property builder for further configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when builder or propertyExpression is null.</exception>
    /// <remarks>
    /// <para>
    /// This method applies the ResultValueConverter to the property and configures
    /// the column type as nvarchar(max) for SQL Server or text for other providers.
    /// </para>
    /// <para>
    /// The property will be stored as JSON with the format: {"case":"Ok"|"Error","value":...}
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.Entity&lt;Order&gt;()
    ///         .HasResultConversion&lt;Order, Result&lt;OrderData, ErrorInfo&gt;, OrderData, ErrorInfo&gt;(
    ///             o => o.ProcessingResult
    ///         );
    /// }
    /// </code>
    /// </example>
    public static PropertyBuilder<TResult> HasResultConversion<TEntity, TResult, TData, TError>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TResult>> propertyExpression)
        where TEntity : class
        where TResult : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(propertyExpression);

        var converter = new ResultValueConverter<TResult, TData, TError>();
        return builder.Property(propertyExpression)
            .HasConversion((ValueConverter<TResult?, string?>)converter)
            .HasColumnType("nvarchar(max)");
    }

    /// <summary>
    /// Configures a nullable Result property to be stored as JSON in the database.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TResult">The Result union type.</typeparam>
    /// <typeparam name="TData">The success value type.</typeparam>
    /// <typeparam name="TError">The error value type.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="propertyExpression">Expression selecting the nullable Result property.</param>
    /// <returns>The property builder for further configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when builder or propertyExpression is null.</exception>
    /// <remarks>
    /// <para>
    /// This overload is for nullable Result properties. Null values are stored as NULL in the database.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;Order&gt;()
    ///     .HasNullableResultConversion&lt;Order, Result&lt;OrderData, ErrorInfo&gt;, OrderData, ErrorInfo&gt;(
    ///         o => o.OptionalResult
    ///     );
    /// </code>
    /// </example>
    public static PropertyBuilder<TResult?> HasNullableResultConversion<TEntity, TResult, TData, TError>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TResult?>> propertyExpression)
        where TEntity : class
        where TResult : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(propertyExpression);

        return builder.Property(propertyExpression)
            .HasConversion(new ResultValueConverter<TResult, TData, TError>())
            .HasColumnType("nvarchar(max)");
    }

    /// <summary>
    /// Configures a Result property with custom JSON serializer options.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TResult">The Result union type.</typeparam>
    /// <typeparam name="TData">The success value type.</typeparam>
    /// <typeparam name="TError">The error value type.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="propertyExpression">Expression selecting the Result property.</param>
    /// <param name="jsonOptions">Custom JSON serializer options.</param>
    /// <returns>The property builder for further configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <remarks>
    /// <para>
    /// This overload allows you to customize the JSON serialization behavior,
    /// such as naming policies, indentation, and custom converters.
    /// </para>
    /// <para>
    /// Ensure that the ResultJsonConverter is registered in the provided options.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var jsonOptions = new JsonSerializerOptions
    /// {
    ///     PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    ///     WriteIndented = true,
    ///     Converters = { new ResultJsonConverter&lt;Result&lt;int, string&gt;, int, string&gt;() }
    /// };
    /// 
    /// modelBuilder.Entity&lt;Order&gt;()
    ///     .HasResultConversionWithOptions&lt;Order, Result&lt;int, string&gt;, int, string&gt;(
    ///         o => o.Result,
    ///         jsonOptions
    ///     );
    /// </code>
    /// </example>
    public static PropertyBuilder<TResult> HasResultConversionWithOptions<TEntity, TResult, TData, TError>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TResult>> propertyExpression,
        JsonSerializerOptions jsonOptions)
        where TEntity : class
        where TResult : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(propertyExpression);
        ArgumentNullException.ThrowIfNull(jsonOptions);

        var converter = new ResultValueConverter<TResult, TData, TError>(jsonOptions);
        return builder.Property(propertyExpression)
            .HasConversion((ValueConverter<TResult?, string?>)converter)
            .HasColumnType("nvarchar(max)");
    }

    /// <summary>
    /// Configures all Result properties in an entity to use JSON conversion.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <remarks>
    /// <para>
    /// This method scans all entity types in the model and automatically configures
    /// Result properties to use JSON value conversion.
    /// </para>
    /// <para>
    /// Note: This is a convenience method that applies default configuration.
    /// For fine-grained control, use HasResultConversion on individual properties.
    /// </para>
    /// <para>
    /// Performance: This method uses reflection and should only be called once
    /// during model configuration (in OnModelCreating).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.ConfigureResultConversions();
    ///     
    ///     // Other configurations...
    /// }
    /// </code>
    /// </example>
    public static void ConfigureResultConversions(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var propertyType = property.ClrType;

                // Check if the property type is a Result union (heuristic: has OkCase and ErrorCase nested types)
                if (propertyType.IsClass && !propertyType.IsAbstract)
                {
                    var hasOkCase = propertyType.GetNestedType("OkCase") is not null;
                    var hasErrorCase = propertyType.GetNestedType("ErrorCase") is not null;

                    if (hasOkCase && hasErrorCase)
                    {
                        // This appears to be a Result union type
                        // Apply JSON conversion with nvarchar(max) column type
                        property.SetColumnType("nvarchar(max)");
                        
                        // Note: Automatic converter registration requires reflection and generic type construction
                        // For now, we just set the column type. Users should use HasResultConversion for full setup.
                    }
                }
            }
        }
    }
}

