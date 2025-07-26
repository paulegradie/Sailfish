# Release Workflow Documentation

## Overview

Sailfish uses an automated release workflow that creates GitHub releases with proper versioning and release notes. This eliminates the version lag problem where you had to update release notes in follow-up PRs.

## Versioning Strategy

- **Major.Minor**: Embedded in workflow filename (e.g., `build-v2.1.yml` → `2.1`)
- **Patch**: GitHub run number (auto-incrementing)
- **Final Version**: `2.1.{run_number}` (e.g., `2.1.45`)
- **PR Versions**: `2.1.{run_number}-pull-request`

## Release Notes Options

### Option 1: Manual Release Notes (Recommended for major releases)

1. **Create release notes during development**:
   ```bash
   # Copy the template
   cp RELEASE_NOTES_TEMPLATE.md RELEASE_NOTES.md
   
   # Edit with your release notes
   # Use NEXT_VERSION as placeholder for version number
   ```

2. **Example content**:
   ```markdown
   ## What's Changed in vNEXT_VERSION
   
   - **Major Feature**: New complex variables system
   - **Breaking Change**: Deprecated .NET 6 support
   - **Bug Fix**: Fixed critical test execution issue
   ```

3. **Commit and push**:
   ```bash
   git add RELEASE_NOTES.md
   git commit -m "Add release notes for next version"
   git push
   ```

4. **Automatic processing**:
   - CI/CD detects `RELEASE_NOTES.md`
   - Replaces `NEXT_VERSION` with actual version
   - Creates GitHub release
   - Attaches NuGet packages
   - Cleans up the release notes file

### Option 2: Auto-Generated Release Notes (For quick releases)

1. **Don't create `RELEASE_NOTES.md`**
2. **Push your changes**
3. **CI/CD automatically**:
   - Generates release notes from recent commits
   - Creates GitHub release
   - Attaches NuGet packages

## Workflow Triggers

### Main Branch (Production Releases)
- **Trigger**: Push to `main` branch
- **Version**: `2.1.{run_number}`
- **Actions**: Build → Test → Pack → Create Release → Push to NuGet

### Pull Requests (Development Builds)
- **Trigger**: PR opened/updated
- **Version**: `2.1.{run_number}-pull-request`
- **Actions**: Build → Test → Pack (no release creation)

## Benefits

✅ **No Version Lag**: Write release notes during development  
✅ **Automatic Versioning**: No manual version number management  
✅ **GitHub Integration**: Releases prominently displayed  
✅ **Asset Management**: NuGet packages attached to releases  
✅ **Flexible**: Choose manual or auto-generated notes  
✅ **Clean Repository**: Release notes file auto-cleaned  

## Migration from Old System

1. **Historical notes**: Preserved in documentation website as archive
2. **Current releases**: Now on GitHub Releases page
3. **User communication**: Documentation updated with redirect
4. **Workflow**: Developers use new `RELEASE_NOTES.md` approach

## Examples

### Major Release with Detailed Notes
```bash
# Create detailed release notes
cp RELEASE_NOTES_TEMPLATE.md RELEASE_NOTES.md
# Edit with comprehensive changes
git add RELEASE_NOTES.md
git commit -m "feat: major complex variables system overhaul"
git push
```

### Quick Bug Fix Release
```bash
# Just push the fix - auto-generated notes
git add .
git commit -m "fix: resolve test execution issue"
git push
```

## Troubleshooting

### Release Not Created
- Check if running on `main` branch
- Verify `pack` job succeeded
- Check GitHub Actions logs

### Wrong Version Number
- Verify workflow filename contains correct major.minor
- Check `set_version` job output

### Missing NuGet Packages
- Verify `pack` job created `.nupkg` files
- Check `find_packages` step output

## Future Enhancements

- **Changelog Generation**: Automatic changelog from release notes
- **Release Notifications**: Slack/Discord integration
- **Semantic Versioning**: Automatic major/minor bumps based on commit messages
