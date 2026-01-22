---
sidebar_position: 2
---

# Development Setup

This guide will help you set up your development environment for contributing to UnionGenerator.

## Prerequisites

### Required Software

| Tool | Minimum Version | Recommended | Purpose |
|------|----------------|-------------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0 | 8.0+ | Build and run the project |
| [Git](https://git-scm.com/) | 2.30+ | Latest | Version control |

### Recommended Tools

| Tool | Purpose |
|------|---------|
| [Visual Studio 2022](https://visualstudio.microsoft.com/) | Full-featured IDE with excellent Roslyn support |
| [JetBrains Rider](https://www.jetbrains.com/rider/) | Cross-platform .NET IDE |
| [Visual Studio Code](https://code.visualstudio.com/) | Lightweight editor with C# Dev Kit |

## 🚀 Quick Setup

### 1. Fork and Clone

```bash
# Fork the repository on GitHub first
# Then clone your fork
git clone https://github.com/YOUR_USERNAME/UnionGenerator.git
cd UnionGenerator
```

### 2. Restore Dependencies

```bash
dotnet restore
```

This will restore all NuGet packages for the solution.

### 3. Build the Solution

```bash
# Build in Debug configuration
dotnet build

# Build in Release configuration
dotnet build --configuration Release
```

### 4. Verify Setup

```bash
# Run all tests to verify everything works
dotnet test

# You should see output like:
# Passed!  - Failed:     0, Passed:   142, Skipped:     0, Total:   142
```

## 📁 Repository Structure

Understanding the project structure helps you navigate the codebase:

```
UnionGenerator/
├── src/                                    # Source code
│   ├── UnionGenerator/
│   │   └── UnionGenerator/                # Core source generator
│   ├── UnionGenerator.AspNetCore.SourceGen/  # ASP.NET Core integration
│   ├── UnionGenerator.EntityFrameworkCore/   # EF Core integration
│   ├── UnionGenerator.FluentValidation/      # FluentValidation integration
│   ├── UnionGenerator.OneOfCompat/           # OneOf compatibility layer
│   ├── UnionGenerator.OneOfExtensions/       # OneOf extensions
│   └── UnionGenerator.Analyzers.CodeFixes/   # Code fix providers
├── tests/                                  # Test projects
│   ├── UnionGenerator.Tests/              # Core generator tests
│   ├── UnionGenerator.AspNetCore.Tests/   # ASP.NET Core tests
│   ├── UnionGenerator.EntityFrameworkCore.Tests/  # EF Core tests
│   ├── UnionGenerator.FluentValidation.Tests/     # FluentValidation tests
│   └── UnionGenerator.Benchmarks/         # Performance benchmarks
├── examples/                               # Example projects
│   ├── aspnetcore-example/                # ASP.NET Core usage example
│   ├── entityframework-example/           # EF Core usage example
│   ├── fluentvalidation-example/          # FluentValidation usage example
│   ├── json-example/                      # JSON serialization example
│   └── oneof-example/                     # OneOf migration example
├── docs/                                   # Documentation (Docusaurus)
├── .github/workflows/                      # CI/CD workflows
└── UnionGenerator.sln                      # Solution file
```

## 🔧 IDE Setup

### Visual Studio 2022

1. **Install Required Workloads:**
   - .NET desktop development
   - ASP.NET and web development
   - .NET compiler platform SDK (for Roslyn work)

2. **Open Solution:**
   ```
   Open UnionGenerator.sln in Visual Studio
   ```

3. **Build Solution:**
   - Press `Ctrl+Shift+B` or select Build → Build Solution

4. **Run Tests:**
   - Open Test Explorer (Test → Test Explorer)
   - Click "Run All Tests"

5. **Debug Source Generator:**
   - Set `ExampleProject` as startup project
   - Press F5 to start debugging
   - Breakpoints in generator code will be hit during compilation

### JetBrains Rider

1. **Open Solution:**
   ```
   Open UnionGenerator.sln in Rider
   ```

2. **Build Solution:**
   - Press `Ctrl+Shift+F9` or select Build → Build Solution

3. **Run Tests:**
   - Open Unit Tests window (View → Tool Windows → Unit Tests)
   - Click "Run All Tests"

4. **Configure Source Generator Debugging:**
   - Edit Run/Debug Configuration
   - Add `DOTNET_CLI_CONFIGURE_MSBUILD_LOGS=true` environment variable
   - Enable "Generate In-Memory" option

### Visual Studio Code

1. **Install Extensions:**
   ```
   - C# Dev Kit (Microsoft)
   - C# (Microsoft)
   ```

2. **Open Project:**
   ```bash
   code .
   ```

3. **Build:**
   ```bash
   # Terminal in VS Code
   dotnet build
   ```

4. **Run Tests:**
   ```bash
   dotnet test
   ```

## 🐛 Debugging Source Generators

Source generators run during compilation, which requires special debugging setup.

### Method 1: Debugger.Launch() (Recommended)

1. **Add Debugger Launch Code:**
   ```csharp
   // In UnionGenerator.cs Execute method
   #if DEBUG
   if (!System.Diagnostics.Debugger.IsAttached)
   {
       System.Diagnostics.Debugger.Launch();
   }
   #endif
   ```

2. **Build a Project That Uses the Generator:**
   ```bash
   cd src/ExampleProject/ExampleProject
   dotnet build
   ```

3. **Choose Debugger:**
   - Select your IDE when prompted
   - Set breakpoints in generator code

### Method 2: Debug Test Project

1. **Set Breakpoints** in generator code
2. **Debug a Test** that triggers the generator
3. **Step Through** generator execution

### Method 3: Attach to Build Process

1. **Start Build in Terminal:**
   ```bash
   dotnet build /p:UseSharedCompilation=false
   ```

2. **Attach Debugger:**
   - Find `csc.exe` or `dotnet.exe` process
   - Attach debugger from IDE

## 🧪 Running Tests

### All Tests

```bash
# Run all tests in the solution
dotnet test

# Run with detailed output
dotnet test --verbosity detailed

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Specific Test Project

```bash
# Core generator tests
dotnet test tests/UnionGenerator.Tests/UnionGenerator.Tests/UnionGenerator.Tests.csproj

# ASP.NET Core tests
dotnet test tests/UnionGenerator.AspNetCore.Tests/UnionGenerator.AspNetCore.Tests.csproj

# Entity Framework Core tests
dotnet test tests/UnionGenerator.EntityFrameworkCore.Tests/UnionGenerator.EntityFrameworkCore.Tests.csproj
```

### Filter Tests

```bash
# Run tests matching a pattern
dotnet test --filter "FullyQualifiedName~UnionGenerator.Tests.GeneratorTests"

# Run tests in a specific class
dotnet test --filter "ClassName=GeneratorTests"

# Run a specific test
dotnet test --filter "Name=GeneratesBasicUnion"
```

## 📊 Running Benchmarks

```bash
cd tests/UnionGenerator.Benchmarks
dotnet run --configuration Release
```

Benchmarks use BenchmarkDotNet and will generate detailed performance reports.

## 🔄 Keeping Your Fork Updated

```bash
# Add upstream remote (one time only)
git remote add upstream https://github.com/selcukgural/UnionGenerator.git

# Fetch latest changes
git fetch upstream

# Update your main branch
git checkout main
git merge upstream/main

# Push updates to your fork
git push origin main
```

## 🐳 Docker Setup (Optional)

If you prefer containerized development:

```dockerfile
# Example Dockerfile for development
FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app
COPY . .

RUN dotnet restore
RUN dotnet build

CMD ["dotnet", "test"]
```

```bash
# Build and run
docker build -t uniongenerator-dev .
docker run -it uniongenerator-dev
```

## 🔍 Common Issues

### Issue: "Source generator not running"

**Solution:**
1. Clean the solution: `dotnet clean`
2. Delete `bin/` and `obj/` folders
3. Restart IDE
4. Rebuild: `dotnet build`

### Issue: "Tests failing with file not found"

**Solution:**
- Ensure you ran `dotnet restore`
- Check that all dependencies are installed
- Try running tests from solution root

### Issue: "Debugger not attaching to generator"

**Solution:**
- Set `UseSharedCompilation=false` in build properties
- Use `Debugger.Launch()` method instead
- Ensure you're building in Debug configuration

### Issue: "Build errors after updating .NET SDK"

**Solution:**
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore

# Rebuild
dotnet build
```

## 📝 Next Steps

Now that your environment is set up:

1. Read [Build and Test Guide](./build-and-test.md) for detailed build commands
2. Review [Code Style Guidelines](./code-style.md) for coding standards
3. Check [Contribution Guidelines](./contribution-guidelines.md) for workflow
4. Pick an issue from [GitHub Issues](https://github.com/selcukgural/UnionGenerator/issues) to work on

## 🆘 Getting Help

If you encounter issues:

- Check existing [GitHub Issues](https://github.com/selcukgural/UnionGenerator/issues)
- Ask in [GitHub Discussions](https://github.com/selcukgural/UnionGenerator/discussions)
- Review [Roslyn Source Generators documentation](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
