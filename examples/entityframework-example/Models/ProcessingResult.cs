using UnionGenerator.Attributes;

namespace EntityFrameworkExample.Models;

/// <summary>
/// Discriminated union representing the result of processing an order.
/// Can be either a successful processing (Success) or a failure with error details.
/// </summary>
[GenerateUnion]
public partial class ProcessingResult
{
    /// <summary>
    /// Creates a successful processing result.
    /// </summary>
    /// <param name="data">The processed data including ID, message and timestamp.</param>
    /// <returns>A ProcessingResult representing successful processing.</returns>
    public static ProcessingResult Success(ProcessedData data) => new SuccessCase(data);

    /// <summary>
    /// Creates a failed processing result.
    /// </summary>
    /// <param name="error">The error information including code, message and details.</param>
    /// <returns>A ProcessingResult representing failed processing.</returns>
    public static ProcessingResult Failed(ErrorInfo error) => new FailedCase(error);
}

/// <summary>
/// Represents successful processing data.
/// </summary>
/// <param name="ProcessedId">Unique identifier assigned during processing.</param>
/// <param name="Message">Human-readable success message.</param>
/// <param name="Timestamp">When the processing completed.</param>
public record ProcessedData(Guid ProcessedId, string Message, DateTime Timestamp);

/// <summary>
/// Represents error information for failed processing.
/// </summary>
/// <param name="Code">Machine-readable error code (e.g., "PAYMENT_DECLINED").</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Details">Additional details about the error (nullable).</param>
public record ErrorInfo(string Code, string Message, string? Details = null);

