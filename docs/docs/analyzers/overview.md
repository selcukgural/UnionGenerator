---
sidebar_position: 1
---

# Analyzers Overview

UnionGenerator includes powerful Roslyn analyzers that provide compile-time safety and help you write better union code. These analyzers catch common mistakes, ensure exhaustive pattern matching, and suggest improvements.

## 📦 Available Analyzer Packages

### UnionGenerator.Analyzers

The main analyzer package provides diagnostics and code fixes for:

- **Pattern matching completeness** - Ensures all union cases are handled
- **Union factory method validation** - Validates factory method signatures
- **ASP.NET Core integration** - Detects misuse in controllers and minimal APIs
- **Debug visualization** - Suggests adding debugger attributes
- **OneOf migration patterns** - Helps migrate from OneOf library

Install via NuGet:

```bash
dotnet add package UnionGenerator.Analyzers
```

### Automatic Installation

The analyzers are automatically included when you install:
- `UnionGenerator` (includes core analyzers)
- `UnionGenerator.AspNetCore` (includes ASP.NET Core analyzers)

## 🎯 Why Use Analyzers?

### Compile-Time Safety

Catch errors before they reach production:

```csharp
var result = GetResult();

// ❌ Analyzer warning: Missing case handling
var value = result switch
{
    { IsSuccess: true } success => success.Value
    // Missing error case - analyzer warns you!
};
```

### Exhaustive Pattern Matching

Ensure all union cases are covered:

```csharp
[GenerateUnion]
public partial record PaymentStatus
{
    public static partial PaymentStatus Pending();
    public static partial PaymentStatus Completed(string transactionId);
    public static partial PaymentStatus Failed(string reason);
}

// ✅ Analyzer ensures all cases are handled
var message = status.Match(
    pending: () => "Processing...",
    completed: tx => $"Success: {tx}",
    failed: reason => $"Failed: {reason}"
);
```

### Maintainability

When you add new union cases, analyzers automatically warn you about missing handlers:

```csharp
// You add a new case:
public static partial PaymentStatus Refunded(decimal amount);

// ✅ Analyzer immediately warns about all switch statements
// that don't handle the new 'Refunded' case
```

## 📊 Diagnostic Severity Levels

| Severity | Icon | Description |
|----------|------|-------------|
| **Error** | 🔴 | Code won't compile - must be fixed |
| **Warning** | 🟡 | Potential bug - should be fixed |
| **Info** | 🔵 | Suggestion for improvement |
| **Hidden** | ⚪ | Available but not shown unless configured |

## 🛠️ Configuration

### Enable/Disable Specific Analyzers

In your `.editorconfig`:

```ini
# Disable specific analyzer
dotnet_diagnostic.UG1001.severity = none

# Change severity level
dotnet_diagnostic.UG002.severity = error

# Set category-wide severity
dotnet_analyzer_diagnostic.category-Usage.severity = warning
```

### Project-Wide Configuration

In your `.csproj`:

```xml
<PropertyGroup>
  <!-- Treat all analyzer warnings as errors -->
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  
  <!-- Disable specific diagnostics -->
  <NoWarn>UG3001;UG3002</NoWarn>
</PropertyGroup>
```

### Suppress in Code

Use `#pragma` directives:

```csharp
#pragma warning disable UG1001 // Incomplete pattern matching
var result = value switch
{
    { IsSuccess: true } => "OK",
    _ => "Error" // Generic handler is intentional
};
#pragma warning restore UG1001
```

Or use attributes:

```csharp
[SuppressMessage("UnionGenerator", "UG1001:Incomplete pattern matching")]
public string ProcessResult(Result result)
{
    // ...
}
```

## 📋 Diagnostic ID Reference

| ID | Severity | Description | Package |
|----|----------|-------------|---------|
| **UG1001** | Warning | Incomplete pattern matching in Match() calls | Core |
| **UG002** | Warning | Missing union case in switch expressions | Core |
| **UG3001** | Info | Consider using generated adapter for OneOf | Analyzers |
| **UG3002** | Info | Add debugger visualization attributes | Analyzers |
| **UG4010** | Info | Union not mapped to IActionResult | ASP.NET Core |
| **UG4011** | Info | Error case without status code | ASP.NET Core |
| **UG4012** | Hidden | Convention override recommended | ASP.NET Core |
| **UG9002** | Warning | No union cases found | Core |
| **UG9003** | Warning | Factory method has multiple parameters | Core |
| **UG9004** | Warning | Duplicate union case signature | Core |

## 🚀 Quick Start

1. **Install the analyzer package**:

```bash
dotnet add package UnionGenerator.Analyzers
```

2. **Write union code**:

```csharp
[GenerateUnion]
public partial record ApiResponse
{
    public static partial ApiResponse Success(Data data);
    public static partial ApiResponse NotFound();
    public static partial ApiResponse Error(string message);
}
```

3. **See warnings for incomplete matching**:

```csharp
// ⚠️ UG1001: Missing 'Error' case
var result = response.Match(
    success: data => "OK",
    notFound: () => "Not Found"
    // Error case missing - analyzer warns!
);
```

4. **Apply code fix** (Ctrl+. or Cmd+.):

```csharp
// ✅ Fixed automatically by code fix provider
var result = response.Match(
    success: data => "OK",
    notFound: () => "Not Found",
    error: message => $"Error: {message}"
);
```

## 🔍 IDE Integration

### Visual Studio

Analyzers appear as:
- Squiggly underlines in the editor
- Entries in the Error List window
- Light bulb suggestions (Ctrl+.)

### JetBrains Rider

Analyzers integrate with:
- ReSharper inspections
- Solution-wide analysis
- Code cleanup profiles

### VS Code

Requires C# extension (OmniSharp):
- Inline diagnostics
- Problems panel
- Quick fix suggestions

## 📚 Learn More

- [Pattern Matching Analyzers](./pattern-matching) - Exhaustive matching enforcement
- [Factory Method Analyzers](./factory-methods) - Union definition validation
- [ASP.NET Core Analyzers](./aspnetcore) - Web API integration checks
- [Code Fixes](./code-fixes) - Automatic refactoring tools

## 💡 Best Practices

### 1. Treat Warnings as Errors in CI/CD

```xml
<PropertyGroup>
  <TreatWarningsAsErrors Condition="'$(Configuration)' == 'Release'">true</TreatWarningsAsErrors>
</PropertyGroup>
```

### 2. Use .editorconfig for Team Standards

```ini
# Team-wide analyzer settings
[*.cs]
dotnet_diagnostic.UG1001.severity = error
dotnet_diagnostic.UG002.severity = error
```

### 3. Review Analyzer Warnings Regularly

- Don't suppress warnings without good reason
- Document suppressions with comments
- Periodically review suppressed diagnostics

### 4. Keep Analyzers Updated

```bash
dotnet list package --outdated
dotnet add package UnionGenerator.Analyzers
```

## 🎓 Next Steps

- Learn about [pattern matching analyzers](./pattern-matching) in detail
- Explore [code fix providers](./code-fixes) for automatic refactoring
- Configure [ASP.NET Core analyzers](./aspnetcore) for web projects
