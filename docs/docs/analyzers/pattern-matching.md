---
sidebar_position: 2
---

# Pattern Matching Analyzers

Pattern matching analyzers ensure exhaustive case handling in switch expressions, switch statements, and Match() calls. They prevent runtime exceptions caused by unhandled union cases.

## 🎯 Overview

UnionGenerator provides two pattern matching analyzers:

| Diagnostic ID | Name | Severity |
|--------------|------|----------|
| **UG1001** | Incomplete pattern matching in Match() | Warning |
| **UG002** | Missing union case in switch | Warning |

Both analyzers detect when you don't handle all union cases and there's no catch-all pattern (`_` or `default`).

## UG1001: Incomplete Match() Calls

### What It Detects

Missing handler parameters in `.Match()` method calls.

### Example Problem

```csharp
[GenerateUnion]
public partial record Result<T>
{
    public static partial Result<T> Success(T value);
    public static partial Result<T> Error(string message);
}

var result = GetResult();

// ❌ UG1001: Missing 'error' parameter
var output = result.Match(
    success: value => $"Got: {value}"
);
```

### Diagnostic Message

> Not all union cases are handled in pattern matching. Missing case(s): Error.

### How to Fix

**Option 1: Handle all cases explicitly**

```csharp
// ✅ All cases handled
var output = result.Match(
    success: value => $"Got: {value}",
    error: message => $"Failed: {message}"
);
```

**Option 2: Use default parameter (if available)**

```csharp
// ✅ Use default handler
var output = result.Match(
    success: value => $"Got: {value}",
    @default: () => "Unknown result"
);
```

### Why This Matters

Without exhaustive checking, adding new union cases becomes dangerous:

```csharp
// Later, someone adds:
public static partial Result<T> Pending();

// ❌ Your existing code now has a bug - Pending case unhandled!
var output = result.Match(
    success: value => $"Got: {value}",
    error: message => $"Failed: {message}"
    // Runtime exception if result is Pending!
);
```

## UG002: Missing Switch Cases

### What It Detects

Switch expressions or statements that don't handle all union cases and lack a discard/default arm.

### Example Problems

#### Switch Expression

```csharp
[GenerateUnion]
public partial record PaymentStatus
{
    public static partial PaymentStatus Pending();
    public static partial PaymentStatus Completed(string txId);
    public static partial PaymentStatus Failed(string reason);
}

var status = GetPaymentStatus();

// ❌ UG002: Missing 'Failed' case
var message = status switch
{
    { IsPending: true } => "Processing...",
    { IsCompleted: true } completed => $"Done: {completed.Value}"
    // No Failed case and no discard arm!
};
```

#### Switch Statement

```csharp
// ❌ UG002: Missing 'Completed' case
switch (status)
{
    case { IsPending: true }:
        Console.WriteLine("Pending");
        break;
    case { IsFailed: true } failed:
        Console.WriteLine($"Error: {failed.Value}");
        break;
    // No default case!
}
```

### Diagnostic Message

> Switch on union 'PaymentStatus' does not handle all cases. Missing: Failed. Consider adding a pattern for the missing case(s) or a discard/default arm ('_').

### How to Fix

**Option 1: Handle all cases explicitly**

```csharp
// ✅ All cases covered
var message = status switch
{
    { IsPending: true } => "Processing...",
    { IsCompleted: true } completed => $"Done: {completed.Value}",
    { IsFailed: true } failed => $"Error: {failed.Value}"
};
```

**Option 2: Add discard arm**

```csharp
// ✅ Use discard for remaining cases
var message = status switch
{
    { IsPending: true } => "Processing...",
    { IsCompleted: true } completed => $"Done: {completed.Value}",
    _ => "Unknown" // Handles Failed and future cases
};
```

**Option 3: Add default case (statements)**

```csharp
// ✅ Default case added
switch (status)
{
    case { IsPending: true }:
        Console.WriteLine("Pending");
        break;
    case { IsFailed: true } failed:
        Console.WriteLine($"Error: {failed.Value}");
        break;
    default:
        Console.WriteLine("Other");
        break;
}
```

### Detection Modes

The analyzer detects incomplete matching in:

#### 1. Property Pattern Matching

```csharp
status switch
{
    { IsPending: true } => ...,
    { IsCompleted: true } => ...
    // Missing IsFailed check
};
```

#### 2. Type Pattern Matching (C# 9+)

```csharp
status switch
{
    { IsPending: true } pending => ...,
    { IsCompleted: true } completed => ...
    // Missing Failed pattern
};
```

#### 3. If-Else Chains

```csharp
// ⚠️ Also detected in if-else chains
if (status.IsPending)
{
    // Handle pending
}
else if (status.IsCompleted)
{
    // Handle completed
}
// Missing else for Failed case!
```

## 🔧 Configuration

### Change Severity

In `.editorconfig`:

```ini
# Treat incomplete matching as error
dotnet_diagnostic.UG1001.severity = error
dotnet_diagnostic.UG002.severity = error
```

### Disable for Specific Code

```csharp
#pragma warning disable UG1001, UG002
var result = status switch
{
    { IsPending: true } => "Pending",
    _ => "Other" // Intentionally generic
};
#pragma warning restore UG1001, UG002
```

### Global Disable (Not Recommended)

In `.editorconfig`:

```ini
# Disable pattern matching warnings
dotnet_diagnostic.UG1001.severity = none
dotnet_diagnostic.UG002.severity = none
```

:::danger
Disabling these analyzers removes compile-time safety and makes refactoring dangerous.
:::

## 📊 Performance Impact

These analyzers have **minimal performance impact**:

- Run only on switch expressions/statements and Match() calls
- Use efficient symbol-based analysis
- Skip generated code automatically
- Enable concurrent execution

Typical overhead: &lt;50ms per thousand lines of code.

## 💡 Best Practices

### 1. Always Handle All Cases Explicitly

```csharp
// ✅ Preferred: Explicit handling
var result = status.Match(
    pending: () => "Pending",
    completed: tx => $"Success: {tx}",
    failed: reason => $"Error: {reason}"
);

// ⚠️ Avoid: Generic catch-all
var result = status.Match(
    pending: () => "Pending",
    @default: () => "Other" // Loses type information
);
```

### 2. Use Discard Only When Appropriate

Discard arms are acceptable when:
- Multiple cases should have identical handling
- You genuinely don't care about distinguishing cases
- Handling "any other case" is semantically correct

```csharp
// ✅ Legitimate use of discard
var isActive = status switch
{
    { IsPending: true } => true,
    { IsCompleted: true } => true,
    _ => false // Any other case means inactive
};
```

### 3. Review Warnings When Adding Cases

When adding new union cases:

1. Build the solution
2. Review all UG1001/UG002 warnings
3. Update each match site appropriately
4. Don't blindly add `_ => ...` to silence warnings

### 4. Document Intentional Suppressions

```csharp
// This API deliberately returns a generic message for all errors.
// Specific error details are logged separately.
#pragma warning disable UG1001
return result.Match(
    success: data => data,
    @default: () => throw new InvalidOperationException("Operation failed")
);
#pragma warning restore UG1001
```

## 🐛 Common False Positives

### Nested Switches

The analyzer may not detect exhaustiveness in nested switch expressions:

```csharp
// May trigger UG002 even though all cases are covered
var result = (outerStatus, innerStatus) switch
{
    ({ IsPending: true }, _) => "Outer pending",
    (_, { IsPending: true }) => "Inner pending",
    ({ IsCompleted: true }, { IsCompleted: true }) => "Both completed",
    // Complex nesting may confuse analyzer
};
```

**Workaround**: Add explicit `_ => throw new UnreachableException()` arm.

### Generic Union Types with Constraints

```csharp
[GenerateUnion]
public partial record Option<T> where T : class
{
    public static partial Option<T> Some(T value);
    public static partial Option<T> None();
}

// May not always detect exhaustiveness in generic contexts
```

**Workaround**: Ensure all cases are explicitly handled.

## 🔍 Technical Details

### How Detection Works

1. **Symbol Analysis**: Identifies union types by `[GenerateUnion]` attribute
2. **Case Discovery**: Enumerates all static factory methods as union cases
3. **Pattern Analysis**: Parses switch arms/Match parameters
4. **Coverage Check**: Compares handled cases vs. defined cases
5. **Diagnostic Reporting**: Reports missing cases if no catch-all exists

### Limitations

- **Indirect matching**: Analyzer doesn't track if-else chains that call helper methods
- **Runtime cases**: Cases determined at runtime can't be analyzed statically
- **External unions**: Only analyzes unions defined in the current compilation

## 📚 Related Topics

- [Factory Method Analyzers](./factory-methods) - Validates union definitions
- [Code Fixes](./code-fixes) - Automatic exhaustiveness fixes
- [Pattern Matching Guide](../core-package/pattern-matching) - Pattern matching features

## 🎓 Examples

### Complete Real-World Example

```csharp
[GenerateUnion]
public partial record ApiResult<T>
{
    public static partial ApiResult<T> Success(T data);
    public static partial ApiResult<T> NotFound();
    public static partial ApiResult<T> Unauthorized();
    public static partial ApiResult<T> ServerError(string message);
}

public IActionResult HandleResult<T>(ApiResult<T> result)
{
    // ✅ All cases handled - no warning
    return result.Match(
        success: data => Ok(data),
        notFound: () => NotFound(),
        unauthorized: () => Unauthorized(),
        serverError: msg => StatusCode(500, msg)
    );
}

public string GetStatusMessage<T>(ApiResult<T> result)
{
    // ✅ Discard is appropriate here - all errors treated same
    return result switch
    {
        { IsSuccess: true } => "Operation succeeded",
        _ => "Operation failed"
    };
}

public void LogResult<T>(ApiResult<T> result)
{
    // ⚠️ UG002: Missing ServerError case
    switch (result)
    {
        case { IsSuccess: true } success:
            _logger.LogInformation("Success: {Data}", success.Value);
            break;
        case { IsNotFound: true }:
            _logger.LogWarning("Not found");
            break;
        case { IsUnauthorized: true }:
            _logger.LogWarning("Unauthorized");
            break;
        // Missing ServerError case - analyzer warns!
    }
}
```

## 🚨 Troubleshooting

### Warning Doesn't Appear

**Check**:
- Analyzer package is installed
- Build is not suppressing warnings
- Diagnostic severity is not set to `none`

**Verify**:
```bash
dotnet build --verbosity diagnostic | grep "UG1001\|UG002"
```

### False Positive Warnings

**Report an issue**: [GitHub Issues](https://github.com/selcukgural/UnionGenerator/issues)

Include:
- Code sample reproducing the issue
- Expected behavior
- Actual diagnostic message
