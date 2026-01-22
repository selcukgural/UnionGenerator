---
sidebar_position: 1
---

# ASP.NET Core Integration

The UnionGenerator.AspNetCore package provides seamless integration with ASP.NET Core, enabling you to build type-safe Web APIs with automatic ProblemDetails generation and HTTP response mapping.

## Overview

This integration package bridges the gap between discriminated unions and HTTP responses, making it easy to:

- Convert union types to appropriate HTTP responses
- Generate RFC 7807 ProblemDetails automatically
- Use unions in controller actions and minimal APIs
- Handle validation errors consistently  
- Map domain errors to HTTP status codes

## Installation

```bash
dotnet add package UnionGenerator.AspNetCore
```

**Requirements:**
- .NET 6.0 or later
- ASP.NET Core 6.0 or later
- UnionGenerator package

## Quick Start

### Define Your Union

```csharp
using UnionGenerator;

[GenerateUnion]
public partial record UserResult
{
    public static partial UserResult Success(User user);
    public static partial UserResult NotFound(string userId);
    public static partial UserResult ValidationError(List<string> errors);
}
```

### Use in Controller

```csharp
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult GetUser(string id)
    {
        var result = _userService.GetUser(id);
        
        return result.Match(
            success => Ok(success.User),
            notFound => NotFound(new ProblemDetails 
            { 
                Title = "User not found",
                Detail = $"User '{notFound.UserId}' does not exist"
            }),
            validationError => BadRequest(validationError.Errors)
        );
    }
}
```

### Use in Minimal APIs

```csharp
app.MapGet("/api/users/{id}", (string id, IUserService service) =>
{
    var result = service.GetUser(id);
    
    return result.Match(
        success => Results.Ok(success.User),
        notFound => Results.NotFound(new { Error = $"User {notFound.UserId} not found" }),
        validationError => Results.BadRequest(new { Errors = validationError.Errors })
    );
});
```

## Core Features

### Result to HTTP Response Mapping

Convert unions directly to HTTP responses:

```csharp
[GenerateUnion]
public partial record ApiResult<T>
{
    public static partial ApiResult<T> Ok(T data);
    public static partial ApiResult<T> NotFound(string resource);
    public static partial ApiResult<T> BadRequest(string message);
    public static partial ApiResult<T> Unauthorized();
}

[HttpGet("{id}")]
public IActionResult GetItem(int id)
{
    return _service.GetItem(id).Match(
        ok => Ok(ok.Data),
        notFound => NotFound(new { Error = notFound.Resource }),
        badRequest => BadRequest(new { Error = badRequest.Message }),
        unauthorized => Unauthorized()
    );
}
```

### ProblemDetails Integration

Generate RFC 7807 compliant error responses:

```csharp
public static class ResultExtensions
{
    public static IActionResult ToProblemDetails<T>(this ApiResult<T> result)
    {
        return result.Match(
            ok => new OkObjectResult(ok.Data),
            notFound => new NotFoundObjectResult(new ProblemDetails
            {
                Status = 404,
                Title = "Resource not found",
                Detail = notFound.Resource,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            }),
            badRequest => new BadRequestObjectResult(new ProblemDetails
            {
                Status = 400,
                Title = "Bad request",
                Detail = badRequest.Message
            }),
            unauthorized => new UnauthorizedObjectResult(new ProblemDetails
            {
                Status = 401,
                Title = "Unauthorized"
            })
        );
    }
}
```

## CRUD Operations

Complete example with all operations:

```csharp
[GenerateUnion]
public partial record UserOperationResult
{
    public static partial UserOperationResult Created(User user);
    public static partial UserOperationResult Updated(User user);
    public static partial UserOperationResult Deleted();
    public static partial UserOperationResult NotFound();
    public static partial UserOperationResult ValidationError(Dictionary<string, string[]> errors);
    public static partial UserOperationResult Conflict(string message);
}

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    [HttpPost]
    public IActionResult Create(CreateUserDto dto)
    {
        return _service.CreateUser(dto).Match(
            created => CreatedAtAction(nameof(Get), new { id = created.User.Id }, created.User),
            updated => throw new InvalidOperationException(),
            deleted => throw new InvalidOperationException(),
            notFound => throw new InvalidOperationException(),
            validationError => BadRequest(new ValidationProblemDetails(validationError.Errors)),
            conflict => Conflict(new { Error = conflict.Message })
        );
    }

    [HttpGet("{id}")]
    public IActionResult Get(int id)
    {
        return _service.GetUser(id).Match(
            created => Ok(created.User),
            updated => Ok(updated.User),
            deleted => throw new InvalidOperationException(),
            notFound => NotFound(),
            validationError => throw new InvalidOperationException(),
            conflict => throw new InvalidOperationException()
        );
    }

    [HttpPut("{id}")]
    public IActionResult Update(int id, UpdateUserDto dto)
    {
        return _service.UpdateUser(id, dto).Match(
            created => throw new InvalidOperationException(),
            updated => Ok(updated.User),
            deleted => throw new InvalidOperationException(),
            notFound => NotFound(),
            validationError => BadRequest(new ValidationProblemDetails(validationError.Errors)),
            conflict => Conflict(new { Error = conflict.Message })
        );
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        return _service.DeleteUser(id).Match(
            created => throw new InvalidOperationException(),
            updated => throw new InvalidOperationException(),
            deleted => NoContent(),
            notFound => NotFound(),
            validationError => throw new InvalidOperationException(),
            conflict => Conflict(new { Error = conflict.Message })
        );
    }
}
```

## Validation Errors

Handle validation errors with ValidationProblemDetails:

```csharp
[GenerateUnion]
public partial record CreateUserResult
{
    public static partial CreateUserResult Success(User user);
    public static partial CreateUserResult ValidationFailed(Dictionary<string, string[]> errors);
}

[HttpPost]
public IActionResult CreateUser(CreateUserDto dto)
{
    return _service.CreateUser(dto).Match(
        success => CreatedAtAction(nameof(GetUser), new { id = success.User.Id }, success.User),
        validationFailed => BadRequest(new ValidationProblemDetails(validationFailed.Errors)
        {
            Title = "Validation failed",
            Detail = "One or more validation errors occurred"
        })
    );
}
```

## File Upload

Handle file uploads with unions:

```csharp
[GenerateUnion]
public partial record FileUploadResult
{
    public static partial FileUploadResult Uploaded(string fileId, long size);
    public static partial FileUploadResult TooLarge(long maxSize);
    public static partial FileUploadResult InvalidFormat(string[] allowedFormats);
}

[HttpPost("upload")]
public IActionResult Upload(IFormFile file)
{
    return _service.Upload(file).Match(
        uploaded => Ok(new { FileId = uploaded.FileId, Size = uploaded.Size }),
        tooLarge => BadRequest(new ProblemDetails
        {
            Title = "File too large",
            Detail = $"Maximum size is {tooLarge.MaxSize} bytes"
        }),
        invalidFormat => BadRequest(new ProblemDetails
        {
            Title = "Invalid format",
            Detail = $"Allowed: {string.Join(", ", invalidFormat.AllowedFormats)}"
        })
    );
}
```

## Best Practices

### Consistent Error Types

Define standard error types for your API:

```csharp
[GenerateUnion]
public partial record ApiError
{
    public static partial ApiError NotFound(string resource, string id);
    public static partial ApiError ValidationFailed(Dictionary<string, string[]> errors);
    public static partial ApiError Unauthorized();
    public static partial ApiError Forbidden(string reason);
    public static partial ApiError Conflict(string message);
    public static partial ApiError ServerError(Exception exception);
}
```

### Extension Methods

Create reusable extension methods:

```csharp
public static class ApiErrorExtensions
{
    public static IActionResult ToActionResult(this ApiError error)
    {
        return error.Match(
            notFound => CreateNotFound(notFound),
            validationFailed => CreateValidationError(validationFailed),
            unauthorized => new UnauthorizedResult(),
            forbidden => CreateForbidden(forbidden),
            conflict => CreateConflict(conflict),
            serverError => CreateServerError(serverError)
        );
    }
    
    private static IActionResult CreateNotFound(ApiError.NotFoundCase notFound) =>
        new NotFoundObjectResult(new ProblemDetails
        {
            Status = 404,
            Title = "Not found",
            Detail = $"{notFound.Resource} with ID '{notFound.Id}' not found"
        });
    
    // ... other methods
}
```

### Middleware Integration

Handle exceptions globally:

```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var error = ApiError.ServerError(ex);
            var result = error.ToActionResult();
            
            context.Response.StatusCode = GetStatusCode(result);
            await context.Response.WriteAsJsonAsync(GetProblemDetails(result));
        }
    }
}
```

## Key Takeaways

✅ **Match method** provides natural conversion to HTTP responses  
✅ **ProblemDetails** ensures RFC 7807 compliance  
✅ **Type safety** eliminates magic status codes  
✅ **Consistent patterns** across all endpoints  
✅ **Works with both** controllers and minimal APIs
