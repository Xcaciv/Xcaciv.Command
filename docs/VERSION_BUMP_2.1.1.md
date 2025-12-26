# Version Bump to 2.1.1 - Complete

## Summary

All project versions have been successfully bumped from **2.1.0** to **2.1.1** to align with the Xcaciv.Loader 2.1.1 migration.

## Projects Updated

| Project | Old Version | New Version | Status |
|---------|-------------|-------------|--------|
| Xcaciv.Command | 2.1.0 | 2.1.1 | ? Updated |
| Xcaciv.Command.Core | 2.1.0 | 2.1.1 | ? Updated |
| Xcaciv.Command.Interface | 2.1.0 | 2.1.1 | ? Updated |
| Xcaciv.Command.FileLoader | 2.1.0 | 2.1.1 | ? Updated |
| Xcaciv.Command.DependencyInjection | (none) | 2.1.1 | ? Added |

## Package References Updated

Updated in `src/Directory.Packages.props`:

| Package | Old Version | New Version |
|---------|-------------|-------------|
| Xcaciv.Command.Interface | 2.1.0 | 2.1.1 |
| Xcaciv.Command.Core | 2.1.0 | 2.1.1 |
| Xcaciv.Loader | 2.1.1 | 2.1.1 (already correct) |

## Verification

### Build Status
```
Build: ? SUCCESS (0 errors, 0 warnings)
```

### Test Results
```
Total tests: 155
Passed: 155
Failed: 0
Skipped: 0
Duration: 2.2s

Status: ? ALL TESTS PASSING
```

## What's in Version 2.1.1

This version includes:

1. **Xcaciv.Loader 2.1.1 Integration**
   - Instance-based security policies
   - Per-plugin sandboxing
   - Enhanced security defaults (Strict mode)
   - Improved trace logging

2. **Security Enhancements**
   - Directory traversal prevention
   - Reflection emit control
   - Path-based plugin isolation
   - Detailed security violation logging

3. **Backward Compatibility**
   - All 155 tests passing
   - No breaking API changes
   - Legacy plugin support via configuration

## Release Notes

### Added
- Instance-based `AssemblySecurityPolicy` configuration in Crawler
- Per-command security context in CommandFactory
- Enhanced trace logging with `[Xcaciv.Loader 2.1.1]` tags
- Comprehensive migration documentation

### Changed
- Default security policy from `Default` to `Strict`
- Enhanced error messages with security context
- Improved exception handling for security violations

### Fixed
- Plugin directory traversal vulnerabilities
- Security context isolation issues

## Documentation

Complete migration documentation available:
- [loader-2.1.1-migration-summary.md](./loader-2.1.1-migration-summary.md)
- [loader-2.1.1-security-quickref.md](./loader-2.1.1-security-quickref.md)
- [MIGRATION_COMPLETE.md](./MIGRATION_COMPLETE.md)

## Next Steps

1. **Review and merge**: This version bump is ready for review
2. **Tag release**: Create git tag `v2.1.1`
3. **Publish packages**: Push to NuGet once approved
4. **Update documentation**: Ensure all references to version numbers are updated
5. **Notify users**: Announce the release with migration guide

## Files Changed

- `src/Xcaciv.Command/Xcaciv.Command.csproj`
- `src/Xcaciv.Command.Core/Xcaciv.Command.Core.csproj`
- `src/Xcaciv.Command.Interface/Xcaciv.Command.Interface.csproj`
- `src/Xcaciv.Command.FileLoader/Xcaciv.Command.FileLoader.csproj`
- `src/Xcaciv.Command.DependencyInjection/Xcaciv.Command.DependencyInjection.csproj`
- `src/Directory.Packages.props`

## Validation Checklist

- [x] All project versions updated to 2.1.1
- [x] Package references updated in Directory.Packages.props
- [x] Build successful with no errors
- [x] All 155 tests passing
- [x] Migration documentation complete
- [x] Security enhancements verified
- [x] Backward compatibility confirmed

---

**Version Bump Completed:** ? December 2024  
**Ready for Release:** ? Yes  
**Breaking Changes:** ? None (backward compatible)
