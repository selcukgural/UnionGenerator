# Publishing Guide

This guide explains how to publish UnionGenerator packages to NuGet.org using GitHub Actions.

## 📋 Table of Contents

- [Prerequisites](#prerequisites)
- [Automatic Publishing (Recommended)](#automatic-publishing-recommended)
- [Tag-Based Publishing](#tag-based-publishing)
- [Manual Publishing](#manual-publishing)
- [Versioning Strategy](#versioning-strategy)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

### 1. NuGet API Key Setup

1. Create a NuGet.org API key:
   - Go to https://www.nuget.org/account/apikeys
   - Click "Create"
   - Key Name: `UnionGenerator GitHub Actions`
   - Glob Pattern: `UnionGenerator*`
   - Select Packages: Choose all UnionGenerator packages
   - Scopes: Select "Push" and "Push new packages and package versions"
   - Expiration: Set appropriate expiration date

2. Add the API key to GitHub Secrets:
   - Go to: https://github.com/selcukgural/UnionGenerator/settings/secrets/actions
   - Click "New repository secret"
   - Name: `NUGET_API_KEY`
   - Value: Paste your NuGet API key
   - Click "Add secret"

### 2. Branch Protection (Recommended)

Enable branch protection for `main` branch:
- Require pull request reviews before merging
- Require status checks to pass before merging
- Require branches to be up to date before merging

This prevents accidental version bumps and ensures quality.

---

## Automatic Publishing (Recommended)

**How it works:** When you merge code to `main` with an updated version number, GitHub Actions automatically publishes to NuGet.org.

### Step-by-Step Process

#### 1. Update Version Number

Edit the version in **all** .csproj files:

```xml
<!-- src/UnionGenerator/UnionGenerator/UnionGenerator.csproj -->
<PropertyGroup>
  <Version>0.2.0</Version>  <!-- Change this -->
</PropertyGroup>
```

**All packages to update:**
- `src/UnionGenerator/UnionGenerator/UnionGenerator.csproj`
- `src/UnionGenerator.AspNetCore/UnionGenerator.AspNetCore.csproj`
- `src/UnionGenerator.EntityFrameworkCore/UnionGenerator.EntityFrameworkCore.csproj`
- `src/UnionGenerator.FluentValidation/UnionGenerator.FluentValidation.csproj`
- `src/UnionGenerator.OneOfCompat/UnionGenerator.OneOfCompat.csproj`
- `src/UnionGenerator.OneOfExtensions/UnionGenerator.OneOfExtensions.csproj`
- `src/UnionGenerator.OneOfSourceGen/UnionGenerator.OneOfSourceGen.csproj`
- `src/UnionGenerator.Analyzers/UnionGenerator.Analyzers.csproj`
- `src/UnionGenerator.Analyzers.CodeFixes/UnionGenerator.Analyzers.CodeFixes.csproj`

**Tip:** Use find-and-replace to update all at once:
```bash
find src -name "*.csproj" -exec sed -i '' 's/<Version>0.1.0<\/Version>/<Version>0.2.0<\/Version>/g' {} \;
```

#### 2. Commit and Push

```bash
git add .
git commit -m "chore: Bump version to 0.2.0"
git push origin main
```

Or merge a pull request with the version change.

#### 3. Monitor the Workflow

1. Go to: https://github.com/selcukgural/UnionGenerator/actions
2. Look for "Publish on Version Change" workflow
3. Click to see progress

**Workflow will:**
- ✅ Detect version change (compares with previous commit)
- ✅ Build all projects in Release mode
- ✅ Run all tests
- ✅ Pack NuGet packages
- ✅ Publish to NuGet.org (with `--skip-duplicate` flag)
- ✅ Create Git tag (e.g., `v0.2.0`)
- ✅ Create GitHub Release with packages attached

#### 4. Verify Publication

After workflow completes (~5-10 minutes):

1. Check NuGet.org packages:
   - https://www.nuget.org/packages/UnionGenerator
   - https://www.nuget.org/packages/UnionGenerator.AspNetCore
   - (etc.)

2. Check GitHub Releases:
   - https://github.com/selcukgural/UnionGenerator/releases

3. Verify version is listed and packages are downloadable

### What if version didn't change?

If you push to `main` without changing the version, the workflow will:
- ✅ Detect no version change
- ℹ️ Skip publish step
- ✅ Exit successfully (no error)

This allows you to push bug fixes, documentation updates, or example changes without triggering a publish.

---

## Tag-Based Publishing

Use this method if you prefer manual release triggers.

### Process

```bash
# 1. Ensure your main branch is up-to-date and tested
git checkout main
git pull

# 2. Create and push a version tag
git tag -a v0.2.0 -m "Release version 0.2.0"
git push origin v0.2.0
```

### What happens:
- `publish-nuget.yml` workflow triggers
- Extracts version from tag (e.g., `v0.2.0` → `0.2.0`)
- Overrides .csproj versions with tag version
- Builds, tests, packs, and publishes

**Note:** This method overrides .csproj versions, so make sure they match or update them before tagging.

---

## Manual Publishing

For emergency releases or testing purposes.

### Via GitHub UI

1. Go to: https://github.com/selcukgural/UnionGenerator/actions/workflows/publish-manual.yml
2. Click "Run workflow"
3. Select branch (usually `main`)
4. Enter version number (e.g., `0.2.0`)
5. Click "Run workflow"

### Via GitHub CLI

```bash
gh workflow run publish-manual.yml --ref main -f version=0.2.0
```

---

## Versioning Strategy

UnionGenerator follows [Semantic Versioning 2.0.0](https://semver.org/):

### Format: `MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes (incompatible API changes)
  - Example: `1.0.0` → `2.0.0`
  - When: Remove public APIs, change behavior significantly
  
- **MINOR**: New features (backward-compatible)
  - Example: `0.1.0` → `0.2.0`
  - When: Add new union types, new integration packages
  
- **PATCH**: Bug fixes (backward-compatible)
  - Example: `0.1.0` → `0.1.1`
  - When: Fix bugs, improve performance, update docs

### Pre-release Versions

For alpha/beta/rc versions, use suffixes:
- `0.2.0-alpha.1` - Alpha release (early testing)
- `0.2.0-beta.1` - Beta release (feature complete, testing)
- `0.2.0-rc.1` - Release candidate (final testing)

**Note:** Workflows automatically detect pre-releases and mark GitHub releases as "Pre-release".

### Version Synchronization

**All packages must use the same version number.**

This ensures:
- Consistent dependency resolution
- Clear release timeline
- Simplified documentation

Use this script to update all versions at once:

```bash
#!/bin/bash
NEW_VERSION="0.2.0"
find src -name "*.csproj" -exec sed -i '' "s/<Version>.*<\/Version>/<Version>$NEW_VERSION<\/Version>/g" {} \;
```

---

## Troubleshooting

### ❌ Workflow fails: "Response status code does not indicate success: 409 (Conflict)"

**Cause:** Package version already exists on NuGet.org.

**Solution:**
1. Increment version number in .csproj files
2. Commit and push again

Alternatively, wait for the workflow to complete - the `--skip-duplicate` flag will skip already-published packages.

### ❌ Workflow fails: "401 Unauthorized"

**Cause:** Invalid or expired NuGet API key.

**Solution:**
1. Create a new API key on NuGet.org
2. Update `NUGET_API_KEY` secret in GitHub repository settings

### ❌ Workflow fails: Tests failed

**Cause:** Tests are failing in Release mode.

**Solution:**
1. Run tests locally: `dotnet test --configuration Release`
2. Fix failing tests
3. Commit and push fixes

### ❌ Version didn't change but workflow triggered

**Cause:** False positive in version detection (rare).

**Solution:**
- Workflow will skip publish step automatically
- Check workflow logs: "ℹ️ Version unchanged"

### ❌ Want to unpublish a package

**Cause:** Accidentally published wrong version.

**Solution:**
NuGet.org doesn't allow package deletion, but you can:
1. Unlist the package: https://www.nuget.org/packages/UnionGenerator/manage
2. Publish a new patch version with fixes

### ❌ Need to publish only one package

**Cause:** Want to test a single package before full release.

**Solution:**
Not directly supported. Options:
1. Use local NuGet feed for testing
2. Create a separate test package (e.g., `UnionGenerator.Test`)
3. Publish all packages (recommended - they're versioned together)

---

## Advanced: Customizing Workflows

### Change trigger conditions

Edit `.github/workflows/publish-on-version-change.yml`:

```yaml
# Only trigger on specific paths
on:
  push:
    branches:
      - main
    paths:
      - 'src/**/*.csproj'
      - 'src/**/*.cs'
      # Add more paths as needed
```

### Skip publish for specific commits

Add `[skip-publish]` to commit message:

```bash
git commit -m "docs: Update README [skip-publish]"
```

Then update workflow:
```yaml
jobs:
  publish:
    if: needs.check-version.outputs.version_changed == 'true' && !contains(github.event.head_commit.message, '[skip-publish]')
```

### Publish to private NuGet feed

Add to workflow:
```yaml
- name: Publish to Private Feed
  run: |
    dotnet nuget push ./nupkgs/*.nupkg \
      --source https://your-private-feed.com/v3/index.json \
      --api-key ${{ secrets.PRIVATE_FEED_KEY }}
```

---

## Questions?

- **Issues**: https://github.com/selcukgural/UnionGenerator/issues
- **Discussions**: https://github.com/selcukgural/UnionGenerator/discussions
- **Contact**: [@selcukgural](https://github.com/selcukgural)
