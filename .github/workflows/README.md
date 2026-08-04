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
- 🌍 **Multi-platform builds** - Windows and Linux for both CLI and GUI
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
**Purpose**: Official release pipeline for both CLI and GUI with concise commit-based release notes

**Triggers**:
- Version tags (`v*` - e.g., `v1.0.0`)
- Manual workflow dispatch with release flag

**Features**:
- 🌍 **Release builds** - Windows and Linux for all applications
- ✅ **Comprehensive testing** and quality checks
- 🔒 **Security scanning** with dotnet-outdated
- 📝 **Concise release notes** listing commits since the previous version
- 🎁 **Single release** with Windows and Linux artifacts for all applications
- 🔄 **Semantic versioning** support
- 👥 **Author attribution** for every listed commit
- 🔗 **GitHub links** to each commit

**Release Note Format**:
- Version heading
- Previous-version reference
- Commit message, author, and commit link for every non-merge commit

**Advantages**:
- Synchronized releases for CLI and GUI
- Consistent versioning (same tag for both)
- Complete, concise commit history for each release
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
- **release.yml**: Official releases with concise commit-based notes

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
4. **Release workflow** builds the CLI and GUIs for Windows and Linux
5. **GitHub release** created with all 8 artifacts and commit-based notes

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
- `EarthTool.WD.GUI-Windows-x64.zip`
- `EarthTool.WD.GUI-Linux-x64.tar.gz`
- `EarthTool.PAR.GUI-Windows-x64.zip`
- `EarthTool.PAR.GUI-Linux-x64.tar.gz`
- `EarthTool.TEX.GUI-Windows-x64.zip`
- `EarthTool.TEX.GUI-Linux-x64.tar.gz`

#### Preview Builds (dev branch)
Downloads available from Actions runs for 30 days:
- `EarthTool.CLI-Preview.zip` (Windows x64)
- `EarthTool.WD.GUI-Preview.zip` (Windows x64)

#### Release Builds (`release.yml`)
Downloads available from GitHub Releases page:
- Windows and Linux builds for the CLI, WD GUI, PAR GUI, and TEX GUI
- Retention: Permanent (GitHub Releases)

## Environment Variables

All workflows use consistent environment variables:

- `DOTNET_VERSION`: .NET SDK version (8.0.x)
- `SOLUTION_FILE`: Solution file (EarthTool.sln)

Project-specific paths are configured in the matrix strategy.

## Secrets Required

- `GITHUB_TOKEN`: Automatically provided by GitHub
- No additional secrets needed

## Release Notes

The `release.yml` workflow lists every non-merge commit since the previous version. Each entry includes the commit message, author, and a link to the commit.

### Example

```markdown
## v1.2.0

Commits since [`v1.1.0`](...):

- feat(cli): add batch processing - Jane Doe ([`a1b2c3`](...))
- fix(wd): handle empty archives - John Smith ([`d4e5f6`](...))
```

## Migration from Legacy Workflows

If you're currently using `cli-ci.yml`, `cli-release.yml`, `gui-ci.yml`, or `gui-release.yml`:

### Benefits of Migration
- ✅ Reduced workflow complexity
- ✅ Synchronized versions for CLI and GUI
- ✅ Concise release notes with complete commit history
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
- **Releases**: View releases with commit-based notes
- **Logs**: Debug issues from detailed logs
- **Commit Comments**: Get preview build links on dev commits

## Best Practices

1. **Use conventional commits** so release entries remain easy to scan
2. **Add scopes** to make commit messages clear
3. **Document breaking changes** in commit body
4. **Reference issues** in commit messages (#123)
5. **Test on dev branch** before releasing
6. **Use preview builds** for validation
7. **Run the local pre-publish qualification** with the private official MSH corpus
8. **Create releases** from stable main/master branch only after qualification passes
9. **Review release notes** before publishing

## Support

For issues or questions about workflows:
- Check workflow logs in Actions tab
- Review this documentation
- Open an issue in the repository
- Check [Conventional Commits](https://www.conventionalcommits.org/) guide
