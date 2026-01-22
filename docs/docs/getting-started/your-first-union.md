---
sidebar_position: 3
---

# Your First Union

Let's build a real-world union type step-by-step. We'll create a file processing system that handles different outcomes: success, validation errors, and system failures.

## Step 1: Define the Problem

Imagine you're building a file upload service. When processing a file, three things can happen:

1. **Success**: File processed successfully with metadata
2. **Validation Error**: File rejected (wrong format, too large, etc.)
3. **System Error**: Something went wrong on our end (disk full, network issue)

Traditional approach with exceptions and nullable types:

```csharp
// ❌ The old way - error-prone and unclear
public class FileProcessor
{
    public FileMetadata? ProcessFile(Stream file)
    {
        try
        {
            // Validation
            if (file.Length > MaxFileSize)
                throw new ValidationException("File too large");
                
            // Processing
            var metadata = ExtractMetadata(file);
            return metadata; // Success case
        }
        catch (ValidationException ex)
        {
            // How do callers know this is validation vs system error?
            return null;
        }
        catch (Exception ex)
        {
            // Mixed error types!
            return null;
        }
    }
}

// Callers can't distinguish between error types:
var result = processor.ProcessFile(fileStream);
if (result == null)
{
    // Was it validation? System error? We don't know! 😕
}
```

## Step 2: Define Your Union

Let's model this with a discriminated union:

```csharp
using UnionGenerator;

[GenerateUnion]
public partial record FileProcessingResult
{
    // Success case with file metadata
    public static partial FileProcessingResult Success(
        string fileName,
        long fileSize,
        string contentType,
        DateTime processedAt
    );
    
    // Validation error with specific reason
    public static partial FileProcessingResult ValidationError(
        string message,
        ValidationErrorCode errorCode
    );
    
    // System error with exception details
    public static partial FileProcessingResult SystemError(
        string message,
        Exception exception
    );
}

// Supporting types
public enum ValidationErrorCode
{
    FileTooLarge,
    InvalidFormat,
    EmptyFile,
    DisallowedContentType
}
```

## Step 3: Implement Your Logic

Now implement the file processor returning the union:

```csharp
public class FileProcessor
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
    private static readonly string[] AllowedTypes = { "image/jpeg", "image/png", "application/pdf" };

    public FileProcessingResult ProcessFile(Stream fileStream, string fileName, string contentType)
    {
        try
        {
            // Validation: Empty file
            if (fileStream.Length == 0)
            {
                return FileProcessingResult.ValidationError(
                    "File is empty",
                    ValidationErrorCode.EmptyFile
                );
            }

            // Validation: File too large
            if (fileStream.Length > MaxFileSize)
            {
                return FileProcessingResult.ValidationError(
                    $"File size {fileStream.Length} bytes exceeds maximum {MaxFileSize} bytes",
                    ValidationErrorCode.FileTooLarge
                );
            }

            // Validation: Content type
            if (!AllowedTypes.Contains(contentType))
            {
                return FileProcessingResult.ValidationError(
                    $"Content type '{contentType}' is not allowed",
                    ValidationErrorCode.DisallowedContentType
                );
            }

            // Process file
            var processedAt = DateTime.UtcNow;
            
            // Success!
            return FileProcessingResult.Success(
                fileName,
                fileStream.Length,
                contentType,
                processedAt
            );
        }
        catch (Exception ex)
        {
            // System error
            return FileProcessingResult.SystemError(
                "Failed to process file due to system error",
                ex
            );
        }
    }
}
```

## Step 4: Handle Results with Pattern Matching

Now consume the union with type-safe pattern matching:

### Example 1: HTTP API Response

```csharp
[ApiController]
[Route("api/files")]
public class FileUploadController : ControllerBase
{
    private readonly FileProcessor _processor;

    [HttpPost("upload")]
    public IActionResult UploadFile(IFormFile file)
    {
        var result = _processor.ProcessFile(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType
        );

        return result.Match(
            success => Ok(new
            {
                Message = "File processed successfully",
                FileName = success.FileName,
                FileSize = success.FileSize,
                ProcessedAt = success.ProcessedAt
            }),
            validationError => BadRequest(new
            {
                Message = validationError.Message,
                ErrorCode = validationError.ErrorCode.ToString()
            }),
            systemError => StatusCode(500, new
            {
                Message = "Internal server error",
                Details = systemError.Message
            })
        );
    }
}
```

### Example 2: Console Application

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var processor = new FileProcessor();
        
        foreach (var filePath in args)
        {
            using var stream = File.OpenRead(filePath);
            var fileName = Path.GetFileName(filePath);
            var contentType = GetContentType(fileName);
            
            var result = processor.ProcessFile(stream, fileName, contentType);
            
            // Match with side effects (no return value)
            result.Match(
                success => 
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ {success.FileName} processed successfully");
                    Console.WriteLine($"  Size: {FormatBytes(success.FileSize)}");
                    Console.WriteLine($"  Time: {success.ProcessedAt:yyyy-MM-dd HH:mm:ss}");
                    Console.ResetColor();
                },
                validationError =>
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"⚠ Validation error: {validationError.Message}");
                    Console.WriteLine($"  Code: {validationError.ErrorCode}");
                    Console.ResetColor();
                },
                systemError =>
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✗ System error: {systemError.Message}");
                    Console.WriteLine($"  Exception: {systemError.Exception.GetType().Name}");
                    Console.ResetColor();
                }
            );
        }
    }
    
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
```

### Example 3: Retry Logic with Variant Checks

```csharp
public async Task<FileProcessingResult> ProcessWithRetry(Stream file, string fileName, string contentType)
{
    const int maxRetries = 3;
    int attempt = 0;

    while (attempt < maxRetries)
    {
        var result = _processor.ProcessFile(file, fileName, contentType);

        // Only retry on system errors, not validation errors
        if (result.IsSystemError)
        {
            attempt++;
            if (attempt < maxRetries)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // Exponential backoff
                file.Position = 0; // Reset stream
                continue;
            }
        }

        return result;
    }

    return FileProcessingResult.SystemError(
        $"Failed after {maxRetries} attempts",
        new Exception("Max retries exceeded")
    );
}
```

### Example 4: TryGet Pattern

```csharp
public void SaveSuccessfulFiles(List<FileProcessingResult> results)
{
    foreach (var result in results)
    {
        // Only care about successful files
        if (result.TryGetSuccess(out var success))
        {
            database.SaveFile(new FileRecord
            {
                FileName = success.FileName,
                FileSize = success.FileSize,
                ContentType = success.ContentType,
                ProcessedAt = success.ProcessedAt
            });
        }
    }
}
```

## Step 5: Add Analytics

Track different outcomes:

```csharp
public class FileProcessingAnalytics
{
    public void TrackResult(FileProcessingResult result)
    {
        // Switch expression for categorization
        var category = result.Match(
            success => "success",
            validationError => $"validation_error_{validationError.ErrorCode}",
            systemError => $"system_error_{systemError.Exception.GetType().Name}"
        );

        // Log to analytics service
        analytics.Track("file_processed", new
        {
            Category = category,
            Timestamp = DateTime.UtcNow
        });
    }
}
```

## Step 6: Testing Your Union

Write comprehensive tests:

```csharp
public class FileProcessorTests
{
    private readonly FileProcessor _processor = new();

    [Fact]
    public void ProcessFile_ValidFile_ReturnsSuccess()
    {
        // Arrange
        var fileContent = CreateTestImage();
        using var stream = new MemoryStream(fileContent);

        // Act
        var result = _processor.ProcessFile(stream, "test.jpg", "image/jpeg");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.TryGetSuccess(out var success));
        Assert.Equal("test.jpg", success.FileName);
        Assert.Equal(fileContent.Length, success.FileSize);
    }

    [Fact]
    public void ProcessFile_EmptyFile_ReturnsValidationError()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        var result = _processor.ProcessFile(stream, "empty.jpg", "image/jpeg");

        // Assert
        Assert.True(result.IsValidationError);
        
        var errorMessage = result.Match(
            success => throw new Exception("Expected validation error"),
            validationError => validationError.Message,
            systemError => throw new Exception("Expected validation error")
        );
        
        Assert.Contains("empty", errorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProcessFile_FileTooLarge_ReturnsValidationError()
    {
        // Arrange
        var largeFile = new byte[11 * 1024 * 1024]; // 11 MB
        using var stream = new MemoryStream(largeFile);

        // Act
        var result = _processor.ProcessFile(stream, "large.jpg", "image/jpeg");

        // Assert
        result.Match(
            success => Assert.Fail("Expected validation error"),
            validationError =>
            {
                Assert.Equal(ValidationErrorCode.FileTooLarge, validationError.ErrorCode);
            },
            systemError => Assert.Fail("Expected validation error")
        );
    }
}
```

## What You've Learned

✅ **Problem definition**: Identify scenarios that need discriminated unions  
✅ **Union definition**: Use `[GenerateUnion]` with multiple variants  
✅ **Implementation**: Return union variants based on logic  
✅ **Pattern matching**: Handle all cases exhaustively  
✅ **Variant checks**: Use `Is*` and `TryGet*` for specific cases  
✅ **Testing**: Write tests for all union variants

## Next Steps

- [Common Patterns](./common-patterns.md) - Explore Result&lt;T,E&gt;, Option&lt;T&gt;, and state machines
<!-- - [Core Package Overview](../core-package/overview.md) - Learn about advanced features (Coming soon) -->
<!-- - [ASP.NET Core Integration](../integration-packages/aspnetcore/overview.md) - See union integration patterns (Coming soon) -->

## Tips & Best Practices

💡 **Name your variants clearly**: Use descriptive names like `Success`, `ValidationError`, not `Case1`, `Case2`  
💡 **Keep variants focused**: Each variant should represent one specific outcome  
💡 **Use meaningful data**: Include all relevant information in each variant  
💡 **Match exhaustively**: Always handle all cases in your Match calls  
💡 **Test all paths**: Write tests for every variant your code can return
