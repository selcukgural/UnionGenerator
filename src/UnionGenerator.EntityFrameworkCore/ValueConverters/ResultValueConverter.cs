using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using UnionGenerator.EntityFrameworkCore.Converters;

namespace UnionGenerator.EntityFrameworkCore.ValueConverters;

/// <summary>
/// EF Core value converter for Result union types.
/// Converts Result instances to/from JSON strings for database storage.
/// </summary>
/// <typeparam name="TResult">The Result union type.</typeparam>
/// <typeparam name="TData">The success value type.</typeparam>
/// <typeparam name="TError">The error value type.</typeparam>
/// <remarks>
/// <para>
/// This converter enables storing Result union types as JSON columns in the database.
/// The JSON format is compact and includes case discrimination.
/// </para>
/// <para>
/// Thread-safety: This converter is stateless and safe for concurrent use.
/// </para>
/// <para>
/// Performance: Serialization/deserialization overhead depends on the size and complexity
/// of TData and TError types. Use value objects or DTOs for best performance.
/// </para>
/// <para>
/// Database considerations:
/// <list type="bullet">
/// <item>The column will be stored as NVARCHAR(MAX) or TEXT depending on the database provider</item>
/// <item>Querying nested JSON properties may require database-specific functions</item>
/// <item>Consider indexing strategies for frequently queried Result properties</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In your DbContext OnModelCreating:
/// modelBuilder.Entity&lt;Order&gt;()
///     .Property(o => o.ProcessingResult)
///     .HasConversion(new ResultValueConverter&lt;Result&lt;OrderData, ErrorInfo&gt;, OrderData, ErrorInfo&gt;());
/// </code>
/// </example>
public sealed class ResultValueConverter<TResult, TData, TError> : ValueConverter<TResult?, string?>
    where TResult : class
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        Converters = { new ResultJsonConverter<TResult, TData, TError>() }
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultValueConverter{TResult, TData, TError}"/> class.
    /// </summary>
    /// <remarks>
    /// Uses default JSON serialization options with the Result JSON converter registered.
    /// </remarks>
    public ResultValueConverter()
        : base(
            v => SerializeToJsonDefault(v),
            v => DeserializeFromJsonDefault(v))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultValueConverter{TResult, TData, TError}"/> class
    /// with custom JSON serializer options.
    /// </summary>
    /// <param name="options">Custom JSON serializer options.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <remarks>
    /// <para>
    /// The provided options will be used for serialization/deserialization.
    /// Ensure that the ResultJsonConverter is registered in the options.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = new JsonSerializerOptions
    /// {
    ///     PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    ///     Converters = { new ResultJsonConverter&lt;Result&lt;int, string&gt;, int, string&gt;() }
    /// };
    /// 
    /// var converter = new ResultValueConverter&lt;Result&lt;int, string&gt;, int, string&gt;(options);
    /// </code>
    /// </example>
    public ResultValueConverter(JsonSerializerOptions options)
        : base(
            v => SerializeToJson(v, options),
            v => DeserializeFromJson(v, options))
    {
        ArgumentNullException.ThrowIfNull(options);
    }

    /// <summary>
    /// Serializes a Result instance to a JSON string using default options.
    /// </summary>
    /// <param name="result">The Result instance to serialize.</param>
    /// <returns>The JSON string representation, or null if the input is null.</returns>
    /// <remarks>
    /// Output format: {"case":"Ok"|"Error","value":...}
    /// </remarks>
    private static string? SerializeToJsonDefault(TResult? result)
    {
        if (result is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(result, DefaultOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to a Result instance using default options.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized Result instance, or null if the input is null or whitespace.</returns>
    /// <exception cref="JsonException">Thrown when the JSON format is invalid.</exception>
    /// <remarks>
    /// Expected format: {"case":"Ok"|"Error","value":...}
    /// </remarks>
    private static TResult? DeserializeFromJsonDefault(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<TResult>(json, DefaultOptions);
    }

    /// <summary>
    /// Serializes a Result instance to a JSON string.
    /// </summary>
    /// <param name="result">The Result instance to serialize.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>The JSON string representation, or null if the input is null.</returns>
    /// <remarks>
    /// Output format: {"case":"Ok"|"Error","value":...}
    /// </remarks>
    private static string? SerializeToJson(TResult? result, JsonSerializerOptions? options = null)
    {
        if (result is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(result, options ?? DefaultOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to a Result instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>The deserialized Result instance, or null if the input is null or whitespace.</returns>
    /// <exception cref="JsonException">Thrown when the JSON format is invalid.</exception>
    /// <remarks>
    /// Expected format: {"case":"Ok"|"Error","value":...}
    /// </remarks>
    private static TResult? DeserializeFromJson(string? json, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<TResult>(json, options ?? DefaultOptions);
    }
}

