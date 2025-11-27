# GitHub Actions Workflows

This directory contains GitHub Actions workflows for building, testing, and publishing the EarthTool applications.

## Active Workflows (Unified Architecture)

### 1. `ci-unified.yml` - Unified CI Pipeline ⭐ **RECOMMENDED**
**Purpose**: Continuous integration for both CLI and GUI development with smart change detection

**Triggers**: 
- Push to main/master/dev/feature branches (when relevant files change)
- Pull requests to main/master/dev
- Manual workflow dispatch

**Features**:
- 🎯 **Smart change detection** - Only builds what changed (CLI, GUI, or both)
- 🌍 **Multi-platform builds** - Windows, Linux, macOS for both CLI and GUI
- ✅ **Comprehensive testing** with test reports
- 🔍 **Code quality analysis** with dotnet-format
- 🔒 **Security checks** for vulnerable packages
- 🚀 **Preview builds** for dev branch (both CLI and GUI)
- 📦 **Path-based triggering** - Optimized for monorepo structure
- ⏱️ **7-day artifact retention** for CI builds, 30 days for previews

**Advantages**:
- Single workflow manages both CLI and GUI
- Reduces duplication and maintenance
- Consistent versioning across applications
- Parallel builds for efficiency

### 2. `release.yml` - Unified Release Pipeline ⭐ **RECOMMENDED**
**Purpose**: Official release pipeline for both CLI and GUI with unified changelog generation

**Triggers**:
- Version tags (`v*` - e.g., `v1.0.0`)
- Manual workflow dispatch with release flag

**Features**:
- 🌍 **Multi-platform builds** - Windows, Linux, macOS for both applications
- ✅ **Comprehensive testing** and quality checks
- 🔒 **Security scanning** with dotnet-outdated
- 📝 **Unified changelog generation** based on conventional commits
- 🎁 **Single release** with all 6 artifacts (3 platforms × 2 apps)
- 🔄 **Semantic versioning** support
- ⚠️ **Breaking change detection** and migration guides
- 👥 **Contributor attribution**
- 🔗 **GitHub integration** (commits, issues, PRs linked)
- 📊 **Release statistics** and highlights

**Changelog Features**:
- Automatic categorization by commit type (feat, fix, perf, etc.)
- Scope-based organization (e.g., `feat(cli):`, `fix(gui):`)
- Issue and PR linking
- Breaking change warnings
- Dependency update tracking
- Release type badges (Major/Minor/Patch)

**Advantages**:
- Synchronized releases for CLI and GUI
- Consistent versioning (same tag for both)
- Comprehensive changelog covering all changes
- Single source of truth for releases

### 3. `ci.yml` - Full Solution CI
**Purpose**: Complete solution build and testing (all projects)

**Triggers**:
- Push to main/master/dev/feature branches
- Pull requests to main/master/dev
- Manual workflow dispatch

**Features**:
- Builds entire solution on all platforms
- Runs all tests with coverage
- Code quality and security audit
- Project structure verification
- Detailed CI summary

## Legacy Workflows (Deprecated)

The following workflows are kept for backward compatibility but are **not recommended for new development**:

- `cli-ci.yml` - Use `ci-unified.yml` instead
- `cli-release.yml` - Use `release.yml` instead
- `gui-ci.yml` - Use `ci-unified.yml` instead
- `gui-release.yml` - Use `release.yml` instead

## Workflow Strategy

The project uses a **unified workflow architecture** for simplified management:

### Unified Approach (Recommended)
- **ci-unified.yml**: Fast CI feedback with smart change detection
- **release.yml**: Official releases with unified changelog

### Benefits of Unified Architecture
- ✅ Single source of truth for CI/CD
- ✅ Reduced code duplication (~800 lines saved)
- ✅ Consistent versioning across CLI and GUI
- ✅ Easier maintenance and updates
- ✅ Synchronized releases
- ✅ Unified changelog for all changes
- ✅ Optimized GitHub Actions usage

## Usage

### For Developers

#### Daily Development
1. **Make changes** to CLI or GUI code
2. **Push to branch** → `ci-unified.yml` runs automatically
3. **Only affected components** are built and tested
4. **Review artifacts** in GitHub Actions

#### Preview Testing (Dev Branch)
1. **Merge to dev** branch
2. **Preview builds** created automatically for both CLI and GUI
3. **Download from commit comment** or Actions artifacts
4. **Test and validate** before release

#### Creating a Release
1. **Ensure all tests pass** on main branch
2. **Create a version tag**: `git tag v1.0.0`
3. **Push the tag**: `git push origin v1.0.0`
4. **Release workflow** builds both CLI and GUI
5. **GitHub release** created with all 6 artifacts and unified changelog

### Commit Message Convention

Use [Conventional Commits](https://www.conventionalcommits.org/) for best changelog generation:

```bash
# Feature (new functionality)
git commit -m "feat(cli): add batch processing support"
git commit -m "feat(gui): implement drag and drop"

# Bug fix
git commit -m "fix(cli): resolve extraction error for large files"
git commit -m "fix(gui): correct window sizing on macOS"

# Performance improvement
git commit -m "perf(wd): optimize compression algorithm"

# Breaking change
git commit -m "feat(api)!: update archive format to v2

BREAKING CHANGE: Archive format changed to v2.
Migration: Use conversion tool to upgrade v1 archives"

# Documentation
git commit -m "docs: update installation guide"

# Refactoring
git commit -m "refactor(msh): simplify mesh parsing logic"

# Tests
git commit -m "test(par): add unit tests for parameter parsing"

# Build/CI
git commit -m "ci: add code coverage reporting"

# Dependencies
git commit -m "chore(deps): bump Avalonia to 11.0.6"
```

**Commit Types:**
- `feat`: New feature
- `fix`: Bug fix
- `perf`: Performance improvement
- `refactor`: Code refactoring
- `docs`: Documentation changes
- `test`: Test additions/changes
- `build`/`ci`: Build or CI changes
- `chore`: Maintenance tasks

**Scopes** (optional but recommended):
- `cli`: CLI-specific changes
- `gui`: GUI-specific changes
- `wd`: WD archive functionality
- `msh`: MSH mesh functionality
- `dae`: DAE/COLLADA functionality
- `par`: PAR parameter functionality
- `tex`: TEX texture functionality
- `common`: Shared/common code

**Breaking Changes:**
- Add `!` after type/scope: `feat!:` or `feat(api)!:`
- Include `BREAKING CHANGE:` in commit body with description

### Manual Triggers

Use workflow dispatch for custom runs:

1. Go to **Actions** tab in GitHub
2. Select workflow (`ci-unified.yml` or `release.yml`)
3. Click **Run workflow**
4. Choose branch and options
5. Click **Run workflow** button

### Artifacts

#### CI Builds (`ci-unified.yml`)
Downloads available from Actions runs for 7 days:
- `EarthTool.CLI-Windows-x64.zip`
- `EarthTool.CLI-Linux-x64.tar.gz`
- `EarthTool.CLI-macOS-x64.tar.gz`
- `EarthTool.WD.GUI-Windows-x64.zip`
- `EarthTool.WD.GUI-Linux-x64.tar.gz`
- `EarthTool.WD.GUI-macOS-x64.tar.gz`

#### Preview Builds (dev branch)
Downloads available from Actions runs for 30 days:
- `EarthTool.CLI-Preview.zip` (Windows x64)
- `EarthTool.WD.GUI-Preview.zip` (Windows x64)

#### Release Builds (`release.yml`)
Downloads available from GitHub Releases page:
- All 6 platform-specific builds for both CLI and GUI
- Retention: Permanent (GitHub Releases)

## Environment Variables

All workflows use consistent environment variables:

- `DOTNET_VERSION`: .NET SDK version (8.0.x)
- `SOLUTION_FILE`: Solution file (EarthTool.sln)

Project-specific paths are configured in the matrix strategy.

## Secrets Required

- `GITHUB_TOKEN`: Automatically provided by GitHub
- No additional secrets needed

## Unified Changelog Generation 📝

The `release.yml` workflow includes an advanced changelog generator based on **Conventional Commits**.

### Features

- **🎯 Automatic Release Type**: Detects Major/Minor/Patch from commits
- **📋 Smart Categorization**: Groups by type (features, fixes, performance, etc.)
- **🔍 Scope Recognition**: Organizes by component (CLI, GUI, WD, MSH, etc.)
- **⚠️ Breaking Changes**: Highlights with migration guides
- **🔗 GitHub Links**: Auto-links to commits, issues, and PRs
- **👥 Contributors**: Credits all contributors
- **📊 Statistics**: Shows commits, files changed, lines modified
- **✨ Highlights**: Summarizes most significant changes
- **📦 Dependencies**: Tracks dependency updates
- **🎁 Unified Release**: Covers both CLI and GUI in one changelog

### Changelog Sections

Generated changelog includes:
1. **Release Type Badge** (Major/Minor/Patch)
2. **Highlights** - Summary of key changes
3. **Breaking Changes** (if any) with migration guide
4. **Features** - New functionality (from `feat:` commits)
5. **Bug Fixes** - Fixed issues (from `fix:` commits)
6. **Performance** - Optimizations (from `perf:` commits)
7. **Refactoring** - Code improvements (from `refactor:` commits)
8. **Documentation** - Docs updates (from `docs:` commits)
9. **Tests** - Test additions (from `test:` commits)
10. **Build System** - CI/build changes (from `build:`/`ci:` commits)
11. **Other Changes** - Misc commits
12. **Contributors** - All contributors
13. **Dependencies** - Package updates
14. **Statistics** - Detailed metrics
15. **Release Info** - Download links, requirements, installation

### Example Changelog Output

```markdown
## 🆕 What's Changed in v1.2.0

> 🟡 **Minor Release** - New features added

**15 commits** since [`v1.1.0`](...)

### 🎯 Highlights

- ✨ **5 new features** added
- 🐛 **3 bug fixes** applied
- ⚡ **1 performance improvement** implemented

**Most significant changes:**
- Add batch processing support for CLI [`a1b2c3`](...)
- Implement drag and drop in GUI [`d4e5f6`](...)
- Optimize WD compression algorithm [`g7h8i9`](...)

### ✨ Features
- **[cli]** Add batch processing support for multiple files [`a1b2c3`](...) - @developer
- **[gui]** Implement drag and drop functionality [`d4e5f6`](...) - @developer
- Support for new Earth 2150 texture format [`j0k1l2`](...) - @contributor

### 🐛 Bug Fixes
- **[cli]** Resolve extraction error for large WD archives [#123](...)  [`m3n4o5`](...) - @developer
- **[gui]** Fix window sizing on macOS [`p6q7r8`](...) - @developer

### ⚡ Performance Improvements
- **[wd]** Optimize compression algorithm by 30% [`g7h8i9`](...) - @developer

...
```

## Migration from Legacy Workflows

If you're currently using `cli-ci.yml`, `cli-release.yml`, `gui-ci.yml`, or `gui-release.yml`:

### Benefits of Migration
- ✅ Reduced workflow complexity
- ✅ Synchronized versions for CLI and GUI
- ✅ Single, comprehensive changelog
- ✅ Easier maintenance
- ✅ Better resource utilization

### Migration Steps

1. **Switch to unified workflows**:
   - CI: Use `ci-unified.yml` (automatically detects changes)
   - Release: Use `release.yml` with `v*` tags

2. **Update tagging convention**:
   - Old: `cli-v1.0.0` and `gui-v1.0.0` (separate)
   - New: `v1.0.0` (unified for both)

3. **Adopt conventional commits**:
   - Add type prefixes: `feat:`, `fix:`, etc.
   - Add scopes for clarity: `feat(cli):`, `fix(gui):`
   - Document breaking changes

4. **Optional: Disable legacy workflows**:
   - Delete or rename old workflow files
   - Or keep them for reference

## Monitoring

- **Actions Tab**: Monitor workflow runs
- **Artifacts**: Download builds from run pages
- **Releases**: View releases with unified changelogs
- **Logs**: Debug issues from detailed logs
- **Commit Comments**: Get preview build links on dev commits

## Best Practices

1. **Use conventional commits** for automatic changelog generation
2. **Add scopes** to commits for better organization
3. **Document breaking changes** in commit body
4. **Reference issues** in commit messages (#123)
5. **Test on dev branch** before releasing
6. **Use preview builds** for validation
7. **Create releases** from stable main/master branch
8. **Review changelogs** before publishing

## Support

For issues or questions about workflows:
- Check workflow logs in Actions tab
- Review this documentation
- Open an issue in the repository
- Check [Conventional Commits](https://www.conventionalcommits.org/) guide
