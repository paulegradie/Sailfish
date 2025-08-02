# Release Notes Template

When you want to create a release with custom release notes, copy this file to `RELEASE_NOTES.md` in the repository root and edit it with your release notes.

The CI/CD pipeline will:
1. Check for the existence of `RELEASE_NOTES.md`
2. If found, use it as the release notes (replacing `NEXT_VERSION` with the actual version)
3. If not found, auto-generate release notes from recent commits
4. Automatically add NuGet package links
5. Clean up the `RELEASE_NOTES.md` file after creating the release

## Template Format:

```markdown
## What's Changed in vNEXT_VERSION

- **Major Feature**: **Enhanced Complex Variables System** - Improved support for complex object variables using modern provider patterns
    - **Interface-Based Approach**: `ISailfishVariables<TType, TProvider>` pattern for explicit data contracts
    - **Class-Based Approach**: `SailfishVariables<T, TProvider>` class for simplified syntax without custom interfaces
    - **Provider Pattern**: Clean separation of data types from variable generation logic through `ISailfishVariablesProvider<T>` interface
    - **Type Safety**: Enhanced type safety, IntelliSense support, and compile-time checking for complex variable scenarios
    - **Mixed Usage**: Support for combining simple attribute-based variables with complex variables in the same test class

- **Breaking Change**: Deprecated .NET 6 support - now supports .NET 8 and .NET 9 only
- **Framework Upgrades**: Updated all projects to target .NET 8.0 and .NET 9.0 for improved performance and latest features
- **Bug Fix**: Fixed critical issue where subsequent test cases would fail to execute if an earlier test case threw an exception

### Technical Details
- Improved memory management and performance optimizations
- Enhanced error handling and logging capabilities
- Better IDE integration and developer experience

### Migration Guide
For users upgrading from previous versions:
1. Update your project to target .NET 8 or .NET 9
2. Review any breaking changes listed above
3. Test your existing Sailfish tests to ensure compatibility
```

## Usage Instructions:

1. **For releases with detailed notes**: Copy this template to `RELEASE_NOTES.md`, edit with your content, commit and push
2. **For simple releases**: Don't create `RELEASE_NOTES.md` - the system will auto-generate from commits
3. **Version placeholder**: Use `NEXT_VERSION` in your notes - it will be replaced with the actual version number

## Benefits:

- ✅ Write release notes during development without knowing the final version number
- ✅ No more follow-up PRs to update version numbers
- ✅ Automatic fallback to commit-based notes for quick releases
- ✅ NuGet package links automatically added
- ✅ Clean repository (release notes file is removed after use)
