using UnionGenerator.Attributes;

namespace ExampleProject;

// ============================================================================
// UNION GENERATOR - REAL-WORLD SCENARIOS WITH EXAMPLES
// ============================================================================
// This file demonstrates all features of the Union Generator from basic to
// advanced, using real-world scenarios that developers can quickly understand.
// ============================================================================

// ----------------------------------------------------------------------------
// 1. BASIC UNION STRUCTURE - API Response Scenario
// ----------------------------------------------------------------------------
// Scenario: Fetching data from an API. It can succeed or return an error.
[GenerateUnion]
public partial class ApiResponse<TData>
{
    public static ApiResponse<TData> Success(TData data) => new SuccessCase(data);
    public static ApiResponse<TData> Failure(string errorMessage) => new FailureCase(errorMessage);
}


// ----------------------------------------------------------------------------
// 2. TWO-PARAMETER UNION - Result Pattern
// ----------------------------------------------------------------------------
// Scenario: An operation result. Can succeed (returns a value) or fail.
[GenerateUnion]
public partial class Result<TValue, TError>
{
    public static Result<TValue, TError> Ok(TValue value) => new OkCase(value);
    public static Result<TValue, TError> Error(TError error) => new ErrorCase(error);
}

// ----------------------------------------------------------------------------
// 3. OPTION PATTERN - Alternative to Nullable
// ----------------------------------------------------------------------------
// Scenario: A value may or may not exist (safe alternative to null)
[GenerateUnion]
public partial class Option<T>
{
    public static Option<T> Some(T value) => new SomeCase(value);
    public static Option<T> None() => new NoneCase();
}

// ----------------------------------------------------------------------------
// MAIN PROGRAM - REAL-WORLD SCENARIOS
// ----------------------------------------------------------------------------
internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   UNION GENERATOR - REAL-WORLD SCENARIOS                      ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // ====================================================================
        // SECTION 1: BASIC USAGE - Pattern Matching Properties
        // ====================================================================
        Console.WriteLine("┌────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ SECTION 1: BASIC USAGE - Pattern Matching Properties           │");
        Console.WriteLine("└────────────────────────────────────────────────────────────────┘");
        Console.WriteLine();

        // Scenario: Fetching user information
        var userResponse = ApiResponse<User>.Success(new User("John Doe", "john@example.com"));
        var errorResponse = ApiResponse<User>.Failure("User not found");

        Console.WriteLine("📋 Scenario: Fetching user information from API");
        Console.WriteLine($"   Is Success? {userResponse.IsSuccess}");
        Console.WriteLine($"   Is Failure? {userResponse.IsFailure}");
        Console.WriteLine();

        // ====================================================================
        // SECTION 2: VALUE PROPERTIES - Direct Value Access
        // ====================================================================
        Console.WriteLine("┌────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ SECTION 2: VALUE PROPERTIES - Direct Value Access             │");
        Console.WriteLine("└────────────────────────────────────────────────────────────────┘");
        Console.WriteLine();

        Console.WriteLine("📋 Scenario: Getting value directly from successful response");

        if (userResponse.IsSuccess)
        {
            var user = userResponse.Value; // Direct property access (Value for single-parameter union)
            Console.WriteLine($"   User: {user.Name} ({user.Email})");
        }

        if (errorResponse.IsFailure)
        {
            var errorMessage = errorResponse.Value; // Error message (Value for FailureCase)
            Console.WriteLine($"   Error: {errorMessage}");
        }

        Console.WriteLine();

        // ====================================================================
        // SECTION 3: MATCH METHOD - Functional Pattern Matching
        // ====================================================================
        Console.WriteLine("┌────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ SECTION 3: MATCH METHOD - Functional Pattern Matching          │");
        Console.WriteLine("└────────────────────────────────────────────────────────────────┘");
        Console.WriteLine();

        Console.WriteLine("📋 Scenario: Converting response to a message");
        var responseMessage = userResponse.Match(success: user => $"✅ User found: {user.Name}", failure: msg => $"❌ Error: {msg}");
        Console.WriteLine($"   {responseMessage}");
        Console.WriteLine();

        // ====================================================================
        // SECTION 4: EQUALITY & TOSTRING - Comparison and Debug
        // ====================================================================
        Console.WriteLine("┌────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ SECTION 4: EQUALITY & TOSTRING - Comparison and Debug          │");
        Console.WriteLine("└────────────────────────────────────────────────────────────────┘");
        Console.WriteLine();

        Console.WriteLine("📋 Scenario: Fetching same user again and comparing");
        var user1 = ApiResponse<User>.Success(new User("John Doe", "john@example.com"));
        var user2 = ApiResponse<User>.Success(new User("John Doe", "john@example.com"));
        var user3 = ApiResponse<User>.Success(new User("Jane Doe", "jane@example.com"));

        Console.WriteLine($"   user1 == user2: {user1 == user2}"); // True - same values
        Console.WriteLine($"   user1 == user3: {user1 == user3}"); // False - different values
        Console.WriteLine($"   user1.ToString(): {user1}");        // For debugging
        Console.WriteLine();

        // ====================================================================
        // SECTION 5: SWITCH EXPRESSIONS - Modern C# Pattern Matching
        // ====================================================================
        Console.WriteLine("┌────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ SECTION 5: SWITCH EXPRESSIONS - Modern C# Pattern Matching    │");
        Console.WriteLine("└────────────────────────────────────────────────────────────────┘");
        Console.WriteLine();

        Console.WriteLine("📋 Scenario: Different operations based on response type");

        var status = userResponse switch
        {
            ApiResponse<User>.SuccessCase success => $"✅ Success: {success.Value.Name}",
            ApiResponse<User>.FailureCase failure => $"❌ Error: {failure.Value}",
            _                                     => "⚠️ Unknown state"
        };
        Console.WriteLine($"   {status}");
        Console.WriteLine();

        // ====================================================================
        // SECTION 6: IEQUATABLE & DECONSTRUCT - Interface and Tuple
        // ====================================================================
        Console.WriteLine("┌────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ SECTION 6: IEQUATABLE & DECONSTRUCT - Interface and Tuple     │");
        Console.WriteLine("└────────────────────────────────────────────────────────────────┘");
        Console.WriteLine();

        Console.WriteLine("📋 Scenario: Type-safe comparison with IEquatable");
        IEquatable<ApiResponse<User>> equatable = user1;
        Console.WriteLine($"   IEquatable comparison: {equatable.Equals(user2)}");
        Console.WriteLine();

        Console.WriteLine("📋 Scenario: Tuple deconstruction with Deconstruct (Result pattern)");
        var deconstructResult = ReadFile("test.txt");
        var (deconstructData, deconstructError) = deconstructResult;

        if (deconstructData != null)
        {
            Console.WriteLine($"   Deconstruct - Data: {deconstructData.Substring(0, Math.Min(20, deconstructData.Length))}...");
        }
        else
        {
            Console.WriteLine($"   Deconstruct - Error: {deconstructError?.Message ?? "null"}");
        }

        Console.WriteLine();

        // ====================================================================
        // SECTION 7: FUNCTIONAL PROGRAMMING - TryGetValue, Map, OrElse
        // ====================================================================
        Console.WriteLine("┌────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ SECTION 7: FUNCTIONAL PROGRAMMING - TryGetValue, Map, OrElse   │");
        Console.WriteLine("└────────────────────────────────────────────────────────────────┘");
        Console.WriteLine();

        Console.WriteLine("📋 Scenario: Safe value extraction with TryGetValue");

        if (userResponse.TryGetSuccess(out var userData))
        {
            Console.WriteLine($"   TryGetSuccess: {userData.Name}");
        }

        if (errorResponse.TryGetFailure(out var errorMsg))
        {
            Console.WriteLine($"   TryGetFailure: {errorMsg}");
        }

        // For Result pattern
        if (deconstructResult.TryGetOk(out var tryGetContent))
        {
            Console.WriteLine($"   TryGetOk: {tryGetContent.Substring(0, Math.Min(20, tryGetContent.Length))}...");
        }

        Console.WriteLine();

        Console.WriteLine("📋 Scenario: Value transformation with Map (Result pattern)");
        var mappedFileResult = deconstructResult.MapOk(content => content.ToUpper());
        Console.WriteLine($"   MapOk result: {mappedFileResult.Value?.Substring(0, Math.Min(20, mappedFileResult.Value.Length))}...");
        Console.WriteLine();

        Console.WriteLine("📋 Scenario: Default value with OrElse (Result pattern)");
        var defaultContent = "Default content";
        var orElseResult = deconstructResult.OkOrElse(defaultContent);
        Console.WriteLine($"   OkOrElse (on error): {orElseResult}");
        Console.WriteLine();

        // ====================================================================
        // SECTION 8: RESULT PATTERN - Two-Parameter Union
        // ====================================================================
        Console.WriteLine("┌────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ SECTION 8: RESULT PATTERN - Two-Parameter Union                │");
        Console.WriteLine("└────────────────────────────────────────────────────────────────┘");
        Console.WriteLine();

        Console.WriteLine("📋 Scenario: File reading operation");
        var fileResult = ReadFile("config.json");

        var fileContent = fileResult.Match(ok: content => $"✅ File read: {content.Length} characters", error: ex => $"❌ Error: {ex.Message}");
        Console.WriteLine($"   {fileContent}");

        // Error handling with Map
        var processedResult = fileResult.MapOk(content => content.ToUpper());
        Console.WriteLine($"   MapOk result: {processedResult.Value?.Substring(0, Math.Min(20, processedResult.Value?.Length ?? 0))}...");
        Console.WriteLine();

        // ====================================================================
        // SECTION 9: OPTION PATTERN - Nullable Alternative
        // ====================================================================
        Console.WriteLine("┌────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ SECTION 9: OPTION PATTERN - Nullable Alternative              │");
        Console.WriteLine("└────────────────────────────────────────────────────────────────┘");
        Console.WriteLine();

        Console.WriteLine("📋 Scenario: User search (may not be found)");
        var foundUser = FindUser("john@example.com");
        var notFoundUser = FindUser("nonexistent@example.com");

        var userInfo = foundUser.Match(some: user => $"✅ User found: {user.Name}", none: () => "❌ User not found");
        Console.WriteLine($"   {userInfo}");

        var notFoundInfo = notFoundUser.Match(some: user => $"✅ User found: {user.Name}", none: () => "❌ User not found");
        Console.WriteLine($"   {notFoundInfo}");
        Console.WriteLine();

        // ====================================================================
        // SECTION 10: REAL-WORLD SCENARIO - API Client Simulation
        // ====================================================================
        Console.WriteLine("┌────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ SECTION 10: REAL-WORLD SCENARIO - API Client Simulation      │");
        Console.WriteLine("└────────────────────────────────────────────────────────────────┘");
        Console.WriteLine();

        Console.WriteLine("📋 Scenario: Complete API client flow");
        var apiClient = new ApiClient();
        var productResult = apiClient.GetProduct(123);

        // Combining all features
        var finalResult = productResult.MapOk(product => $"Product: {product.Name} - Price: {product.Price:C}")
                                       .Match(ok: message => $"✅ {message}", error: error => $"❌ API Error: {error.Message}");

        Console.WriteLine($"   {finalResult}");
        Console.WriteLine();

        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   ALL EXAMPLES COMPLETED!                                     ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
    }

    // ========================================================================
    // HELPER METHODS - Real-World Scenarios
    // ========================================================================

    /// <summary>
    /// Simulated file reading operation
    /// </summary>
    static Result<string, Exception> ReadFile(string fileName)
    {
        try
        {
            // Simulated file reading
            if (fileName == "config.json")
            {
                return Result<string, Exception>.Ok("{\"key\": \"value\"}");
            }

            throw new FileNotFoundException($"File not found: {fileName}");
        }
        catch (Exception ex)
        {
            return Result<string, Exception>.Error(ex);
        }
    }

    /// <summary>
    /// Simulated user search
    /// </summary>
    static Option<User> FindUser(string email)
    {
        // Simulated database query
        if (email == "john@example.com")
        {
            return Option<User>.Some(new User("John Doe", email));
        }

        return Option<User>.None();
    }
}

// ========================================================================
// DATA MODELS
// ========================================================================

public record User(string Name, string Email);

public record Product(int Id, string Name, decimal Price);

// ========================================================================
// API CLIENT SIMULATION
// ========================================================================

public class ApiClient
{
    public Result<Product, Exception> GetProduct(int productId)
    {
        // Simulated API call
        if (productId == 123)
        {
            return Result<Product, Exception>.Ok(new Product(123, "Laptop", 1299.99m));
        }

        return Result<Product, Exception>.Error(new Exception("Product not found"));
    }
}