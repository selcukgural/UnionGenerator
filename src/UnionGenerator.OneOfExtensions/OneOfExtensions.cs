using System.Reflection;

namespace UnionGenerator.OneOfExtensions;

/// <summary>
/// Provides extension methods for converting OneOf values to generated union types.
/// </summary>
public static class OneOfExtensions
{
    /// <summary>
    /// Converts a OneOf&lt;T0,T1&gt; to a generated union type by invoking the appropriate factory (Ok/Error).
    /// </summary>
    /// <typeparam name="TGenerated">Generated union type (e.g., Result&lt;T0,T1&gt;).</typeparam>
    /// <typeparam name="T0">First alternative type.</typeparam>
    /// <typeparam name="T1">Second alternative type.</typeparam>
    /// <param name="oneOf">The OneOf instance.</param>
    /// <returns>An instance of the generated union type.</returns>
    public static TGenerated ToGeneratedResult<TGenerated, T0, T1>(this OneOf.OneOf<T0, T1> oneOf) where TGenerated : class
    {
        if (oneOf.IsT0)
        {
            return CreateFromFactory<TGenerated>("Ok", oneOf.AsT0);
        }

        return !oneOf.IsT1 ? throw new InvalidOperationException("Unsupported OneOf variant.") : CreateFromFactory<TGenerated>("Error", oneOf.AsT1);
    }

    private static TGenerated CreateFromFactory<TGenerated>(string factoryName, object? value) where TGenerated : class
    {
        var target = typeof(TGenerated);
        var method = target.GetMethod(factoryName, BindingFlags.Public | BindingFlags.Static);

        if (method == null)
        {
            throw new InvalidOperationException($"Factory '{factoryName}' not found on {target}.");
        }

        var res = method.Invoke(null, [value]);
        return res as TGenerated ?? throw new InvalidOperationException("Factory returned unexpected result.");
    }
}