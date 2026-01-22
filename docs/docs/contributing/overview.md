---
sidebar_position: 1
---

# Contributing to UnionGenerator

Thank you for your interest in contributing to UnionGenerator! This guide will help you get started with contributing to the project.

## 🎯 Ways to Contribute

There are many ways to contribute to UnionGenerator:

### Code Contributions
- **Bug fixes** - Fix reported issues
- **New features** - Implement feature requests
- **Performance improvements** - Optimize existing code
- **Test coverage** - Add missing tests
- **Code refactoring** - Improve code quality

### Documentation
- **Fix typos and errors** - Improve existing docs
- **Add examples** - Create real-world usage examples
- **Write tutorials** - Help new users get started
- **API documentation** - Document undocumented APIs
- **Translation** - Translate docs to other languages

### Community
- **Answer questions** - Help users on GitHub Discussions
- **Report bugs** - Submit detailed bug reports
- **Request features** - Suggest new features
- **Review PRs** - Help review pull requests
- **Share your usage** - Write blog posts or create videos

## 📋 Quick Start Checklist

Before you start contributing, make sure you have:

- ✅ Forked the repository
- ✅ Cloned your fork locally
- ✅ Installed required dependencies (.NET 8.0+ SDK)
- ✅ Read our [Development Setup](./development-setup.md) guide
- ✅ Familiarized yourself with our [Code Style](./code-style.md) guidelines
- ✅ Understood our [Contribution Workflow](./contribution-guidelines.md)

## 🚀 Getting Started

### 1. Fork and Clone

```bash
# Fork the repository on GitHub first, then:
git clone https://github.com/YOUR_USERNAME/UnionGenerator.git
cd UnionGenerator
```

### 2. Set Up Development Environment

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test
```

See [Development Setup](./development-setup.md) for detailed instructions.

### 3. Create a Branch

```bash
# Create a new branch for your work
git checkout -b feature/your-feature-name
```

### 4. Make Your Changes

Follow our [Code Style Guidelines](./code-style.md) and write tests for your changes.

### 5. Test Your Changes

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/UnionGenerator.Tests/UnionGenerator.Tests/UnionGenerator.Tests.csproj
```

### 6. Submit a Pull Request

Push your branch and open a pull request against the `main` branch.

## 📝 What Makes a Good Contribution?

### Good Bug Reports
- **Clear title** - Summarize the issue in the title
- **Reproduction steps** - Provide steps to reproduce the bug
- **Expected vs actual** - Describe what should happen vs what does happen
- **Environment** - Include .NET version, OS, IDE
- **Code sample** - Provide a minimal reproducible example

### Good Feature Requests
- **Clear use case** - Explain why the feature is needed
- **Examples** - Show how the feature would be used
- **Alternatives** - Mention alternatives you've considered
- **Breaking changes** - Discuss potential breaking changes

### Good Pull Requests
- **Single purpose** - One PR should address one issue
- **Tests included** - Add tests for new functionality
- **Documentation** - Update docs if needed
- **Clean commits** - Use clear commit messages
- **Up to date** - Rebase on latest main before submitting

## 🔍 Development Areas

### Core Generator
Location: `src/UnionGenerator/UnionGenerator/`

The core source generator that analyzes C# code and generates union types.

**Skills needed:** C#, Roslyn APIs, Source Generators

### Integration Packages
Locations:
- ASP.NET Core: `src/UnionGenerator.AspNetCore.SourceGen/`
- Entity Framework Core: `src/UnionGenerator.EntityFrameworkCore/`
- FluentValidation: `src/UnionGenerator.FluentValidation/`
- OneOf Compatibility: `src/UnionGenerator.OneOfCompat/`

Integration packages that extend UnionGenerator functionality.

**Skills needed:** C#, relevant framework knowledge

### Analyzers
Locations:
- Analyzers: `src/UnionGenerator.Analyzers/`
- Code Fixes: `src/UnionGenerator.Analyzers.CodeFixes/`

Roslyn analyzers that provide diagnostics and code fixes.

**Skills needed:** C#, Roslyn Analyzers API

### Tests
Location: `tests/`

Comprehensive test suites for all components.

**Skills needed:** C#, xUnit, test design

### Documentation
Location: `docs/`

Docusaurus-based documentation website.

**Skills needed:** Markdown, React/TypeScript (for advanced customization)

### Examples
Location: `examples/`

Real-world example projects demonstrating usage.

**Skills needed:** C#, relevant framework knowledge

## 🤝 Code of Conduct

We are committed to providing a welcoming and inclusive environment. Please be:

- **Respectful** - Treat everyone with respect
- **Collaborative** - Work together constructively
- **Patient** - Help others learn
- **Inclusive** - Welcome diverse perspectives
- **Professional** - Keep discussions focused and productive

## 🆘 Getting Help

Need help contributing? Reach out:

- **GitHub Discussions** - Ask questions and get help
- **GitHub Issues** - Report bugs or request features
- **Pull Requests** - Get feedback on your contributions

## 📚 Additional Resources

- [Development Setup Guide](./development-setup.md) - Setup your environment
- [Build and Test Guide](./build-and-test.md) - Build and test commands
- [Contribution Guidelines](./contribution-guidelines.md) - Detailed workflow
- [Code Style Guide](./code-style.md) - Coding standards

## 🎉 Recognition

All contributors are recognized in our release notes and GitHub contributors page. Thank you for helping make UnionGenerator better!

## 📜 License

By contributing to UnionGenerator, you agree that your contributions will be licensed under the MIT License.
