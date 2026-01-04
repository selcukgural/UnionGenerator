using JsonExample.Models;
using System.Text.Json;

Console.WriteLine("=== UnionGenerator + JSON Serialization Example ===\n");

var options = new JsonSerializerOptions { WriteIndented = true };

// Example 1: Serialize success case
Console.WriteLine("1. Serializing success response...");
var successUser = new User { Id = 1, Name = "John Doe", Email = "john@example.com" };
var successResponse = ApiResponse<User>.Success(successUser);

var successJson = JsonSerializer.Serialize(successResponse, options);
Console.WriteLine($"   JSON:\n{successJson}\n");

// Example 2: Serialize failure case
Console.WriteLine("2. Serializing failure response...");
var failureResponse = ApiResponse<User>.Failed(
    new ErrorInfo("NOT_FOUND", "User not found", "User with ID 1 does not exist")
);

var failureJson = JsonSerializer.Serialize(failureResponse, options);
Console.WriteLine($"   JSON:\n{failureJson}\n");

// Example 3: Deserialize success case
Console.WriteLine("3. Deserializing success response...");
var successDeserialized = JsonSerializer.Deserialize<ApiResponse<User>>(successJson);

successDeserialized?.Match(
    success: user => Console.WriteLine($"   ✓ Success: {user.Name} ({user.Email})"),
    failed: error => Console.WriteLine($"   ✗ Error: {error.Code}")
);
Console.WriteLine();

// Example 4: Deserialize failure case
Console.WriteLine("4. Deserializing failure response...");
var failureDeserialized = JsonSerializer.Deserialize<ApiResponse<User>>(failureJson);

failureDeserialized?.Match(
    success: user => Console.WriteLine($"   ✓ Success: {user.Name}"),
    failed: error => Console.WriteLine($"   ✗ Error: {error.Code} - {error.Message}")
);
Console.WriteLine();

// Example 5: Array of responses
Console.WriteLine("5. Serializing array of responses...");
var responses = new object[]
{
    ApiResponse<User>.Success(new User { Id = 1, Name = "Alice", Email = "alice@example.com" }),
    ApiResponse<User>.Failed(new ErrorInfo("UNAUTHORIZED", "Access denied")),
    ApiResponse<User>.Success(new User { Id = 2, Name = "Bob", Email = "bob@example.com" })
};

var arrayJson = JsonSerializer.Serialize(responses, options);
Console.WriteLine($"   JSON:\n{arrayJson}\n");

// Example 6: Complex nested type
Console.WriteLine("6. Serializing complex nested response...");
var products = new[]
{
    new Product { Id = 1, Name = "Laptop", Price = 1299.99m },
    new Product { Id = 2, Name = "Mouse", Price = 29.99m }
};

var complexResponse = ApiResponse<Product[]>.Success(products);
var complexJson = JsonSerializer.Serialize(complexResponse, options);
Console.WriteLine($"   JSON:\n{complexJson}\n");

// Example 7: Pattern matching on deserialized results
Console.WriteLine("7. Pattern matching after deserialization...");
var jsonStrings = new[]
{
    successJson,
    failureJson
};

var results = jsonStrings
    .Select(json => JsonSerializer.Deserialize<ApiResponse<User>>(json))
    .ToList();

Console.WriteLine($"   Deserialized {results.Count} responses:");
foreach (var result in results)
{
    var status = result?.Match(
        success: _ => "✓ Success",
        failed: error => $"✗ Error: {error.Code}"
    ) ?? "? Unknown";
    
    Console.WriteLine($"     {status}");
}
Console.WriteLine();

// Example 8: Real-world API response simulation
Console.WriteLine("8. Simulating real-world API responses...");

var apiResponses = new[]
{
    ApiResponse<User>.Success(
        new User { Id = 1, Name = "Charlie", Email = "charlie@example.com" }
    ),
    ApiResponse<User>.Failed(
        new ErrorInfo("VALIDATION_ERROR", "Invalid input", "Email format is invalid")
    ),
    ApiResponse<User>.Success(
        new User { Id = 2, Name = "Diana", Email = "diana@example.com" }
    ),
    ApiResponse<User>.Failed(
        new ErrorInfo("SERVER_ERROR", "Internal server error", "Database connection failed")
    )
};

var successCount = 0;
var failureCount = 0;

foreach (var response in apiResponses)
{
    var json = JsonSerializer.Serialize(response, options);
    var deserialized = JsonSerializer.Deserialize<ApiResponse<User>>(json);
    
    deserialized?.Match(
        success: user => 
        {
            successCount++;
            Console.WriteLine($"   ✓ User {user.Id}: {user.Name}");
        },
        failed: error => 
        {
            failureCount++;
            Console.WriteLine($"   ✗ {error.Code}: {error.Message}");
        }
    );
}

Console.WriteLine($"\n   Summary: {successCount} success, {failureCount} failures\n");

// Example 9: Handling different response types
Console.WriteLine("9. Handling different generic types...");

var userResponse = ApiResponse<User>.Success(
    new User { Id = 1, Name = "Eve", Email = "eve@example.com" }
);

var productResponse = ApiResponse<Product>.Success(
    new Product { Id = 1, Name = "Keyboard", Price = 99.99m }
);

var userJson = JsonSerializer.Serialize(userResponse, options);
var productJson = JsonSerializer.Serialize(productResponse, options);

Console.WriteLine("   User Response:");
Console.WriteLine(userJson);
Console.WriteLine("\n   Product Response:");
Console.WriteLine(productJson);

Console.WriteLine("\n=== Example completed successfully! ===");

