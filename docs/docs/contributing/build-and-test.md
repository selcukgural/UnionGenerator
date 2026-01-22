---
sidebar_position: 3
---

# Build and Test Guide

Comprehensive guide for building and testing UnionGenerator.

## 🏗️ Building the Project

### Quick Build

```bash
# Build all projects in Debug configuration
dotnet build

# Build in Release configuration
dotnet build --configuration Release

# Build without restoring dependencies (faster for incremental builds)
dotnet build --no-restore
```

### Clean Build

```bash
# Clean all build artifacts
dotnet clean

# Clean and rebuild
dotnet clean && dotnet build
```

### Build Specific Projects

```bash
# Build core generator only
dotnet build src/UnionGenerator/UnionGenerator/UnionGenerator.csproj

# Build ASP.NET Core integration
dotnet build src/UnionGenerator.AspNetCore.SourceGen/UnionGenerator.AspNetCore.SourceGen.csproj

# Build all integration packages
dotnet build src/UnionGenerator.EntityFrameworkCore/
dotnet build src/UnionGenerator.FluentValidation/
dotnet build src/UnionGenerator.OneOfCompat/
```

### Build with MSBuild Options

```bash
# Build with maximum verbosity (useful for debugging)
dotnet build --verbosity detailed

# Build with specific framework
dotnet build --framework net8.0

# Build with diagnostic output
dotnet build -p:GenerateFullPaths=true
```

## 🧪 Running Tests

### All Tests

```bash
# Run all tests with default settings
dotnet test

# Run tests with detailed output
dotnet test --verbosity detailed

# Run tests without building (use existing build)
dotnet test --no-build

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Output Options

```bash
# Minimal output (default)
dotnet test --verbosity quiet

# Normal output - shows test progress
dotnet test --verbosity normal

# Detailed output - shows all test details
dotnet test --verbosity detailed

# Diagnostic output - full MSBuild logs
dotnet test --verbosity diagnostic
```

### Filtering Tests

#### By Test Name

```bash
# Run tests containing "Basic" in their name
dotnet test --filter "Name~Basic"

# Run tests exactly matching a name
dotnet test --filter "FullyQualifiedName=UnionGenerator.Tests.GeneratorTests.GeneratesBasicUnion"

# Run tests NOT matching a pattern
dotnet test --filter "Name!~Integration"
```

#### By Category

```bash
# Run only unit tests (if tagged)
dotnet test --filter "Category=Unit"

# Run integration tests
dotnet test --filter "Category=Integration"

# Exclude slow tests
dotnet test --filter "Category!=Slow"
```

#### By Test Class

```bash
# Run all tests in a specific class
dotnet test --filter "ClassName=GeneratorTests"

# Run tests in multiple classes
dotnet test --filter "ClassName=GeneratorTests|ClassName=AnalyzerTests"
```

### Project-Specific Tests

```bash
# Core generator tests
dotnet test tests/UnionGenerator.Tests/UnionGenerator.Tests/UnionGenerator.Tests.csproj

# ASP.NET Core integration tests
dotnet test tests/UnionGenerator.AspNetCore.Tests/

# Entity Framework Core tests
dotnet test tests/UnionGenerator.EntityFrameworkCore.Tests/

# FluentValidation tests
dotnet test tests/UnionGenerator.FluentValidation.Tests/

# All test projects
dotnet test tests/
```

## 📊 Code Coverage

### Generate Coverage Report

```bash
# Generate coverage in default format (XML)
dotnet test --collect:"XPlat Code Coverage"

# Generate coverage in multiple formats
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=json,cobertura,lcov,opencover
```

### View Coverage Report

Coverage reports are generated in `TestResults/` directory.

```bash
# Install report generator tool (one time)
dotnet tool install --global dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coveragereport" \
  -reporttypes:Html

# Open report
open coveragereport/index.html  # macOS
start coveragereport/index.html # Windows
xdg-open coveragereport/index.html # Linux
```

### Coverage Thresholds

The project aims for:
- **Line coverage**: >80%
- **Branch coverage**: >75%
- **Core generator**: >90%

## ⚡ Performance Testing

### Run Benchmarks

```bash
cd tests/UnionGenerator.Benchmarks

# Run all benchmarks
dotnet run --configuration Release

# Run specific benchmark
dotnet run --configuration Release --filter *PatternMatchingBenchmarks*

# Run with memory diagnostics
dotnet run --configuration Release --memory
```

### Benchmark Results

Benchmarks generate detailed reports in `BenchmarkDotNet.Artifacts/`:
- **results/** - Markdown and HTML reports
- **logs/** - Execution logs
- **measurements/** - Raw measurement data

## 🔄 Continuous Integration

### GitHub Actions Workflows

The project uses several CI workflows:

#### Build and Test (`.github/workflows/build.yml`)

Runs on every push and pull request:

```yaml
- Checkout code
- Setup .NET 8.0
- Restore dependencies
- Build (Release configuration)
- Run tests with coverage
- Upload coverage to Codecov
```

Trigger manually:
```bash
gh workflow run build.yml
```

#### NuGet Publish (`.github/workflows/publish-nuget.yml`)

Publishes packages on version tag:

```bash
git tag v1.0.0
git push origin v1.0.0
```

#### Documentation Deploy (`.github/workflows/deploy-docs.yml`)

Deploys docs on changes to `docs/` folder.

### Running CI Locally

You can simulate CI builds locally:

```bash
# Clean environment
rm -rf bin obj

# Restore exactly as CI does
dotnet restore

# Build as CI does
dotnet build --configuration Release --no-restore

# Test as CI does
dotnet test --configuration Release --no-build --verbosity normal --collect:"XPlat Code Coverage"
```

## 🐛 Debugging Tests

### Visual Studio

1. **Open Test Explorer** (Test → Test Explorer)
2. **Find your test** in the list
3. **Right-click** → Debug
4. **Set breakpoints** in test or source code

### JetBrains Rider

1. **Open Unit Tests window** (View → Tool Windows → Unit Tests)
2. **Find your test**
3. **Right-click** → Debug
4. **Set breakpoints** as needed

### VS Code / Command Line

```bash
# Debug a specific test
dotnet test --filter "FullyQualifiedName=YourTestName" --logger "console;verbosity=detailed"

# Debug with environment variable
VSTEST_HOST_DEBUG=1 dotnet test --filter "YourTestName"
```

## 📝 Test Patterns

### Generator Tests

Generator tests verify source generation output:

```csharp
[Fact]
public void GeneratesBasicUnion()
{
    var source = @"
        [GenerateUnion]
        public partial class Result<T, E>
        {
            public static Result<T, E> Ok(T value);
            public static Result<T, E> Error(E error);
        }
    ";
    
    var result = Generate(source);
    
    result.Should().ContainGeneratedCode("public sealed class OkCase");
    result.Should().ContainGeneratedCode("public sealed class ErrorCase");
}
```

### Integration Tests

Integration tests verify end-to-end scenarios:

```csharp
[Fact]
public async Task AspNetCore_ProblemDetails_Integration()
{
    var result = Result<User, ValidationError>.Error(new ValidationError());
    
    var problemDetails = result.ToProblemDetails();
    
    problemDetails.Status.Should().Be(400);
    problemDetails.Title.Should().Be("Validation Failed");
}
```

### Benchmark Tests

Benchmarks measure performance:

```csharp
[Benchmark]
public void PatternMatching()
{
    var result = Result<int, string>.Ok(42);
    
    var value = result.Match(
        ok: x => x,
        error: _ => 0
    );
}
```

## 🔧 Build Troubleshooting

### Common Build Errors

#### Error: "Source generator not found"

```bash
# Clean and restore
dotnet clean
dotnet restore
dotnet build
```

#### Error: "Duplicate type definitions"

```bash
# Delete all bin/obj folders
find . -name "bin" -o -name "obj" | xargs rm -rf
dotnet build
```

#### Error: "Assembly binding errors"

```bash
# Clear NuGet cache
dotnet nuget locals all --clear
dotnet restore
dotnet build
```

### Performance Issues

If builds are slow:

```bash
# Use parallel builds
dotnet build -m

# Skip analyzers during development
dotnet build -p:RunAnalyzers=false

# Use shared compilation
dotnet build -p:UseSharedCompilation=true
```

## 📋 Build Scripts

### Custom Build Script (build.sh/build.ps1)

Create custom build scripts for common tasks:

```bash
#!/bin/bash
# build.sh

echo "Cleaning..."
dotnet clean

echo "Restoring..."
dotnet restore

echo "Building..."
dotnet build --configuration Release

echo "Testing..."
dotnet test --configuration Release --no-build

echo "Done!"
```

```powershell
# build.ps1

Write-Host "Cleaning..." -ForegroundColor Green
dotnet clean

Write-Host "Restoring..." -ForegroundColor Green
dotnet restore

Write-Host "Building..." -ForegroundColor Green
dotnet build --configuration Release

Write-Host "Testing..." -ForegroundColor Green
dotnet test --configuration Release --no-build

Write-Host "Done!" -ForegroundColor Green
```

## 🚀 Next Steps

- Review [Contribution Guidelines](./contribution-guidelines.md) for PR workflow
- Check [Code Style Guide](./code-style.md) for coding standards
- Read [Development Setup](./development-setup.md) for environment setup

## 📚 Additional Resources

- [.NET CLI Documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/)
- [MSBuild Reference](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild)
- [xUnit Documentation](https://xunit.net/docs/getting-started/netcore/cmdline)
- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
