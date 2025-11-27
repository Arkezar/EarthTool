# Conventional Commits Guide for EarthTool

This guide explains how to use [Conventional Commits](https://www.conventionalcommits.org/) in the EarthTool project for automatic changelog generation.

## Quick Reference

```
<type>(<scope>): <subject>

[optional body]

[optional footer(s)]
```

## Commit Types

| Type | Description | Changelog Section | Example |
|------|-------------|-------------------|---------|
| `feat` | New feature | ✨ Features | `feat(cli): add batch processing` |
| `fix` | Bug fix | 🐛 Bug Fixes | `fix(gui): resolve window crash` |
| `perf` | Performance improvement | ⚡ Performance | `perf(wd): optimize compression` |
| `refactor` | Code refactoring | ♻️ Refactoring | `refactor(msh): simplify parser` |
| `docs` | Documentation | 📝 Documentation | `docs: update README` |
| `test` | Tests | ✅ Tests | `test(par): add parsing tests` |
| `build` | Build system | 👷 Build System | `build: update .NET to 8.0.1` |
| `ci` | CI configuration | 👷 Build System | `ci: add coverage reporting` |
| `chore` | Maintenance | 🔧 Other Changes | `chore: update .gitignore` |

## Scopes

Scopes help organize changes by component:

| Scope | Component | Example |
|-------|-----------|---------|
| `cli` | CLI application | `feat(cli): add --verbose flag` |
| `gui` | GUI application | `fix(gui): correct theme switching` |
| `wd` | WD archive handling | `perf(wd): improve decompression speed` |
| `msh` | MSH mesh format | `feat(msh): support animation data` |
| `dae` | DAE/COLLADA format | `fix(dae): handle materials correctly` |
| `par` | PAR parameters | `feat(par): add new entity types` |
| `tex` | TEX textures | `fix(tex): correct DXT compression` |
| `common` | Shared code | `refactor(common): extract base reader` |
| `api` | Public API | `feat(api)!: redesign archive interface` |
| `deps` | Dependencies | `chore(deps): bump Avalonia to 11.1.0` |

## Examples

### Simple Feature
```bash
git commit -m "feat(cli): add support for multiple input files"
```

### Bug Fix with Issue Reference
```bash
git commit -m "fix(gui): prevent crash when opening corrupted archives (#42)"
```

### Performance Improvement
```bash
git commit -m "perf(wd): reduce memory usage during extraction by 40%"
```

### Breaking Change (Short Form)
```bash
git commit -m "feat(api)!: change archive format to version 2"
```

### Breaking Change (Long Form)
```bash
git commit -m "feat(api): update to archive format v2

BREAKING CHANGE: Archive format changed from v1 to v2.
Existing archives need to be converted using the migration tool.

Migration steps:
1. Download the migration tool
2. Run: earthtool migrate old-archive.wd new-archive.wd
3. Verify the converted archive"
```

### Multiple Changes
```bash
# Feature with detailed description
git commit -m "feat(gui): add drag and drop support

- Allow dropping files directly into the archive view
- Support multiple file selection
- Show progress during file addition
- Add visual feedback for drag operations"
```

### Scoped Refactoring
```bash
git commit -m "refactor(msh): extract mesh parsing into separate class

Move parsing logic from MeshReader to dedicated MeshParser class
for better testability and separation of concerns."
```

### Documentation Update
```bash
git commit -m "docs(wd): add format specification and examples"
```

### Test Addition
```bash
git commit -m "test(par): add comprehensive parameter parsing tests

- Test all entity types
- Test edge cases and invalid input
- Add benchmark tests for performance regression detection"
```

### Dependency Update
```bash
git commit -m "chore(deps): update .NET dependencies

- Bump System.Text.Json to 8.0.1
- Update Avalonia to 11.1.0
- Update all test frameworks to latest versions"
```

### CI/Build Changes
```bash
git commit -m "ci: add unified release workflow

Combines CLI and GUI releases into a single workflow with
comprehensive changelog generation based on conventional commits."
```

## Breaking Changes

Breaking changes MUST be indicated in two ways:

1. **Exclamation mark** after type/scope: `feat!:` or `feat(api)!:`
2. **BREAKING CHANGE footer** in commit body

### Example with Migration Guide
```bash
git commit -m "feat(wd)!: change compression algorithm to ZSTD

BREAKING CHANGE: Default compression changed from DEFLATE to ZSTD.

Archives created with this version are not compatible with older
versions. To maintain compatibility, use --legacy-compression flag.

Migration:
- Old archives can still be read
- New archives use ZSTD by default
- Use --legacy-compression for backward compatibility"
```

## Issue and PR References

Reference issues and PRs in commits:

```bash
# Fix an issue
git commit -m "fix(cli): handle empty archive files (#123)"

# Close multiple issues
git commit -m "feat(gui): add search functionality

Implements search across all files in archive with regex support.

Closes #45, #67, #89"

# Reference a PR
git commit -m "refactor(common): improve error handling (PR #156)"
```

## Changelog Impact

Your commits directly affect the generated changelog:

### Input Commits
```
feat(cli): add batch processing support
feat(gui): implement drag and drop
fix(cli): resolve extraction error for large files
fix(gui): correct window sizing on macOS
perf(wd): optimize compression algorithm by 30%
docs: update installation guide
chore(deps): bump Avalonia to 11.1.0
```

### Generated Changelog
```markdown
### ✨ Features
- **[cli]** Add batch processing support [`a1b2c3`](...) - @developer
- **[gui]** Implement drag and drop [`d4e5f6`](...) - @developer

### 🐛 Bug Fixes
- **[cli]** Resolve extraction error for large files [`m3n4o5`](...) - @developer
- **[gui]** Correct window sizing on macOS [`p6q7r8`](...) - @developer

### ⚡ Performance Improvements
- **[wd]** Optimize compression algorithm by 30% [`g7h8i9`](...) - @developer

### 📝 Documentation
- Update installation guide [`s9t0u1`](...) - @developer

### 📦 Dependencies
- Bump Avalonia to 11.1.0 [`v2w3x4`](...) - @dependabot
```

## Best Practices

### ✅ DO
- Use present tense: "add feature" not "added feature"
- Use imperative mood: "move cursor to" not "moves cursor to"
- Keep first line under 72 characters
- Add scope when possible for better organization
- Reference issues/PRs when relevant
- Provide detailed body for complex changes
- Document breaking changes thoroughly
- Use consistent terminology

### ❌ DON'T
- Don't use past tense
- Don't capitalize first letter of subject (after type/scope)
- Don't end subject with a period
- Don't mix multiple unrelated changes in one commit
- Don't forget to add type prefix
- Don't skip breaking change documentation

## Commit Message Template

Create `.gitmessage` in your repository:

```
# <type>(<scope>): <subject>
#
# <body>
#
# <footer>

# Types: feat, fix, perf, refactor, docs, test, build, ci, chore
# Scopes: cli, gui, wd, msh, dae, par, tex, common, api, deps
# Breaking changes: Add ! after type/scope and BREAKING CHANGE: in footer
# Issue references: Closes #123, Fixes #456, Refs #789

# Example:
# feat(cli): add batch processing support
#
# - Allow processing multiple files at once
# - Add progress reporting
# - Support wildcard patterns
#
# Closes #123
```

Set it as your default:
```bash
git config commit.template .gitmessage
```

## Verification

Use commit message linting tools:

### commitlint (Recommended)
```bash
npm install --save-dev @commitlint/cli @commitlint/config-conventional

# .commitlintrc.json
{
  "extends": ["@commitlint/config-conventional"],
  "rules": {
    "scope-enum": [2, "always", [
      "cli", "gui", "wd", "msh", "dae", "par", "tex", "common", "api", "deps"
    ]]
  }
}
```

### Git hook (pre-commit)
```bash
# .git/hooks/commit-msg
#!/bin/sh
npx commitlint --edit $1
```

## Release Type Detection

The release workflow automatically determines the version bump:

| Commits Include | Release Type | Version Change |
|----------------|--------------|----------------|
| `BREAKING CHANGE` or `!` | Major | 1.0.0 → 2.0.0 |
| `feat:` | Minor | 1.0.0 → 1.1.0 |
| `fix:`, `perf:`, etc. | Patch | 1.0.0 → 1.0.1 |

## Integration with GitHub

### Automatic Issue Closing
```bash
# These keywords automatically close issues when PR is merged:
git commit -m "fix(gui): resolve crash on startup

Fixes #123
Closes #456
Resolves #789"
```

### PR Title Convention
PRs should also follow conventional commits:
```
feat(cli): add multi-file support
fix(gui): prevent memory leak
docs: update contributing guidelines
```

## Advanced Examples

### Multi-Scope Feature
```bash
git commit -m "feat(cli,gui): add archive encryption support

- Implement AES-256 encryption in common library
- Add --encrypt flag to CLI
- Add encryption toggle in GUI settings
- Update documentation with encryption guide

Closes #234"
```

### Performance Fix with Benchmark
```bash
git commit -m "perf(wd): optimize decompression for large files

Improved memory allocation strategy reduces peak usage by 60%
and speeds up decompression by 35% for files >100MB.

Benchmark results:
- 100MB file: 2.5s → 1.6s (36% faster)
- 500MB file: 15.2s → 9.8s (35% faster)
- Peak memory: 450MB → 180MB (60% reduction)

Closes #567"
```

### Breaking Change with Detailed Migration
```bash
git commit -m "feat(api)!: redesign archive interface for better async support

BREAKING CHANGE: Archive interface methods are now async.

Old:
  archive.Extract(path)
  archive.Add(file)

New:
  await archive.ExtractAsync(path)
  await archive.AddAsync(file)

Migration Guide:
1. Add async keyword to methods calling archive operations
2. Add await before archive method calls
3. Update return types to Task or Task<T>
4. Run migration tool: earthtool migrate-code ./src

Closes #890"
```

## Summary

- **Always use conventional commit format** for automatic changelog generation
- **Add scopes** to organize changes by component
- **Document breaking changes** thoroughly
- **Reference issues and PRs** when applicable
- **Keep commits focused** on a single logical change
- **Write clear, descriptive messages** for future maintainers

Following these conventions ensures:
- ✅ Automatic, well-organized changelogs
- ✅ Clear project history
- ✅ Easy identification of breaking changes
- ✅ Better collaboration and code review
- ✅ Semantic versioning automation
