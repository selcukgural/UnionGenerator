---
sidebar_position: 4
---

# Code Fixes

UnionGenerator provides automatic code fixes that help you quickly resolve analyzer warnings. These refactorings appear as light bulb suggestions in your IDE.

## 🎯 Overview

Code fix providers automatically generate solutions for analyzer diagnostics. Press **Ctrl+.** (Windows/Linux) or **Cmd+.** (Mac) when your cursor is on a diagnostic to see available fixes.

## 🔧 Available Code Fixes

### 1. Add Missing Match Cases

**Triggers on**: UG1001 (Incomplete pattern matching)

**What it does**: Automatically adds missing handler parameters to `.Match()` calls.

#### Before

```csharp
[GenerateUnion]
public partial record Result<T>
{
    public static partial Result<T> Success(T value);
    public static partial Result<T> Error(string message);
}

// ⚠️ UG1001: Missing 'error' parameter
var output = result.Match(
    success: value => $"Value: {value}"
);
```

#### After (Code Fix Applied)

```csharp
// ✅ All cases handled
var output = result.Match(
    success: value => $"Value: {value}",
    error: message => throw new NotImplementedException()
);
```

**Default behavior**: Inserts `throw new NotImplementedException()` for missing handlers, prompting you to implement the logic.

### 2. Add Missing Switch Arms

**Triggers on**: UG002 (Missing union case in switch)

**What it does**: Adds missing pattern arms to switch expressions.

#### Before

```csharp
// ⚠️ UG002: Missing 'Failed' case
var message = status switch
{
    { IsPending: true } => "Processing",
    { IsCompleted: true } => "Done"
};
```

#### After (Code Fix Applied)

```csharp
// ✅ All cases covered
var message = status switch
{
    { IsPending: true } => "Processing",
    { IsCompleted: true } => "Done",
    { IsFailed: true } => throw new NotImplementedException()
};
```

### 3. Add Discard Arm

**Triggers on**: UG002 (Missing union case in switch)

**What it does**: Adds a catch-all discard pattern (`_`) instead of explicit cases.

#### Before

```csharp
// ⚠️ UG002: Missing cases
var isSuccessful = result switch
{
    { IsSuccess: true } => true
};
```

#### After (Code Fix Applied - Option 2)

```csharp
// ✅ Discard handles remaining cases
var isSuccessful = result switch
{
    { IsSuccess: true } => true,
    _ => false
};
```

### 4. Add Debugger Visualization

**Triggers on**: UG3002 (Missing debugger attributes)

**What it does**: Adds `[DebuggerDisplay]` and `[DebuggerTypeProxy]` attributes to union types.

#### Before

```csharp
// ⚠️ UG3002: Missing debugger visualization
[GenerateUnion]
public partial record ApiResult
{
    public static partial ApiResult Success(Data data);
    public static partial ApiResult Error(string message);
}
```

#### After (Code Fix Applied)

```csharp
// ✅ Debug visualization added
[DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
[DebuggerTypeProxy(typeof(ApiResultDebugView))]
[GenerateUnion]
public partial record ApiResult
{
    public static partial ApiResult Success(Data data);
    public static partial ApiResult Error(string message);
    
    private string GetDebuggerDisplay() =>
        Match(
            success: d => $"Success: {d}",
            error: e => $"Error: {e}"
        );
}

internal class ApiResultDebugView
{
    private readonly ApiResult _result;
    
    public ApiResultDebugView(ApiResult result)
    {
        _result = result;
    }
    
    public string CurrentCase => _result.Match(
        success: _ => nameof(Success),
        error: _ => nameof(Error)
    );
    
    public object? Value => _result.Match<object?>(
        success: d => d,
        error: e => e
    );
}
```

### 5. Use Generated OneOf Adapter

**Triggers on**: UG3001 (Repetitive OneOf if/else chain)

**What it does**: Replaces `if/else` chains checking `IsT0`, `IsT1` with generated adapter methods.

#### Before

```csharp
// ⚠️ UG3001: Can be simplified
OneOf<string, int> oneOfValue = GetValue();

if (oneOfValue.IsT0)
{
    var str = oneOfValue.AsT0;
    Console.WriteLine($"String: {str}");
}
else if (oneOfValue.IsT1)
{
    var num = oneOfValue.AsT1;
    Console.WriteLine($"Number: {num}");
}
```

#### After (Code Fix Applied)

```csharp
// ✅ Using generated adapter
OneOf<string, int> oneOfValue = GetValue();
var result = MyUnion.FromOneOf(oneOfValue);

result.Match(
    case1: str => Console.WriteLine($"String: {str}"),
    case2: num => Console.WriteLine($"Number: {num}")
);
```

## 🎨 IDE Integration

### Visual Studio

1. Position cursor on the warning squiggle
2. Click the light bulb icon or press **Ctrl+.**
3. Select the code fix from the menu
4. Press **Enter** to apply

**Preview changes**: Press **Ctrl+.**, then use arrow keys to navigate, and press **Space** to preview before applying.

### JetBrains Rider

1. Position cursor on the warning
2. Press **Alt+Enter** (Windows/Linux) or **Option+Enter** (Mac)
3. Select "Fix all issues of this type in file" for bulk fixes
4. Press **Enter** to apply

**Preview**: Rider shows a diff preview before applying.

### VS Code

1. Position cursor on the diagnostic
2. Click the light bulb or press **Ctrl+.** (**Cmd+.** on Mac)
3. Select the code fix
4. Press **Enter**

**Requires**: C# extension (OmniSharp)

## 🔄 Batch Code Fixes

Apply fixes to multiple locations simultaneously.

### Fix All in Document

**Visual Studio**: Ctrl+. → "Fix all occurrences in Document"

**Rider**: Alt+Enter → "Fix all issues of this type in file"

### Fix All in Project/Solution

**Visual Studio**:
1. Error List → Right-click diagnostic
2. "Fix all occurrences in Project/Solution"

**Rider**:
1. Solution-wide analysis (Alt+` → Enable)
2. Inspection Results → Right-click → "Fix all"

### Example: Bulk Fix

Suppose you add a new case to an existing union:

```csharp
[GenerateUnion]
public partial record Status
{
    public static partial Status Pending();
    public static partial Status Active();
    // ➕ New case added
    public static partial Status Suspended();
}
```

Now all existing `Match()` calls show UG1001 warnings. Apply batch fix:

**Before** (20 locations):
```csharp
status.Match(
    pending: () => "Pending",
    active: () => "Active"
);
```

**After** (one action fixes all 20):
```csharp
status.Match(
    pending: () => "Pending",
    active: () => "Active",
    suspended: () => throw new NotImplementedException()
);
```

Then manually implement each new `suspended` handler.

## ⚙️ Customizing Code Fix Behavior

### Change Default Handler

By default, missing handlers use `throw new NotImplementedException()`. Customize this:

#### Option 1: Use EditorConfig Snippets

```ini
# .editorconfig
[*.cs]

# Default TODO comment
csharp_code_fix_missing_case_handler = // TODO: Implement {0} case
```

:::note
Custom handler templates are not yet supported but are planned for future releases.
:::

#### Option 2: Configure Post-Fix Behavior

After applying a code fix:
1. Use **Find and Replace** to update all `NotImplementedException` instances
2. Or manually implement each handler

```csharp
// After bulk fix, use regex find-replace:
// Find:    throw new NotImplementedException\(\)
// Replace: HandleUnknownCase()
```

### Disable Specific Code Fixes

In `.editorconfig`:

```ini
# Disable code fix for UG3001
dotnet_code_fix.UG3001.enabled = false
```

Or suppress the underlying diagnostic:

```ini
dotnet_diagnostic.UG3001.severity = none
```

## 💡 Best Practices

### 1. Apply Fixes Incrementally for New Cases

When adding a new union case:

```csharp
// 1. Add the case
public static partial Result Timeout();

// 2. Build to see all UG1001/UG002 warnings

// 3. Review each location and decide:
//    - Should this handle Timeout explicitly?
//    - Can Timeout be grouped with another case?

// 4. Apply fixes one by one, not in bulk
```

### 2. Replace NotImplementedException Immediately

```csharp
// ❌ Don't leave this in production code
result.Match(
    success: v => v,
    error: e => throw new NotImplementedException()
);

// ✅ Replace with actual logic
result.Match(
    success: v => v,
    error: e => LogAndReturnDefault(e)
);
```

### 3. Use TODO Comments

If you can't implement immediately:

```csharp
result.Match(
    success: v => v,
    error: e => 
    {
        // TODO: Implement proper error handling for production
        throw new NotImplementedException();
    }
);
```

Add to your CI pipeline:

```bash
# Fail build if TODO comments exist
dotnet build && ! grep -r "TODO.*NotImplementedException" src/
```

### 4. Review Discard Arms Carefully

Code fix might suggest discard arms:

```csharp
// ✅ Acceptable when remaining cases should be treated identically
var isError = result switch
{
    { IsSuccess: true } => false,
    _ => true // All errors treated as true
};

// ⚠️ Review carefully - might hide future bugs
var message = result switch
{
    { IsSuccess: true } s => s.Value,
    _ => "Error" // Loses specific error information!
};
```

## 🐛 Troubleshooting

### Code Fix Not Appearing

**Check**:
1. Analyzer package is installed
2. Build the project to ensure diagnostics are current
3. Cursor is positioned on the diagnostic
4. IDE extension for C# is up to date

**Verify**:
```bash
dotnet list package | grep UnionGenerator.Analyzers
```

### Code Fix Generates Invalid Code

**Report an issue**: [GitHub Issues](https://github.com/selcukgural/UnionGenerator/issues)

Include:
- Original code snippet
- Diagnostic ID
- Generated code after fix
- Expected result

### Batch Fix Doesn't Work

**Limitations**:
- Batch fixes only work within the same diagnostic ID
- Cross-file fixes may require project rebuild
- Some IDEs limit batch operations

**Workaround**:
1. Apply fix in one file
2. Build project
3. Repeat for next file

## 🔍 Technical Details

### How Code Fixes Work

1. **Trigger**: Analyzer reports diagnostic (UG1001, UG002, etc.)
2. **Registration**: Code fix provider registers available fixes
3. **User Action**: User invokes light bulb menu
4. **Code Action**: Provider generates new code using Roslyn APIs
5. **Application**: IDE applies text changes to document

### Performance Considerations

- Code fixes compute on-demand (not on every keystroke)
- Lazy evaluation until light bulb is opened
- Minimal performance impact on IDE
- Complex fixes may take 100-500ms to compute

### Supported Scenarios

Code fixes handle:
- ✅ Switch expressions and statements
- ✅ Match() method calls
- ✅ Generic union types
- ✅ Nested unions
- ✅ Partial matches with default arms

Not yet supported:
- ❌ If-else chain refactoring (UG1001 in if statements)
- ❌ Custom handler templates
- ❌ Smart case grouping suggestions

## 📚 Related Topics

- [Pattern Matching Analyzers](./pattern-matching) - Diagnostics that trigger fixes
- [Factory Method Analyzers](./factory-methods) - Union definition validation
- [Best Practices](../core-package/best-practices) - Union design patterns

## 🎓 Complete Example

```csharp
// Initial union
[GenerateUnion]
public partial record TaskStatus
{
    public static partial TaskStatus NotStarted();
    public static partial TaskStatus InProgress(int percentComplete);
    public static partial TaskStatus Completed(DateTime completedAt);
}

// Code using the union
public string GetStatusMessage(TaskStatus status)
{
    return status.Match(
        notStarted: () => "Not started yet",
        inProgress: percent => $"{percent}% complete",
        completed: dt => $"Done at {dt:g}"
    );
}

// ➕ Later: Add new case
public static partial TaskStatus Failed(string error);

// ⚠️ Now GetStatusMessage shows UG1001 warning

// Apply code fix (Ctrl+.)
public string GetStatusMessage(TaskStatus status)
{
    return status.Match(
        notStarted: () => "Not started yet",
        inProgress: percent => $"{percent}% complete",
        completed: dt => $"Done at {dt:g}",
        failed: error => throw new NotImplementedException() // ✅ Added by code fix
    );
}

// ✅ Manually replace NotImplementedException
public string GetStatusMessage(TaskStatus status)
{
    return status.Match(
        notStarted: () => "Not started yet",
        inProgress: percent => $"{percent}% complete",
        completed: dt => $"Done at {dt:g}",
        failed: error => $"Failed: {error}" // ✅ Implemented
    );
}
```

## 🚀 Next Steps

- Learn about [pattern matching analyzers](./pattern-matching) that trigger code fixes
- Explore [ASP.NET Core analyzers](./aspnetcore) for web-specific refactorings
- Review [best practices](../core-package/best-practices) for union design
