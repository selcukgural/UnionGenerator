---
sidebar_position: 4
---

# Installation

Get started with UnionGenerator in minutes. Choose the packages you need based on your use case.

## Core Package

The core package provides all basic union functionality.

### NuGet Package Manager

```bash
dotnet add package UnionGenerator
```

### Package Manager Console

```powershell
Install-Package UnionGenerator
```

### PackageReference

```xml
<PackageReference Include="UnionGenerator" Version="1.0.0" />
```

## Integration Packages

Add these packages for framework-specific features.

### ASP.NET Core

For automatic `ProblemDetails` mapping and HTTP status code integration:

```bash
dotnet add package UnionGenerator.AspNetCore
```

**Features:**
- Automatic `IActionResult` conversion
- HTTP status code attributes
- `ProblemDetails` generation
- Middleware integration

[Learn more about ASP.NET Core integration →](../integrations/aspnetcore)

### Entity Framework Core

For database storage and querying:

```bash
dotnet add package UnionGenerator.EntityFrameworkCore
```

**Features:**
- Automatic value converters
- JSON column storage
- Query translation
- Migration support

[Learn more about EF Core integration →](../integrations/entityframeworkcore)

### FluentValidation

For validation error mapping:

```bash
dotnet add package UnionGenerator.FluentValidation
```

**Features:**
- Validation result to union conversion
- Error aggregation
- Property path mapping

[Learn more about FluentValidation integration →](../integrations/fluentvalidation)

### OneOf Compatibility

For migrating from the OneOf library:

```bash
dotnet add package UnionGenerator.OneOfCompat
```

**Features:**
- OneOf-compatible API
- Migration helpers
- Drop-in replacement

[Learn more about OneOf migration →](../integrations/oneof-compat)

## Analyzer Packages (Optional)

Add Roslyn analyzers for compile-time warnings and code fixes.

### Analyzers

```bash
dotnet add package UnionGenerator.Analyzers
```

**Features:**
- Exhaustive pattern matching warnings
- Unused case detection
- Invalid access warnings
- Performance suggestions

### Code Fixes

```bash
dotnet add package UnionGenerator.Analyzers.CodeFixes
```

**Features:**
- Automatic pattern completion
- Missing case generation
- Quick fixes for common issues

## Version Compatibility

| Package | .NET Version | C# Version |
|---------|--------------|------------|
| UnionGenerator | .NET 6.0+ | C# 10.0+ |
| UnionGenerator.AspNetCore | .NET 6.0+ | C# 10.0+ |
| UnionGenerator.EntityFrameworkCore | .NET 6.0+ | C# 10.0+ |
| UnionGenerator.FluentValidation | .NET 6.0+ | C# 10.0+ |
| UnionGenerator.OneOfCompat | .NET 6.0+ | C# 10.0+ |
| UnionGenerator.Analyzers | .NET 6.0+ | C# 10.0+ |

**Recommended:** .NET 8.0+ and C# 12.0+ for the best experience.

## Project Setup

### Enable Source Generators

Source generators are enabled by default in modern .NET projects. If you're using an older SDK, ensure your project file has:

```xml
<PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable> <!-- Recommended -->
</PropertyGroup>
```

### Verify Installation

Create a simple union to verify everything works:

```csharp
using UnionGenerator.Attributes;

[GenerateUnion]
public partial class TestUnion
{
    public static TestUnion CaseA(string value) => new CaseACase(value);
    public static TestUnion CaseB(int value) => new CaseBCase(value);
}
```

Build your project. If IntelliSense shows `CaseACase` and `CaseBCase`, you're all set! ✅

### Troubleshooting

#### Generator Not Running

If generated code doesn't appear:

1. **Clean and rebuild:**
   ```bash
   dotnet clean
   dotnet build
   ```

2. **Check build output** for generator errors

3. **Restart your IDE** (Visual Studio/Rider)

4. **Verify package is restored:**
   ```bash
   dotnet restore
   ```

#### IntelliSense Not Working

1. **Reload project** in your IDE
2. **Close and reopen** the solution
3. **Delete** `obj/` and `bin/` folders, then rebuild

#### Version Conflicts

If you see version conflicts:

```bash
# Update all UnionGenerator packages to the same version
dotnet add package UnionGenerator --version 1.0.0
dotnet add package UnionGenerator.AspNetCore --version 1.0.0
```

## IDE Support

### Visual Studio

- ✅ Full support in VS 2022 17.0+
- ✅ IntelliSense for generated code
- ✅ Go to definition works
- ✅ Debugger support

### JetBrains Rider

- ✅ Full support in Rider 2021.3+
- ✅ IntelliSense for generated code
- ✅ Refactoring support
- ✅ Debugger support

### Visual Studio Code

- ✅ Works with C# extension
- ✅ Basic IntelliSense support
- ⚠️ Some features may require reload

## Next Steps

Installation complete! Now:

1. [Learn basic concepts](../getting-started/basic-concepts)
2. [Create your first union](../getting-started/your-first-union)
3. [Explore common patterns](../getting-started/common-patterns)

## Package Links

All packages are available on NuGet.org:

- [UnionGenerator](https://www.nuget.org/packages/UnionGenerator) ![NuGet](https://img.shields.io/nuget/v/UnionGenerator.svg)
- [UnionGenerator.AspNetCore](https://www.nuget.org/packages/UnionGenerator.AspNetCore) ![NuGet](https://img.shields.io/nuget/v/UnionGenerator.AspNetCore.svg)
- [UnionGenerator.EntityFrameworkCore](https://www.nuget.org/packages/UnionGenerator.EntityFrameworkCore) ![NuGet](https://img.shields.io/nuget/v/UnionGenerator.EntityFrameworkCore.svg)
- [UnionGenerator.FluentValidation](https://www.nuget.org/packages/UnionGenerator.FluentValidation) ![NuGet](https://img.shields.io/nuget/v/UnionGenerator.FluentValidation.svg)
- [UnionGenerator.OneOfCompat](https://www.nuget.org/packages/UnionGenerator.OneOfCompat) ![NuGet](https://img.shields.io/nuget/v/UnionGenerator.OneOfCompat.svg)
- [UnionGenerator.Analyzers](https://www.nuget.org/packages/UnionGenerator.Analyzers) ![NuGet](https://img.shields.io/nuget/v/UnionGenerator.Analyzers.svg)
- [UnionGenerator.Analyzers.CodeFixes](https://www.nuget.org/packages/UnionGenerator.Analyzers.CodeFixes) ![NuGet](https://img.shields.io/nuget/v/UnionGenerator.Analyzers.CodeFixes.svg)

## Need Help?

- 📖 [Documentation](/)
- 🐛 [Report an issue](https://github.com/selcukgural/UnionGenerator/issues)
- 💬 [Discussions](https://github.com/selcukgural/UnionGenerator/discussions)
