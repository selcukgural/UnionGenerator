# UnionGenerator Benchmarks

This project contains comprehensive performance benchmarks for the UnionGenerator library using BenchmarkDotNet.

## Benchmark Status Overview

### ✅ Working Benchmarks (Reliable Results)

#### 1. Pattern Matching (`PatternMatchingBenchmarks`)
**Status**: ✅ **Reliable and accurate**

Results show clear performance characteristics:
- `Match()` method performance
- `Switch()` method performance  
- `TryGetValue()` performance
- C# pattern matching (`is` operator and switch expressions)
- Comparison across different active types

**Key Findings**:
- `TryGetValue` and `is` patterns are fastest (~0.3-0.5 ns)
- `Match` operations are efficient (~0.3-0.8 ns)
- `Switch` operations show measurable overhead (~13-14 ns with 152 B allocation)

#### 2. Memory Allocations (`AllocationBenchmarks`)
**Status**: ✅ **Reliable and accurate**

Shows clear allocation patterns:
- Value types vs reference types allocation behavior
- Closure allocations in Match operations
- Performance comparison of different matching strategies

**Key Findings**:
- Value types create zero-allocation unions
- Reference types incur expected allocations (4000 B for 1000 operations)
- Match with closure: 168 B allocation
- Match without closure: Zero allocation
- `TryGetValue` and direct matching strategies are allocation-free

#### 3. Complex Unions (`ComplexUnionBenchmarks`)
**Status**: ✅ **Reliable and accurate**

8-type union performance characteristics:
- Type position impact on performance
- Exhaustive type checking patterns

**Key Findings**:
- T0 (first type) creates 24 B, T7 (last type) creates 32 B
- Match on T0: ~2.9 ns (zero allocation)
- Match on T7: ~8.3 ns (32 B allocation)
- `TryGetValue` optimized completely (0.0 ns - inlined)
- Position in union affects performance (~3x difference)

### ⚠️ Benchmarks with Issues

#### 4. Union Creation (`UnionCreationBenchmarks`)
**Status**: ⚠️ **Results unreliable (optimized away by JIT)**

All benchmarks show 0.0 ns, indicating JIT compiler completely optimized away the operations.
- Creation operations are too simple for meaningful measurement
- Results don't reflect real-world usage patterns
- **Recommendation**: Treat creation as effectively zero-cost in practice

#### 5. OneOf Comparison (`OneOfComparisonBenchmarks`)
**Status**: ⚠️ **Partial results (many operations optimized away)**

Several benchmarks show 0.0 ns due to JIT optimization:
- Creation benchmarks are unreliable
- Match operations show measurable but inconsistent results
- `TryGetValue` operations optimized away

**Limited Findings**:
- Both libraries have comparable match performance (~0.3-0.9 ns)
- Cannot make definitive performance claims without fixing benchmark methodology

#### 6. JSON Serialization (`JsonSerializationBenchmarks`)
**Status**: ❌ **Currently failing - all benchmarks report errors**

All JSON benchmarks are failing with runtime issues:
- Serialization/deserialization operations not completing
- Likely missing JSON converter registration or setup issue
- **Action Required**: Fix benchmark implementation before drawing conclusions

## Running the Benchmarks

### Prerequisites
- .NET 8.0 SDK or later
- Release configuration required (Debug builds are not representative)
- Recommend running on dedicated hardware without other intensive processes

### Run All Benchmarks
```bash
dotnet run -c Release
```

### Run Specific Benchmark Class
```bash
# Run working benchmarks
dotnet run -c Release --filter "*PatternMatchingBenchmarks*"
dotnet run -c Release --filter "*AllocationBenchmarks*"
dotnet run -c Release --filter "*ComplexUnionBenchmarks*"

# Run benchmarks with known issues (for debugging)
dotnet run -c Release --filter "*UnionCreationBenchmarks*"
dotnet run -c Release --filter "*JsonSerializationBenchmarks*"
dotnet run -c Release --filter "*OneOfComparisonBenchmarks*"
```

### Run Specific Benchmark Method
```bash
dotnet run -c Release --filter "*PatternMatchingBenchmarks.MatchInt*"
```

### Generate Reports
BenchmarkDotNet automatically generates reports in the `BenchmarkDotNet.Artifacts/results` folder:
- Markdown reports (`.md`)
- HTML reports (`.html`)
- CSV exports (`.csv`)

## Understanding Results

### Key Metrics

- **Mean**: Average execution time per operation
- **Error**: Half of 99.9% confidence interval
- **StdDev**: Standard deviation of all measurements
- **Ratio**: Performance relative to baseline (lower is better)
- **Gen0/Gen1/Gen2**: GC collections per 1000 operations
- **Allocated**: Memory allocated per operation (lower is better)

### Interpreting Special Values

- **0.0 ns**: Operation was optimized away by JIT compiler (not a real measurement)
- **NA**: Benchmark failed to execute or encountered an error
- **?**: Baseline undefined or ratio cannot be calculated

### What to Look For in Reliable Results

1. **Pattern Matching**: 
   - Sub-nanosecond for `TryGetValue` and `is` patterns
   - Single-digit nanoseconds for `Match` operations
   - Avoid `Switch` in hot paths (13-14 ns overhead + allocations)

2. **Memory Allocations**: 
   - Value types should show zero allocations
   - Reference types show expected heap allocations
   - Closures in `Match` add ~168 B overhead

3. **Complex Unions**: 
   - First type (T0) is fastest
   - Last type (T7) is ~3x slower
   - Consider type ordering for hot paths

## Best Practices Based on Verified Benchmark Results

### ✅ Proven Recommendations

1. **Use `TryGetValue` or `is` patterns for type checking** 
   - Fastest approach (~0.3-0.5 ns)
   - Zero allocations
   - Best for hot paths where you expect a specific type

2. **Use `Match` for multi-branch logic**
   - Efficient (~0.3-0.8 ns) with zero allocations when no closures
   - Clean, readable code
   - Avoid closures in performance-critical paths (adds ~168 B overhead)

3. **Avoid `Switch` in hot paths**
   - 13-14 ns overhead (17-20x slower than `Match`)
   - Allocates 152 B per operation
   - Only use when code clarity outweighs performance

4. **Prefer value types in unions for zero allocation**
   - Value types create unions with zero heap allocations
   - Reference types incur expected allocations
   - Choose based on your data model requirements

5. **Order union types by usage frequency**
   - First type (T0) is ~3x faster than last type (T7) in 8-type unions
   - Place most common types first
   - Significant impact in large unions

6. **Keep type count moderate**
   - 2-4 types show best performance
   - 8-type unions still performant but show position sensitivity
   - Consider splitting very large unions

### ⚠️ Pending Verification

These recommendations require benchmark fixes before confirming:

- **Implicit vs explicit conversions**: Currently optimized away, treat as equivalent
- **JSON serialization performance**: Benchmarks currently failing
- **vs OneOf performance**: Partial data, cannot make definitive claims

### ❌ Not Recommended

- Using `Switch` in hot paths (proven allocation overhead)
- Capturing closures in `Match` operations for high-frequency calls
- Placing frequently-used types at the end of large unions

## Known Issues and Fixes Needed

### Issue 1: Union Creation Benchmarks Optimized Away
**Problem**: JIT compiler completely optimizes away simple creation operations (0.0 ns results)

**Potential Solutions**:
- Use `[MethodImpl(MethodImplOptions.NoInlining)]` on creation methods
- Add side effects that prevent optimization (e.g., sum results)
- Use `GC.KeepAlive()` to prevent dead code elimination
- Increase operation complexity

### Issue 2: JSON Serialization Benchmarks Failing
**Problem**: All JSON benchmarks report errors (NA results)

**Required Investigation**:
- Verify JSON converter registration
- Check for missing setup/initialization
- Validate benchmark data models
- Add proper error handling and logging

### Issue 3: OneOf Comparison Partial Results
**Problem**: Many comparison benchmarks show 0.0 ns (optimized away)

**Potential Solutions**:
- Apply fixes from Issue 1
- Ensure fair comparison setup
- Add anti-optimization techniques consistently

## Adding New Benchmarks

To add new benchmarks:

1. Create a new class in this project
2. Add required attributes:
   ```csharp
   [SimpleJob(RuntimeMoniker.Net80)]
   [MemoryDiagnoser]
   [MarkdownExporter]
   public class MyBenchmarks
   ```
3. Mark one method with `[Benchmark(Baseline = true)]`
4. Mark other methods with `[Benchmark]`
5. Add XML documentation to all members
6. **Important**: Add anti-optimization techniques to prevent JIT from eliminating code:
   ```csharp
   private volatile object _sink; // Prevents optimization
   
   [Benchmark]
   public void MyBenchmark()
   {
       var result = CreateUnion();
       _sink = result; // Prevents dead code elimination
   }
   ```

## CI/CD Integration

### Running in Continuous Integration

**For Reliable Benchmarks Only** (recommended):
```bash
# Run only working benchmarks
dotnet run -c Release \
  --filter "*PatternMatchingBenchmarks*" \
  --exporters json markdown

dotnet run -c Release \
  --filter "*AllocationBenchmarks*" \
  --exporters json markdown

dotnet run -c Release \
  --filter "*ComplexUnionBenchmarks*" \
  --exporters json markdown
```

**For All Benchmarks** (includes failing ones):
```bash
dotnet run -c Release --exporters json markdown --filter "*"
```

### Performance Regression Detection

Parse JSON output to track metrics over time:
- Track Mean execution time (watch for >10% increases)
- Track Allocated memory (watch for any increase)
- Alert on Gen0/Gen1/Gen2 increases
- Compare ratio changes against baseline

**Example**: Set up alerts if:
- `PatternMatchingBenchmarks.MatchInt` exceeds 1.0 ns
- `AllocationBenchmarks.MatchWithoutClosure` shows any allocation
- `ComplexUnionBenchmarks.Match8TypeT0` exceeds 3.5 ns

### Baseline Comparisons

Each benchmark class should have a `Baseline = true` benchmark that serves as the reference point. All other results are shown as ratios compared to this baseline. This allows tracking relative performance changes even when absolute numbers vary across different hardware.

## Contributing

When submitting benchmark changes:

1. **Run benchmarks locally** before submitting PR
2. **Include before/after results** in PR description
3. **Document any methodology changes**
4. **Fix failing benchmarks** before adding new ones
5. **Update this README** if results change significantly

