# Quick Start: Unified Workflows

**5-Minute Guide to the New Unified GitHub Actions Workflows**

## TL;DR

### Old Way
```bash
# Create separate releases
git tag cli-v1.0.0
git tag gui-v1.0.0
git push origin cli-v1.0.0 gui-v1.0.0
# Result: 2 separate releases with basic changelogs
```

### New Way
```bash
# Create single unified release
git tag v1.0.0
git push origin v1.0.0
# Result: 1 release with both CLI and GUI, rich changelog
```

## Three Things You Need to Know

### 1. Use Conventional Commits
```bash
# Instead of:
git commit -m "added new feature"

# Write:
git commit -m "feat(cli): add batch processing support"
```

**Format:** `<type>(<scope>): <description>`

**Common Types:**
- `feat` - New feature
- `fix` - Bug fix
- `perf` - Performance improvement
- `docs` - Documentation
- `refactor` - Code refactoring
- `test` - Tests

**Common Scopes:**
- `cli` - CLI app
- `gui` - GUI app
- `wd` - WD archives
- `msh` - MSH meshes
- `dae` - DAE/COLLADA

### 2. Use Unified Tags
```bash
# Instead of: cli-v1.0.0 and gui-v1.0.0
# Use: v1.0.0 (releases both together)

git tag v1.2.3
git push origin v1.2.3
```

### 3. Smart CI Builds Only What Changed
```bash
# Change CLI code → Only CLI builds
# Change GUI code → Only GUI builds
# Change shared code → Both build

# No manual configuration needed!
```

## Daily Workflow

### Making Changes
```bash
# 1. Create branch
git checkout -b feat/my-feature

# 2. Make changes to code
vim EarthTool.CLI/Program.cs

# 3. Commit with conventional format
git commit -m "feat(cli): add verbose logging option"

# 4. Push (CI runs automatically)
git push origin feat/my-feature

# 5. Create PR
gh pr create --title "feat(cli): add verbose logging"
```

### Testing Preview Builds
```bash
# Merge to dev branch
git checkout dev
git merge feat/my-feature
git push origin dev

# Preview builds created automatically
# Download from: Actions tab → latest workflow run
```

### Creating Release
```bash
# 1. Merge to main
git checkout main
git merge dev
git push

# 2. Tag the release
git tag v1.0.0

# 3. Push the tag
git push origin v1.0.0

# Done! Release created automatically with:
# - 6 artifacts (CLI + GUI × 3 platforms)
# - Beautiful changelog
# - All commits categorized
```

## Commit Examples

### Feature Addition
```bash
git commit -m "feat(gui): add dark mode support"
```

### Bug Fix
```bash
git commit -m "fix(cli): resolve crash when opening empty archives"
```

### Performance Improvement
```bash
git commit -m "perf(wd): reduce memory usage by 40%"
```

### Breaking Change
```bash
git commit -m "feat(api)!: update archive format to v2

BREAKING CHANGE: Archives now use format v2.
Use migration tool to convert old archives."
```

### Multiple Scopes
```bash
git commit -m "feat(cli,gui): add encryption support for archives"
```

## What You Get

### Automatic Changelog
Your commits automatically become a professional changelog:

```markdown
## 🆕 What's Changed in v1.2.0

> 🟡 Minor Release - New features added

### ✨ Features
- **[cli]** Add batch processing support [`abc123`] - @developer
- **[gui]** Add dark mode support [`def456`] - @developer

### 🐛 Bug Fixes
- **[cli]** Resolve crash when opening empty archives [`ghi789`] - @developer

### ⚡ Performance Improvements
- **[wd]** Reduce memory usage by 40% [`jkl012`] - @developer

### 👥 Contributors
- @developer
- @contributor1
- @contributor2

### 📦 Downloads
#### CLI
- Windows (x64): `EarthTool.CLI-Windows-x64.zip`
- Linux (x64): `EarthTool.CLI-Linux-x64.tar.gz`
- macOS (x64): `EarthTool.CLI-macOS-x64.tar.gz`

#### GUI
- Windows (x64): `EarthTool.WD.GUI-Windows-x64.zip`
- Linux (x64): `EarthTool.WD.GUI-Linux-x64.tar.gz`
- macOS (x64): `EarthTool.WD.GUI-macOS-x64.tar.gz`
```

## Cheat Sheet

### Commit Types
| Type | Use When | Example |
|------|----------|---------|
| `feat` | Adding new feature | `feat(cli): add --output flag` |
| `fix` | Fixing a bug | `fix(gui): correct window size` |
| `perf` | Improving performance | `perf(wd): faster compression` |
| `refactor` | Restructuring code | `refactor(msh): simplify parser` |
| `docs` | Updating docs | `docs: update README` |
| `test` | Adding tests | `test(par): add unit tests` |
| `build` | Build changes | `build: update .NET to 8.0.1` |
| `ci` | CI changes | `ci: add code coverage` |
| `chore` | Maintenance | `chore(deps): bump packages` |

### Scopes
| Scope | Component |
|-------|-----------|
| `cli` | CLI application |
| `gui` | GUI application |
| `wd` | WD archives |
| `msh` | MSH meshes |
| `dae` | DAE/COLLADA |
| `par` | PAR parameters |
| `tex` | TEX textures |
| `common` | Shared code |

### Breaking Changes
```bash
# Option 1: Exclamation mark
git commit -m "feat(api)!: change method signatures"

# Option 2: Footer (preferred for details)
git commit -m "feat(api): update archive API

BREAKING CHANGE: Method signatures changed.
See migration guide in docs/MIGRATION.md"
```

### Issue References
```bash
# Link to issues
git commit -m "fix(cli): resolve extraction bug (#123)"

# Close issues automatically
git commit -m "fix(gui): fix crash on startup

Fixes #42
Closes #43"
```

## Workflows Overview

### `ci-unified.yml` - Development
- **Runs on:** Every push, every PR
- **Does:** Builds what changed, runs tests
- **Creates:** Build artifacts (7 days)
- **Special:** Preview builds on dev branch (30 days)

### `release.yml` - Releases
- **Runs on:** Tag push (`v*`)
- **Does:** Builds both CLI and GUI, generates changelog
- **Creates:** GitHub Release with 6 artifacts
- **Special:** Automatic version detection from commits

## Common Questions

### Q: Do I need to change old commits?
**A:** No, just start using conventional format for new commits.

### Q: What if I forget the format?
**A:** CI still works, but changelog won't be as nice. Try to remember!

### Q: Can I still release CLI and GUI separately?
**A:** Not with unified workflows. They're designed for synchronized releases.
If you need separate releases, keep legacy workflows.

### Q: What if CI builds the wrong component?
**A:** Check that you modified files in the right path. 
Smart detection looks at changed file paths.

### Q: How do I test without pushing?
**A:** Use workflow dispatch in GitHub Actions tab for manual testing.

### Q: Can I see an example release?
**A:** After first release with unified workflow, check Releases tab.

## Tips

### ✅ DO
- Use conventional commits for better changelogs
- Add scopes for better organization
- Reference issues in commits (#123)
- Test in dev branch before releasing
- Keep commit messages clear and concise

### ❌ DON'T
- Mix multiple unrelated changes in one commit
- Use past tense ("added" → use "add")
- Forget to push tags after creating them
- Create releases from feature branches
- Skip the scope if it's relevant

## Need More Info?

- **Full Documentation:** `README.md`
- **Commit Guide:** `CONVENTIONAL_COMMITS.md`
- **Migration Guide:** `MIGRATION.md`
- **Architecture Details:** `WORKFLOW_SUMMARY.md`

## One-Command Setup

```bash
# Set commit message template (optional)
cat > .gitmessage << 'EOF'
# <type>(<scope>): <subject>
#
# <body>
#
# <footer>
#
# Types: feat, fix, perf, refactor, docs, test, build, ci, chore
# Scopes: cli, gui, wd, msh, dae, par, tex, common
# Breaking: Add ! after type/scope and BREAKING CHANGE: in footer
EOF

git config commit.template .gitmessage

echo "✅ Commit template set! Run 'git commit' to see it."
```

## Getting Started Right Now

1. **Make a test commit:**
   ```bash
   git commit -m "docs: update workflow documentation" --allow-empty
   ```

2. **See it in action:**
   ```bash
   git push
   # Watch Actions tab in GitHub
   ```

3. **Check the smart detection:**
   ```bash
   # Make CLI change
   echo "// test" >> EarthTool.CLI/Program.cs
   git commit -m "test(cli): verify CI detection"
   git push
   # Only CLI should build!
   ```

4. **Create your first unified release:**
   ```bash
   git tag v1.0.0-test
   git push origin v1.0.0-test
   # Check Releases tab for result!
   ```

---

**That's it!** You're ready to use the unified workflows. Remember:
- ✅ Conventional commits
- ✅ Unified tags (`v*`)
- ✅ Let CI handle the rest

For questions, check the full documentation or open an issue.
