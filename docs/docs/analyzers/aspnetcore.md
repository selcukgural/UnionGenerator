---
sidebar_position: 5
---

# ASP.NET Core Analyzers

Specialized analyzers for ASP.NET Core integration that detect common misuse patterns when using union types in controllers and minimal APIs.

## 🎯 Overview

These analyzers ensure union types are properly handled in ASP.NET Core contexts, preventing common mistakes like returning raw union types or missing status code mappings.

| Diagnostic ID | Name | Severity |
|--------------|------|----------|
| **UG4010** | Union not mapped to IActionResult | Info |
| **UG4011** | Error case without status code | Info |
| **UG4012** | Convention override recommended | Hidden |

## UG4010: Union Not Mapped to IActionResult

### What It Detects

Controller methods that return union types directly instead of mapping them to `IActionResult`.

### Example Problem

```csharp
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    // ⚠️ UG4010: Returning union type directly
    public Result<User, UserError> GetUser(int id)
    {
        var user = _repository.GetById(id);
        if (user == null)
            return Result<User, UserError>.Error(UserError.NotFound);
            
        return Result<User, UserError>.Ok(user);
    }
}
```

### Diagnostic Message

> Method 'GetUser' returns a union type directly. Consider mapping it to IActionResult using ToActionResult() or equivalent.

### Why This Is a Problem

Returning raw union types:
- ❌ Doesn't set HTTP status codes
- ❌ Doesn't serialize correctly for clients
- ❌ Bypasses content negotiation
- ❌ Missing OpenAPI/Swagger metadata

### How to Fix

**Option 1: Use ToActionResult() extension**

```csharp
using UnionGenerator.AspNetCore;

public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    // ✅ Maps to proper IActionResult
    public IActionResult GetUser(int id)
    {
        var result = _repository.GetById(id);
        return result.ToActionResult(); // Automatic status code mapping
    }
}
```

**Option 2: Manual Match() mapping**

```csharp
[HttpGet("{id}")]
public IActionResult GetUser(int id)
{
    var result = _repository.GetById(id);
    
    // ✅ Explicit mapping
    return result.Match<IActionResult>(
        ok: user => Ok(user),
        error: err => err switch
        {
            UserError.NotFound => NotFound(),
            UserError.Unauthorized => Unauthorized(),
            _ => BadRequest()
        }
    );
}
```

**Option 3: Use ActionResult&lt;T&gt; wrapper**

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<User>> GetUser(int id)
{
    var result = await _repository.GetByIdAsync(id);
    
    // ✅ Maps union to ActionResult<T>
    return result.Match<ActionResult<User>>(
        ok: user => user,
        error: err => err switch
        {
            UserError.NotFound => NotFound(),
            _ => BadRequest()
        }
    );
}
```

### When to Suppress

Suppress only if:
- Using custom middleware that handles union serialization
- Building internal APIs where raw unions are intentional
- Using minimal APIs with custom result converters

```csharp
#pragma warning disable UG4010
[HttpGet("internal/{id}")]
public Result<User, UserError> GetUserInternal(int id)
{
    // Internal endpoint - raw union is intentional
    return _repository.GetById(id);
}
#pragma warning restore UG4010
```

## UG4011: Error Case Without Status Code

### What It Detects

Union error types that don't declare explicit HTTP status codes.

### Example Problem

```csharp
// ⚠️ UG4011: No status code information
public record UserError(string Message);

[GenerateUnion]
public partial record UserResult
{
    public static partial UserResult Success(User user);
    public static partial UserResult Error(UserError error); // Missing status code!
}
```

### Diagnostic Message

> Error type 'UserError' used in union does not have an explicit status code. Consider adding [UnionStatusCode] attribute or implementing StatusCode property.

### Why Status Codes Matter

Without explicit status codes:
- ❌ `ToActionResult()` uses default 500 for all errors
- ❌ No way to distinguish 404 vs. 400 vs. 401
- ❌ API clients can't handle errors appropriately
- ❌ OpenAPI documentation lacks proper error responses

### How to Fix

**Option 1: Use [UnionStatusCode] attribute**

```csharp
using UnionGenerator.AspNetCore;

// ✅ Explicit status code attribute
[UnionStatusCode(404)]
public record NotFoundError(string ResourceType, string ResourceId);

[UnionStatusCode(401)]
public record UnauthorizedError(string Reason);

[UnionStatusCode(400)]
public record ValidationError(Dictionary<string, string[]> Errors);

[GenerateUnion]
public partial record ApiResult<T>
{
    public static partial ApiResult<T> Success(T data);
    public static partial ApiResult<T> NotFound(NotFoundError error);
    public static partial ApiResult<T> Unauthorized(UnauthorizedError error);
    public static partial ApiResult<T> ValidationFailed(ValidationError error);
}
```

**Option 2: Implement StatusCode property**

```csharp
// ✅ StatusCode property
public record UserError
{
    public required string Message { get; init; }
    public int StatusCode { get; init; } = 400;
}

public record NotFoundError : UserError
{
    public NotFoundError(string resource) : base()
    {
        Message = $"{resource} not found";
        StatusCode = 404;
    }
}
```

**Option 3: Use ProblemDetails**

```csharp
using Microsoft.AspNetCore.Mvc;

// ✅ ProblemDetails includes status
public record ApiError
{
    public required ProblemDetails Problem { get; init; }
}

var error = new ApiError
{
    Problem = new ProblemDetails
    {
        Status = 404,
        Title = "Resource Not Found",
        Detail = "User with ID 123 does not exist"
    }
};
```

### Status Code Conventions

The analyzer recognizes these conventions:

| Error Type Name | Inferred Status |
|----------------|-----------------|
| `*NotFound*` | 404 |
| `*Unauthorized*` | 401 |
| `*Forbidden*` | 403 |
| `*BadRequest*` | 400 |
| `*Validation*` | 400 |
| `*Conflict*` | 409 |
| `*Timeout*` | 408 |

```csharp
// ✅ Inferred by convention
public record NotFoundError(string Message); // → 404
public record ValidationError(string Message); // → 400
public record UnauthorizedAccess(string Reason); // → 401
```

### When to Suppress

```csharp
// Suppress if using custom status code resolution
#pragma warning disable UG4011
public record CustomError(string Code, string Message);
// Custom middleware handles status code mapping
#pragma warning restore UG4011
```

## UG4012: Convention Override Recommended

### What It Detects

Error types that rely on name-based convention for status code inference but lack explicit `[UnionStatusCode]` attribute.

### Example

```csharp
// ⚠️ UG4012: Convention-based, but explicit is better
public record NotFoundError(string Message);
// Inferred as 404 by name, but not explicit
```

### Why Make It Explicit

Name-based inference:
- ⚠️ Fragile - renaming breaks the convention
- ⚠️ Not obvious to maintainers
- ⚠️ Harder to document
- ⚠️ Can be ambiguous

### How to Fix

```csharp
// ✅ Explicit and clear
[UnionStatusCode(404)]
public record NotFoundError(string Message);
```

### Configuration

This diagnostic is **hidden by default**. Enable it:

```ini
# .editorconfig
dotnet_diagnostic.UG4012.severity = suggestion
```

## 🔧 Configuration

### Enable/Disable Analyzers

```ini
# .editorconfig

# Disable all ASP.NET Core analyzers
dotnet_analyzer_diagnostic.category-AspNetCore.severity = none

# Enable specific analyzers
dotnet_diagnostic.UG4010.severity = warning
dotnet_diagnostic.UG4011.severity = warning
dotnet_diagnostic.UG4012.severity = suggestion
```

### Project-Level Settings

```xml
<!-- .csproj -->
<PropertyGroup>
  <!-- Treat union mapping warnings as errors in Release -->
  <WarningsAsErrors Condition="'$(Configuration)' == 'Release'">UG4010;UG4011</WarningsAsErrors>
</PropertyGroup>
```

## 💡 Best Practices

### 1. Always Map Unions to IActionResult

```csharp
// ✅ Recommended pattern
[HttpPost]
public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
{
    var result = await _userService.CreateUserAsync(dto);
    return result.ToActionResult();
}

// ❌ Avoid returning raw unions
[HttpPost]
public async Task<Result<User, CreateUserError>> CreateUser([FromBody] CreateUserDto dto)
{
    return await _userService.CreateUserAsync(dto);
}
```

### 2. Define Error Types with Status Codes

```csharp
// ✅ Clear status code semantics
[UnionStatusCode(404)]
public record ResourceNotFound(string ResourceType, string ResourceId);

[UnionStatusCode(400)]
public record InvalidInput(string Field, string Error);

[UnionStatusCode(409)]
public record DuplicateResource(string Field, string Value);

[GenerateUnion]
public partial record CreateResult<T>
{
    public static partial CreateResult<T> Created(T resource);
    public static partial CreateResult<T> NotFound(ResourceNotFound error);
    public static partial CreateResult<T> Invalid(InvalidInput error);
    public static partial CreateResult<T> Duplicate(DuplicateResource error);
}
```

### 3. Use ProblemDetails for Rich Errors

```csharp
using Microsoft.AspNetCore.Mvc;

[UnionStatusCode(400)]
public record ValidationProblem
{
    public required ProblemDetails Details { get; init; }
    
    public static ValidationProblem Create(Dictionary<string, string[]> errors)
    {
        return new ValidationProblem
        {
            Details = new ValidationProblemDetails(errors)
            {
                Title = "Validation Failed",
                Status = 400,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            }
        };
    }
}
```

### 4. Document Status Codes in Swagger

```csharp
[HttpGet("{id}")]
[ProducesResponseType(typeof(User), 200)]
[ProducesResponseType(typeof(ProblemDetails), 404)]
[ProducesResponseType(typeof(ProblemDetails), 401)]
public IActionResult GetUser(int id)
{
    var result = _userService.GetUser(id);
    return result.ToActionResult();
}
```

## 📊 Minimal API Support

These analyzers also work with minimal APIs:

### Example Problem

```csharp
// ⚠️ UG4010: Should return IResult
app.MapGet("/users/{id}", (int id, IUserRepository repo) =>
{
    return repo.GetById(id); // Returns Result<User, UserError>
});
```

### Fixed

```csharp
// ✅ Maps to IResult
app.MapGet("/users/{id}", (int id, IUserRepository repo) =>
{
    var result = repo.GetById(id);
    return result.ToResult(); // Extension for minimal APIs
});
```

## 🔍 Technical Details

### How UG4010 Detection Works

1. Find methods in classes inheriting from `ControllerBase`
2. Check return type is a union (has `[GenerateUnion]`)
3. Verify return type is not `IActionResult` or `ActionResult<T>`
4. Report diagnostic if raw union is returned

### How UG4011 Detection Works

1. Analyze union case types
2. Check for `[UnionStatusCode]` attribute
3. Check for `StatusCode` property
4. Check for name-based conventions
5. Report if no status code found

### Performance

- Runs only on controller/API methods
- Symbol-based analysis (fast)
- Concurrent execution enabled
- Typical overhead: &lt;20ms per controller

## 📚 Related Topics

- [ASP.NET Core Integration Guide](../integration-packages/aspnetcore) - Full integration docs
- [Pattern Matching Analyzers](./pattern-matching) - Exhaustive matching
- [ProblemDetails Mapping](../integration-packages/aspnetcore#problemdetails-integration) - Error handling

## 🎓 Complete Example

```csharp
using Microsoft.AspNetCore.Mvc;
using UnionGenerator;
using UnionGenerator.AspNetCore;

// Error types with explicit status codes
[UnionStatusCode(404)]
public record NotFound(string Resource, string Id);

[UnionStatusCode(400)]
public record ValidationFailed(Dictionary<string, string[]> Errors);

[UnionStatusCode(409)]
public record AlreadyExists(string Resource, string Field);

// Union result type
[GenerateUnion]
public partial record CreateUserResult
{
    public static partial CreateUserResult Success(User user);
    public static partial CreateUserResult NotFound(NotFound error);
    public static partial CreateUserResult Invalid(ValidationFailed error);
    public static partial CreateUserResult Exists(AlreadyExists error);
}

// ✅ Controller properly mapping unions
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(User), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        var result = await _userService.CreateUserAsync(dto);
        
        // ✅ Proper mapping with status codes
        return result.ToActionResult(
            success: user => CreatedAtAction(
                nameof(GetUser),
                new { id = user.Id },
                user
            )
        );
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var result = await _userService.GetUserByIdAsync(id);
        return result.ToActionResult();
    }
}
```
