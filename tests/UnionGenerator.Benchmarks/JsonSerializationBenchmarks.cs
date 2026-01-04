using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace UnionGenerator.Benchmarks;

/// <summary>
/// Benchmarks for JSON serialization and deserialization performance.
/// Measures System.Text.Json performance with union types.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[MarkdownExporter]
public class JsonSerializationBenchmarks
{
    private Result<int, string> _intResult;
    private Result<int, string> _stringResult;
    private string _serializedInt = default!;
    private string _serializedString = default!;
    private JsonSerializerOptions _options = default!;

    /// <summary>
    /// Setup method to initialize test data and JSON options.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _intResult = Result<int, string>.FromT0(42);
        _stringResult = Result<int, string>.FromT1("error");
        _options = new JsonSerializerOptions
        {
            WriteIndented = false
        };
        _serializedInt = JsonSerializer.Serialize(_intResult, _options);
        _serializedString = JsonSerializer.Serialize(_stringResult, _options);
    }

    /// <summary>
    /// Benchmark serializing union containing int value.
    /// </summary>
    /// <returns>Serialized JSON string.</returns>
    [Benchmark(Baseline = true)]
    public string SerializeInt()
    {
        return JsonSerializer.Serialize(_intResult, _options);
    }

    /// <summary>
    /// Benchmark serializing union containing string value.
    /// </summary>
    /// <returns>Serialized JSON string.</returns>
    [Benchmark]
    public string SerializeString()
    {
        return JsonSerializer.Serialize(_stringResult, _options);
    }

    /// <summary>
    /// Benchmark deserializing union with int value.
    /// </summary>
    /// <returns>Deserialized union instance.</returns>
    [Benchmark]
    public Result<int, string>? DeserializeInt()
    {
        return JsonSerializer.Deserialize<Result<int, string>>(_serializedInt, _options);
    }

    /// <summary>
    /// Benchmark deserializing union with string value.
    /// </summary>
    /// <returns>Deserialized union instance.</returns>
    [Benchmark]
    public Result<int, string>? DeserializeString()
    {
        return JsonSerializer.Deserialize<Result<int, string>>(_serializedString, _options);
    }

    /// <summary>
    /// Benchmark full round-trip (serialize + deserialize) with int value.
    /// </summary>
    /// <returns>Deserialized union instance.</returns>
    [Benchmark]
    public Result<int, string>? RoundTripInt()
    {
        var json = JsonSerializer.Serialize(_intResult, _options);
        return JsonSerializer.Deserialize<Result<int, string>>(json, _options);
    }

    /// <summary>
    /// Benchmark full round-trip (serialize + deserialize) with string value.
    /// </summary>
    /// <returns>Deserialized union instance.</returns>
    [Benchmark]
    public Result<int, string>? RoundTripString()
    {
        var json = JsonSerializer.Serialize(_stringResult, _options);
        return JsonSerializer.Deserialize<Result<int, string>>(json, _options);
    }
}

