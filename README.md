# UnionGenerator

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0+-purple.svg)](https://dotnet.microsoft.com/download)
[![C#](https://img.shields.io/badge/C%23-12.0+-orange.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)

**Compile-time discriminated unions for C# with zero runtime overhead.**

Eliminate boilerplate, enable type-safe error handling, and leverage exhaustive pattern matching—all without exceptions or nullable chains.

---

## 🚀 Quick Start

### 1. Installation

```bash
dotnet add package UnionGenerator
```

### 2. Define Your Union

```csharp
using UnionGenerator.Attributes;

[GenerateUnion]
public partial class Result<T, E>
{
    public static Result<T, E> Ok(T value) => new OkCase(value);
    public static Result<T, E> Error(E error) => new ErrorCase(error);
}
```

### 3. Use It

```csharp
var result = Result<int, string>.Ok(42);

// Pattern matching
var message = result switch
{
    { IsSuccess: true, Data: var data } => $"Success: {data}",
    { IsSuccess: false, Error: var error } => $"Error: {error}",
};

// Or use Match method
string message = result.Match(
    ok: data => $"Success: {data}",
    error: err => $"Error: {err}"
);
```

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| 🔧 **Source Generation** | Zero runtime reflection, pure compile-time generated code |
| 🎯 **Type Safety** | Exhaustive pattern matching with compiler-enforced case coverage |
| 🚀 **Zero Overhead** | No allocations, no boxing, minimal memory footprint |
| 🔗 **ASP.NET Core Integration** | Automatic ProblemDetails (RFC 7807) conversion |
| 💾 **EF Core Support** | JSON value converters for database persistence |
| ✅ **FluentValidation Integration** | Seamless validation error mapping |
| 🔄 **OneOf Compatibility** | Drop-in replacement for OneOf library |
| 📊 **Roslyn Analyzers** | Compile-time diagnostics and code fixes |

---

## 📦 Packages

### Core Package

| Package | Description | NuGet |
|---------|-------------|-------|
| **UnionGenerator** | Core source generator and attributes | [![NuGet](https://img.shields.io/nuget/v/UnionGenerator.svg)](https://www.nuget.org/packages/UnionGenerator) |

### Integration Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| **UnionGenerator.AspNetCore** | ASP.NET Core ProblemDetails integration | [![NuGet](https://img.shields.io/nuget/v/UnionGenerator.AspNetCore.svg)](https://www.nuget.org/packages/UnionGenerator.AspNetCore) |
| **UnionGenerator.EntityFrameworkCore** | EF Core value converters for unions | [![NuGet](https://img.shields.io/nuget/v/UnionGenerator.EntityFrameworkCore.svg)](https://www.nuget.org/packages/UnionGenerator.EntityFrameworkCore) |
| **UnionGenerator.FluentValidation** | FluentValidation error mapping | [![NuGet](https://img.shields.io/nuget/v/UnionGenerator.FluentValidation.svg)](https://www.nuget.org/packages/UnionGenerator.FluentValidation) |
| **UnionGenerator.OneOfCompat** | OneOf drop-in replacement | [![NuGet](https://img.shields.io/nuget/v/UnionGenerator.OneOfCompat.svg)](https://www.nuget.org/packages/UnionGenerator.OneOfCompat) |

### Analyzer Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| **UnionGenerator.Analyzers** | Roslyn analyzers for union usage | [![NuGet](https://img.shields.io/nuget/v/UnionGenerator.Analyzers.svg)](https://www.nuget.org/packages/UnionGenerator.Analyzers) |
| **UnionGenerator.Analyzers.CodeFixes** | Code fix providers | [![NuGet](https://img.shields.io/nuget/v/UnionGenerator.Analyzers.CodeFixes.svg)](https://www.nuget.org/packages/UnionGenerator.Analyzers.CodeFixes) |

---

## 📚 Documentation

### Core Documentation

- **[Core Library Documentation](src/UnionGenerator/README.md)** - Complete guide to union generation, patterns, and best practices
- **[Examples Overview](examples/README.md)** - Quick reference for all examples

### Integration Guides

| Integration | Documentation | Example Project |
|-------------|---------------|-----------------|
| ASP.NET Core | [View Docs](src/UnionGenerator.AspNetCore/) | [aspnetcore-example](examples/aspnetcore-example/) |
| Entity Framework Core | [View Docs](src/UnionGenerator.EntityFrameworkCore/) | [entityframework-example](examples/entityframework-example/) |
| FluentValidation | [View Docs](src/UnionGenerator.FluentValidation/) | [fluentvalidation-example](examples/fluentvalidation-example/) |
| JSON Serialization | - | [json-example](examples/json-example/) |
| OneOf Migration | [View Docs](src/UnionGenerator.OneOfCompat/) | [oneof-example](examples/oneof-example/) |

---

## 🎯 Example Projects

All examples are production-ready and demonstrate real-world patterns:

### [ASP.NET Core Example](examples/aspnetcore-example/)
Complete REST API with automatic ProblemDetails conversion, Swagger integration, and controller/minimal API examples.

```bash
cd examples/aspnetcore-example
dotnet run
# Navigate to https://localhost:5001/swagger
```

### [Entity Framework Core Example](examples/entityframework-example/)
Database persistence with JSON value converters, CRUD operations, and query patterns.

```bash
cd examples/entityframework-example
dotnet run
```

### [FluentValidation Example](examples/fluentvalidation-example/)
Declarative validation with automatic error conversion to ProblemDetails format.

```bash
cd examples/fluentvalidation-example
dotnet run
```

### [JSON Serialization Example](examples/json-example/)
System.Text.Json integration with roundtrip serialization of union types.

```bash
cd examples/json-example
dotnet run
```

### [OneOf Compatibility Example](examples/oneof-example/)
Drop-in replacement demonstration for migrating from OneOf library.

```bash
cd examples/oneof-example
dotnet run
```

---

## 🏗️ Project Structure

```
UnionGenerator/
├── src/
│   ├── UnionGenerator/                    # Core source generator
│   ├── UnionGenerator.Analyzers/          # Roslyn analyzers
│   ├── UnionGenerator.Analyzers.CodeFixes/# Code fix providers
│   ├── UnionGenerator.AspNetCore/         # ASP.NET Core integration
│   ├── UnionGenerator.EntityFrameworkCore/# EF Core integration
│   ├── UnionGenerator.FluentValidation/   # FluentValidation integration
│   ├── UnionGenerator.OneOfCompat/        # OneOf compatibility
│   └── UnionGenerator.OneOfExtensions/    # OneOf extensions
├── examples/                              # Production-ready examples
│   ├── aspnetcore-example/
│   ├── entityframework-example/
│   ├── fluentvalidation-example/
│   ├── json-example/
│   └── oneof-example/
├── tests/                                 # Comprehensive test suite
│   ├── UnionGenerator.Tests/
│   ├── UnionGenerator.AspNetCore.Tests/
│   ├── UnionGenerator.EntityFrameworkCore.Tests/
│   └── UnionGenerator.FluentValidation.Tests/
└── docs/                                  # Additional documentation
```

---

## 💡 Common Use Cases

### ✅ Error Handling Without Exceptions

```csharp
[GenerateUnion]
public partial class Result<T, E>
{
    public static Result<T, E> Ok(T value) => new OkCase(value);
    public static Result<T, E> Error(E error) => new ErrorCase(error);
}

public Result<User, string> GetUser(int id)
{
    if (id <= 0)
    {
        return Result<User, string>.Error("Invalid ID");
    }
    
    var user = _userService.FindById(id);
    return user != null 
        ? Result<User, string>.Ok(user)
        : Result<User, string>.Error("User not found");
}
```

### ✅ API Response Types

```csharp
[GenerateUnion]
public partial class ApiResponse<T>
{
    public static ApiResponse<T> Success(T data) => new SuccessCase(data);
    public static ApiResponse<T> NotFound(string message) => new NotFoundCase(message);
    public static ApiResponse<T> Unauthorized() => new UnauthorizedCase();
    public static ApiResponse<T> ValidationError(Dictionary<string, string[]> errors) 
        => new ValidationErrorCase(errors);
}
```

### ✅ State Machines

```csharp
[GenerateUnion]
public partial class OrderState
{
    public static OrderState Pending() => new PendingCase();
    public static OrderState Processing(string workerId) => new ProcessingCase(workerId);
    public static OrderState Completed(DateTime completedAt) => new CompletedCase(completedAt);
    public static OrderState Cancelled(string reason) => new CancelledCase(reason);
}
```

### ✅ Optional Values (Maybe Pattern)

```csharp
[GenerateUnion]
public partial class Option<T>
{
    public static Option<T> Some(T value) => new SomeCase(value);
    public static Option<T> None() => new NoneCase();
}
```

---

## 🔬 Why UnionGenerator?

### The Problem

Traditional C# approaches for handling multiple return types are problematic:

| Approach | Issues |
|----------|--------|
| **Nullable types** | Lose type information, awkward null-checking chains |
| **Exceptions** | Performance overhead, lose error context, not for control flow |
| **Out parameters** | Unidiomatic, not composable |
| **Custom base classes** | Boilerplate-heavy, no exhaustiveness checking |

### The Solution

Discriminated unions provide:

- ✅ **Type Safety** - Compiler-enforced exhaustive matching
- ✅ **Performance** - Zero runtime overhead, no exceptions
- ✅ **Clarity** - Intent is explicit, errors are values
- ✅ **Composability** - Natural functional patterns
- ✅ **Maintainability** - Less boilerplate, more focus on logic

---

## 🛠️ Building the Project

### Prerequisites

- .NET 8.0 SDK or later
- JetBrains Rider (recommended) or Visual Studio 2022+

### Build Commands

```bash
# Clone the repository
git clone https://github.com/yourusername/UnionGenerator.git
cd UnionGenerator

# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Run tests
dotnet test

# Pack NuGet packages
dotnet pack -c Release -o nupkgs
```

### Running Examples

Each example project can be run independently:

```bash
# ASP.NET Core example
cd examples/aspnetcore-example
dotnet run

# Entity Framework Core example
cd examples/entityframework-example
dotnet run

# All examples follow the same pattern
```

---

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/UnionGenerator.Tests/

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## 📤 Publishing to NuGet

This project uses GitHub Actions for automated NuGet package publishing.

### Quick Publish

```bash
# Create and push a version tag
git tag -a v0.2.0 -m "Release version 0.2.0"
git push origin v0.2.0

# GitHub Actions will automatically:
# ✅ Build and test
# ✅ Pack NuGet packages
# ✅ Publish to NuGet.org
# ✅ Create GitHub Release
```

### Setup Required

1. **NuGet API Key**: Add `NUGET_API_KEY` secret to GitHub repository settings
2. **Full Instructions**: See [Publishing Guide](.github/PUBLISHING.md)

### Publishing Methods

- **Automatic** (Recommended): Push a version tag (e.g., `v0.1.0`)
- **Manual**: Use GitHub Actions workflow dispatch UI

---

## 🤝 Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines

- Follow the existing code style (Rider/ReSharper conventions)
- Add tests for new features
- Update documentation as needed
- Keep commits focused and atomic

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- Inspired by F#'s discriminated unions and Rust's enums
- Built with Roslyn source generators
- Community feedback and contributions

---

## 📞 Support & Community

- **Issues**: [GitHub Issues](https://github.com/yourusername/UnionGenerator/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/UnionGenerator/discussions)
- **Documentation**: [Core Docs](src/UnionGenerator/README.md) | [Examples](examples/README.md)

---

**Made with ❤️ for the C# community**

