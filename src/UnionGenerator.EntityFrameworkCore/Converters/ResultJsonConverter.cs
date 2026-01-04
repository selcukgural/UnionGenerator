using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnionGenerator.EntityFrameworkCore.Converters;

/// <summary>
/// JSON converter for Result union types.
/// </summary>
/// <typeparam name="TResult">The Result union type (must have OkCase and ErrorCase nested types).</typeparam>
/// <typeparam name="TData">The success value type.</typeparam>
/// <typeparam name="TError">The error value type.</typeparam>
/// <remarks>
/// <para>
/// This converter serializes Result unions to a compact JSON format with case discrimination.
/// Format: { "case": "Ok"|"Error", "value": TData|TError }
/// </para>
/// <para>
/// Thread-safety: This converter is stateless and safe for concurrent use.
/// </para>
/// <para>
/// Performance: Serialization/deserialization is O(1) for the structure itself,
/// plus the cost of serializing the inner value.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register the converter
/// var options = new JsonSerializerOptions();
/// options.Converters.Add(new ResultJsonConverter&lt;Result&lt;int, string&gt;, int, string&gt;());
/// 
/// // Serialize
/// var result = Result&lt;int, string&gt;.Ok(42);
/// var json = JsonSerializer.Serialize(result, options);
/// // Output: {"case":"Ok","value":42}
/// 
/// // Deserialize
/// var deserialized = JsonSerializer.Deserialize&lt;Result&lt;int, string&gt;&gt;(json, options);
/// </code>
/// </example>
public sealed class ResultJsonConverter<TResult, TData, TError> : JsonConverter<TResult>
    where TResult : class
{
    /// <summary>
    /// Reads and converts JSON to a Result union instance.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The deserialized Result union instance.</returns>
    /// <exception cref="JsonException">Thrown when the JSON format is invalid.</exception>
    /// <remarks>
    /// Expected JSON format: { "case": "Ok"|"Error", "value": TData|TError }
    /// </remarks>
    public override TResult? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token.");
        }

        string? caseName = null;
        JsonElement? valueElement = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected PropertyName token.");
            }

            var propertyName = reader.GetString();

            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of JSON.");
            }

            if (propertyName == "case")
            {
                caseName = reader.GetString();
            }
            else if (propertyName == "value")
            {
                // Store the value element for later deserialization
                var document = JsonDocument.ParseValue(ref reader);
                valueElement = document.RootElement.Clone();
            }
        }

        if (caseName is null)
        {
            throw new JsonException("Missing 'case' property in Result JSON.");
        }

        if (valueElement is null)
        {
            throw new JsonException("Missing 'value' property in Result JSON.");
        }

        // Find the case type and factory method
        var resultType = typeof(TResult);
        var caseTypeName = caseName == "Ok" ? "OkCase" : "ErrorCase";
        var caseType = resultType.GetNestedType(caseTypeName);

        if (caseType is null)
        {
            throw new JsonException($"Case type '{caseTypeName}' not found in Result type '{resultType.Name}'.");
        }

        // Deserialize the value to the appropriate type
        var valueType = caseName == "Ok" ? typeof(TData) : typeof(TError);
        var value = JsonSerializer.Deserialize(valueElement.Value.GetRawText(), valueType, options);

        if (value is null)
        {
            throw new JsonException($"Failed to deserialize value for case '{caseName}'.");
        }

        // Create an instance of the case using the constructor
        var caseInstance = Activator.CreateInstance(caseType, value);

        if (caseInstance is null)
        {
            throw new JsonException($"Failed to create instance of case '{caseTypeName}'.");
        }

        return (TResult)caseInstance;
    }

    /// <summary>
    /// Writes a Result union instance as JSON.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The Result union instance to serialize.</param>
    /// <param name="options">The serializer options.</param>
    /// <remarks>
    /// Output format: { "case": "Ok"|"Error", "value": TData|TError }
    /// </remarks>
    public override void Write(Utf8JsonWriter writer, TResult value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();

        var resultType = value.GetType();
        var isOk = resultType.Name.Contains("OkCase") || resultType.Name.Contains("SuccessCase");

        writer.WriteString("case", isOk ? "Ok" : "Error");

        // Get the value from the case
        var valueProperty = resultType.GetProperty("Value") ?? resultType.GetProperty("Data");

        if (valueProperty is null)
        {
            throw new JsonException($"Case type '{resultType.Name}' does not have a Value or Data property.");
        }

        var caseValue = valueProperty.GetValue(value);

        writer.WritePropertyName("value");
        JsonSerializer.Serialize(writer, caseValue, caseValue?.GetType() ?? typeof(object), options);

        writer.WriteEndObject();
    }
}

