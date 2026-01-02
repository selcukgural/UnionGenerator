using System;

namespace UnionGenerator.Attributes;

/// <summary>
/// Marks a class as a discriminated union type that should have case classes and pattern matching properties generated.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateUnionAttribute : Attribute;