using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using OneOf;

namespace UnionGenerator.Benchmarks;

/// <summary>
/// Benchmarks comparing UnionGenerator unions with OneOf library.
/// Measures relative performance for common operations.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[MarkdownExporter]
public class OneOfComparisonBenchmarks
{
    private Result<int, string> _unionGenInt;
    private Result<int, string> _unionGenString;
    private OneOf<int, string> _oneOfInt;
    private OneOf<int, string> _oneOfString;

    /// <summary>
    /// Setup method to initialize test data.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _unionGenInt = Result<int, string>.FromT0(42);
        _unionGenString = Result<int, string>.FromT1("error");
        _oneOfInt = 42;
        _oneOfString = "error";
    }

    /// <summary>
    /// Benchmark creating UnionGenerator union from int.
    /// </summary>
    /// <returns>Created union instance.</returns>
    [Benchmark(Baseline = true)]
    public Result<int, string> UnionGen_CreateInt()
    {
        return Result<int, string>.FromT0(42);
    }

    /// <summary>
    /// Benchmark creating OneOf union from int.
    /// </summary>
    /// <returns>Created OneOf instance.</returns>
    [Benchmark]
    public OneOf<int, string> OneOf_CreateInt()
    {
        return OneOf<int, string>.FromT0(42);
    }

    /// <summary>
    /// Benchmark creating UnionGenerator union from string.
    /// </summary>
    /// <returns>Created union instance.</returns>
    [Benchmark]
    public Result<int, string> UnionGen_CreateString()
    {
        return Result<int, string>.FromT1("error");
    }

    /// <summary>
    /// Benchmark creating OneOf union from string.
    /// </summary>
    /// <returns>Created OneOf instance.</returns>
    [Benchmark]
    public OneOf<int, string> OneOf_CreateString()
    {
        return OneOf<int, string>.FromT1("error");
    }

    /// <summary>
    /// Benchmark Match operation on UnionGenerator union with int.
    /// </summary>
    /// <returns>Matched result as string.</returns>
    [Benchmark]
    public string UnionGen_MatchInt()
    {
        return _unionGenInt.Match(
            value => value.ToString(),
            error => error
        );
    }

    /// <summary>
    /// Benchmark Match operation on OneOf union with int.
    /// </summary>
    /// <returns>Matched result as string.</returns>
    [Benchmark]
    public string OneOf_MatchInt()
    {
        return _oneOfInt.Match(
            value => value.ToString(),
            error => error
        );
    }

    /// <summary>
    /// Benchmark Match operation on UnionGenerator union with string.
    /// </summary>
    /// <returns>Matched result as string.</returns>
    [Benchmark]
    public string UnionGen_MatchString()
    {
        return _unionGenString.Match(
            value => value.ToString(),
            error => error
        );
    }

    /// <summary>
    /// Benchmark Match operation on OneOf union with string.
    /// </summary>
    /// <returns>Matched result as string.</returns>
    [Benchmark]
    public string OneOf_MatchString()
    {
        return _oneOfString.Match(
            value => value.ToString(),
            error => error
        );
    }

    /// <summary>
    /// Benchmark TryGetValue on UnionGenerator union.
    /// </summary>
    /// <returns>True if value was extracted.</returns>
    [Benchmark]
    public bool UnionGen_TryGetValue()
    {
        return _unionGenInt.TryGetT0(out var value);
    }

    /// <summary>
    /// Benchmark TryPickT0 on OneOf union.
    /// </summary>
    /// <returns>True if value was extracted.</returns>
    [Benchmark]
    public bool OneOf_TryPickT0()
    {
        return _oneOfInt.TryPickT0(out var value, out _);
    }
}

