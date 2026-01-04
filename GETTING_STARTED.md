# UnionGenerator - Getting Started Guide

Welcome to UnionGenerator! This guide will help you choose the right package for your needs and get started quickly.

## 📦 What is UnionGenerator?

UnionGenerator is a comprehensive .NET library ecosystem for creating **discriminated unions** (tagged unions) in C# with:
- ✅ Compile-time code generation
- ✅ Zero runtime reflection overhead
- ✅ Full pattern matching support
- ✅ ASP.NET Core integration
- ✅ Entity Framework Core support
- ✅ OneOf library compatibility

---

## 🎯 Quick Decision Tree

**What do you want to do?**

### "I want to create discriminated unions in C#"
→ Install **`UnionGenerator`** (core package)

### "I'm building an ASP.NET Core API with error handling"
→ Install **`UnionGenerator.AspNetCore`** (+ UnionGenerator)

### "I need analyzers to catch union-related mistakes"
→ Install **`UnionGenerator.Analyzers`** (or add to existing project)

### "I'm using FluentValidation"
→ Install **`UnionGenerator.FluentValidation`** (+ UnionGenerator.AspNetCore)

### "I'm using Entity Framework Core"
→ Install **`UnionGenerator.EntityFrameworkCore`** (+ UnionGenerator)

### "I'm using the OneOf library and want to migrate"
→ Install **`UnionGenerator.OneOfCompat`** or **`UnionGenerator.OneOfExtensions`** or **`UnionGenerator.OneOfSourceGen`**

---

## 📚 Package Overview

| Package | Purpose | Requires | Performance | Best For |
|---------|---------|----------|-------------|----------|
| **UnionGenerator** | Core discriminated unions | None | Compile-time | Everyone |
| **UnionGenerator.AspNetCore** | HTTP response mapping | UnionGenerator | ~100 µs (reflection) | ASP.NET Core APIs |
| **UnionGenerator.Analyzers** | Compile-time diagnostics | None | Compile-time | Code quality (UG4010, UG4011) |
| **UnionGenerator.Analyzers.CodeFixes** | Auto-fix analyzer warnings | Analyzers | Compile-time | IDE lightbulb fixes |
| **UnionGenerator.AspNetCore.SourceGen** | Ultra-fast HTTP mapping (Phase 2) | AspNetCore | ~50 ns (generated) | High-perf APIs (future) |
| **UnionGenerator.FluentValidation** | Validation integration | AspNetCore | ~50 µs | ASP.NET Core + FluentValidation |
| **UnionGenerator.EntityFrameworkCore** | EF Core value converters | UnionGenerator | Depends on JSON size | Database storage |
| **UnionGenerator.OneOfCompat** | OneOf v2/v3 adapters (runtime) | UnionGenerator | ~15-65 µs (reflection) | Legacy OneOf code |
| **UnionGenerator.OneOfExtensions** | OneOf v3 fluent API | UnionGenerator | ~10-35 µs (cached) | Standard OneOf v3 |
| **UnionGenerator.OneOfSourceGen** | OneOf compile-time adapters | UnionGenerator | ~10-50 ns (generated) | High-perf OneOf |

---

## 🚀 Getting Started by Scenario

### Scenario 1: Simple Discriminated Union

**Goal**: Create Result<T, E> pattern for error handling

**Steps**:
```bash
dotnet add package UnionGenerator
```

```csharp
using UnionGenerator.Attributes;

[GenerateUnion]
public partial class Result<T, E>
{
    public static Result<T, E> Ok(T value) => new OkCase(value);
    public static Result<T, E> Error(E error) => new ErrorCase(error);
}

// Use it
var result = Result<int, string>.Ok(42);
result.Match(
    ok: v => Console.WriteLine($"Success: {v}"),
    error: e => Console.WriteLine($"Error: {e}")
);
```

**Done!** ✅

---

### Scenario 2: ASP.NET Core API with Error Handling

**Goal**: Create REST API with automatic HTTP status code mapping

**Steps**:
```bash
dotnet add package UnionGenerator.AspNetCore
```

```csharp
// Program.cs
builder.Services.AddUnionResultHandling();

// Models
[UnionStatusCode(404)]
public class NotFoundError { public string Message { get; set; } }

[UnionStatusCode(422)]
public class ValidationError { public Dictionary<string, string[]> Errors { get; set; } }

// Controllers
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult GetUser(int id)
    {
        return _service.GetUser(id).ToActionResult(); // Auto-maps status codes!
    }
}
```

**Done!** ✅

---

### Scenario 3: ASP.NET Core + FluentValidation

**Goal**: Automatic validation with ProblemDetails responses

**Steps**:
```bash
dotnet add package UnionGenerator.FluentValidation
```

```csharp
// Program.cs
builder.Services.AddUnionFluentValidation<CreateUserValidator>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<FluentValidationFilter>();
});

// Validators
public class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Age).GreaterThanOrEqualTo(18);
    }
}

// Controllers
[HttpPost]
public IActionResult CreateUser([FromBody] CreateUserDto dto)
{
    // Validation happens automatically via filter
    var user = _service.CreateUser(dto);
    return Result<User, ProblemDetailsError>.Ok(user).ToActionResult();
}
```

**Done!** ✅

---

### Scenario 4: ASP.NET Core + Entity Framework Core

**Goal**: Store union types (Result) in database as JSON

**Steps**:
```bash
dotnet add package UnionGenerator.EntityFrameworkCore
```

```csharp
public class Order
{
    public int Id { get; set; }
    public Result<OrderData, ErrorInfo> ProcessingResult { get; set; } = null!;
}

public class AppDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .HasResultConversion<Order, Result<OrderData, ErrorInfo>, OrderData, ErrorInfo>(
                o => o.ProcessingResult
            );
    }
}

// Usage
var order = new Order
{
    ProcessingResult = Result<OrderData, ErrorInfo>.Ok(
        new OrderData("ORD-001", 99.99m)
    )
};

await dbContext.Orders.AddAsync(order);
await dbContext.SaveChangesAsync();
```

**Done!** ✅

---

### Scenario 5: Code Quality - Catch Common Mistakes

**Goal**: Enforce union usage patterns with compile-time checks

**Steps**:
```bash
dotnet add package UnionGenerator.Analyzers
```

**That's it!** Build your project and warnings appear automatically:
- UG4010: Union not mapped to IActionResult (info)
- UG4011: Error case without status code (info)
- UG4012: Convention override recommended (hidden by default)

**Bonus**: Install `UnionGenerator.Analyzers.CodeFixes` for one-click fixes!

```bash
dotnet add package UnionGenerator.Analyzers.CodeFixes
```

Press Ctrl+. in Visual Studio to apply automatic fixes. 🚀

---

### Scenario 6: Migrate from OneOf Library

**Goal**: Convert OneOf<T0, T1> to UnionGenerator unions

**Three Options**:

#### Option A: Runtime Helpers (No Dependencies)
```bash
dotnet add package UnionGenerator.OneOfCompat
```
Best for: Minimal dependency projects

#### Option B: Fluent Extension Methods (With JSON)
```bash
dotnet add package UnionGenerator.OneOfExtensions
```
Best for: Standard OneOf v3 projects with JSON support

#### Option C: Compile-Time Adapters (Zero Reflection)
```bash
dotnet add package UnionGenerator.OneOfSourceGen
```
Best for: High-performance code paths

**Usage**:
```csharp
var oneOf = OneOf<User, Error>.FromT0(user);

// OneOfCompat: Static helper
var result = OneOfCompat.FromT0<Result<User, Error>, User, Error>(user);

// OneOfExtensions: Fluent extension
var result = oneOf.ToGeneratedResult<Result<User, Error>, User, Error>();

// OneOfSourceGen: Generated adapter (fastest)
var result = oneOf.FromOneOf<Result<User, Error>, User, Error>();
```

---

## 📊 Common Setup Combinations

### 🎯 Minimal API Service
```bash
dotnet add package UnionGenerator
dotnet add package UnionGenerator.AspNetCore
```

### 🎯 Full-Featured REST API
```bash
dotnet add package UnionGenerator
dotnet add package UnionGenerator.AspNetCore
dotnet add package UnionGenerator.Analyzers
dotnet add package UnionGenerator.Analyzers.CodeFixes
```

### 🎯 REST API + Validation
```bash
dotnet add package UnionGenerator
dotnet add package UnionGenerator.AspNetCore
dotnet add package UnionGenerator.FluentValidation
```

### 🎯 REST API + Database
```bash
dotnet add package UnionGenerator
dotnet add package UnionGenerator.AspNetCore
dotnet add package UnionGenerator.EntityFrameworkCore
```

### 🎯 REST API + Everything
```bash
dotnet add package UnionGenerator
dotnet add package UnionGenerator.AspNetCore
dotnet add package UnionGenerator.FluentValidation
dotnet add package UnionGenerator.EntityFrameworkCore
dotnet add package UnionGenerator.Analyzers
dotnet add package UnionGenerator.Analyzers.CodeFixes
```

---

## 🧪 Verify Installation

After installing packages, verify code generation works:

```csharp
[GenerateUnion]
public partial class TestUnion<T>
{
    public static TestUnion<T> Success(T value) => new SuccessCase(value);
    public static TestUnion<T> Failure(string error) => new FailureCase(error);
}

// Build project
// Check IntelliSense - you should see:
// - .IsSuccess property
// - .Data property
// - .Error property
// - .Match() method
// - Pattern matching support in switch expressions
```

If IntelliSense doesn't show generated members:
1. Rebuild project: `dotnet clean && dotnet build`
2. Reload IDE window
3. Check `obj/Debug/net*/generated/` for `.g.cs` files

---

## 📚 Next Steps

### Core Learning Path

1. **Read**: [UnionGenerator README](./src/UnionGenerator/README.md) - Understand discriminated unions
2. **Code**: Write your first union type with pattern matching
3. **Integrate**: Choose scenario above and follow steps
4. **Deploy**: Use in production with confidence

### For ASP.NET Core Developers

1. **Start**: [UnionGenerator.AspNetCore README](./src/UnionGenerator.AspNetCore/README.md)
2. **Setup**: Add to Program.cs
3. **Define**: Create error types with `[UnionStatusCode]`
4. **Use**: Return Result<T, E> from services
5. **Map**: Call `.ToActionResult()` in controllers

### For Entity Framework Users

1. **Start**: [UnionGenerator.EntityFrameworkCore README](./src/UnionGenerator.EntityFrameworkCore/README.md)
2. **Configure**: Add `.HasResultConversion()` in OnModelCreating
3. **Store**: Save union types as JSON columns
4. **Query**: Retrieve and deserialize

### For Quality-Conscious Teams

1. **Install**: UnionGenerator.Analyzers
2. **Review**: Build output for UG4010, UG4011 warnings
3. **Fix**: Use code fixes (UnionGenerator.Analyzers.CodeFixes)
4. **Enforce**: Add to CI/CD pipeline

---

## 🔗 Project Structure

```
UnionGenerator/
├── src/
│   ├── UnionGenerator/                    # ✅ Start here: Core package
│   ├── UnionGenerator.AspNetCore/         # ASP.NET Core integration
│   ├── UnionGenerator.AspNetCore.SourceGen/  # Future: Phase 2 (high-perf)
│   ├── UnionGenerator.Analyzers/          # Code quality checks
│   ├── UnionGenerator.Analyzers.CodeFixes/   # Auto-fixes
│   ├── UnionGenerator.EntityFrameworkCore/   # EF Core integration
│   ├── UnionGenerator.FluentValidation/   # FluentValidation integration
│   ├── UnionGenerator.OneOfCompat/        # OneOf compatibility
│   ├── UnionGenerator.OneOfExtensions/    # OneOf fluent API
│   └── UnionGenerator.OneOfSourceGen/     # OneOf compile-time adapters
├── examples/
│   ├── aspnetcore-example/                # Full ASP.NET Core example
│   ├── json-example/                      # JSON serialization example
│   └── oneof-example/                     # OneOf migration example
└── tests/                                  # Comprehensive test suite
```

---

## 🚨 Common Questions

### Q: Do I need all packages?
**A**: No! Install only what you need:
- `UnionGenerator` is required for everyone
- Others are optional based on scenario
- Start minimal, add as needed

### Q: Will union types slow down my application?
**A**: No! Generated code is compile-time only. Zero runtime reflection overhead (unless using reflection-based converters like OneOfCompat).

### Q: Can I use unions in production?
**A**: Absolutely! UnionGenerator is production-ready:
- Used in real ASP.NET Core applications
- Comprehensive test suite
- Semantic versioning
- Stable public API

### Q: How does this compare to OneOf library?
**A**: 
- OneOf: Existing union library, still supported
- UnionGenerator: More features (source generation, ASP.NET Core integration, EF Core support)
- You can migrate gradually using OneOfCompat/OneOfExtensions/OneOfSourceGen

### Q: Is there a performance cost?
**A**: 
- Union creation: ~5-15 ns (zero overhead)
- Pattern matching: ~10-20 ns (zero overhead)
- OneOf converters: ~10-65 µs (reflection-based, one-time cost)
- ASP.NET response mapping: ~100 µs (reflection-based today, <1 µs when Phase 2 completes)

### Q: Can I use with Entity Framework Core?
**A**: Yes! Install `UnionGenerator.EntityFrameworkCore` for automatic JSON storage.

### Q: Does it work with minimal APIs?
**A**: Yes! Works seamlessly with `.ToActionResult()` extension on union types.

---

## 📞 Getting Help

- **GitHub Issues**: Report bugs and request features
- **Discussions**: Ask questions and share ideas
- **README files**: Each package has comprehensive documentation
- **Examples folder**: Reference implementations

---

---

## 📚 Working Examples

UnionGenerator includes comprehensive, production-ready examples demonstrating real-world usage:

### Available Examples

1. **[ASP.NET Core Integration](./examples/aspnetcore-example/)** - REST API with ProblemDetails
   - Build complete REST APIs
   - Automatic error-to-ProblemDetails conversion
   - Controller and Minimal API endpoints
   - Request validation integration

2. **[Entity Framework Core](./examples/entityframework-example/)** - Store unions in databases
   - Save discriminated unions as JSON columns
   - CRUD operations with pattern matching
   - Query and filter results
   - Real-world persistence patterns

3. **[FluentValidation](./examples/fluentvalidation-example/)** - Input validation
   - Declarative validation rules
   - Auto-conversion to ProblemDetailsError
   - Batch validation patterns
   - Service layer integration

4. **[JSON Serialization](./examples/json-example/)** - Exchange data over HTTP
   - Serialize/deserialize unions
   - System.Text.Json integration
   - Real-world API response patterns
   - Complex nested types

5. **[OneOf Compatibility](./examples/oneof-example/)** - Migrate from OneOf library
   - Three migration approaches
   - Gradual migration patterns
   - Performance characteristics
   - Coexistence strategies

### Quick Start: Run an Example

```bash
cd examples/<example-name>
dotnet run
```

**Recommended Learning Order:**
1. `json-example` (simplest)
2. `fluentvalidation-example` (validation)
3. `entityframework-example` (persistence)
4. `aspnetcore-example` (complete API)
5. `oneof-example` (interoperability)

For detailed guides, see [Examples README](./examples/README.md).

---

## 📄 License

MIT License - Free to use in any project (commercial or open source)

---

## 🎉 You're Ready!

Pick a scenario above, follow the steps, and start building better error handling in .NET! 🚀

**Happy coding!** 💪

