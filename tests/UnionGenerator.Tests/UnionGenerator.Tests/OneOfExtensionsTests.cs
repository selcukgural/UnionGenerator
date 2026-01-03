using System.Reflection;

namespace UnionGenerator.Tests;

public class OneOfExtensionsTests
{
    [Fact]
    public void OneOf_ToGeneratedResult_Extension_Works()
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

        // create a OneOf<string,string> using the internal constructor: (object value, int index)
        var oneType = typeof(OneOf.OneOf<string, string>);
        // Create instance via op_Implicit(string) to avoid constructor/version differences
        var implicitMethods = oneType.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m is { IsSpecialName: true, Name: "op_Implicit" }).ToList();
        MethodInfo? implicitMethod = null;
        foreach (var m in implicitMethods)
        {
            var ps = m.GetParameters();
            if (ps.Length == 1 && ps[0].ParameterType.IsAssignableFrom(typeof(string)))
            {
                implicitMethod = m;
                break;
            }
        }
        Assert.NotNull(implicitMethod);
        var one = implicitMethod.Invoke(null, ["ok"]);

        // call extension method via reflection to specify TGenerated at runtime
        var method = typeof(global::UnionGenerator.OneOfExtensions.OneOfExtensions)
                     .GetMethods(BindingFlags.Public | BindingFlags.Static)
                     .FirstOrDefault(m => m.Name == "ToGeneratedResult");
        Assert.NotNull(method);

        var generic = method.MakeGenericMethod(resultType, typeof(string), typeof(string));
        var generated = generic.Invoke(null, [one]);
        Assert.NotNull(generated);

        var isOkProp = resultType.GetProperty("IsOk");
        Assert.NotNull(isOkProp);
        var isOk = isOkProp.GetValue(generated);
        Assert.IsType<bool>(isOk);
        Assert.True((bool)isOk);
    }
}