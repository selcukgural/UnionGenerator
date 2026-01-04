using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace UnionGenerator.Benchmarks;

/// <summary>
/// Benchmarks for complex union scenarios with multiple type parameters.
/// Tests performance scaling with increasing type count.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[MarkdownExporter]
public class ComplexUnionBenchmarks
{
    private Union8<int, string, bool, double, decimal, DateTime, Guid, byte[]> _union8T0;
    private Union8<int, string, bool, double, decimal, DateTime, Guid, byte[]> _union8T7;
    
    /// <summary>
    /// Setup method to initialize test data.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _union8T0 = Union8<int, string, bool, double, decimal, DateTime, Guid, byte[]>.FromT0(42);
        _union8T7 = Union8<int, string, bool, double, decimal, DateTime, Guid, byte[]>.FromT7(new byte[] { 1, 2, 3 });
    }

    /// <summary>
    /// Benchmark creating 8-type union from first type.
    /// </summary>
    /// <returns>Created union instance.</returns>
    [Benchmark(Baseline = true)]
    public Union8<int, string, bool, double, decimal, DateTime, Guid, byte[]> Create8TypeUnionT0()
    {
        return Union8<int, string, bool, double, decimal, DateTime, Guid, byte[]>.FromT0(42);
    }

    /// <summary>
    /// Benchmark creating 8-type union from last type.
    /// </summary>
    /// <returns>Created union instance.</returns>
    [Benchmark]
    public Union8<int, string, bool, double, decimal, DateTime, Guid, byte[]> Create8TypeUnionT7()
    {
        return Union8<int, string, bool, double, decimal, DateTime, Guid, byte[]>.FromT7(new byte[] { 1, 2, 3 });
    }

    /// <summary>
    /// Benchmark Match on 8-type union with first type active.
    /// </summary>
    /// <returns>Matched result as string.</returns>
    [Benchmark]
    public string Match8TypeT0()
    {
        return _union8T0.Match(
            t0 => t0.ToString(),
            t1 => t1,
            t2 => t2.ToString(),
            t3 => t3.ToString(),
            t4 => t4.ToString(),
            t5 => t5.ToString(),
            t6 => t6.ToString(),
            t7 => Convert.ToBase64String(t7)
        );
    }

    /// <summary>
    /// Benchmark Match on 8-type union with last type active.
    /// </summary>
    /// <returns>Matched result as string.</returns>
    [Benchmark]
    public string Match8TypeT7()
    {
        return _union8T7.Match(
            t0 => t0.ToString(),
            t1 => t1,
            t2 => t2.ToString(),
            t3 => t3.ToString(),
            t4 => t4.ToString(),
            t5 => t5.ToString(),
            t6 => t6.ToString(),
            t7 => Convert.ToBase64String(t7)
        );
    }

    /// <summary>
    /// Benchmark TryGetValue on 8-type union with first type active.
    /// </summary>
    /// <returns>True if value was extracted.</returns>
    [Benchmark]
    public bool TryGetValue8TypeT0()
    {
        return _union8T0.TryGetT0(out var value);
    }

    /// <summary>
    /// Benchmark TryGetValue on 8-type union with last type active (worst case).
    /// </summary>
    /// <returns>True if value was extracted.</returns>
    [Benchmark]
    public bool TryGetValue8TypeT7()
    {
        return _union8T7.TryGetT7(out var value);
    }

    /// <summary>
    /// Benchmark checking all types sequentially on 8-type union.
    /// Simulates exhaustive type checking pattern.
    /// </summary>
    /// <returns>The matched type index.</returns>
    [Benchmark]
    public int ExhaustiveTypeCheck()
    {
        if (_union8T0.TryGetT0(out _))
        {
            return 0;
        }
        if (_union8T0.TryGetT1(out _))
        {
            return 1;
        }
        if (_union8T0.TryGetT2(out _))
        {
            return 2;
        }
        if (_union8T0.TryGetT3(out _))
        {
            return 3;
        }
        if (_union8T0.TryGetT4(out _))
        {
            return 4;
        }
        if (_union8T0.TryGetT5(out _))
        {
            return 5;
        }
        if (_union8T0.TryGetT6(out _))
        {
            return 6;
        }
        if (_union8T0.TryGetT7(out _))
        {
            return 7;
        }
        return -1;
    }
}


