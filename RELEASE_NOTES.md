## What's Changed in vNEXT_VERSION

- **New Feature**: Transitioned to GitHub Releases for better release management
    - **Automated Release Creation**: CI/CD pipeline now automatically creates GitHub releases
    - **Flexible Release Notes**: Support for both manual and auto-generated release notes
    - **NuGet Package Integration**: Packages are automatically attached to releases
    - **Better Discoverability**: Releases are prominently displayed on the GitHub repository

- **Documentation Update**: Updated release notes page to redirect to GitHub Releases
    - **Historical Archive**: Previous release notes preserved for reference
    - **Clear Migration Path**: Users directed to new location for current releases

- **Developer Experience**: Simplified release workflow eliminates version number lag
    - **Write During Development**: Create release notes without knowing final version number
    - **Automatic Version Replacement**: `NEXT_VERSION` placeholder replaced with actual version
    - **Clean Repository**: Release notes file automatically cleaned up after use

### Migration Benefits

This change solves the version lag problem where release notes had to be updated in follow-up PRs after seeing the final version number. Now you can:

1. Write release notes during feature development
2. Use `NEXT_VERSION` as a placeholder
3. Commit and push - the CI/CD pipeline handles the rest
4. No more follow-up PRs needed!

### For Users

- Visit [GitHub Releases](https://github.com/paulegradie/Sailfish/releases) for the latest release information
- Subscribe to release notifications on GitHub to stay updated
- Download NuGet packages directly from release pages
