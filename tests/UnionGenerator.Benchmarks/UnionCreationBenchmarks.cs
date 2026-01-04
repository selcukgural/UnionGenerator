using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace UnionGenerator.Benchmarks;

/// <summary>
/// Benchmarks for union creation performance.
/// Measures the cost of creating union instances from different value types.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[MarkdownExporter]
public class UnionCreationBenchmarks
{
    /// <summary>
    /// Benchmark creating a union from an integer value.
    /// </summary>
    /// <returns>The created union instance.</returns>
    [Benchmark(Baseline = true)]
    public Result<int, string> CreateFromInt()
    {
        return Result<int, string>.FromT0(42);
    }

    /// <summary>
    /// Benchmark creating a union from a string value.
    /// </summary>
    /// <returns>The created union instance.</returns>
    [Benchmark]
    public Result<int, string> CreateFromString()
    {
        return Result<int, string>.FromT1("error");
    }

    /// <summary>
    /// Benchmark creating a union from an integer using implicit conversion.
    /// </summary>
    /// <returns>The created union instance.</returns>
    [Benchmark]
    public Result<int, string> CreateFromIntImplicit()
    {
        Result<int, string> result = 42;
        return result;
    }

    /// <summary>
    /// Benchmark creating a union from a string using implicit conversion.
    /// </summary>
    /// <returns>The created union instance.</returns>
    [Benchmark]
    public Result<int, string> CreateFromStringImplicit()
    {
        Result<int, string> result = "error";
        return result;
    }

    /// <summary>
    /// Benchmark creating a 4-type union from the first type.
    /// </summary>
    /// <returns>The created union instance.</returns>
    [Benchmark]
    public Result4<int, string, bool, double> CreateUnion4FromT0()
    {
        return Result4<int, string, bool, double>.FromT0(42);
    }

    /// <summary>
    /// Benchmark creating a 4-type union from the fourth type.
    /// </summary>
    /// <returns>The created union instance.</returns>
    [Benchmark]
    public Result4<int, string, bool, double> CreateUnion4FromT3()
    {
        return Result4<int, string, bool, double>.FromT3(3.14);
    }
}


