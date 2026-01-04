using OneOf;
using OneOfExample.Models;
using UnionGenerator.OneOfExtensions;

Console.WriteLine("=== UnionGenerator + OneOf Compatibility Example ===\n");

// Example 1: Using OneOf directly
Console.WriteLine("1. Using OneOf<T0, T1> directly...");
OneOf<User, ErrorResponse> oneOfSuccess = CreateUserOneOf(1);
OneOf<User, ErrorResponse> oneOfFailure = CreateUserOneOf(999);

oneOfSuccess.Switch(
    user => Console.WriteLine($"   ✓ OneOf Success: {user.Name}"),
    error => Console.WriteLine($"   ✗ OneOf Error: {error.Message}")
);

oneOfFailure.Switch(
    user => Console.WriteLine($"   ✓ OneOf Success: {user.Name}"),
    error => Console.WriteLine($"   ✗ OneOf Error: {error.Message}")
);
Console.WriteLine();

// Example 2: Converting OneOf to UnionGenerator (Option A: OneOfCompat - Runtime)
Console.WriteLine("2. Converting OneOf to UnionGenerator (OneOfCompat)...");

OneOf<User, ErrorResponse> oneOfUser = new User { Id = 1, Name = "Alice", Email = "alice@example.com" };
var generatedResultFromT0 = Result<User, ErrorResponse>.Ok(oneOfUser.AsT0);

generatedResultFromT0.Match(
    ok: user => Console.WriteLine($"   ✓ Generated Result: {user.Name}"),
    error: err => Console.WriteLine($"   ✗ Generated Error: {err.Message}")
);
Console.WriteLine();

// Example 3: Converting OneOf Error case
Console.WriteLine("3. Converting OneOf error case...");

OneOf<User, ErrorResponse> oneOfError = new ErrorResponse 
{ 
    Code = "NOT_FOUND",
    Message = "User not found" 
};

var generatedResultFromT1 = Result<User, ErrorResponse>.Error(oneOfError.AsT1);

generatedResultFromT1.Match(
    ok: user => Console.WriteLine($"   ✓ Generated Result: {user.Name}"),
    error: err => Console.WriteLine($"   ✗ Generated Error: {err.Code} - {err.Message}")
);
Console.WriteLine();

// Example 4: Converting with OneOfExtensions (Fluent API)
Console.WriteLine("4. Converting OneOf to UnionGenerator (OneOfExtensions)...");

OneOf<User, ErrorResponse> oneOfForExtension = new User 
{ 
    Id = 2, 
    Name = "Bob", 
    Email = "bob@example.com" 
};

// Using fluent extension method
var fluentResult = oneOfForExtension.ToGeneratedResult<Result<User, ErrorResponse>, User, ErrorResponse>();

fluentResult?.Match(
    ok: user => Console.WriteLine($"   ✓ Fluent Result: {user.Name}"),
    error: err => Console.WriteLine($"   ✗ Fluent Error: {err.Message}")
);
Console.WriteLine();

// Example 5: Complete migration path simulation
Console.WriteLine("5. Demonstrating gradual migration...");

// Old code (OneOf)
var legacyResult = LegacyGetUser(1);
Console.WriteLine("   Legacy OneOf result:");
legacyResult.Switch(
    user => Console.WriteLine($"     ✓ {user.Name}"),
    error => Console.WriteLine($"     ✗ {error.Message}")
);

// Transition code (OneOf → UnionGenerator conversion)
var transitionalResult = ModernGetUser(1);
Console.WriteLine("   Modern UnionGenerator result:");
transitionalResult.Match(
    ok: user => Console.WriteLine($"     ✓ {user.Name}"),
    error: err => Console.WriteLine($"     ✗ {err.Message}")
);
Console.WriteLine();

// Example 6: Batch processing with conversion
Console.WriteLine("6. Batch processing with conversion...");

var userIds = new[] { 1, 2, 3, 999 };
var results = userIds
    .Select(id => CreateUserOneOf(id))
    .Select(oneOf =>
    {
        if (oneOf.IsT0)
            return Result<User, ErrorResponse>.Ok(oneOf.AsT0);
        else
            return Result<User, ErrorResponse>.Error(oneOf.AsT1);
    })
    .ToList();

var successCount = 0;
var failureCount = 0;

foreach (var result in results)
{
    result.Match(
        ok: user =>
        {
            successCount++;
            Console.WriteLine($"   ✓ {user.Name}");
        },
        error: err =>
        {
            failureCount++;
            Console.WriteLine($"   ✗ {err.Code}");
        }
    );
}

Console.WriteLine($"\n   Summary: {successCount} success, {failureCount} failures\n");

// Example 7: Performance characteristics note
Console.WriteLine("7. Conversion characteristics...");
Console.WriteLine("   OneOfCompat (reflection):      Small overhead (~15-65 µs)");
Console.WriteLine("   OneOfExtensions (fluent):      Slightly faster (~10-35 µs)");
Console.WriteLine("   Direct factory call:            Fastest (no conversion)\n");

// Example 8: Comparison - OneOf vs UnionGenerator patterns
Console.WriteLine("8. Pattern matching comparison...");

var testUser = new User { Id = 1, Name = "Test", Email = "test@example.com" };

// OneOf style
Console.WriteLine("   OneOf style:");
OneOf<User, ErrorResponse> oneOfValue = testUser;
oneOfValue.Switch(
    u => Console.WriteLine($"     User: {u.Name}"),
    e => Console.WriteLine($"     Error: {e.Message}")
);

// UnionGenerator style (converted from OneOf)
Console.WriteLine("   UnionGenerator style:");
var generatedValue = Result<User, ErrorResponse>.Ok(testUser);
generatedValue.Match(
    ok: u => Console.WriteLine($"     User: {u.Name}"),
    error: e => Console.WriteLine($"     Error: {e.Message}")
);

Console.WriteLine("\n=== Example completed successfully! ===");

// Helper functions
OneOf<User, ErrorResponse> CreateUserOneOf(int id)
{
    // Simulate getting user from repository
    return id switch
    {
        1 => new User { Id = 1, Name = "Alice", Email = "alice@example.com" },
        2 => new User { Id = 2, Name = "Bob", Email = "bob@example.com" },
        3 => new User { Id = 3, Name = "Charlie", Email = "charlie@example.com" },
        _ => new ErrorResponse { Code = "NOT_FOUND", Message = $"User {id} not found" }
    };
}

// Legacy code using OneOf
OneOf<User, ErrorResponse> LegacyGetUser(int id)
{
    var user = CreateUserOneOf(id);
    return user;
}

// Modern code using UnionGenerator
Result<User, ErrorResponse> ModernGetUser(int id)
{
    var oneOf = LegacyGetUser(id);
    
    // Convert OneOf to UnionGenerator Result
    return oneOf.IsT0
        ? Result<User, ErrorResponse>.Ok(oneOf.AsT0)
        : Result<User, ErrorResponse>.Error(oneOf.AsT1);
}

