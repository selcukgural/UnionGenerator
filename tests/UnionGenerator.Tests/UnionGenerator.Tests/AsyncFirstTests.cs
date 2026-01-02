namespace UnionGenerator.Tests;

public class AsyncFirstTests
{
    [Fact]
    public async Task AsyncMethodsWorkAtRuntime()
    {
        var source = @"
using UnionGenerator.Attributes;
using System;
using System.Threading.Tasks;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}

public class Runner
{
    public static async Task<int> TestMatchAsync()
    {
        var result = TestNamespace.Result<int, string>.Ok(10);
        return await result.MatchAsync(
            ok: async v => { await Task.Delay(1); return v * 2; },
            error: async e => { await Task.Delay(1); return e.Length; }
        );
    }

    public static async Task<int> TestBindAsync()
    {
        var result = TestNamespace.Result<int, string>.Ok(10);
        return await result.BindAsync(async v => { await Task.Delay(1); return v + 5; });
    }
}";
        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        var type = assembly.GetType("Runner");
            
        var matchAsyncMethod = type.GetMethod("TestMatchAsync");
        var matchOutputTask = (Task<int>)matchAsyncMethod.Invoke(null, null);
        var matchOutput = await matchOutputTask;
        Assert.Equal(20, matchOutput);

        var bindAsyncMethod = type.GetMethod("TestBindAsync");
        var bindOutputTask = (Task<int>)bindAsyncMethod.Invoke(null, null);
        var bindOutput = await bindOutputTask;
        Assert.Equal(15, bindOutput);
    }

    [Fact]
    public async Task TaskExtensionsWorkAtRuntime()
    {
        var source = @"
using UnionGenerator.Attributes;
using System;
using System.Threading.Tasks;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}

public class Runner
{
    public static async Task<int> TestTaskMatchAsync()
    {
        var task = Task.FromResult(TestNamespace.Result<int, string>.Ok(100));
        return await TestNamespace.ResultAsyncExtensions.MatchAsync(task,
            ok: v => v / 2,
            error: e => e.Length
        );
    }
}";
        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        var type = assembly.GetType("Runner");
            
        var method = type.GetMethod("TestTaskMatchAsync");
        var outputTask = (Task<int>)method.Invoke(null, null);
        var output = await outputTask;
        Assert.Equal(50, output);
    }
}