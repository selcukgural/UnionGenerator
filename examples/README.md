# UnionGenerator Examples

Comprehensive, production-ready examples demonstrating UnionGenerator usage across different scenarios and integrations.

## 📚 Available Examples

### 1. **ASP.NET Core Integration** [`aspnetcore-example/`](./aspnetcore-example/)

Learn how to use UnionGenerator with ASP.NET Core for REST APIs with automatic `ProblemDetails` conversion.

**What You'll Learn:**
- Result pattern implementation
- ProblemDetails RFC 7807 integration
- Controller and Minimal API endpoints
- Request validation with UnionGenerator
- OpenAPI/Swagger documentation
- Error handling best practices

**Key Features:**
- ✅ Full CRUD REST API
- ✅ Traditional controllers and modern Minimal APIs
- ✅ Automatic error-to-ProblemDetails conversion
- ✅ Model validation integration
- ✅ Swagger UI documentation

**Run it:**
```bash
cd examples/aspnetcore-example
dotnet run
```

---

### 2. **Entity Framework Core Integration** [`entityframework-example/`](./entityframework-example/)

Store discriminated unions in databases with automatic JSON serialization using EF Core value converters.

**What You'll Learn:**
- JSON column storage of union types
- Value converter configuration
- CRUD operations with unions
- Pattern matching with database results
- Query filtering and aggregation
- Data persistence best practices

**Key Features:**
- ✅ JSON conversion for database storage
- ✅ In-memory database example (ready for SQL Server/PostgreSQL)
- ✅ Full CRUD with union types
- ✅ Query and update patterns
- ✅ Batch operations

**Run it:**
```bash
cd examples/entityframework-example
dotnet run
```

---

### 3. **FluentValidation Integration** [`fluentvalidation-example/`](./fluentvalidation-example/)

Implement declarative validation with automatic conversion to RFC 7807 `ProblemDetailsError`.

**What You'll Learn:**
- Fluent validator setup and usage
- Converting validation results to ProblemDetailsError
- Field-based error mapping
- Batch validation
- Conditional and async validation
- Integration with service layer

**Key Features:**
- ✅ Declarative validation rules
- ✅ Automatic error structure conversion
- ✅ Multiple validators example
- ✅ Error message customization
- ✅ Real-world patterns

**Run it:**
```bash
cd examples/fluentvalidation-example
dotnet run
```

---

### 4. **JSON Serialization** [`json-example/`](./json-example/)

Serialize and deserialize union types to/from JSON using `System.Text.Json`.

**What You'll Learn:**
- Union type JSON serialization
- System.Text.Json integration
- Roundtrip serialization (serialize → deserialize)
- Complex nested types
- JSON format conventions
- Error case handling

**Key Features:**
- ✅ Clean JSON representation
- ✅ Roundtrip guarantees
- ✅ Complex nested types
- ✅ Array serialization
- ✅ Real-world API response patterns

**Run it:**
```bash
cd examples/json-example
dotnet run
```

---

### 5. **OneOf Compatibility** [`oneof-example/`](./oneof-example/)

Migrate from the OneOf library to UnionGenerator with seamless interoperability during transition.

**What You'll Learn:**
- OneOf library basics
- Three migration approaches (runtime, fluent, compile-time)
- Converting OneOf to UnionGenerator unions
- Gradual migration patterns
- Performance characteristics
- Coexistence strategies

**Key Features:**
- ✅ OneOf to UnionGenerator conversion
- ✅ Multiple adapter approaches
- ✅ Batch conversion patterns
- ✅ Legacy code compatibility
- ✅ Migration examples

**Run it:**
```bash
cd examples/oneof-example
dotnet run
```

---

## 🚀 Quick Start

All examples follow the same pattern:

1. **Navigate to example directory:**
   ```bash
   cd examples/<example-name>
   ```

2. **Build the project:**
   ```bash
   dotnet build
   ```

3. **Run the example:**
   ```bash
   dotnet run
   ```

Each example is standalone and can run independently without building the entire solution.

---

## 📋 Choosing the Right Example

| Use Case | Example | Why |
|----------|---------|-----|
| **Building REST APIs** | ASP.NET Core | Full server setup with error handling |
| **Database persistence** | Entity Framework Core | Store results safely in databases |
| **Input validation** | FluentValidation | Validate user input declaratively |
| **Message formatting** | JSON Serialization | Exchange data over HTTP/queues |
| **Migrating from OneOf** | OneOf Compatibility | Learn migration strategies |
| **Everything together** | ASP.NET Core + others | Real-world integration patterns |

---

## 🔍 Architecture Overview

```
UnionGenerator
    ↓
┌───────────────────────┐
│   Union Types         │  Discriminated unions with pattern matching
└───────────────────────┘
    ↓
    ├─→ [ASP.NET Core] → HTTP responses + ProblemDetails
    ├─→ [EF Core] → Database JSON columns
    ├─→ [FluentValidation] → Validation + error mapping
    ├─→ [JSON] → Serialization
    └─→ [OneOf] → Migration/interop
```

---

## 💡 Common Patterns Across Examples

### Pattern 1: Success/Error Result
```csharp
[GenerateUnion]
public partial class Result<T, E>
{
    public static Result<T, E> Ok(T value) => new OkCase(value);
    public static Result<T, E> Error(E error) => new ErrorCase(error);
}
```

### Pattern 2: Pattern Matching
```csharp
result.Match(
    ok: data => Console.WriteLine($"Success: {data}"),
    error: err => Console.WriteLine($"Error: {err}")
);
```

### Pattern 3: Guard Clauses
```csharp
if (result is Result<User, Error>.ErrorCase errorCase)
{
    return BadRequest(errorCase.Value);
}
```

---

## 📚 Related Documentation

- [UnionGenerator Main README](../src/UnionGenerator/README.md)
- [UnionGenerator.AspNetCore](../src/UnionGenerator.AspNetCore/README.md)
- [UnionGenerator.EntityFrameworkCore](../src/UnionGenerator.EntityFrameworkCore/README.md)
- [UnionGenerator.FluentValidation](../src/UnionGenerator.FluentValidation/README.md)
- [UnionGenerator.OneOfCompat](../src/UnionGenerator.OneOfCompat/README.md)
- [UnionGenerator.OneOfExtensions](../src/UnionGenerator.OneOfExtensions/README.md)

---

## 🎯 Learning Path

1. **Start here:** `json-example/` - Understand basic serialization
2. **Then:** `fluentvalidation-example/` - Learn validation
3. **Then:** `entityframework-example/` - Learn persistence
4. **Then:** `aspnetcore-example/` - Build complete API
5. **Finally:** `oneof-example/` - Explore interoperability

---

## 🤝 Contributing

Found a bug in examples? Want to suggest improvements? Create an issue or PR on the repository!

---

## 📄 License

All examples are part of UnionGenerator and follow the same license.

---

**Ready to explore?** Pick an example and `dotnet run` to get started! 🎉

