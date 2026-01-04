using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace UnionGenerator.Benchmarks;

/// <summary>
/// Benchmarks for memory allocation patterns of union operations.
/// Focuses on allocation-heavy scenarios and allocation avoidance.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[MarkdownExporter]
public class AllocationBenchmarks
{
    private const int IterationCount = 100;
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
    /// Benchmark allocations from creating multiple union instances with value types.
    /// </summary>
    /// <returns>Last created union instance.</returns>
    [Benchmark(Baseline = true)]
    public Result<int, string> CreateManyValueTypes()
    {
        Result<int, string> result = default;
        for (int i = 0; i < IterationCount; i++)
        {
            result = Result<int, string>.FromT0(i);
        }
        return result;
    }

    /// <summary>
    /// Benchmark allocations from creating multiple union instances with reference types.
    /// </summary>
    /// <returns>Last created union instance.</returns>
    [Benchmark]
    public Result<int, string> CreateManyReferenceTypes()
    {
        Result<int, string> result = default;
        for (int i = 0; i < IterationCount; i++)
        {
            result = Result<int, string>.FromT1($"error_{i}");
        }
        return result;
    }

    /// <summary>
    /// Benchmark allocations from Match operations with closures.
    /// </summary>
    /// <returns>Total sum of matched values.</returns>
    [Benchmark]
    public int MatchWithClosure()
    {
        int sum = 0;
        for (int i = 0; i < IterationCount; i++)
        {
            sum += _intResult.Match(
                value => value + sum, // Captures 'sum', creates closure
                error => sum
            );
        }
        return sum;
    }

    /// <summary>
    /// Benchmark allocations from Match operations without closures.
    /// </summary>
    /// <returns>Total sum of matched values.</returns>
    [Benchmark]
    public int MatchWithoutClosure()
    {
        int sum = 0;
        for (int i = 0; i < IterationCount; i++)
        {
            sum += _intResult.Match(
                value => value, // No closure
                error => 0
            );
        }
        return sum;
    }

    /// <summary>
    /// Benchmark allocations from TryGetValue operations.
    /// </summary>
    /// <returns>Total sum of extracted values.</returns>
    [Benchmark]
    public int TryGetValueLoop()
    {
        int sum = 0;
        for (int i = 0; i < IterationCount; i++)
        {
            if (_intResult.TryGetT0(out var value))
            {
                sum += value;
            }
        }
        return sum;
    }

    /// <summary>
    /// Benchmark allocations from Switch operations.
    /// </summary>
    /// <returns>Total sum of switched values.</returns>
    [Benchmark]
    public int SwitchLoop()
    {
        int sum = 0;
        for (int i = 0; i < IterationCount; i++)
        {
            _intResult.Switch(
                value => sum += value,
                error => { }
            );
        }
        return sum;
    }

    /// <summary>
    /// Benchmark creating unions with struct types vs reference types.
    /// </summary>
    /// <returns>Last created union instance.</returns>
    [Benchmark]
    public ResultStruct<ValueHolder, ReferenceHolder> CreateMixedTypes()
    {
        ResultStruct<ValueHolder, ReferenceHolder> result = default;
        for (int i = 0; i < IterationCount; i++)
        {
            if (i % 2 == 0)
            {
                result = ResultStruct<ValueHolder, ReferenceHolder>.FromT0(new ValueHolder { Value = i });
            }
            else
            {
                result = ResultStruct<ValueHolder, ReferenceHolder>.FromT1(new ReferenceHolder { Value = i });
            }
        }
        return result;
    }
}

/// <summary>
/// Value type holder for allocation benchmarks.
/// </summary>
public struct ValueHolder
{
    /// <summary>
    /// Gets or sets the integer value.
    /// </summary>
    public int Value { get; set; }
}

/// <summary>
/// Reference type holder for allocation benchmarks.
/// </summary>
public class ReferenceHolder
{
    /// <summary>
    /// Gets or sets the integer value.
    /// </summary>
    public int Value { get; set; }
}


