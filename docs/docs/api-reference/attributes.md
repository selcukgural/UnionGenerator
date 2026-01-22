---
sidebar_position: 3
---

# Attributes Reference

Complete reference for all attributes provided by UnionGenerator.

## 📋 Available Attributes

UnionGenerator currently provides one core attribute:

- `[GenerateUnion]` - Marks a class for union type generation

Future versions may include additional attributes for customization.

## 🏷️ GenerateUnion Attribute

The `[GenerateUnion]` attribute is the core of UnionGenerator. Apply it to a partial class to generate a discriminated union type.

### Namespace

```csharp
using UnionGenerator.Attributes;
```

### Definition

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateUnionAttribute : Attribute
{
}
```

### Basic Usage

```csharp
using UnionGenerator.Attributes;

[GenerateUnion]
public partial class Result<T, TError>
{
    public static partial Result<T, TError> Ok(T value);
    public static partial Result<T, TError> Error(TError error);
}
```

## 🎯 Attribute Target

### Valid Targets
- **Classes only**: Must be applied to a `class` declaration
- **Partial required**: The class must be marked `partial`
- **One per class**: Cannot apply multiple times to the same class

### Invalid Targets
```csharp
// ❌ Not a partial class
[GenerateUnion]
public class MyUnion { } // Compilation error

// ❌ Applied to struct
[GenerateUnion]
public partial struct MyUnion { } // Compilation error

// ❌ Applied to interface
[GenerateUnion]
public interface IMyUnion { } // Compilation error

// ❌ Applied to record
[GenerateUnion]
public partial record MyUnion { } // Compilation error
```

### Valid Examples
```csharp
// ✅ Simple class
[GenerateUnion]
public partial class SimpleUnion
{
    public static partial SimpleUnion CaseA();
    public static partial SimpleUnion CaseB(string value);
}

// ✅ Generic class
[GenerateUnion]
public partial class GenericUnion<T>
{
    public static partial GenericUnion<T> Some(T value);
    public static partial GenericUnion<T> None();
}

// ✅ Nested class
public class Container
{
    [GenerateUnion]
    public partial class NestedUnion
    {
        public static partial NestedUnion Success();
        public static partial NestedUnion Failure();
    }
}

// ✅ With generic constraints
[GenerateUnion]
public partial class ConstrainedUnion<T> where T : class
{
    public static partial ConstrainedUnion<T> Value(T item);
    public static partial ConstrainedUnion<T> Empty();
}
```

## 📐 Requirements

### Class Requirements

For the attribute to work correctly, your class must meet these requirements:

#### 1. Must be Partial

```csharp
// ✅ Correct
[GenerateUnion]
public partial class MyUnion { }

// ❌ Wrong - Missing partial keyword
[GenerateUnion]
public class MyUnion { }
```

**Why:** The source generator adds members to your class. The `partial` keyword allows this.

#### 2. Must Have Static Partial Methods

```csharp
[GenerateUnion]
public partial class MyUnion
{
    // ✅ Correct - static partial
    public static partial MyUnion Success(int value);
    
    // ❌ Wrong - not static
    public partial MyUnion Failed();
    
    // ❌ Wrong - not partial
    public static MyUnion Pending();
}
```

**Why:** Each method becomes a factory method for creating union instances.

#### 3. Methods Must Return the Union Type

```csharp
[GenerateUnion]
public partial class Result<T>
{
    // ✅ Correct - returns Result<T>
    public static partial Result<T> Ok(T value);
    
    // ❌ Wrong - returns void
    public static partial void Error(string message);
    
    // ❌ Wrong - returns wrong type
    public static partial string Failed(string reason);
}
```

#### 4. At Least One Case Required

```csharp
// ❌ Wrong - No cases defined
[GenerateUnion]
public partial class EmptyUnion
{
}

// ✅ Correct - At least one case
[GenerateUnion]
public partial class ValidUnion
{
    public static partial ValidUnion SingleCase();
}
```

### Parameter Requirements

Case factory methods can have:
- **Zero or more parameters**
- **Any parameter types** (including generics, nullable, etc.)
- **Any parameter names** (used as field names in generated code)

```csharp
[GenerateUnion]
public partial class FlexibleUnion<T>
{
    // No parameters
    public static partial FlexibleUnion<T> Empty();
    
    // Single parameter
    public static partial FlexibleUnion<T> Single(T value);
    
    // Multiple parameters
    public static partial FlexibleUnion<T> Multiple(T value, string name, int count);
    
    // Generic parameters
    public static partial FlexibleUnion<T> Generic<TData>(TData data);
    
    // Nullable parameters
    public static partial FlexibleUnion<T> Nullable(T? value);
    
    // Complex types
    public static partial FlexibleUnion<T> Complex(List<T> items, Dictionary<string, int> map);
}
```

## 🔧 Advanced Scenarios

### Inheritance

Union classes can inherit from other classes or implement interfaces:

```csharp
public interface IValidationResult
{
    bool IsValid { get; }
}

[GenerateUnion]
public partial class ValidationResult : IValidationResult
{
    public static partial ValidationResult Success();
    public static partial ValidationResult Failed(string error);
    
    // Must implement interface members
    public bool IsValid => IsSuccess;
}
```

**Note:** You cannot inherit from another union type.

### Multiple Generic Parameters

```csharp
[GenerateUnion]
public partial class BiResult<TOk, TError, TWarning>
{
    public static partial BiResult<TOk, TError, TWarning> Ok(TOk value);
    public static partial BiResult<TOk, TError, TWarning> Error(TError error);
    public static partial BiResult<TOk, TError, TWarning> Warning(TWarning warning);
}
```

### Generic Constraints

All generic constraints are preserved:

```csharp
[GenerateUnion]
public partial class ConstrainedResult<T, TError>
    where T : IComparable<T>
    where TError : Exception, new()
{
    public static partial ConstrainedResult<T, TError> Ok(T value);
    public static partial ConstrainedResult<T, TError> Error(TError error);
}
```

### Nested Generics

```csharp
[GenerateUnion]
public partial class NestedGeneric<T>
{
    // Generic case method
    public static partial NestedGeneric<T> Wrapped<TInner>(TInner value);
    
    // Using outer generic
    public static partial NestedGeneric<T> Direct(T value);
}
```

### Nullable Reference Types

UnionGenerator respects nullable reference type annotations:

```csharp
#nullable enable

[GenerateUnion]
public partial class NullableAwareUnion
{
    // Non-nullable string
    public static partial NullableAwareUnion Valid(string name);
    
    // Nullable string
    public static partial NullableAwareUnion Maybe(string? name);
    
    // Non-nullable with nullable generic
    public static partial NullableAwareUnion GenericNullable<T>(T? value) where T : class;
}

#nullable restore
```

## 🎨 Naming Conventions

### Case Names

Case names (method names) should be:
- **PascalCase**: `Success`, `Error`, `NotFound`
- **Descriptive**: Clearly indicate what the case represents
- **Concise**: Avoid overly long names

```csharp
// ✅ Good case names
[GenerateUnion]
public partial class HttpResult<T>
{
    public static partial HttpResult<T> Success(T data);
    public static partial HttpResult<T> NotFound();
    public static partial HttpResult<T> Unauthorized(string message);
    public static partial HttpResult<T> ServerError(Exception exception);
}

// ❌ Poor case names
[GenerateUnion]
public partial class BadNaming<T>
{
    public static partial BadNaming<T> case1(T data); // Not PascalCase
    public static partial BadNaming<T> NOTFOUND(); // All caps
    public static partial BadNaming<T> TheUnauthorizedCaseWhereTheUserDoesNotHavePermission(); // Too long
}
```

### Parameter Names

Parameter names become field names in generated case classes:
- **camelCase**: `value`, `errorMessage`, `userId`
- **Meaningful**: Describe what the parameter represents

```csharp
[GenerateUnion]
public partial class User
{
    // ✅ Good parameter names
    public static partial User Active(string userId, string email);
    public static partial User Suspended(string reason, DateTime suspendedAt);
    
    // ❌ Poor parameter names
    public static partial User Created(string s, string e); // Not descriptive
    public static partial User Deleted(string Message); // Should be camelCase
}
```

## ⚠️ Common Mistakes

### Mistake 1: Forgetting Partial Keyword

```csharp
// ❌ Error: Class not partial
[GenerateUnion]
public class MyUnion
{
    public static partial MyUnion Case1();
}
```

**Fix:**
```csharp
[GenerateUnion]
public partial class MyUnion
{
    public static partial MyUnion Case1();
}
```

### Mistake 2: Non-Static Method

```csharp
[GenerateUnion]
public partial class MyUnion
{
    // ❌ Error: Not static
    public partial MyUnion Case1();
}
```

**Fix:**
```csharp
[GenerateUnion]
public partial class MyUnion
{
    public static partial MyUnion Case1();
}
```

### Mistake 3: Wrong Return Type

```csharp
[GenerateUnion]
public partial class Result
{
    // ❌ Error: Returns void instead of Result
    public static partial void Success();
}
```

**Fix:**
```csharp
[GenerateUnion]
public partial class Result
{
    public static partial Result Success();
}
```

### Mistake 4: No Cases Defined

```csharp
// ❌ Error: No cases
[GenerateUnion]
public partial class EmptyUnion
{
}
```

**Fix:**
```csharp
[GenerateUnion]
public partial class ValidUnion
{
    public static partial ValidUnion DefaultCase();
}
```

## 🧪 Compiler Integration

### Design-Time Experience

When you add `[GenerateUnion]`:
1. ✅ IntelliSense shows generated members immediately
2. ✅ Code completion works for pattern matching
3. ✅ Errors are shown inline in IDE
4. ✅ Go to Definition works for generated code

### Build-Time Behavior

During compilation:
1. Source generator scans for `[GenerateUnion]` attributes
2. Validates class structure and method declarations
3. Generates implementation code
4. Code is compiled together with your source

### Error Messages

The generator provides helpful error messages:

```csharp
// Example: Missing partial keyword
[GenerateUnion]
public class BadUnion { }
// Error: UG001: Union type 'BadUnion' must be declared as partial
```

Common diagnostic IDs:
- **UG001**: Class is not partial
- **UG002**: No valid case methods found
- **UG003**: Method is not static or partial
- **UG004**: Method return type doesn't match class type

## 🔍 Inspecting Generated Code

### View Generated Files

In Visual Studio / Rider:
1. Expand project in Solution Explorer
2. Find "Dependencies" → "Analyzers" → "UnionGenerator"
3. View generated `.g.cs` files

### Example Generated Code

For this input:
```csharp
[GenerateUnion]
public partial class Option<T>
{
    public static partial Option<T> Some(T value);
    public static partial Option<T> None();
}
```

UnionGenerator creates (simplified):
```csharp
public partial class Option<T>
{
    // Factory implementations
    public static Option<T> Some(T value) => new SomeCase(value);
    public static Option<T> None() => new NoneCase();
    
    // Case classes
    public sealed class SomeCase : Option<T> { /* ... */ }
    public sealed class NoneCase : Option<T> { /* ... */ }
    
    // Pattern matching
    public TResult Match<TResult>(
        Func<T, TResult> some,
        Func<TResult> none) { /* ... */ }
    
    // Type checks
    public bool IsSome => this is SomeCase;
    public bool IsNone => this is NoneCase;
    
    // And more...
}
```

## 📚 Additional Resources

- [Union Generation Guide](../core-package/union-generation.md) - Detailed guide on defining unions
- [Generated API Reference](./generated-api.md) - Complete API documentation
- [Best Practices](../core-package/best-practices.md) - Design recommendations

## 🚀 Next Steps

- Learn about [Generated APIs](./generated-api.md) to see what the attribute creates
- Check [Extension Methods](./extension-methods.md) for additional helpers
- Review [Pattern Matching](../core-package/pattern-matching.md) for usage patterns
