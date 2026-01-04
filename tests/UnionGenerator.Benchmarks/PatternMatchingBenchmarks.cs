using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace UnionGenerator.Benchmarks;

/// <summary>
/// Benchmarks for pattern matching performance on unions.
/// Compares Match, Switch, and manual type checking approaches.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[MarkdownExporter]
public class PatternMatchingBenchmarks
{
    private Result<int, string> _intResult;
    private Result<int, string> _stringResult;

    /// <summary>
    /// Setup method to initialize test data.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _intResult = Result<int, string>.FromT0(42);
        _stringResult = Result<int, string>.FromT1("error");
    }

    /// <summary>
    /// Benchmark using Match method on int value.
    /// </summary>
    /// <returns>The matched value converted to string.</returns>
    [Benchmark(Baseline = true)]
    public string MatchInt()
    {
        return _intResult.Match(
            value => value.ToString(),
            error => error
        );
    }

    /// <summary>
    /// Benchmark using Match method on string value.
    /// </summary>
    /// <returns>The matched string value.</returns>
    [Benchmark]
    public string MatchString()
    {
        return _stringResult.Match(
            value => value.ToString(),
            error => error
        );
    }

    /// <summary>
    /// Benchmark using Switch method on int value.
    /// </summary>
    /// <returns>The matched value converted to string.</returns>
    [Benchmark]
    public string SwitchInt()
    {
        string result = string.Empty;
        _intResult.Switch(
            value => result = value.ToString(),
            error => result = error
        );
        return result;
    }

    /// <summary>
    /// Benchmark using Switch method on string value.
    /// </summary>
    /// <returns>The matched string value.</returns>
    [Benchmark]
    public string SwitchString()
    {
        string result = string.Empty;
        _stringResult.Switch(
            value => result = value.ToString(),
            error => result = error
        );
        return result;
    }

    /// <summary>
    /// Benchmark using TryGetValue for type checking on int value.
    /// </summary>
    /// <returns>The extracted or default value as string.</returns>
    [Benchmark]
    public string TryGetValueInt()
    {
        if (_intResult.TryGetT0(out var value))
        {
            return value.ToString();
        }
        return string.Empty;
    }

    /// <summary>
    /// Benchmark using TryGetValue for type checking on string value.
    /// </summary>
    /// <returns>The extracted or default string value.</returns>
    [Benchmark]
    public string TryGetValueString()
    {
        if (_stringResult.TryGetT1(out var error))
        {
            return error;
        }
        return string.Empty;
    }

    /// <summary>
    /// Benchmark using C# pattern matching (is operator) on int value.
    /// </summary>
    /// <returns>The matched value converted to string.</returns>
    [Benchmark]
    public string IsPatternInt()
    {
        return _intResult switch
        {
            { IsT0: true } r => r.AsT0.ToString(),
            { IsT1: true } r => r.AsT1,
            _ => string.Empty
        };
    }

    /// <summary>
    /// Benchmark using C# pattern matching (is operator) on string value.
    /// </summary>
    /// <returns>The matched string value.</returns>
    [Benchmark]
    public string IsPatternString()
    {
        return _stringResult switch
        {
            { IsT0: true } r => r.AsT0.ToString(),
            { IsT1: true } r => r.AsT1,
            _ => string.Empty
        };
    }
}

