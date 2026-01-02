OneOf Source Generator Support
=============================

This optional generator emits compile-time adapter helpers to convert OneOf<T0,..,TN> values to generated union types created by `UnionGenerator`.

Goals for developers
- Clear diagnostics (UG2001, UG2002) with suggested fixes and example signatures.
- IntelliSense-friendly XML docs on generated adapters (summary, example, remarks).
- Throwing `FromOneOf<T...>` and no-throw `TryFromOneOf<T...>` helpers.

Quick usage
-----------
Annotate a class with `[GenerateUnion]` as usual. The generator will emit a `XxxOneOfAdapter` class in the same namespace with:

- `XxxOneOfAdapter.FromOneOf<T0,...,TN>(this OneOf.OneOf<T0,...,TN> one)`
- `XxxOneOfAdapter.TryFromOneOf<T0,...,TN>(this OneOf.OneOf<T0,...,TN> one, out Xxx<T0,...,TN> result)`

Example
-------
```csharp
[GenerateUnion]
public partial class Result<T, E>
{
    public static Result<T,E> Ok(T v) => new OkCase(v);
    public static Result<T,E> Error(E e) => new ErrorCase(e);
}

// After generator runs you can do:
OneOf.OneOf<string, string> one = OneOf.OneOf<string, string>.FromT0("ok");
var result = one.FromOneOf<Result, string, string>();

// Or safe try-pattern:
if (one.TryFromOneOf<Result, string, string>(out var res)) { /* use res */ }
```

Diagnostics
-----------
- UG2001: Missing factory method — suggests the factory name and shows an example signature.
- UG2002: Factory parameter type mismatch — shows expected parameter type.

If you prefer a simpler runtime (reflection) adapter, use `UnionGenerator.OneOfCompat` helpers.

Contributing
------------
- Tests live in `tests/UnionGenerator.Tests` and include runtime and compile-time checks. See `OneOfSourceGenAdapterTests.cs` for an example of how source-generation is validated in tests.

