OneOfExtensions
================

Provides optional runtime helpers to convert OneOf&lt;T0,T1&gt; values into generated union types created by the `UnionGenerator` source generator.

Usage
-----
- Add a project reference to `UnionGenerator.OneOfExtensions` (this project already references `OneOf` v3 and `Newtonsoft.Json` v13 internally).
- Call the extension `ToGeneratedResult<TGenerated,T0,T1>()` on a `OneOf<T0,T1>` instance to obtain the generated union instance.

Example (runtime)
------------------
```csharp
// Suppose generated type: TestNamespace.Result with static factories Result.Ok(string) / Result.Error(string)
OneOf.OneOf<string,string> one = OneOf.OneOf<string,string>.FromT0("value");
var generated = one.ToGeneratedResult<TestNamespace.Result, string, string>();
// use generated.IsOk / generated.Value
```

Notes
-----
- This project is optional: the core compatibility helpers that do not depend on OneOf live in `src/UnionGenerator.OneOfCompat`.
- We aim to support OneOf v3.x. If you rely on an older OneOf binary, use `OneOfCompat` helpers instead.
- The extension uses reflection to invoke the generated union's static factory methods (`Ok`, `Error`). The generated union must expose such static factory methods with those exact names.

Security & Compatibility
------------------------
- We upgraded `OneOf` to v3.x and `Newtonsoft.Json` to v13 to reduce compatibility and security warnings. If your project has strict rules about transitive packages, review dependencies before adding this project to your builds.

License
-------
MIT (same as repository)

