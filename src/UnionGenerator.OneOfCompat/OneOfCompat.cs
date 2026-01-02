using System.Reflection;

namespace UnionGenerator.OneOfCompat;

/// <summary>
/// Provides simple compatibility helpers to interoperate with OneOf types and generated union types.
/// </summary>
public static class OneOfCompat
{
    private static TGenerated CreateFromFactory<TGenerated>(string factoryName, object? value)
        where TGenerated : class
    {
        var targetType = typeof(TGenerated);
        var method = targetType.GetMethod(factoryName, BindingFlags.Public | BindingFlags.Static);
        if (method == null)
        {
            throw new InvalidOperationException($"Factory method '{factoryName}' not found on type '{targetType}'.");
        }

        var result = method.Invoke(null, new[] { value });
        return result as TGenerated ?? throw new InvalidOperationException("Factory invocation returned unexpected result.");
    }

    /// <summary>
    /// Helper to create a generated union instance corresponding to the first case (T0).
    /// </summary>
    public static TGenerated FromT0<TGenerated, TSuccess, TError>(TSuccess value)
        where TGenerated : class
    {
        return CreateFromFactory<TGenerated>("Ok", value);
    }

    /// <summary>
    /// Helper to create a generated union instance corresponding to the second case (T1).
    /// </summary>
    public static TGenerated FromT1<TGenerated, TSuccess, TError>(TError value)
        where TGenerated : class
    {
        return CreateFromFactory<TGenerated>("Error", value);
    }
}