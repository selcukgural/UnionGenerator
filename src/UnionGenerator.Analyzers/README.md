# UnionGenerator.Analyzers

Catch union-related bugs at compile time. Detects common mistakes like forgetting HTTP status codes, wrong error types, and unsafe conversions—before they reach production.

## ❓ Why These Analyzers?

### The Problem

Union types are powerful but easy to misuse:

```csharp
// ❌ Forgot to convert to IActionResult → returns wrong format
public Result<User, NotFoundError> GetUser(int id)
{
    return _service.GetUser(id); // Oops! Returns union, not HTTP response
}

// ❌ Error type missing status code → 500 fallback
public class DatabaseError 
{
    public string Message { get; set; }
    // No [UnionStatusCode(code)] attribute!
}

// ❌ Unsafe conversion → runtime exception possible
var result = (Result<User, Error>)someObject; // Cast can fail!

// ❌ Unreachable error cases in pattern match
var response = result switch
{
    { IsSuccess: true, Data: var user } => Ok(user),
    { IsSuccess: false, Error: NotFoundError _ } => NotFound(),
    // Missing case: ValidationError!
};
```

These bugs are easy to introduce and hard to catch manually.

### The Solution

```csharp
// ✅ Analyzer warns immediately (UG4010)
// 🔴 Warn: Convert Result<T,E> to IActionResult using .ToActionResult()
public IActionResult GetUser(int id)  // ← Fixed!
{
    return _service.GetUser(id).ToActionResult();
}

// ✅ Analyzer warns immediately (UG4011)
// 🔴 Warn: Error type DatabaseError lacks [UnionStatusCode] attribute
[UnionStatusCode(500)]  // ← Fixed!
public class DatabaseError { /* ... */ }

// ✅ Analyzer catches unsafe casts (UG4020)
// ✅ Safe helper instead
var result = Result<User, Error>.Ok(user);

// ✅ Analyzer detects exhaustiveness violations (UG4030)
// 🔴 Warn: Missing pattern case for 'ValidationError'
var response = result switch
{
    { IsSuccess: true, Data: var user } => Ok(user),
    { IsSuccess: false, Error: NotFoundError _ } => NotFound(),
    { IsSuccess: false, Error: ValidationError _ } => UnprocessableEntity(),  // ← Added!
};
```

---

## 🚀 Quick Start

Install and it works automatically - no configuration needed!

```bash
dotnet add package UnionGenerator.Analyzers
```

Build your project and warnings/errors appear automatically in your IDE.

---

## 📋 Diagnostics

### UG4010: Union Not Mapped to IActionResult

**Severity**: Info | **Enabled**: Yes

Warns when a union type is returned directly from a controller action instead of being converted to `IActionResult`.

**Example**:

```csharp
// ❌ UG4010: Union result not mapped to IActionResult
public Result<User, NotFoundError> GetUser(int id)
{
    return _service.GetUser(id);
}
```

**Why it matters**: Returning union types directly bypasses HTTP response handling.

**Fix**: Convert to IActionResult:

```csharp
// ✅ Correct
public IActionResult GetUser(int id)
{
    return _service.GetUser(id).ToActionResult();
}
```

---

### UG4011: Error Case Lacks Status Code

**Severity**: Info | **Enabled**: Yes

Warns when an error type used in a union doesn't have an explicit status code declaration.

**Example**:

```csharp
// ❌ UG4011: Error case lacks explicit status code
public class CustomError { }

// Usage
public Result<Data, CustomError> GetData() => ...
```

**Why it matters**: Without a status code, the error returns 500 (internal server error) by default, which may not be appropriate.

**Fix**: Add status code:

```csharp
// ✅ Option 1: Explicit attribute
[UnionStatusCode(400)]
public class CustomError { }

// ✅ Option 2: Named convention
public class BadRequestError { } // Convention infers 400

// ✅ Option 3: Property
public class CustomError 
{ 
    public int StatusCode => 422; 
}
```

---

### UG4012: Convention Override Recommended

**Severity**: Hidden | **Enabled**: No (enable if you prefer explicit)

Suggests adding explicit `[UnionStatusCode]` attribute even when convention-based inference works.

**Example**:

```csharp
// ℹ️ UG4012: Convention can infer 404, but explicit is clearer
public class NotFoundError { }

// vs

// ✅ Preferred
[UnionStatusCode(404)]
public class NotFoundError { }
```

**Why it matters**: Explicit declarations make intent clear and are immune to naming changes.

**When to enable**: If your team prefers explicit over implicit.

---

## 🔧 Configuration

### In .editorconfig

```ini
# Suppress specific diagnostics
[**/*.cs]
dotnet_diagnostic.UG4010.severity = silent
dotnet_diagnostic.UG4011.severity = error
dotnet_diagnostic.UG4012.severity = suggestion
```

### In .csproj

```xml
<PropertyGroup>
    <EnableAnalyzers>true</EnableAnalyzers>
</PropertyGroup>
```

### IDE Settings

In Visual Studio:
1. Tools → Options → Text Editor → C# → Code Analysis
2. Find "UnionGenerator.Analyzers"
3. Configure severity per diagnostic

---

## 📚 Common Patterns

### Pattern 1: REST API Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    // ✅ UG4010 fixed: Returns IActionResult
    [HttpGet("{id}")]
    public IActionResult GetUser(int id)
    {
        return _service.GetUser(id).ToActionResult();
    }

    // ✅ UG4011 fixed: Error has status code
    [HttpPost]
    public IActionResult CreateUser(CreateUserDto dto)
    {
        return _service.CreateUser(dto).ToActionResult();
    }
}

// ✅ Error types properly declared
[UnionStatusCode(404)]
public class NotFoundError { public string Message { get; set; } }

[UnionStatusCode(422)]
public class ValidationError { public Dictionary<string, string[]> Errors { get; set; } }
```

### Pattern 2: Multiple Error Types

```csharp
[HttpPost("{id}/role")]
public IActionResult AssignRole(int id, string role)
{
    // ✅ Handles multiple error types
    var result = _service.AssignRole(id, role);
    return result.ToActionResult();
}

// Each error type declares its own status code
[UnionStatusCode(404)]
public class UserNotFoundError { }

[UnionStatusCode(404)]
public class RoleNotFoundError { }

[UnionStatusCode(409)]
public class RoleAlreadyAssignedError { }

[UnionStatusCode(400)]
public class InvalidRoleError { }
```

### Pattern 3: Named Convention

```csharp
// ✅ Let convention infer status codes
public class NotFoundError { }      // → 404 (inferred)
public class BadRequestError { }    // → 400 (inferred)
public class ValidationError { }    // → 400 (inferred)
public class ConflictError { }      // → 409 (inferred)

// Both patterns work equally well:
// - Explicit [UnionStatusCode]: Clearer intent
// - Naming convention: Less boilerplate
// Choose based on team preference
```

---

## 🎯 Workflow

### 1. Write Code

```csharp
public Result<User, CustomError> GetUser(int id)
{
    return _service.GetUser(id);
}

public class CustomError { }
```

### 2. Build Project

```bash
dotnet build
```

### 3. Warnings Appear

```
warning UG4010: Method 'GetUser' returns a union type directly. 
                Consider mapping it to IActionResult using ToActionResult()

warning UG4011: Error type 'CustomError' used in union does not have an explicit status code. 
                Consider adding [UnionStatusCode] attribute or implementing StatusCode property.
```

### 4. Fix Issues

```csharp
// Fix UG4010
public IActionResult GetUser(int id)
{
    return _service.GetUser(id).ToActionResult();
}

// Fix UG4011
[UnionStatusCode(400)]
public class CustomError { }
```

### 5. Build Again

```bash
dotnet build
# ✅ Warnings gone, code is correct
```

---

## 🔍 Analyzer Rules Reference

| ID | Rule | Severity | Category | Auto-Fix |
|---|---|---|---|---|
| **UG4010** | UnionNotMappedToActionResult | Info | Usage | No |
| **UG4011** | ErrorCaseWithoutStatusCode | Info | Usage | No |
| **UG4012** | ConventionOverrideRecommended | Hidden | Style | No |

---

## 🚀 Best Practices

### ✅ DO

- Always return `IActionResult` from controller actions
- Declare status codes for all error types
- Use `[UnionStatusCode]` for clarity in public APIs
- Let convention infer for internal/standard errors
- Enable diagnostics in CI/CD pipeline

### ❌ DON'T

- Return union types directly from controllers
- Create error types without status codes
- Ignore analyzer warnings in build
- Suppress UG4010/UG4011 without good reason
- Mix union types with different status code approaches

---

## 🔧 Troubleshooting

### Analyzer Not Working

**Problem**: No warnings appear

**Solution**:
1. Ensure `UnionGenerator.Analyzers` NuGet package is installed
2. Rebuild project: `dotnet clean && dotnet build`
3. Check project file for `<EnableAnalyzers>false</EnableAnalyzers>`
4. Verify IDE is showing analysis warnings (VS: Tools → Options → Text Editor → C#)

### Too Many Warnings

**Problem**: Too many UG4010/UG4011 warnings

**Solution**:
1. That's expected during migration
2. Fix warnings incrementally (one file at a time)
3. Or suppress if temporary: `#pragma warning disable UG4010`
4. Or configure in .editorconfig:
   ```ini
   dotnet_diagnostic.UG4010.severity = silent
   ```

### Warning Not Appearing

**Problem**: Code has issue but no warning

**Solution**:
1. Check if diagnostic is enabled in .editorconfig
2. Reload solution in IDE
3. Do full rebuild: `dotnet clean && dotnet build`
4. Check rule severity (Hidden diagnostics might be invisible)

---

## 📊 Impact

### Before Analyzers

```
❌ No compile-time checks
❌ Union types leaked into HTTP layer
❌ Inconsistent error handling
❌ Status codes defaulted to 500
❌ Issues found only in testing
```

### After Analyzers

```
✅ Compile-time detection
✅ Union types properly mapped
✅ Consistent error handling
✅ Correct status codes automatic
✅ Issues caught before testing
```

---

## 📚 Related Packages

- **UnionGenerator.AspNetCore**: Convention-based status code mapping (complements this analyzer)
- **UnionGenerator**: Core union type generation
- **UnionGenerator.OneOfCompat**: OneOf library compatibility

---

## 💡 Tips

### Tip 1: Start with UG4010/UG4011

These are the most valuable. They catch real mistakes that affect HTTP responses.

### Tip 2: Use [UnionStatusCode] for Public APIs

Makes intent crystal clear for API consumers and maintainers.

### Tip 3: Let Convention Infer for Internal Code

Reduces boilerplate. Naming is self-documenting if you follow patterns.

### Tip 4: Enable in CI/CD

Add to build pipeline to enforce standards:

```yaml
# GitHub Actions
- name: Build
  run: dotnet build
  env:
    TreatWarningsAsErrors: true
    # This makes all analyzer warnings fail the build
```

---

## 📄 License

MIT License - See LICENSE file for details

---

## ✨ Summary

| Feature | Benefit |
|---------|---------|
| **UG4010** | Catch unmapped union returns |
| **UG4011** | Catch missing status codes |
| **UG4012** | Encourage explicit code |
| **Zero Config** | Works out of the box |
| **IDE Integration** | Real-time warnings |

**Start now**: Install package and build. Warnings guide you to correct code! 🚀

