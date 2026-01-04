# UnionGenerator.Analyzers.CodeFixes

Roslyn code fix providers for UnionGenerator analyzers. Provides automatic code fixes and refactorings for common union-related issues detected at compile time.

## 🚀 Quick Start

### 1. Install

```bash
dotnet add package UnionGenerator.Analyzers.CodeFixes
```

### 2. That's it!

Code fixes are automatically available in your IDE. Build your project and you'll see lightbulb icons (💡) in the editor for fixable diagnostics.

---

## 📚 Features

### ✅ Automatic Code Fixes
One-click fixes for analyzer warnings. Just press Ctrl+. (or Cmd+.) in your IDE.

### ✅ Safe Refactoring
Code fixes are validated and tested to preserve semantics.

### ✅ Zero Configuration
Works out-of-the-box with UnionGenerator.Analyzers. No setup required.

### ✅ IDE Integration
Full integration with Visual Studio, VS Code (with Roslyn extension), and Rider.

---

## 🔧 Supported Fixes

### Fix UG4010: Map Union to IActionResult

**Diagnostic**: Union result returned directly from controller without mapping.

**Before**:
```csharp
public Result<User, NotFoundError> GetUser(int id)
{
    return _service.GetUser(id);
}
```

**After (Generated)**:
```csharp
public IActionResult GetUser(int id)
{
    return _service.GetUser(id).ToActionResult();
}
```

**What Changed**:
- Return type changed from `Result<T, E>` to `IActionResult`
- `.ToActionResult()` call added (maps union to HTTP response)

**When Applied**:
- Lightbulb appears on method declaration
- Action: "Convert to IActionResult and add ToActionResult()"
- Applied to single method or all methods in controller via batch fix

---

### Fix UG4011: Add Status Code to Error Type

**Diagnostic**: Error type used in union doesn't have explicit status code.

**Before**:
```csharp
public class CustomError 
{ 
    public string Message { get; set; } 
}
```

**After (Option 1 - Attribute)**:
```csharp
[UnionStatusCode(400)]
public class CustomError 
{ 
    public string Message { get; set; } 
}
```

**After (Option 2 - Property)**:
```csharp
public class CustomError 
{ 
    public string Message { get; set; }
    public int StatusCode => 400;
}
```

**What Gets Fixed**:
- Adds `[UnionStatusCode(400)]` attribute (or configurable code)
- OR adds `StatusCode` property returning HTTP code
- Requires manual selection of which approach (attribute vs property)

**When Applied**:
- Lightbulb appears on error class declaration
- Options shown: "Add [UnionStatusCode] attribute" or "Add StatusCode property"

---

### Fix UG4012: Make Status Code Explicit

**Diagnostic**: Status code inferred by convention, but explicit is better.

**Before**:
```csharp
public class NotFoundError { }
```

**After**:
```csharp
[UnionStatusCode(404)]
public class NotFoundError { }
```

**What Changed**:
- Adds explicit `[UnionStatusCode]` based on naming convention
- Maps: `*NotFound*` → 404, `*BadRequest*` → 400, etc.

**When Applied**:
- Hidden by default (info diagnostic)
- Enable in .editorconfig if team prefers explicit code
- Lightbulb shows: "Make status code explicit"

---

## ⚙️ Configuration

### Enable Fixes in IDE

**Visual Studio 2022+**:
1. Tools → Options → Text Editor → C# → Code Analysis → General
2. Enable "Enable analyzers from NuGet packages"
3. Rebuild project

**VS Code with Roslyn Extension**:
1. Install "Roslyn Analyzers" extension
2. Reload window
3. Fixes appear automatically

**JetBrains Rider**:
1. Settings → Tools → Roslyn Analyzers
2. Enable "UnionGenerator.Analyzers.CodeFixes"
3. Rebuild project

### Configure Severity for Batch Fix

In `.editorconfig`:

```ini
# Show all fixable diagnostics
[**/*.cs]
dotnet_diagnostic.UG4010.severity = suggestion
dotnet_diagnostic.UG4011.severity = suggestion
dotnet_diagnostic.UG4012.severity = suggestion

# Or disable specific fixes
dotnet_diagnostic.UG4010.severity = silent  # Don't suggest IActionResult conversion
```

### Scope of Fixes

**Single Fix**: Apply to one occurrence
- Right-click diagnostic → "Quick Fix" → applies to current line

**Scope Fix**: Apply to current file
- In some IDEs: "Fix all in document"

**Project Fix**: Apply to entire project  
- Visual Studio: right-click solution, "Run Code Analysis and Fix"
- CLI: `dotnet fix --verbosity diag`

---

## 📋 Common Scenarios

### Scenario 1: Bulk Convert Controllers to IActionResult

**Problem**: All your controller methods return `Result<T, E>` instead of `IActionResult`.

**Solution**:
1. Open `.csproj` and add (temporary):
   ```xml
   <PropertyGroup>
       <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
   </PropertyGroup>
   ```
2. Build project (forces all UG4010 to appear)
3. Visual Studio → Analyze → Run Code Analysis → Fix Code
4. Remove `TreatWarningsAsErrors` after batch fix

**Result**: All controller methods now return `IActionResult` with `.ToActionResult()` calls.

---

### Scenario 2: Standardize Error Status Codes

**Problem**: Some error types have explicit `[UnionStatusCode]`, others rely on convention.

**Solution**:
1. In `.editorconfig`, enable UG4012:
   ```ini
   dotnet_diagnostic.UG4012.severity = suggestion
   ```
2. Build project
3. Apply fix "Make status code explicit" to all types
4. All errors now have explicit attributes

**Result**: Clear, consistent error status codes across codebase.

---

### Scenario 3: Add Missing Status Codes to New Errors

**Problem**: You create a new error type but forget to add status code.

**Solution**:
1. Build project (UG4011 warning appears)
2. Click lightbulb (💡) in editor
3. Choose "Add [UnionStatusCode] attribute"
4. Editor prompts for HTTP code
5. Code is inserted automatically

**Result**: Error type is immediately production-ready.

---

## 🔍 How Code Fixes Work

### Detection Phase

Code fix provider listens for these diagnostics:
- `UG4010` - Union not mapped to IActionResult
- `UG4011` - Error lacks status code
- `UG4012` - Convention override recommended

### Analysis Phase

For each diagnostic, the code fix:
1. Parses the syntax tree
2. Finds the target method/class
3. Analyzes current code structure
4. Determines appropriate fix

### Generation Phase

Code is generated and inserted:
- Preserves original formatting and style
- Maintains indentation
- Respects existing attributes/properties
- Updates using statements if needed

### Validation Phase

Generated code is validated:
- Syntax is valid C#
- No naming conflicts
- Type references are correct
- Compilation succeeds

---

## 🛠️ Advanced Usage

### Programmatic Code Fixing

If you want to apply fixes programmatically (e.g., in a build step):

```csharp
// Using Roslyn CodeFixProvider API
var workspace = MSBuildWorkspace.Create();
var project = await workspace.OpenProjectAsync("MyProject.csproj");

foreach (var document in project.Documents)
{
    var semanticModel = await document.GetSemanticModelAsync();
    var diagnostics = await document.GetDiagnosticsAsync();
    
    foreach (var diagnostic in diagnostics)
    {
        if (diagnostic.Id == "UG4010")
        {
            // Apply fix programmatically
            // Details: See Roslyn SDK docs
        }
    }
}
```

Note: This requires the Roslyn SDK and is beyond typical usage.

---

## 📊 Fix Application Workflow

```
Build Project
    ↓
Analyzer detects issue (UG4010, UG4011, UG4012)
    ↓
Code fix provider registers fix
    ↓
IDE shows lightbulb (💡)
    ↓
Developer clicks "Quick Fix" or "Apply Fix"
    ↓
Code is generated and inserted
    ↓
Project recompiles
    ↓
✅ Issue resolved
```

---

## 🎯 Best Practices

### ✅ DO

- Apply fixes as soon as diagnostics appear (don't let them pile up)
- Review generated code before committing (should be clean and idiomatic)
- Use batch fixes for large-scale refactoring (but review carefully)
- Keep analyzers enabled in CI/CD (prevents regression)
- Document why a fix was declined (if you suppress a diagnostic)

### ❌ DON'T

- Apply all fixes without reviewing (they're usually correct, but review first)
- Ignore batches of diagnostics and then try to apply all at once
- Use fixes as a substitute for proper error handling design (fix the root cause)
- Suppress diagnostics without documenting why
- Disable code analysis in production builds

---

## 🔗 Related Packages

- **UnionGenerator.Analyzers**: Diagnostic rules (UG4010, UG4011, UG4012)
- **UnionGenerator.AspNetCore**: Provides the `.ToActionResult()` extension that fixes depend on

---

## 🚨 Troubleshooting

### Lightbulb Not Appearing

**Problem**: No fix lightbulb shows up for diagnostics

**Solution**:
1. Ensure `UnionGenerator.Analyzers.CodeFixes` NuGet package is installed
2. Rebuild project: `dotnet clean && dotnet build`
3. Reload IDE window (Visual Studio: File → Recent Projects → Reopen)
4. Check IDE settings: Code analyzers must be enabled

### Fix Not Working as Expected

**Problem**: Applied fix produces incorrect code

**Solution**:
1. Undo the fix (Ctrl+Z)
2. Check C# language version (11+ recommended)
3. Verify `.editorconfig` isn't overriding fix behavior
4. Report issue with minimal reproduction case

### Status Code Not Suggested

**Problem**: Fix for UG4011 doesn't suggest the right status code

**Solution**:
1. UG4011 fix defaults to 400 (Bad Request)
2. Manually change to correct code if different
3. Or use naming convention (NotFound → 404) and apply UG4012 fix instead

### Batch Fix Partial Application

**Problem**: Batch fix applies to some files but not others

**Solution**:
1. May be due to syntax errors in some files
2. Fix syntax errors first: `dotnet build`
3. Re-run batch fix operation
4. Check build output for details

---

## 📄 License

MIT License - Part of UnionGenerator project

---

## ✨ Summary

| Feature | Benefit |
|---------|---------|
| **One-Click Fixes** | Faster refactoring |
| **Batch Operations** | Bulk modernization |
| **IDE Integrated** | Works in all major editors |
| **Safe Refactoring** | Preserves semantics |
| **Zero Config** | Works out-of-box |

**Quick action**: Install the package and press Ctrl+. on any analyzer warning to see available fixes! 🚀

