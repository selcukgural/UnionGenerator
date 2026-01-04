using BenchmarkDotNet.Running;

namespace UnionGenerator.Benchmarks;

/// <summary>
/// Entry point for benchmark execution.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Main entry point. Runs all benchmarks.
    /// </summary>
    /// <param name="args">Command line arguments passed to BenchmarkDotNet.</param>
    private static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}

