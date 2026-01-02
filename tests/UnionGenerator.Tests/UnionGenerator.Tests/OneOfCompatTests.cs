using System.Reflection;

namespace UnionGenerator.Tests;

/// <summary>
/// Tests for the OneOf compatibility helpers.
/// </summary>
public class OneOfCompatTests
{
    [Fact]
    public void OneOf_ToGeneratedResult_ConvertsToOkCase()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<TSuccess, TError>
    {
        public static Result<TSuccess, TError> Ok(TSuccess value) => new OkCase(value);
        public static Result<TSuccess, TError> Error(TError error) => new ErrorCase(error);
    }
}
";

        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        Assert.NotNull(assembly);

        var resultTypeDefinition = assembly.GetType("TestNamespace.Result`2");
        Assert.NotNull(resultTypeDefinition);
        var resultType = resultTypeDefinition.MakeGenericType(typeof(string), typeof(string));

        // Create generated instance using helper via reflection to supply the actual generated Result type
        var fromT0Method = typeof(global::UnionGenerator.OneOfCompat.OneOfCompat).GetMethod("FromT0", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(fromT0Method);
        var genericFromT0 = fromT0Method.MakeGenericMethod(resultType, typeof(string), typeof(string));
        var generated = genericFromT0.Invoke(null, ["ok"]);
        Assert.NotNull(generated);

        // Verify it is Ok case by checking IsOk property
        var isOkProp = resultType.GetProperty("IsOk");
        Assert.NotNull(isOkProp);
        var isOkObj = isOkProp.GetValue(generated);
        Assert.IsType<bool>(isOkObj);
        Assert.True((bool)isOkObj);
    }

    [Fact]
    public void OneOf_ToGeneratedResult_ConvertsToErrorCase()
    {
        var source = @"
using UnionGenerator.Attributes;

namespace TestNamespace
{
    [GenerateUnion]
    public partial class Result<TSuccess, TError>
    {
        public static Result<TSuccess, TError> Ok(TSuccess value) => new OkCase(value);
        public static Result<TSuccess, TError> Error(TError error) => new ErrorCase(error);
    }
}
";

        var assembly = IntegrationTestHelper.CompileAndLoadAssembly(source, out _);
        Assert.NotNull(assembly);

        var resultTypeDefinition = assembly.GetType("TestNamespace.Result`2");
        Assert.NotNull(resultTypeDefinition);
        var resultType = resultTypeDefinition.MakeGenericType(typeof(string), typeof(string));

        var fromT1Method = typeof(global::UnionGenerator.OneOfCompat.OneOfCompat).GetMethod("FromT1", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(fromT1Method);
        var genericFromT1 = fromT1Method.MakeGenericMethod(resultType, typeof(string), typeof(string));
        var generated = genericFromT1.Invoke(null, ["err"]);
        Assert.NotNull(generated);

        var isErrorProp = resultType.GetProperty("IsError");
        Assert.NotNull(isErrorProp);
        var isErrorObj = isErrorProp.GetValue(generated);
        Assert.IsType<bool>(isErrorObj);
        Assert.True((bool)isErrorObj);
    }
}