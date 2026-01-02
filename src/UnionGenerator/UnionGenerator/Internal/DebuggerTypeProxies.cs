using System.Diagnostics;

namespace UnionGenerator.Internal;

/// <summary>
/// Debugger type proxies used by generated unions to improve debug-time visualization.
/// </summary>
/// <typeparam name="T">The union type.</typeparam>
[DebuggerDisplay("{ToString()}")]
internal sealed class DebuggerTypeProxies
{
    /// <summary>
    /// Generic proxy placeholder for union types.
    /// </summary>
    /// <typeparam name="TUnion">Union type being proxied.</typeparam>
    internal sealed class GenericUnionDebuggerProxy<TUnion>(TUnion value)
    {
        public override string ToString()
        {
            return value?.ToString() ?? "<null>";
        }
    }
}