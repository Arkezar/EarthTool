# Workflow Architecture Summary

## Quick Overview

### New Files (Recommended)
- **`release.yml`** - Unified release workflow for both CLI and GUI
- **`ci-unified.yml`** - Unified CI workflow with smart change detection
- **`CONVENTIONAL_COMMITS.md`** - Guide for commit message format
- **`MIGRATION.md`** - Migration guide from legacy workflows
- **This file** - Quick reference summary

### Existing Files
- **`ci.yml`** - Full solution CI (kept for comprehensive testing)
- **`cli-ci.yml`** - Legacy CLI CI (deprecated)
- **`cli-release.yml`** - Legacy CLI release (deprecated)
- **`gui-ci.yml`** - Legacy GUI CI (deprecated)
- **`gui-release.yml`** - Legacy GUI release (deprecated)

## Architecture Comparison

```
┌─────────────────────────────────────────────────────────────┐
│                      LEGACY ARCHITECTURE                    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────┐                    ┌──────────────┐      │
│  │  cli-ci.yml  │                    │  gui-ci.yml  │      │
│  │              │                    │              │      │
│  │  • Build CLI │                    │  • Build GUI │      │
│  │  • Test CLI  │                    │  • Test GUI  │      │
│  │  • Artifacts │                    │  • Artifacts │      │
│  └──────────────┘                    └──────────────┘      │
│                                                             │
│  ┌──────────────┐                    ┌──────────────┐      │
│  │cli-release   │                    │gui-release   │      │
│  │              │                    │              │      │
│  │  Tag: cli-v* │                    │  Tag: gui-v* │      │
│  │  • Build     │                    │  • Build     │      │
│  │  • Changelog │                    │  • Changelog │      │
│  │  • Release   │                    │  • Release   │      │
│  └──────────────┘                    └──────────────┘      │
│                                                             │
│  Result: 4 separate workflows, duplicate code              │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                   UNIFIED ARCHITECTURE (NEW)                │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────────────────────────────────┐       │
│  │              ci-unified.yml                      │       │
│  │                                                  │       │
│  │  Smart Change Detection                          │       │
│  │  ┌──────────────┐       ┌──────────────┐        │       │
│  │  │  Build CLI   │       │  Build GUI   │        │       │
│  │  │  (if changed)│       │  (if changed)│        │       │
│  │  └──────────────┘       └──────────────┘        │       │
│  │                                                  │       │
│  │  Preview Builds (dev branch)                    │       │
│  │  ┌──────────────┐       ┌──────────────┐        │       │
│  │  │  CLI Preview │       │  GUI Preview │        │       │
│  │  └──────────────┘       └──────────────┘        │       │
│  └─────────────────────────────────────────────────┘       │
│                                                             │
│  ┌─────────────────────────────────────────────────┐       │
│  │               release.yml                        │       │
│  │                                                  │       │
│  │  Tag: v* (unified)                               │       │
│  │                                                  │       │
│  │  ┌──────────────┐       ┌──────────────┐        │       │
│  │  │  Build CLI   │       │  Build GUI   │        │       │
│  │  │  3 platforms │       │  3 platforms │        │       │
│  │  └──────────────┘       └──────────────┘        │       │
│  │                                                  │       │
│  │  ┌───────────────────────────────────┐          │       │
│  │  │  Unified Changelog                │          │       │
│  │  │  • Conventional Commits           │          │       │
│  │  │  • Scoped by component (CLI/GUI)  │          │       │
│  │  │  • Auto categorization            │          │       │
│  │  └───────────────────────────────────┘          │       │
│  │                                                  │       │
│  │  Single Release with 6 artifacts                │       │
│  └─────────────────────────────────────────────────┘       │
│                                                             │
│  Result: 2 unified workflows, shared code, better changelog│
└─────────────────────────────────────────────────────────────┘
```

## Workflow Triggers

### `ci-unified.yml`
```yaml
Triggers:
  - push: main/master/dev/feature/** (when relevant files change)
  - pull_request: main/master/dev (when relevant files change)
  - workflow_dispatch: manual trigger

Smart Detection:
  - Detects CLI changes → builds CLI only
  - Detects GUI changes → builds GUI only
  - Detects shared code → builds both
  - Manual dispatch → builds both
```

### `release.yml`
```yaml
Triggers:
  - push: tags v* (e.g., v1.0.0)
  - workflow_dispatch: manual with release flag

Behavior:
  - Always builds both CLI and GUI
  - Creates single release with all artifacts
  - Generates unified changelog
```

## Conventional Commits Integration

### Commit Format
```
<type>(<scope>): <subject>

[optional body]

[optional footer]
```

### Changelog Mapping

| Commit Pattern | Changelog Section | Example |
|----------------|-------------------|---------|
| `feat(cli):` | ✨ Features → [cli] | `feat(cli): add batch mode` |
| `feat(gui):` | ✨ Features → [gui] | `feat(gui): add dark theme` |
| `fix(cli):` | 🐛 Bug Fixes → [cli] | `fix(cli): resolve crash` |
| `fix(gui):` | 🐛 Bug Fixes → [gui] | `fix(gui): fix layout` |
| `perf(wd):` | ⚡ Performance → [wd] | `perf(wd): faster compress` |
| `docs:` | 📝 Documentation | `docs: update README` |
| `test:` | ✅ Tests | `test: add unit tests` |
| `chore(deps):` | 📦 Dependencies | `chore(deps): bump package` |

### Release Type Detection

```
Commits Include             → Release Type → Version Bump
─────────────────────────────────────────────────────────
BREAKING CHANGE or feat!:   → Major        → 1.0.0 → 2.0.0
feat:                       → Minor        → 1.0.0 → 1.1.0
fix:, perf:, refactor:      → Patch        → 1.0.0 → 1.0.1
```

## Migration Path

### Phase 1: Testing (Week 1-2)
```
1. Keep all workflows active
2. Test unified workflows in branches
3. Verify smart change detection works
4. Practice conventional commits
```

### Phase 2: Parallel Running (Week 3-4)
```
1. Start using unified workflows primarily
2. Keep legacy as backup
3. Create releases with both:
   - New: v1.0.0 (unified)
   - Old: cli-v1.0.0, gui-v1.0.0 (legacy)
4. Monitor for issues
```

### Phase 3: Full Migration (Week 5+)
```
1. Use only unified workflows
2. Only v* tags for releases
3. Archive/delete legacy workflows
4. Update documentation
```

## Quick Command Reference

### Development Flow
```bash
# Make changes
git checkout -b feature/new-feature

# Commit with conventional format
git commit -m "feat(cli): add new feature"

# Push - ci-unified.yml runs automatically
git push origin feature/new-feature

# Create PR - tests run again
gh pr create
```

### Release Flow
```bash
# Ensure on main/master
git checkout main
git pull

# Create release tag
git tag v1.0.0

# Push tag - release.yml runs
git push origin v1.0.0

# Release created automatically with:
# - 6 build artifacts (CLI + GUI, 3 platforms each)
# - Unified changelog
# - All commits since last release
```

### Preview Testing (dev branch)
```bash
# Merge to dev
git checkout dev
git merge feature/new-feature
git push origin dev

# ci-unified.yml creates preview builds
# Check Actions tab for download links
```

## File Structure

```
.github/
└── workflows/
    ├── ci-unified.yml          ⭐ NEW - Unified CI
    ├── release.yml             ⭐ NEW - Unified Release
    ├── CONVENTIONAL_COMMITS.md ⭐ NEW - Commit guide
    ├── MIGRATION.md            ⭐ NEW - Migration guide
    ├── WORKFLOW_SUMMARY.md     ⭐ NEW - This file
    ├── README.md               ✏️  UPDATED - Main docs
    ├── ci.yml                  ✅ KEEP - Full solution CI
    ├── cli-ci.yml              ❌ DEPRECATED
    ├── cli-release.yml         ❌ DEPRECATED
    ├── gui-ci.yml              ❌ DEPRECATED
    └── gui-release.yml         ❌ DEPRECATED
```

## Benefits Summary

### Code Reduction
- **Before**: ~1600 lines (4 workflows × ~400 lines)
- **After**: ~800 lines (2 workflows × ~400 lines)
- **Savings**: 50% reduction in workflow code

### Maintenance
- **Before**: Update 4 files for any workflow change
- **After**: Update 1-2 files (typically just 1)
- **Savings**: 75% reduction in maintenance effort

### Release Process
- **Before**: 
  - Create 2 tags (cli-v*, gui-v*)
  - Get 2 separate releases
  - Manual version synchronization
  - Separate changelogs
  
- **After**:
  - Create 1 tag (v*)
  - Get 1 unified release
  - Automatic version synchronization
  - Unified changelog with component sections

### Changelog Quality
- **Before**: Basic git log output, manual categorization
- **After**: 
  - Automatic categorization by type
  - Scoped by component
  - Breaking change detection
  - Issue/PR linking
  - Contributor attribution
  - Statistics and metrics

## Common Use Cases

### Case 1: CLI-only change
```bash
# Modify CLI code
vim EarthTool.CLI/Program.cs

# Commit with scope
git commit -m "feat(cli): add verbose logging"

# Push - only CLI builds
git push

# Result: 3 CLI artifacts (win/linux/mac), no GUI builds
```

### Case 2: GUI-only change
```bash
# Modify GUI code
vim EarthTool.WD.GUI/Views/MainWindow.axaml.cs

# Commit with scope
git commit -m "fix(gui): correct window title"

# Push - only GUI builds
git push

# Result: 3 GUI artifacts (win/linux/mac), no CLI builds
```

### Case 3: Shared library change
```bash
# Modify shared code
vim EarthTool.WD/Services/ArchiverService.cs

# Commit with scope
git commit -m "perf(wd): optimize compression"

# Push - both CLI and GUI build
git push

# Result: 6 artifacts total (both apps, all platforms)
```

### Case 4: Release with multiple changes
```bash
# Multiple commits
git commit -m "feat(cli): add batch mode"
git commit -m "feat(gui): add drag and drop"
git commit -m "fix(cli): resolve crash on empty files"
git commit -m "perf(wd): improve decompression speed"

# Tag and release
git tag v1.2.0
git push origin v1.2.0

# Result: Single release with:
# - All 6 artifacts
# - Changelog with 4 sections:
#   ✨ Features (2 items: 1 CLI, 1 GUI)
#   🐛 Bug Fixes (1 item: CLI)
#   ⚡ Performance (1 item: WD library)
```

## Decision Matrix

### When to use `ci-unified.yml`?
- ✅ Every push to watched branches
- ✅ Every pull request
- ✅ Testing changes before release
- ✅ Getting preview builds from dev
- ✅ Manual testing via workflow dispatch

### When to use `release.yml`?
- ✅ Creating official releases
- ✅ Publishing to GitHub Releases
- ✅ Generating changelogs
- ✅ Distributing to end users

### When to use `ci.yml`?
- ✅ Full solution validation
- ✅ Testing all projects together
- ✅ Comprehensive quality checks
- ✅ Pre-release validation

## Metrics

### Build Time (Estimated)

| Scenario | Legacy | Unified | Improvement |
|----------|--------|---------|-------------|
| CLI change only | 15 min | 15 min | Same |
| GUI change only | 15 min | 15 min | Same |
| Both changed | 30 min (2×15) | 30 min | Same |
| Full release | 30 min (2 releases) | 30 min (1 release) | Same time, simpler |

### GitHub Actions Minutes

| Period | Legacy | Unified | Savings |
|--------|--------|---------|---------|
| Typical development (80% single component changes) | 100 min | 80 min | 20% |
| Heavy development (50/50 split) | 100 min | 100 min | 0% |
| Release | 30 min | 30 min | 0% |

### Developer Experience

| Aspect | Legacy | Unified | Winner |
|--------|--------|---------|--------|
| Tag creation | 2 tags | 1 tag | ✅ Unified |
| Version sync | Manual | Automatic | ✅ Unified |
| Changelog | Basic | Rich | ✅ Unified |
| Release count | 2 | 1 | ✅ Unified |
| Workflow maint. | 4 files | 2 files | ✅ Unified |
| Build artifacts | 3+3 | 3+3 | Tie |

## Next Steps

1. **Read Documentation**
   - README.md (comprehensive guide)
   - CONVENTIONAL_COMMITS.md (commit format)
   - MIGRATION.md (migration plan)

2. **Test in Branch**
   - Create test branch
   - Make changes
   - Verify workflows run

3. **Adopt Conventions**
   - Start using conventional commits
   - Add scopes to commits
   - Practice on feature branches

4. **Monitor and Adjust**
   - Watch workflow runs
   - Review generated changelogs
   - Fine-tune as needed

5. **Complete Migration**
   - Use unified workflows exclusively
   - Archive legacy workflows
   - Update team documentation

## Support

For questions or issues:
- Check workflow logs in Actions tab
- Review documentation in this directory
- Open an issue with logs and details
