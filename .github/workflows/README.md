# GitHub Actions Workflows

This directory contains GitHub Actions workflows for building, testing, and publishing the EarthTool applications.

## Workflows

### 1. `cli-ci.yml` - CLI CI
**Purpose**: Continuous integration for CLI development
**Triggers**: 
- Push to main/master/dev/feature branches (when CLI files change)
- Pull requests to main/master/dev
- Manual workflow dispatch

**Features**:
- Multi-platform builds (Windows, Linux, macOS)
- Comprehensive testing with test reports
- Code quality analysis with dotnet-format
- Security checks for vulnerable packages
- Path-based triggering (only when CLI files change)
- 7-day artifact retention

### 2. `cli-release.yml` - CLI Release Pipeline
**Purpose**: Official release pipeline for CLI with advanced changelog generation
**Triggers**:
- Version tags (v*, cli-v*)
- Manual workflow dispatch with release flag

**Features**:
- Multi-platform builds (Windows, Linux, macOS)
- Comprehensive testing and quality checks
- Security scanning with dotnet-outdated
- **Dynamic changelog generation** with smart categorization
- Automated releases with beautiful release notes
- Semantic versioning support
- Breaking change detection
- Contributor attribution
- GitHub integration (commits, issues, PRs linked)

### 3. `gui-ci.yml` - GUI CI and Preview Builds
**Purpose**: Continuous integration and preview builds for GUI development
**Triggers**:
- Push to main/master/dev/feature branches (when GUI files change)
- Pull requests to main/master/dev
- Manual workflow dispatch

**Features**:
- Multi-platform builds (Windows, Linux, macOS)
- Automated testing with test reports
- Code quality and security checks
- **Preview builds for dev branch** with automatic commit comments
- Path-based triggering (only when GUI files change)
- 7-day artifact retention (30 days for previews)

### 4. `gui-release.yml` - GUI Release Pipeline
**Purpose**: Official release pipeline for GUI with advanced changelog generation
**Triggers**:
- Version tags (v*, gui-v*)
- Manual workflow dispatch with release flag

**Features**:
- Multi-platform builds (Windows, Linux, macOS)
- Comprehensive testing and quality checks
- Security scanning with dotnet-outdated
- **Dynamic changelog generation** with smart categorization
- Automated releases with beautiful release notes
- Semantic versioning support
- Breaking change detection
- Contributor attribution
- GitHub integration (commits, issues, PRs linked)

## Configuration Files

### `GitVersion.yml`
- Semantic versioning configuration
- Branch-specific version strategies
- Pre-release tag management

## Workflow Strategy

The project uses a **consistent two-workflow strategy** for both CLI and GUI:

### CLI Workflows
- **cli-ci.yml**: Fast CI feedback for development
- **cli-release.yml**: Official releases with advanced changelog

### GUI Workflows
- **gui-ci.yml**: Fast CI feedback and preview builds
- **gui-release.yml**: Official releases with advanced changelog

This separation allows:
- ✅ Fast CI feedback without heavy release logic
- ✅ Preview builds on dev branch for testing (GUI)
- ✅ Professional releases with comprehensive changelogs
- ✅ Clear separation of concerns
- ✅ Consistent structure across all projects

## Usage

### For Developers

#### CLI Development
1. **CI Build**: Push to main/master/dev/feature → `cli-ci.yml` runs
2. **Release**: Create tag `v1.0.0` or `cli-v1.0.0` → `cli-release.yml` creates release with advanced changelog

#### GUI Development
1. **Quick Feedback**: Push to any branch → `gui-ci.yml` runs automatically
2. **Preview Builds**: Push to `dev` branch → Preview artifacts created with commit comment
3. **Full Release**: Create tag `v1.0.0` or `gui-v1.0.0` → `gui-release.yml` creates release with changelog

### Manual Triggers
Use workflow dispatch to manually run workflows with custom parameters:
- Choose which workflow to run
- Control publishing behavior
- Override automatic triggers

### Artifacts
Each workflow produces artifacts that can be downloaded:
- **CLI CI Builds**: Platform-specific executables (Windows/Linux/macOS) - 7 days retention
- **CLI Release Builds**: Platform-specific archives for distribution - 30 days retention
- **GUI CI Builds**: Platform-specific executables (Windows/Linux/macOS) - 7 days retention
- **GUI Preview Builds**: Windows executable from dev branch - 30 days retention
- **GUI Release Builds**: Platform-specific archives for distribution - 30 days retention

## Environment Variables

All workflows use consistent environment variables:

- `DOTNET_VERSION`: .NET SDK version (8.0.x)
- `PROJECT_PATH`: Project file path (e.g., EarthTool.CLI/EarthTool.CLI.csproj)
- `SOLUTION_FILE`: Solution file (EarthTool.sln)

## Secrets Required

- `GITHUB_TOKEN`: Automatically provided by GitHub for releases
- No additional secrets needed for basic functionality

## Dynamic Changelog Generation 📝

Both `cli-release.yml` and `gui-release.yml` workflows include an advanced changelog generator that automatically creates comprehensive release notes.

### Key Features

- **🎯 Automatic Release Type Detection**: Major/Minor/Patch based on commits
- **📋 Smart Categorization**: Groups commits by type (features, fixes, docs, etc.)
- **⚠️ Breaking Change Detection**: Highlights breaking changes with migration guides
- **🔗 GitHub Integration**: Links to commits, issues, and PRs
- **👥 Contributor Attribution**: Automatically credits all contributors
- **📊 Statistics**: Shows commits, files changed, and lines modified
- **✨ Highlights**: Summarizes the most important changes

### Quick Start

Use [Conventional Commits](https://www.conventionalcommits.org/) format:

```bash
# Feature
git commit -m "feat: add dark mode support"

# Bug fix with issue reference
git commit -m "fix: resolve extraction error #123"

# Breaking change
git commit -m "feat!: update API to v2

BREAKING CHANGE: API endpoints changed.
Migration: Update to /api/v2/ prefix"
```

### Documentation

- **[CHANGELOG_QUICKSTART.md](./CHANGELOG_QUICKSTART.md)** - Quick reference guide
- **[CHANGELOG_FEATURES.md](./CHANGELOG_FEATURES.md)** - Complete feature documentation

## Monitoring

- Check Actions tab in GitHub for workflow runs
- Download artifacts from workflow run pages
- Monitor release creation in Releases section
- Review build logs for debugging
- Check generated changelogs in release notes