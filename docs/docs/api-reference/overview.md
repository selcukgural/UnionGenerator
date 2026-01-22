---
sidebar_position: 1
---

# API Reference Overview

Complete API reference for UnionGenerator. This section provides detailed documentation for all types, attributes, and extension methods available in the library.

## 📚 What's Included

### Generated Union Types
Every union you create with `[GenerateUnion]` gets a comprehensive API automatically generated:
- Factory methods for creating instances
- Pattern matching methods (`Match`, `MatchAsync`)
- Type checking properties (`Is*`)
- Value extraction methods (`TryGet*`)
- Equality and comparison members
- ToString and GetHashCode implementations

### Attributes
Attributes you use to configure union generation:
- `[GenerateUnion]` - The core attribute for marking union types

### Extension Methods
Helper methods that make working with unions easier:
- **MatchVoid Extensions** - Simplified matching for void-like Result types
- **ResultComposition Extensions** - Monadic operations (Bind, Map, MapError)

## 🎯 Quick Navigation

| Section | Description | Use When |
|---------|-------------|----------|
| [Generated API](./generated-api.md) | Complete API of generated union members | You need to know what methods are available on your union |
| [Attributes](./attributes.md) | Configuration attributes reference | You're defining a new union type |
| [Extension Methods](./extension-methods.md) | Helper extensions for common patterns | You're working with Result types or composing operations |

## 💡 Understanding Generated APIs

When you mark a class with `[GenerateUnion]`, the source generator creates:

```csharp
[GenerateUnion]
public partial class PaymentResult
{
    public static partial PaymentResult Success(decimal amount);
    public static partial PaymentResult Failed(string reason);
    public static partial PaymentResult Pending();
}
```

The generator adds to your partial class:
- ✅ All case classes as nested types
- ✅ Factory methods for each case
- ✅ Pattern matching infrastructure
- ✅ Type safety guarantees
- ✅ Equality semantics
- ✅ Serialization support

## 🔍 API Conventions

### Naming Patterns
- **Factory Methods**: Named exactly as your declared methods (e.g., `Success`, `Failed`)
- **Type Check Properties**: `Is{CaseName}` (e.g., `IsSuccess`, `IsFailed`)
- **Value Extraction**: `TryGet{CaseName}` (e.g., `TryGetSuccess`, `TryGetFailed`)
- **Pattern Matching**: `Match` and `MatchAsync` methods

### Type Safety
All generated APIs are fully type-safe:
- Generic parameters are preserved
- Nullable reference types are respected
- Return types are correctly inferred
- No runtime casting required

### Performance Characteristics
- **Factory Methods**: O(1) allocation
- **Pattern Matching**: O(1) switch on internal tag
- **Type Checks**: O(1) integer comparison
- **Value Extraction**: O(1) cast operation

## 📖 How to Read This Reference

Each API entry includes:
- **Signature**: Full method/property signature with types
- **Description**: What it does and when to use it
- **Parameters**: Input parameters and their constraints
- **Returns**: Return type and value semantics
- **Exceptions**: Possible exceptions thrown
- **Examples**: Real-world usage examples
- **Remarks**: Performance notes, thread-safety, best practices

## 🚀 Next Steps

- Start with [Generated API](./generated-api.md) to understand what your unions can do
- Review [Attributes](./attributes.md) to learn configuration options
- Explore [Extension Methods](./extension-methods.md) for advanced patterns

## 💬 Need More Help?

- Check [Pattern Matching Guide](../core-package/pattern-matching.md) for practical examples
- See [Best Practices](../core-package/best-practices.md) for production patterns
- Review [Common Patterns](../getting-started/common-patterns.md) for real-world scenarios
