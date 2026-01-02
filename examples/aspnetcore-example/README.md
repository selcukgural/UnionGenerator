# ASP.NET Core Integration Example

This example demonstrates how to use UnionGenerator with ASP.NET Core to implement the Result pattern with automatic ProblemDetails conversion.

## Features Demonstrated

1. **Result Pattern**: Using generated union types for API responses
2. **ProblemDetails Integration**: Automatic conversion of errors to RFC 7807 ProblemDetails
3. **Controller Support**: Traditional MVC controller endpoints
4. **Minimal API Support**: Modern Minimal API endpoints with filters
5. **Model Validation**: Integration with ASP.NET Core model validation
6. **OpenAPI/Swagger**: Full API documentation support

## Running the Example

```bash
cd examples/aspnetcore-example
dotnet run
```

The API will be available at `https://localhost:5001` (or the port shown in the console).
Swagger UI will be available at the root URL: `https://localhost:5001`

## API Endpoints

### Controller Endpoints (Traditional)

- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users` - Create a new user
- `PUT /api/users/{id}` - Update an existing user
- `DELETE /api/users/{id}` - Delete a user

### Minimal API Endpoints

- `GET /api/minimal/users` - Get all users
- `GET /api/minimal/users/{id}` - Get user by ID
- `POST /api/minimal/users` - Create a new user
- `PUT /api/minimal/users/{id}` - Update an existing user
- `DELETE /api/minimal/users/{id}` - Delete a user

## Example Requests

### Get User (Success - 200 OK)

```bash
curl -X GET https://localhost:5001/api/users/1
```

Response:
```json
{
  "id": 1,
  "name": "John Doe",
  "email": "john@example.com",
  "age": 30
}
```

### Get User (Not Found - 404)

```bash
curl -X GET https://localhost:5001/api/users/999
```

Response:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "User not found.",
  "status": 404,
  "detail": "User with ID 999 was not found.",
  "instance": "/api/users/999"
}
```

### Create User (Success - 201 Created)

```bash
curl -X POST https://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Alice Johnson",
    "email": "alice@example.com",
    "age": 28
  }'
```

Response:
```json
{
  "id": 4,
  "name": "Alice Johnson",
  "email": "alice@example.com",
  "age": 28
}
```

### Create User (Validation Error - 400)

```bash
curl -X POST https://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Young User",
    "email": "young@example.com",
    "age": 15
  }'
```

Response:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "The request contains invalid data. Please check the errors and try again.",
  "instance": "/api/users",
  "errors": {
    "Age": ["Age must be at least 18."]
  }
}
```

### Create User (Conflict - 409)

```bash
curl -X POST https://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Duplicate User",
    "email": "john@example.com",
    "age": 30
  }'
```

Response:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "A conflict occurred.",
  "status": 409,
  "detail": "A user with email 'john@example.com' already exists.",
  "instance": "/api/users"
}
```

## Project Structure

```
aspnetcore-example/
├── Controllers/
│   └── UsersController.cs      # Traditional MVC controller
├── Models/
│   ├── Result.cs                # Union type definition
│   └── User.cs                  # User entity and DTOs
├── Services/
│   └── UserService.cs           # Business logic service
├── Program.cs                   # Application entry point
├── appsettings.json            # Configuration
└── AspNetCoreExample.csproj    # Project file
```

## Key Implementation Details

### 1. Result Union Type

```csharp
[GenerateUnion]
public partial class Result<TSuccess, TError>
{
    public static Result<TSuccess, TError> Ok(TSuccess value) => new OkCase(value);
    public static Result<TSuccess, TError> Error(TError error) => new ErrorCase(error);
}
```

### 2. Service Layer with Result Pattern

```csharp
public Result<User, ProblemDetailsError> GetUser(int id)
{
    var user = _users.FirstOrDefault(u => u.Id == id);
    
    if (user == null)
    {
        return Result<User, ProblemDetailsError>.Error(
            ProblemDetailsErrorFactory.NotFound(
                instance: $"/api/users/{id}",
                detail: $"User with ID {id} was not found.",
                resourceType: "User"
            )
        );
    }
    
    return Result<User, ProblemDetailsError>.Ok(user);
}
```

### 3. Controller with Extension Methods

```csharp
[HttpGet("{id}")]
public IActionResult GetUser(int id)
{
    var result = _userService.GetUser(id);
    return result.ToActionResult();
}
```

### 4. Minimal API with Endpoint Filter

```csharp
app.MapGet("/api/minimal/users/{id}", GetUserMinimal)
   .AddEndpointFilter<UnionEndpointFilter>();

Result<User, ProblemDetailsError> GetUserMinimal(int id, IUserService userService)
{
    return userService.GetUser(id);
}
```

## Benefits

1. **Type Safety**: Compile-time guarantees for success/error cases
2. **Consistent Error Format**: All errors follow RFC 7807 ProblemDetails
3. **Reduced Boilerplate**: No manual status code checking or error response building
4. **Better Documentation**: OpenAPI/Swagger automatically documents response types
5. **Testability**: Easy to test business logic with union types
6. **Clean Separation**: Service layer is independent of HTTP concerns

## Learn More

- [UnionGenerator Documentation](../../README.md)
- [UnionGenerator.AspNetCore README](../../src/UnionGenerator.AspNetCore/README.md)
- [RFC 7807 - Problem Details for HTTP APIs](https://tools.ietf.org/html/rfc7807)

