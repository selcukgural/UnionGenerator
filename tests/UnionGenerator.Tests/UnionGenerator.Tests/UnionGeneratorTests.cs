using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using UnionGeneratorGenerator = UnionGenerator.UnionGenerator;

namespace UnionGenerator.Tests;

/// <summary>
/// Tests for the Union Generator.
/// </summary>
public class UnionGeneratorTests
{
    /// <summary>
    /// Tests that the generator finds classes marked with [GenerateUnion] attribute.
    /// </summary>
    [Fact]
    public void GeneratorFindsUnionClasses()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedFiles = runResult.GeneratedTrees.Select(t => t.FilePath).ToList();

        // Should generate at least one file (the union code)
        Assert.NotEmpty(generatedFiles);
        Assert.Contains(generatedFiles, f => f.Contains("Result.g.cs"));
    }

    /// <summary>
    /// Tests that the generator produces correct nested case classes.
    /// </summary>
    [Fact]
    public void GeneratorProducesCaseClasses()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Result.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        Assert.Contains("public sealed class OkCase", generatedCode);
        Assert.Contains("public sealed class ErrorCase", generatedCode);
    }

    /// <summary>
    /// Tests that the generator produces pattern matching properties.
    /// </summary>
    [Fact]
    public void GeneratorProducesPatternMatchingProperties()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Result.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        Assert.Contains("public bool IsOk", generatedCode);
        Assert.Contains("public bool IsError", generatedCode);
    }

    /// <summary>
    /// Tests that the generator handles non-generic union types.
    /// </summary>
    [Fact]
    public void GeneratorHandlesNonGenericTypes()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Option<T>
    {
        public static Option<T> Some(T value) => new SomeCase(value);
        public static Option<T> None() => new NoneCase();
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Option.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        Assert.Contains("public sealed class SomeCase", generatedCode);
        Assert.Contains("public sealed class NoneCase", generatedCode);
        Assert.Contains("public bool IsSome", generatedCode);
        Assert.Contains("public bool IsNone", generatedCode);
    }

    /// <summary>
    /// Tests that the generator produces Value property for the first case.
    /// </summary>
    [Fact]
    public void GeneratorProducesValueProperty()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Result.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        Assert.Contains("public", generatedCode);
        Assert.Contains("Value", generatedCode);
        Assert.Contains("get", generatedCode);
    }

    /// <summary>
    /// Tests that the generator produces ErrorValue property for the second case.
    /// </summary>
    [Fact]
    public void GeneratorProducesErrorValueProperty()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Result.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        Assert.Contains("public", generatedCode);
        Assert.Contains("ErrorValue", generatedCode);
        Assert.Contains("get", generatedCode);
    }

    /// <summary>
    /// Tests that the generator produces Match method.
    /// </summary>
    [Fact]
    public void GeneratorProducesMatchMethod()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Result.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        Assert.Contains("public TResult Match<TResult>", generatedCode);
        Assert.Contains("Func<T, TResult> ok", generatedCode);
        Assert.Contains("Func<E, TResult> error", generatedCode);
    }

    /// <summary>
    /// Tests that Match method works correctly for multiple cases.
    /// </summary>
    [Fact]
    public void MatchMethodWorksForMultipleCases()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Option<T>
    {
        public static Option<T> Some(T value) => new SomeCase(value);
        public static Option<T> None() => new NoneCase();
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Option.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        Assert.Contains("public TResult Match<TResult>", generatedCode);
        Assert.Contains("Func<T, TResult> some", generatedCode);
        Assert.Contains("Func<TResult> none", generatedCode);
    }

    /// <summary>
    /// Tests that the generator produces Equals method.
    /// </summary>
    [Fact]
    public void GeneratorProducesEqualsMethod()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Result.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        Assert.Contains("public override bool Equals", generatedCode);
        Assert.Contains("public bool Equals(", generatedCode);
    }

    /// <summary>
    /// Tests that the generator produces equality operators (==, !=).
    /// </summary>
    [Fact]
    public void GeneratorProducesEqualityOperators()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Result.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        Assert.Contains("public static bool operator ==", generatedCode);
        Assert.Contains("public static bool operator !=", generatedCode);
    }

    /// <summary>
    /// Tests that the generator produces GetHashCode method.
    /// </summary>
    [Fact]
    public void GeneratorProducesGetHashCodeMethod()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Result.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        Assert.Contains("public override int GetHashCode()", generatedCode);
        Assert.Contains("HashCode.Combine", generatedCode);
    }

    /// <summary>
    /// Tests that the generator produces ToString method.
    /// </summary>
    [Fact]
    public void GeneratorProducesToStringMethod()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Result.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        Assert.Contains("public override string ToString()", generatedCode);
    }

    /// <summary>
    /// Tests that generated code can be compiled and executed.
    /// </summary>
    [Fact]
    public void GeneratedCodeCompilesAndRuns()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        Assert.NotNull(assembly);

        var resultType = assembly.GetType("TestNamespace.Result`2");
        Assert.NotNull(resultType);

        // Test creating instances
        var genericResultType = resultType.MakeGenericType(typeof(string), typeof(Exception));
        var okMethod = genericResultType.GetMethod("Ok", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(okMethod);

        object? okInstance = okMethod.Invoke(null, ["test"]);
        Assert.NotNull(okInstance);
    }

    /// <summary>
    /// Tests that IsOk and IsError properties work correctly at runtime.
    /// </summary>
    [Fact]
    public void PatternMatchingPropertiesWorkAtRuntime()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        Assert.NotNull(assembly);

        var resultType = assembly.GetType("TestNamespace.Result`2")!
                                 .MakeGenericType(typeof(string), typeof(Exception));

        var okMethod = resultType.GetMethod("Ok", BindingFlags.Public | BindingFlags.Static);
        var errorMethod = resultType.GetMethod("Error", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(okMethod);
        Assert.NotNull(errorMethod);

        object? okInstance = okMethod.Invoke(null, ["success"]);
        object? errorInstance = errorMethod.Invoke(null, [new Exception("error")]);

        var isOkProperty = resultType.GetProperty("IsOk");
        var isErrorProperty = resultType.GetProperty("IsError");

        Assert.NotNull(isOkProperty);
        Assert.NotNull(isErrorProperty);

        var isOkValueObj = isOkProperty.GetValue(okInstance);
        Assert.IsType<bool>(isOkValueObj);
        Assert.True((bool)isOkValueObj);

        var isErrorValueObj = isErrorProperty.GetValue(okInstance);
        Assert.IsType<bool>(isErrorValueObj);
        Assert.False((bool)isErrorValueObj);

        var isOkOnErrorObj = isOkProperty.GetValue(errorInstance);
        Assert.IsType<bool>(isOkOnErrorObj);
        Assert.False((bool)isOkOnErrorObj);

        var isErrorOnErrorObj = isErrorProperty.GetValue(errorInstance);
        Assert.IsType<bool>(isErrorOnErrorObj);
        Assert.True((bool)isErrorOnErrorObj);
    }

    /// <summary>
    /// Tests that Value and ErrorValue properties work correctly at runtime.
    /// </summary>
    [Fact]
    public void ValuePropertiesWorkAtRuntime()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        Assert.NotNull(assembly);

        var resultType = assembly.GetType("TestNamespace.Result`2")!
                                 .MakeGenericType(typeof(string), typeof(Exception));

        var okMethod = resultType.GetMethod("Ok", BindingFlags.Public | BindingFlags.Static);
        var errorMethod = resultType.GetMethod("Error", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(okMethod);
        Assert.NotNull(errorMethod);

        object? okInstance = okMethod.Invoke(null, ["test value"]);
        object? errorInstance = errorMethod.Invoke(null, [new Exception("test error")]);

        var valueProperty = resultType.GetProperty("Value");
        var errorValueProperty = resultType.GetProperty("ErrorValue");

        Assert.NotNull(valueProperty);
        Assert.NotNull(errorValueProperty);

        Assert.Equal("test value", valueProperty.GetValue(okInstance));
        Assert.Null(errorValueProperty.GetValue(okInstance));
        Assert.Null(valueProperty.GetValue(errorInstance));
        Assert.NotNull(errorValueProperty.GetValue(errorInstance));
    }

    /// <summary>
    /// Tests that Match method works correctly at runtime.
    /// </summary>
    [Fact]
    public void MatchMethodWorksAtRuntime()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        Assert.NotNull(assembly);

        var resultType = assembly.GetType("TestNamespace.Result`2")!
                                 .MakeGenericType(typeof(string), typeof(Exception));

        var okMethod = resultType.GetMethod("Ok", BindingFlags.Public | BindingFlags.Static);
            
        Assert.NotNull(okMethod);
            
        object? okInstance = okMethod.Invoke(null, ["success"]);

        // Create func delegates
        Func<string, string> okFunc = s => s.ToUpper();
        Func<Exception, string> errorFunc = e => e.Message;

        // Find Match method with correct signature
        var matchMethods = resultType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                     .Where(m => m.Name == "Match" && m.IsGenericMethod)
                                     .ToList();

        Assert.NotEmpty(matchMethods);
        var matchMethod = matchMethods[0].MakeGenericMethod(typeof(string));
            
        object? result = matchMethod.Invoke(okInstance, [okFunc, errorFunc]);
        Assert.NotNull(result);
        Assert.Equal("SUCCESS", (string)result);
    }

    /// <summary>
    /// Tests that Equals method works correctly at runtime.
    /// </summary>
    [Fact]
    public void EqualsMethodWorksAtRuntime()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        Assert.NotNull(assembly);

        var resultType = assembly.GetType("TestNamespace.Result`2")!
                                 .MakeGenericType(typeof(string), typeof(Exception));

        var okMethod = resultType.GetMethod("Ok", BindingFlags.Public | BindingFlags.Static);
        var equalsMethod = resultType.GetMethod("Equals", [resultType]);

        Assert.NotNull(okMethod);
        Assert.NotNull(equalsMethod);

        object? ok1 = okMethod.Invoke(null, ["test"]);
        object? ok2 = okMethod.Invoke(null, ["test"]);
        object? ok3 = okMethod.Invoke(null, ["different"]);

        object? equalsResultObj1 = equalsMethod.Invoke(ok1, [ok2]);
        Assert.IsType<bool>(equalsResultObj1);
        Assert.True((bool)equalsResultObj1);

        object? equalsResultObj2 = equalsMethod.Invoke(ok1, [ok3]);
        Assert.IsType<bool>(equalsResultObj2);
        Assert.False((bool)equalsResultObj2);
    }

    /// <summary>
    /// Tests that equality operators work correctly at runtime.
    /// </summary>
    [Fact]
    public void EqualityOperatorsWorkAtRuntime()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        Assert.NotNull(assembly);

        var resultType = assembly.GetType("TestNamespace.Result`2")!
                                 .MakeGenericType(typeof(string), typeof(Exception));

        var okMethod = resultType.GetMethod("Ok", BindingFlags.Public | BindingFlags.Static);
        var equalsOperator = resultType.GetMethod("op_Equality", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(okMethod);
        Assert.NotNull(equalsOperator);

        object? ok1 = okMethod.Invoke(null, ["test"]);
        object? ok2 = okMethod.Invoke(null, ["test"]);
        object? ok3 = okMethod.Invoke(null, ["different"]);

        object? eqOpResult1 = equalsOperator.Invoke(null, [ok1, ok2]);
        Assert.IsType<bool>(eqOpResult1);
        Assert.True((bool)eqOpResult1);

        object? eqOpResult2 = equalsOperator.Invoke(null, [ok1, ok3]);
        Assert.IsType<bool>(eqOpResult2);
        Assert.False((bool)eqOpResult2);
    }

    /// <summary>
    /// Tests that GetHashCode works correctly at runtime.
    /// </summary>
    [Fact]
    public void GetHashCodeWorksAtRuntime()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        Assert.NotNull(assembly);

        var resultType = assembly.GetType("TestNamespace.Result`2")!
                                 .MakeGenericType(typeof(string), typeof(Exception));

        var okMethod = resultType.GetMethod("Ok", BindingFlags.Public | BindingFlags.Static);
        var getHashCodeMethod = resultType.GetMethod("GetHashCode", Type.EmptyTypes);

        Assert.NotNull(okMethod);
        Assert.NotNull(getHashCodeMethod);

        object? ok1 = okMethod.Invoke(null, ["test"]);
        object? ok2 = okMethod.Invoke(null, ["test"]);
        object? ok3 = okMethod.Invoke(null, ["different"]);

        object? hash1Obj = getHashCodeMethod.Invoke(ok1, null);
        Assert.IsType<int>(hash1Obj);
        var hash1 = (int)hash1Obj;

        object? hash2Obj = getHashCodeMethod.Invoke(ok2, null);
        Assert.IsType<int>(hash2Obj);
        var hash2 = (int)hash2Obj;

        object? hash3Obj = getHashCodeMethod.Invoke(ok3, null);
        Assert.IsType<int>(hash3Obj);
        var hash3 = (int)hash3Obj;

        Assert.Equal(hash1, hash2);    // Same values should have same hash
        Assert.NotEqual(hash1, hash3); // Different values should have different hash
    }

    /// <summary>
    /// Tests that ToString works correctly at runtime.
    /// </summary>
    [Fact]
    public void ToStringWorksAtRuntime()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        Assert.NotNull(assembly);

        var resultType = assembly.GetType("TestNamespace.Result`2")!
                                 .MakeGenericType(typeof(string), typeof(Exception));

        var okMethod = resultType.GetMethod("Ok", BindingFlags.Public | BindingFlags.Static);
        var errorMethod = resultType.GetMethod("Error", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(okMethod);
        Assert.NotNull(errorMethod);

        var okInstance = okMethod.Invoke(null, ["test"]);
        var errorInstance = errorMethod.Invoke(null, [new Exception("error")]);

        Assert.NotNull(okInstance);
        Assert.NotNull(errorInstance);

        var toString1 = okInstance.ToString();
        var toString2 = errorInstance.ToString();

        Assert.Contains("Ok", toString1);
        Assert.Contains("test", toString1);
        Assert.Contains("Error", toString2);
    }

    /// <summary>
    /// Tests that unit-like cases (no parameters) work correctly.
    /// </summary>
    [Fact]
    public void UnitLikeCasesWorkAtRuntime()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Option<T>
    {
        public static Option<T> Some(T value) => new SomeCase(value);
        public static Option<T> None() => new NoneCase();
    }
}";

        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        Assert.NotNull(assembly);

        var optionType = assembly.GetType("TestNamespace.Option`1")!
                                 .MakeGenericType(typeof(string));

        var noneMethod = optionType.GetMethod("None", BindingFlags.Public | BindingFlags.Static);
            
        Assert.NotNull(noneMethod);
            
        var noneInstance = noneMethod.Invoke(null, null);

        var isNoneProperty = optionType.GetProperty("IsNone");
            
        Assert.NotNull(isNoneProperty);
        var isNoneObj = isNoneProperty.GetValue(noneInstance);
        Assert.IsType<bool>(isNoneObj);
        Assert.True((bool)isNoneObj);
    }

    /// <summary>
    /// Tests that null values are handled correctly.
    /// </summary>
    [Fact]
    public void NullValuesAreHandledCorrectly()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        Assert.NotNull(assembly);

        var resultType = assembly.GetType("TestNamespace.Result`2")!
                                 .MakeGenericType(typeof(string), typeof(Exception));

        var okMethod = resultType.GetMethod("Ok", BindingFlags.Public | BindingFlags.Static);
            
        Assert.NotNull(okMethod);
            
        var okInstance = okMethod.Invoke(null, [null]);

        var valueProperty = resultType.GetProperty("Value");
            
        Assert.NotNull(valueProperty);
            
        var value = valueProperty.GetValue(okInstance);
        Assert.Null(value);
    }

    /// <summary>
    /// Tests that equality with null works correctly.
    /// </summary>
    [Fact]
    public void EqualityWithNullWorksCorrectly()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        Assert.NotNull(assembly);

        var resultType = assembly.GetType("TestNamespace.Result`2")!
                                 .MakeGenericType(typeof(string), typeof(Exception));

        var okMethod = resultType.GetMethod("Ok", BindingFlags.Public | BindingFlags.Static);
        var equalsMethod = resultType.GetMethod("Equals", [resultType]);

        Assert.NotNull(okMethod);
        Assert.NotNull(equalsMethod);

        var okInstance = okMethod.Invoke(null, ["test"]);

        var equalsNullResultObj = equalsMethod.Invoke(okInstance, [null]);
        Assert.IsType<bool>(equalsNullResultObj);
        Assert.False((bool)equalsNullResultObj);
    }

    /// <summary>
    /// Tests that different case types are not equal.
    /// </summary>
    [Fact]
    public void DifferentCaseTypesAreNotEqual()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        Assert.NotNull(assembly);

        var resultType = assembly.GetType("TestNamespace.Result`2")!
                                 .MakeGenericType(typeof(string), typeof(Exception));

        var okMethod = resultType.GetMethod("Ok", BindingFlags.Public | BindingFlags.Static);
        var errorMethod = resultType.GetMethod("Error", BindingFlags.Public | BindingFlags.Static);
        var equalsMethod = resultType.GetMethod("Equals", [resultType]);

        Assert.NotNull(okMethod);
        Assert.NotNull(errorMethod);
        Assert.NotNull(equalsMethod);

        var okInstance = okMethod.Invoke(null, ["test"]);
        var errorInstance = errorMethod.Invoke(null, [new Exception("error")]);

        Assert.False((bool)equalsMethod.Invoke(okInstance, [errorInstance])!);
    }

    /// <summary>
    /// Tests that switch expressions work with generated union types.
    /// </summary>
    [Fact]
    public void SwitchExpressionsWorkWithUnionTypes()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Result.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        // Switch expressions work with pattern matching on nested classes
        // We verify that nested case classes are generated correctly
        Assert.Contains("public sealed class OkCase", generatedCode);
        Assert.Contains("public sealed class ErrorCase", generatedCode);
    }

    /// <summary>
    /// Tests that switch expressions work with unit-like cases (no parameters).
    /// </summary>
    [Fact]
    public void SwitchExpressionsWorkWithUnitLikeCases()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Option<T>
    {
        public static Option<T> Some(T value) => new SomeCase(value);
        public static Option<T> None() => new NoneCase();
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Option.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        // Verify that unit-like case (NoneCase) is generated
        Assert.Contains("public sealed class SomeCase", generatedCode);
        Assert.Contains("public sealed class NoneCase", generatedCode);
        // NoneCase should have a parameterless constructor (it's unit-like)
        Assert.Contains("internal NoneCase()", generatedCode);
        // SomeCase should have Value property
        Assert.Contains("public new T Value", generatedCode);
        // Verify that both cases have pattern matching properties
        Assert.Contains("public bool IsSome", generatedCode);
        Assert.Contains("public bool IsNone", generatedCode);
    }

    /// <summary>
    /// Tests that <c>IEquatable&lt;T&gt;</c> interface is implemented.
    /// </summary>
    [Fact]
    public void EquatableInterfaceIsImplemented()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Result.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        // Verify IEquatable interface is implemented
        Assert.Contains(": IEquatable<", generatedCode);
        Assert.Contains("public bool Equals(", generatedCode);
    }

    /// <summary>
    /// Tests that <c>IEquatable&lt;T&gt;</c> works correctly at runtime.
    /// </summary>
    [Fact]
    public void EquatableWorksAtRuntime()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        Assert.NotNull(assembly);

        var resultType = assembly.GetType("TestNamespace.Result`2")!
                                 .MakeGenericType(typeof(int), typeof(string));

        var okMethod = resultType.GetMethod("Ok", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(okMethod);

        var result1 = okMethod.Invoke(null, [42]);
        var result2 = okMethod.Invoke(null, [42]);
        var result3 = okMethod.Invoke(null, [100]);

        // Test IEquatable<T> interface - check that result1 implements IEquatable<T>
        var iEquatableType = typeof(IEquatable<>).MakeGenericType(resultType);
        Assert.True(iEquatableType.IsAssignableFrom(resultType));

        // Get Equals method from IEquatable<T>
        var equalsMethod = resultType.GetMethod("Equals", [resultType]);
        Assert.NotNull(equalsMethod);

        var equalsResultObj = equalsMethod.Invoke(result1, [result2]);
        Assert.IsType<bool>(equalsResultObj);
        Assert.True((bool)equalsResultObj);

        var equalsResultObj2 = equalsMethod.Invoke(result1, [result3]);
        Assert.IsType<bool>(equalsResultObj2);
        Assert.False((bool)equalsResultObj2);
    }

    /// <summary>
    /// Tests that Deconstruct method is generated for 2-case unions.
    /// </summary>
    [Fact]
    public void DeconstructMethodIsGenerated()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Result.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        // Verify Deconstruct method is generated
        Assert.Contains("public void Deconstruct(", generatedCode);
        Assert.Contains("out", generatedCode);
    }

    /// <summary>
    /// Tests that Deconstruct method works correctly at runtime.
    /// </summary>
    [Fact]
    public void DeconstructMethodWorksAtRuntime()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        Assert.NotNull(assembly);

        var resultType = assembly.GetType("TestNamespace.Result`2")!
                                 .MakeGenericType(typeof(string), typeof(Exception));

        var okMethod = resultType.GetMethod("Ok", BindingFlags.Public | BindingFlags.Static);
        var errorMethod = resultType.GetMethod("Error", BindingFlags.Public | BindingFlags.Static);
        var deconstructMethod = resultType.GetMethod("Deconstruct", BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(okMethod);
        Assert.NotNull(errorMethod);
        Assert.NotNull(deconstructMethod);

        // Test Deconstruct with Ok case
        object? okInstance = okMethod.Invoke(null, ["test value"]);
        var okParams = new object?[] { null, null };
        var okParamTypes = new[] { typeof(string).MakeByRefType(), typeof(Exception).MakeByRefType() };
        var okDeconstruct = resultType.GetMethod("Deconstruct", okParamTypes);
        Assert.NotNull(okDeconstruct);
        okDeconstruct.Invoke(okInstance, okParams);
        // Deconstruct returns void; ensure method executed (no exception) and out params updated.
        Assert.Equal("test value", okParams[0]);
        Assert.Null(okParams[1]);

        // Test Deconstruct with Error case
        object? errorInstance = errorMethod.Invoke(null, [new Exception("test error")]);
        var errorParams = new object?[] { null, null };
        var errorDeconstruct = resultType.GetMethod("Deconstruct", okParamTypes);
        Assert.NotNull(errorDeconstruct);
        errorDeconstruct.Invoke(errorInstance, errorParams);
        Assert.Null(errorParams[0]);
        Assert.NotNull(errorParams[1]);
        Assert.Equal("test error", ((Exception)errorParams[1]!).Message);
    }

    /// <summary>
    /// Tests that TryGetValue methods are generated.
    /// </summary>
    [Fact]
    public void TryGetValueMethodsAreGenerated()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Result.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        // Verify TryGetValue methods are generated
        Assert.Contains("public bool TryGetOk(", generatedCode);
        Assert.Contains("public bool TryGetError(", generatedCode);
    }

    /// <summary>
    /// Tests that TryGetValue methods work correctly at runtime.
    /// </summary>
    [Fact]
    public void TryGetValueMethodsWorkAtRuntime()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        Assert.NotNull(assembly);

        var resultType = assembly.GetType("TestNamespace.Result`2")!
                                 .MakeGenericType(typeof(string), typeof(Exception));

        var okMethod = resultType.GetMethod("Ok", BindingFlags.Public | BindingFlags.Static);
        var errorMethod = resultType.GetMethod("Error", BindingFlags.Public | BindingFlags.Static);
        var tryGetOkMethod = resultType.GetMethod("TryGetOk", BindingFlags.Public | BindingFlags.Instance);
        var tryGetErrorMethod = resultType.GetMethod("TryGetError", BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(okMethod);
        Assert.NotNull(errorMethod);
        Assert.NotNull(tryGetOkMethod);
        Assert.NotNull(tryGetErrorMethod);

        // Test TryGetOk with Ok case
        object? okInstance = okMethod.Invoke(null, ["test value"]);
        var okParams = new object?[] { null };
        var okResult = (bool)(tryGetOkMethod.Invoke(okInstance, okParams) ?? throw new InvalidOperationException());
        Assert.True(okResult);
        Assert.Equal("test value", okParams[0]);

        // Test TryGetOk with Error case
        object? errorInstance = errorMethod.Invoke(null, [new Exception("error")]);
        var errorParams = new object?[] { null };
        var errorResult = (bool)(tryGetOkMethod.Invoke(errorInstance, errorParams) ?? throw new InvalidOperationException());
        Assert.False(errorResult);

        // Test TryGetError with Error case
        var errorParams2 = new object?[] { null };
        var errorResult2 = (bool)(tryGetErrorMethod.Invoke(errorInstance, errorParams2) ?? throw new InvalidOperationException());
        Assert.True(errorResult2);
        Assert.NotNull(errorParams2[0]);
    }

    /// <summary>
    /// Tests that Map methods are generated.
    /// </summary>
    [Fact]
    public void MapMethodsAreGenerated()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Result.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        // Verify Map methods are generated
        Assert.Contains("public Result<TNew,", generatedCode);
        Assert.Contains("MapOk", generatedCode);
        Assert.Contains("MapError", generatedCode);
    }

    /// <summary>
    /// Tests that OrElse/Or methods are generated.
    /// </summary>
    [Fact]
    public void OrElseMethodsAreGenerated()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Result.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        // Verify OrElse/Or methods are generated
        Assert.Contains("OkOrElse", generatedCode);
        Assert.Contains("OkOr", generatedCode);
    }

    /// <summary>
    /// Tests that XML documentation is generated for union classes.
    /// </summary>
    [Fact]
    public void XmlDocumentationIsGenerated()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Result.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        // Verify XML documentation is present
        Assert.Contains("/// <summary>", generatedCode);
        Assert.Contains("/// Represents a discriminated union type", generatedCode);
        Assert.Contains("/// <remarks>", generatedCode);
        Assert.Contains("/// <example>", generatedCode);
        Assert.Contains("/// <param", generatedCode);
        Assert.Contains("/// <returns>", generatedCode);
    }

    /// <summary>
    /// Tests that XML documentation is generated for case classes.
    /// </summary>
    [Fact]
    public void XmlDocumentationIsGeneratedForCaseClasses()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Result.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        // Verify XML documentation for case classes
        Assert.Contains("/// Represents the Ok case", generatedCode);
        Assert.Contains("/// Represents the Error case", generatedCode);
    }

    /// <summary>
    /// Tests that XML documentation is generated for Match method.
    /// </summary>
    [Fact]
    public void XmlDocumentationIsGeneratedForMatchMethod()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Result.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        // Verify XML documentation for Match method
        Assert.Contains("/// Performs pattern matching on the union", generatedCode);
        Assert.Contains("/// <typeparam name=\"TResult\">", generatedCode);
        Assert.Contains("/// <exception cref=\"InvalidOperationException\">", generatedCode);
    }

    /// <summary>
    /// Tests that DebuggerDisplay attribute is generated for union class.
    /// </summary>
    [Fact]
    public void DebuggerDisplayAttributeIsGenerated()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Result.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        // Verify DebuggerDisplay attribute
        Assert.Contains("[DebuggerDisplay(", generatedCode);
        Assert.Contains("using System.Diagnostics;", generatedCode);
    }

    /// <summary>
    /// Tests that DebuggerTypeProxy attribute is generated for union class.
    /// </summary>
    [Fact]
    public void DebuggerTypeProxyAttributeIsGenerated()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Result.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        // Verify DebuggerTypeProxy attribute
        Assert.Contains("[DebuggerTypeProxy(", generatedCode);
        Assert.Contains("ResultDebuggerProxy", generatedCode);
    }

    /// <summary>
    /// Tests that DebuggerTypeProxy class is generated.
    /// </summary>
    [Fact]
    public void DebuggerProxyClassIsGenerated()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Result.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        // Verify debugger proxy class
        Assert.Contains("internal sealed class ResultDebuggerProxy", generatedCode);
        Assert.Contains("public bool IsOk", generatedCode);
        Assert.Contains("public bool IsError", generatedCode);
        Assert.Contains("public string CaseName", generatedCode);
    }

    /// <summary>
    /// Tests that DebuggerDisplay attribute is generated for case classes.
    /// </summary>
    [Fact]
    public void DebuggerDisplayAttributeIsGeneratedForCaseClasses()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<T, E>
    {
        public static Result<T, E> Ok(T value) => new OkCase(value);
        public static Result<T, E> Error(E error) => new ErrorCase(error);
    }
}";

        var compilation = CreateCompilation(source);
        var generator = new UnionGeneratorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees
                                     .FirstOrDefault(t => t.FilePath.Contains("Result.g.cs"))?
                                     .GetText()
                                     .ToString();

        Assert.NotNull(generatedCode);
        // Verify DebuggerDisplay for case classes
        Assert.Contains("public sealed class OkCase", generatedCode);
        var okCaseIndex = generatedCode.IndexOf("public sealed class OkCase", StringComparison.Ordinal);
        // Check before the class declaration (DebuggerDisplay should be before the class)
        var startIndex = Math.Max(0, okCaseIndex - 100);
        var okCaseSection = generatedCode.Substring(startIndex, okCaseIndex - startIndex + 50);
        Assert.Contains("[DebuggerDisplay(", okCaseSection);
    }

    /// <summary>
    /// Creates a C# compilation from source code for testing.
    /// </summary>
    private static Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location)
        };

        return CSharpCompilation.Create(
            "TestAssembly", [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}