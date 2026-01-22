---
sidebar_position: 4
---

# Contribution Guidelines

Complete workflow for contributing to UnionGenerator.

## 🔄 Contribution Workflow

### 1. Find or Create an Issue

Before starting work:

**For Bug Fixes:**
- Search existing [GitHub Issues](https://github.com/selcukgural/UnionGenerator/issues)
- If not found, create a new issue with reproduction steps
- Wait for maintainer confirmation before starting work

**For New Features:**
- Check if feature is already requested
- Create a feature request issue
- Discuss approach with maintainers
- Get approval before implementing

**For Documentation:**
- Small fixes (typos, clarity) can be done directly
- Large changes should be discussed in an issue first

### 2. Fork the Repository

1. Click "Fork" button on [GitHub](https://github.com/selcukgural/UnionGenerator)
2. Clone your fork:
```bash
git clone https://github.com/YOUR_USERNAME/UnionGenerator.git
cd UnionGenerator
```

3. Add upstream remote:
```bash
git remote add upstream https://github.com/selcukgural/UnionGenerator.git
```

### 3. Create a Feature Branch

```bash
# Update main branch
git checkout main
git pull upstream main

# Create feature branch
git checkout -b feature/your-feature-name

# Or for bug fixes
git checkout -b fix/bug-description
```

#### Branch Naming Conventions

- **Features**: `feature/description-of-feature`
- **Bug fixes**: `fix/description-of-bug`
- **Documentation**: `docs/what-you-changed`
- **Performance**: `perf/what-you-optimized`
- **Refactoring**: `refactor/what-you-refactored`

Examples:
```
feature/add-async-pattern-matching
fix/null-reference-in-match-method
docs/improve-getting-started-guide
perf/optimize-source-generation
refactor/simplify-analyzer-logic
```

### 4. Make Your Changes

#### Code Changes

1. **Write code** following our [Code Style Guide](./code-style.md)
2. **Add tests** for new functionality
3. **Update documentation** if APIs changed
4. **Run tests** to ensure nothing broke

```bash
# Build and test
dotnet build
dotnet test

# Verify your changes
git diff
```

#### Documentation Changes

1. **Edit markdown files** in `docs/docs/`
2. **Test locally**:
```bash
cd docs
npm install
npm start
```
3. **Check for broken links**
4. **Preview in browser** at http://localhost:3000

### 5. Commit Your Changes

#### Commit Message Format

```
type(scope): short description

Longer description if needed. Explain:
- What changed
- Why it changed
- Any breaking changes

Fixes #123
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation only
- `style`: Code style (formatting, no logic change)
- `refactor`: Code refactoring
- `perf`: Performance improvement
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

**Scopes:**
- `generator`: Core generator
- `analyzers`: Roslyn analyzers
- `aspnetcore`: ASP.NET Core integration
- `ef`: Entity Framework integration
- `validation`: FluentValidation integration
- `docs`: Documentation

#### Good Commit Messages

✅ **Good:**
```
feat(generator): add async pattern matching support

Implement MatchAsync method for async/await scenarios.
- Add MatchAsync overload accepting Task-returning functions
- Generate async case handlers
- Add tests for async matching

Fixes #45
```

```
fix(analyzers): correct null reference in exhaustiveness check

The exhaustiveness analyzer threw NRE when analyzing
generic union types without constraints.

Fixes #78
```

```
docs(getting-started): improve quick start examples

Add more realistic examples and common pitfalls section.
Clarify ASP.NET Core integration steps.
```

❌ **Bad:**
```
update code
```

```
fix bug
```

```
WIP
```

### 6. Push Your Changes

```bash
# Push to your fork
git push origin feature/your-feature-name
```

If you need to update your branch with upstream changes:

```bash
# Fetch upstream changes
git fetch upstream

# Rebase on upstream/main
git rebase upstream/main

# Force push (if you rebased)
git push origin feature/your-feature-name --force
```

### 7. Create a Pull Request

1. **Go to your fork** on GitHub
2. **Click "Compare & pull request"**
3. **Fill out PR template**:

```markdown
## Description
Brief description of changes

## Related Issue
Fixes #123

## Changes Made
- Change 1
- Change 2
- Change 3

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing performed

## Checklist
- [ ] Code follows project style guidelines
- [ ] Self-review completed
- [ ] Documentation updated
- [ ] Tests pass locally
- [ ] No breaking changes (or documented)
```

4. **Submit the PR**

## 📋 Pull Request Guidelines

### PR Title

Follow commit message convention:

```
feat(generator): add async pattern matching
fix(analyzers): correct null reference in exhaustiveness check
docs(api): improve generated API documentation
```

### PR Description

Include:
- **What** changed
- **Why** it changed
- **How** to test it
- **Screenshots** (if UI changes)
- **Breaking changes** (if any)
- **Related issues** (Fixes #123, Closes #456)

### PR Size

- **Small PRs** are better (&lt;300 lines)
- **Large PRs** (&gt;500 lines) should be discussed first
- **Split large features** into multiple PRs if possible

### Draft PRs

Use draft PRs for:
- Work in progress
- Seeking early feedback
- Demonstrating an approach

```
Mark as "Draft" when creating the PR
```

## 🔍 Code Review Process

### Review Timeline

- Initial review: **1-3 days**
- Follow-up reviews: **1-2 days**
- Complex PRs may take longer

### Addressing Feedback

```bash
# Make requested changes
# ... edit files ...

# Commit changes
git add .
git commit -m "address review feedback: specific changes"

# Push updates
git push origin feature/your-feature-name
```

### Requesting Re-review

After addressing feedback:
1. **Comment on PR**: "Ready for re-review @reviewer"
2. **Re-request review** using GitHub UI

### Merge Requirements

Before merging, PRs must:
- ✅ Pass all CI checks
- ✅ Have at least one approval
- ✅ Have no merge conflicts
- ✅ Follow code style guidelines
- ✅ Include tests for new functionality
- ✅ Update documentation if needed

## 🚨 Breaking Changes

If your PR introduces breaking changes:

1. **Discuss with maintainers first**
2. **Document in PR description**
3. **Update migration guide**
4. **Add deprecation warnings** if possible
5. **Include in CHANGELOG**

Example:

```markdown
## Breaking Changes

### Removed `Match` overload with single parameter

**Before:**
```csharp
result.Match(x => x);
```

**After:**
```csharp
result.Match(
    ok: x => x,
    error: e => defaultValue
);
```

**Migration:** Add explicit error handler to all Match calls.
```

## 🏷️ Labels

Maintainers will add labels to your PR:

| Label | Meaning |
|-------|---------|
| `bug` | Bug fix |
| `enhancement` | New feature |
| `documentation` | Documentation changes |
| `good first issue` | Good for newcomers |
| `help wanted` | Extra attention needed |
| `breaking change` | Introduces breaking changes |
| `work in progress` | Not ready for review |

## ✅ Checklist Before Submitting

- [ ] Issue exists for this change
- [ ] Branch is up to date with main
- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Comments added for complex logic
- [ ] Documentation updated
- [ ] Tests added for new functionality
- [ ] All tests pass locally
- [ ] No console warnings or errors
- [ ] Commit messages are clear
- [ ] PR description is complete

## 🎯 What Gets Accepted

### ✅ Will Be Accepted

- Bug fixes with tests
- Features requested in issues
- Performance improvements
- Documentation improvements
- Test coverage improvements
- Code quality improvements

### ❌ Will Not Be Accepted

- Features without prior discussion
- Breaking changes without approval
- Code without tests
- Large refactorings without discussion
- Changes that don't follow style guide
- PRs without clear description

## 🤝 Getting Help

If you need help:

- **Ask in the issue** before starting work
- **Use draft PRs** for early feedback
- **Join discussions** on GitHub Discussions
- **Tag maintainers** if you need input

## 🎉 After Your PR is Merged

1. **Delete your branch:**
```bash
git branch -d feature/your-feature-name
git push origin --delete feature/your-feature-name
```

2. **Update your fork:**
```bash
git checkout main
git pull upstream main
git push origin main
```

3. **Celebrate!** 🎊 You're now a contributor!

## 📚 Additional Resources

- [GitHub Flow Guide](https://guides.github.com/introduction/flow/)
- [How to Write a Git Commit Message](https://chris.beams.io/posts/git-commit/)
- [Code Review Best Practices](https://github.com/google/eng-practices/blob/master/review/reviewer/)

## 🙏 Thank You!

Your contributions make UnionGenerator better for everyone. Thank you for taking the time to contribute!
