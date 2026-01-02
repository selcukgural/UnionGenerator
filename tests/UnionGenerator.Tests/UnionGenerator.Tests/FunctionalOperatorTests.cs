namespace UnionGenerator.Tests;

public class FunctionalOperatorTests
{
    [Fact]
    public void BindAndTapWorkAtRuntime()
    {
        var source = @"
using UnionGenerator.Attributes;
using System;

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
    public static int Test()
    {
        int tappedValue = 0;
        var result = TestNamespace.Result<int, string>.Ok(10)
            .Tap(v => tappedValue = v)
            .Bind(v => v * 2);
        
        return result == 20 && tappedValue == 10 ? 1 : 0;
    }
}";
        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        var type = assembly.GetType("Runner");
        var method = type.GetMethod("Test");
        var output = (int)method.Invoke(null, null);

        Assert.Equal(1, output);
    }

    [Fact]
    public void LinqSelectAndSelectManyWorkAtRuntime()
    {
        var source = @"
using UnionGenerator.Attributes;
using System;
using System.Linq;

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
    public static int Test()
    {
        var result = from x in TestNamespace.Result<int, string>.Ok(5)
                     from y in TestNamespace.Result<int, string>.Ok(10)
                     select x + y;
        
        return result.Match(ok: v => v, error: _ => -1);
    }
}";
        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        var type = assembly.GetType("Runner");
        var method = type.GetMethod("Test");
        var output = (int)method.Invoke(null, null);

        Assert.Equal(15, output);
    }

    [Fact]
    public void BiMapAndFoldWorkAtRuntime()
    {
        var source = @"
using UnionGenerator.Attributes;
using System;

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
    public static int Test()
    {
        var result = TestNamespace.Result<int, string>.Ok(10)
            .BiMap(ok => ok * 2, err => err.Length);
        
        var foldResult = result.Fold(ok => ok + 5, err => err + 1);
        
        return foldResult; // (10 * 2) + 5 = 25
    }
}";
        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        var type = assembly.GetType("Runner");
        var method = type.GetMethod("Test");
        var output = (int)method.Invoke(null, null);

        Assert.Equal(25, output);
    }

    [Fact]
    public void WhereAndEnsureWorkAtRuntime()
    {
        var source = @"
using UnionGenerator.Attributes;
using System;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Option<T>
    {
        public static Option<T> Some(T value) => new SomeCase(value);
        public static Option<T> None() => new NoneCase();
    }

    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}

public class Runner
{
    public static int Test()
    {
        var option = TestNamespace.Option<int>.Some(10)
            .Where(v => v > 5);
        
        var noneOption = TestNamespace.Option<int>.Some(3)
            .Where(v => v > 5);

        var result = TestNamespace.Result<int, string>.Ok(10)
            .Ensure(v => v > 15, v => ""Too small"");

        return (option.IsSome && noneOption.IsNone && result.IsError) ? 1 : 0;
    }
}";
        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        var type = assembly.GetType("Runner");
        var method = type.GetMethod("Test");
        var output = (int)method.Invoke(null, null);

        Assert.Equal(1, output);
    }

    [Fact]
    public void OrElseThrowWorksAtRuntime()
    {
        var source = @"
using UnionGenerator.Attributes;
using System;

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
    public static int Test()
    {
        try
        {
            TestNamespace.Result<int, string>.Error(""Fail"").OrElseThrow(() => new Exception(""Custom Error""));
            return 0;
        }
        catch (Exception ex) when (ex.Message == ""Custom Error"")
        {
            return 1;
        }
    }
}";
        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        var type = assembly.GetType("Runner");
        var method = type.GetMethod("Test");
        var output = (int)method.Invoke(null, null);

        Assert.Equal(1, output);
    }
}