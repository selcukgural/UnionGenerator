using FluentValidationExample.Models;
using FluentValidationExample.Validators;
using UnionGenerator.FluentValidation.Extensions;

Console.WriteLine("=== UnionGenerator + FluentValidation Example ===\n");

// Initialize validator
var userValidator = new CreateUserDtoValidator();
var productValidator = new CreateProductDtoValidator();

// Example 1: Valid user creation
Console.WriteLine("1. Creating valid user...");
var validUser = new CreateUserDto
{
    FirstName = "John",
    LastName = "Doe",
    Email = "john.doe@example.com",
    Age = 28,
    Username = "johndoe"
};

var validationResult = userValidator.Validate(validUser);
if (validationResult.IsValid)
{
    Console.WriteLine("   ✓ User is valid!\n");
}

// Example 2: Invalid user with multiple validation errors
Console.WriteLine("2. Validating invalid user (multiple errors)...");
var invalidUser = new CreateUserDto
{
    FirstName = "",  // Missing
    LastName = "X",  // Too short
    Email = "not-an-email",  // Invalid format
    Age = 15,  // Too young
    Username = "ab"  // Too short
};

var invalidResult = userValidator.Validate(invalidUser);
if (!invalidResult.IsValid)
{
    Console.WriteLine($"   ✗ Validation failed with {invalidResult.Errors.Count} errors:");
    foreach (var error in invalidResult.Errors)
    {
        Console.WriteLine($"     - {error.PropertyName}: {error.ErrorMessage}");
    }
    Console.WriteLine();
}

// Example 3: Convert validation result to ProblemDetailsError
Console.WriteLine("3. Converting validation result to ProblemDetailsError...");
var problemDetailsError = invalidResult.ToProblemDetailsError("/api/users");

Console.WriteLine($"   Status: {problemDetailsError.Status}");
Console.WriteLine($"   Title: {problemDetailsError.Title}");
Console.WriteLine($"   Instance: {problemDetailsError.Instance}");
Console.WriteLine($"   Errors:");
foreach (var (field, messages) in problemDetailsError.Errors!)
{
    Console.WriteLine($"     {field}:");
    foreach (var message in messages)
    {
        Console.WriteLine($"       - {message}");
    }
}
Console.WriteLine();

// Example 4: Valid product
Console.WriteLine("4. Creating valid product...");
var validProduct = new CreateProductDto
{
    Name = "Laptop",
    Description = "High-performance laptop for professionals",
    Price = 1299.99m,
    Stock = 50,
    Sku = "LAPTOP-001"
};

var productResult = productValidator.Validate(validProduct);
if (productResult.IsValid)
{
    Console.WriteLine("   ✓ Product is valid!\n");
}

// Example 5: Invalid product
Console.WriteLine("5. Validating invalid product...");
var invalidProduct = new CreateProductDto
{
    Name = "",  // Missing
    Description = "D",  // Too short
    Price = -10,  // Negative price
    Stock = -1,  // Negative stock
    Sku = ""  // Missing
};

var invalidProductResult = productValidator.Validate(invalidProduct);
if (!invalidProductResult.IsValid)
{
    Console.WriteLine($"   ✗ Validation failed with {invalidProductResult.Errors.Count} errors:");
    
    var productProblemDetails = invalidProductResult.ToProblemDetailsError("/api/products");
    foreach (var (field, messages) in productProblemDetails.Errors!)
    {
        Console.WriteLine($"     - {field}: {string.Join(", ", messages)}");
    }
    Console.WriteLine();
}

// Example 6: Pattern matching on validation
Console.WriteLine("6. Pattern matching on validation result...");
var testUser = new CreateUserDto
{
    FirstName = "Alice",
    LastName = "Smith",
    Email = "alice@example.com",
    Age = 30,
    Username = "asmith"
};

var testResult = userValidator.Validate(testUser);
var status = testResult.IsValid
    ? "✓ Valid"
    : $"✗ Invalid ({testResult.Errors.Count} errors)";

Console.WriteLine($"   User validation: {status}\n");

// Example 7: Batch validation
Console.WriteLine("7. Batch validating multiple users...");
var users = new[]
{
    new CreateUserDto { FirstName = "Bob", LastName = "Johnson", Email = "bob@example.com", Age = 25, Username = "bjohnson" },
    new CreateUserDto { FirstName = "", LastName = "Brown", Email = "invalid-email", Age = 17, Username = "bb" },
    new CreateUserDto { FirstName = "Charlie", LastName = "White", Email = "charlie@example.com", Age = 35, Username = "cwhite" }
};

var results = users
    .Select((user, index) => (
        Index: index + 1,
        User: user,
        Validation: userValidator.Validate(user)
    ))
    .ToList();

foreach (var item in results)
{
    var icon = item.Validation.IsValid ? "✓" : "✗";
    Console.WriteLine($"   {icon} User {item.Index}: {item.User.FirstName} - {(item.Validation.IsValid ? "Valid" : "Invalid")}");
}
Console.WriteLine();

// Example 8: Working with validation errors structure
Console.WriteLine("8. Demonstrating error structure...");
var complexUser = new CreateUserDto
{
    FirstName = "",
    LastName = "X",
    Email = "bad-email",
    Age = 10,
    Username = "u"
};

var complexResult = userValidator.Validate(complexUser);
var errorProblemDetails = complexResult.ToProblemDetailsError("/api/users/create");

Console.WriteLine($"   Error Type: {errorProblemDetails.Type}");
Console.WriteLine($"   Title: {errorProblemDetails.Title}");
Console.WriteLine($"   Status Code: {errorProblemDetails.Status}");
Console.WriteLine($"   Detail: {errorProblemDetails.Detail}");
Console.WriteLine($"   Instance: {errorProblemDetails.Instance}");
Console.WriteLine($"   Field Errors:");
foreach (var (field, errorMessages) in errorProblemDetails.Errors!)
{
    Console.WriteLine($"     {field}:");
    foreach (var message in errorMessages)
    {
        Console.WriteLine($"       * {message}");
    }
}
Console.WriteLine();

Console.WriteLine("=== Example completed successfully! ===");

