# Migration Guide: Legacy to Unified Workflows

This guide helps you migrate from the legacy separate CLI/GUI workflows to the new unified architecture.

## Overview

### Before (Legacy)
- 4 separate workflows: `cli-ci.yml`, `cli-release.yml`, `gui-ci.yml`, `gui-release.yml`
- Duplicate code (~800 lines)
- Separate releases with tags `cli-v*` and `gui-v*`
- Separate changelogs
- Potential version mismatch between CLI and GUI

### After (Unified)
- 2 unified workflows: `ci-unified.yml`, `release.yml`
- Shared code and logic
- Single release with tag `v*`
- Unified changelog covering both CLI and GUI
- Synchronized versioning

## Migration Steps

### Step 1: Understand the New Workflows

#### `ci-unified.yml` - Replaces `cli-ci.yml` and `gui-ci.yml`
- Smart change detection (builds only what changed)
- Supports both CLI and GUI
- Preview builds for dev branch (both apps)
- Same triggers as before

#### `release.yml` - Replaces `cli-release.yml` and `gui-release.yml`
- Builds both CLI and GUI
- Creates single release with all artifacts
- Unified changelog based on conventional commits
- Tag format: `v*` (e.g., `v1.0.0`)

### Step 2: Update Your Tagging Convention

#### Old Approach
```bash
# Separate tags for each component
git tag cli-v1.0.0
git tag gui-v1.0.0
git push origin cli-v1.0.0
git push origin gui-v1.0.0
```

#### New Approach
```bash
# Single tag for synchronized release
git tag v1.0.0
git push origin v1.0.0
# This automatically builds and releases BOTH CLI and GUI
```

### Step 3: Adopt Conventional Commits

The new unified workflows generate comprehensive changelogs based on conventional commits.

#### Migration
1. **Read the guide**: See `CONVENTIONAL_COMMITS.md`
2. **Update commit style**: Use `type(scope): subject` format
3. **Add scopes**: Use `cli`, `gui`, `wd`, `msh`, etc.

#### Examples
```bash
# Before
git commit -m "Added batch processing"
git commit -m "Fixed bug in GUI"

# After
git commit -m "feat(cli): add batch processing support"
git commit -m "fix(gui): resolve window sizing issue"
```

### Step 4: Test the New Workflows

#### Option A: Test in Fork/Branch First
1. Create a test branch: `git checkout -b test-unified-workflow`
2. Make a small change
3. Push to test branch
4. Verify `ci-unified.yml` runs correctly
5. Test tag creation: `git tag v0.0.1-test`
6. Push tag: `git push origin v0.0.1-test`
7. Verify release creation

#### Option B: Use Workflow Dispatch
1. Go to Actions tab
2. Select "CI (Unified)" or "Release"
3. Click "Run workflow"
4. Choose branch and options
5. Verify results

### Step 5: Clean Up Legacy Workflows (Optional)

You have three options:

#### Option A: Keep Both (Recommended Initially)
- Keep legacy workflows for backward compatibility
- Use unified workflows for new releases
- Monitor for issues
- Remove legacy later

#### Option B: Rename Legacy Workflows
```bash
cd .github/workflows
mv cli-ci.yml cli-ci.yml.legacy
mv cli-release.yml cli-release.yml.legacy
mv gui-ci.yml gui-ci.yml.legacy
mv gui-release.yml gui-release.yml.legacy
```
GitHub will ignore `.legacy` files.

#### Option C: Delete Legacy Workflows
```bash
cd .github/workflows
git rm cli-ci.yml cli-release.yml gui-ci.yml gui-release.yml
git commit -m "chore(ci): remove legacy workflows, use unified workflows"
```

## Key Differences

### CI Workflow

| Feature | Legacy | Unified |
|---------|--------|---------|
| **Files** | 2 files (cli-ci.yml, gui-ci.yml) | 1 file (ci-unified.yml) |
| **Change Detection** | Path-based triggers only | Smart change detection + path triggers |
| **Builds** | Separate for CLI and GUI | Conditional based on changes |
| **Preview** | GUI only | Both CLI and GUI |
| **Artifacts** | 3 per workflow | 6 total (smart, only changed) |

### Release Workflow

| Feature | Legacy | Unified |
|---------|--------|---------|
| **Files** | 2 files (cli-release.yml, gui-release.yml) | 1 file (release.yml) |
| **Tags** | `cli-v*` and `gui-v*` | `v*` |
| **Releases** | 2 separate releases | 1 unified release |
| **Artifacts** | 3 per release | 6 per release |
| **Changelog** | Separate for each | Unified with sections |
| **Versioning** | Can differ | Always synchronized |

### Changelog Format

#### Legacy
Two separate changelogs:
- "EarthTool CLI v1.0.0" with CLI changes
- "EarthTool.WD.GUI v1.0.0" with GUI changes

#### Unified
One comprehensive changelog:
- "EarthTool v1.0.0" 
- Organized by conventional commit types
- Scopes show which component (CLI/GUI)
- Covers all changes in one place

## Troubleshooting

### Issue: "CI workflow not triggering"

**Solution**: Check path filters in `ci-unified.yml`. The workflow only runs if relevant files changed.

**Fix**: Use workflow dispatch to manually trigger, or push changes to watched paths.

### Issue: "Release creating separate CLI and GUI releases"

**Solution**: You're probably still using old tags (`cli-v*`, `gui-v*`).

**Fix**: Use unified tags (`v*`) instead.

### Issue: "Changelog missing commit information"

**Solution**: Commits not following conventional format.

**Fix**: Adopt conventional commits (see `CONVENTIONAL_COMMITS.md`).

### Issue: "Build failing on unified workflow but passed on legacy"

**Solution**: Matrix configuration difference or dependency issue.

**Fix**: 
1. Check workflow logs for specific error
2. Compare matrix configuration
3. Ensure all dependencies are in solution file

### Issue: "Want to release only CLI or only GUI"

**Solution**: Unified workflow always builds both.

**Options**:
1. Keep legacy workflows for selective releases
2. Modify `release.yml` to add conditional logic
3. Accept synchronized releases (recommended)

## Rollback Plan

If you need to rollback to legacy workflows:

### Step 1: Disable Unified Workflows
```bash
cd .github/workflows
mv ci-unified.yml ci-unified.yml.disabled
mv release.yml release.yml.disabled
```

### Step 2: Restore Legacy Workflows
```bash
# If renamed:
mv cli-ci.yml.legacy cli-ci.yml
mv cli-release.yml.legacy cli-release.yml
mv gui-ci.yml.legacy gui-ci.yml
mv gui-release.yml.legacy gui-release.yml

# If deleted, restore from git:
git checkout HEAD~1 -- .github/workflows/cli-ci.yml
git checkout HEAD~1 -- .github/workflows/cli-release.yml
git checkout HEAD~1 -- .github/workflows/gui-ci.yml
git checkout HEAD~1 -- .github/workflows/gui-release.yml
```

### Step 3: Return to Old Tagging
```bash
# Use old tag format
git tag cli-v1.0.1
git tag gui-v1.0.1
git push origin cli-v1.0.1 gui-v1.0.1
```

## Benefits Checklist

After migration, you should have:

- ✅ Single source of truth for CI/CD configuration
- ✅ Reduced workflow code (from ~1600 to ~800 lines)
- ✅ Synchronized CLI and GUI versions
- ✅ Unified, comprehensive changelogs
- ✅ Conventional commits standard
- ✅ Automatic release type detection (Major/Minor/Patch)
- ✅ Better organized commit history
- ✅ Reduced GitHub Actions minutes usage
- ✅ Easier maintenance and updates
- ✅ Consistent release process

## Timeline Recommendation

### Week 1: Preparation
- Read documentation (README.md, CONVENTIONAL_COMMITS.md)
- Test unified workflows in a branch
- Train team on conventional commits

### Week 2: Transition
- Start using conventional commits
- Use both legacy and unified workflows
- Monitor for issues

### Week 3: Adoption
- Primary use of unified workflows
- Legacy workflows as backup
- Address any issues

### Week 4: Completion
- Full migration to unified workflows
- Remove or archive legacy workflows
- Update all documentation

## Getting Help

If you encounter issues during migration:

1. **Check Documentation**
   - `README.md` - Complete workflow guide
   - `CONVENTIONAL_COMMITS.md` - Commit format guide
   - This file - Migration guide

2. **Review Workflow Logs**
   - Actions tab → Failed workflow → Logs
   - Look for specific error messages

3. **Compare Configurations**
   - Check differences between legacy and unified
   - Verify environment variables
   - Validate matrix configurations

4. **Test Incrementally**
   - Use workflow dispatch for testing
   - Test in a branch first
   - Validate one component at a time

5. **Open an Issue**
   - Provide workflow logs
   - Describe expected vs actual behavior
   - Include reproduction steps

## Summary

The unified workflow architecture provides:
- **Simplification**: 2 workflows instead of 4
- **Consistency**: Same version for CLI and GUI
- **Quality**: Better changelogs with conventional commits
- **Efficiency**: Reduced duplication and maintenance

Migration is straightforward:
1. Test unified workflows
2. Adopt conventional commits
3. Use `v*` tags for releases
4. Remove legacy workflows when confident

The new system is designed to be backward compatible during transition, so you can migrate at your own pace.
