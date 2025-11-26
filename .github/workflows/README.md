# GitHub Actions Workflows

This directory contains GitHub Actions workflows for building, testing, and publishing the EarthTool applications.

## Workflows

### 1. `build-and-publish-gui.yml` - Comprehensive Release Workflow
**Purpose**: Full build, test, and release pipeline for the GUI application
**Triggers**: 
- Push to main/master with version tags (v*)
- Pull requests to main/master
- Manual workflow dispatch

**Features**:
- Multi-platform builds (Windows, Linux, macOS)
- Comprehensive testing
- Code quality analysis
- Security scanning
- **Dynamic changelog generation** with smart categorization
- Automated releases with beautiful release notes
- Semantic versioning support
- Breaking change detection
- Contributor attribution

### 2. `gui-build.yml` - Quick Build and Test
**Purpose**: Fast feedback for GUI development
**Triggers**:
- Push to main/master/dev/feature branches (when GUI files change)
- Pull requests
- Manual workflow dispatch

**Features**:
- Quick builds for all platforms
- Basic testing
- Preview builds for dev branch
- Path-based triggering (only when GUI files change)

### 3. `dotnet.yml` - CLI Build and Release
**Purpose**: Build and release the CLI tool
**Triggers**:
- Version tags (v*)
- Pull requests to master

**Features**:
- Windows-focused builds
- Automated releases

## Configuration Files

### `GitVersion.yml`
- Semantic versioning configuration
- Branch-specific version strategies
- Pre-release tag management

## Usage

### For Developers
1. **Quick Feedback**: Push to any branch → `gui-build.yml` runs automatically
2. **Preview Builds**: Push to `dev` branch → Preview artifacts are created
3. **Full Release**: Create version tag (e.g., `v1.0.0`) → Full release workflow runs

### Manual Triggers
Use workflow dispatch to manually run workflows with custom parameters:
- Choose which workflow to run
- Control publishing behavior
- Override automatic triggers

### Artifacts
Each workflow produces artifacts that can be downloaded:
- **GUI Builds**: Platform-specific executables (Windows/Linux/macOS)
- **CLI Builds**: Windows executable
- **Preview Builds**: Development versions from dev branch

## Environment Variables

- `DOTNET_VERSION`: .NET SDK version (8.0.x)
- `PROJECT_NAME`: GUI project name (EarthTool.WD.GUI)
- `SOLUTION_FILE`: Solution file (EarthTool.sln)

## Secrets Required

- `GITHUB_TOKEN`: Automatically provided by GitHub for releases
- No additional secrets needed for basic functionality

## Dynamic Changelog Generation 📝

The `build-and-publish-gui.yml` workflow includes an advanced changelog generator that automatically creates comprehensive release notes.

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