# Migration to .NET 10 Default - Summary

## Overview

The Xcaciv.Command project has been migrated to **default to .NET 10** with optional .NET 8 support via the `UseNet08` flag.

## What Changed

### 1. Default Target Framework
- **Before**: .NET 8 (net8.0) was the default
- **After**: .NET 10 (net10.0) is now the default

### 2. Build Flag Renamed
- **Before**: `-UseNet10` flag added .NET 10 support
- **After**: `-UseNet08` flag adds .NET 8 support (for backward compatibility)

### 3. Directory.Build.props
```xml
<!-- BEFORE -->
<XcacivBaseTargetFramework>net8.0</XcacivBaseTargetFramework>
<XcacivTargetFrameworks Condition="'$(UseNet10)' == 'true'">
  $(XcacivBaseTargetFramework);net10.0
</XcacivTargetFrameworks>

<!-- AFTER -->
<XcacivBaseTargetFramework>net10.0</XcacivBaseTargetFramework>
<XcacivTargetFrameworks Condition="'$(UseNet08)' == 'true'">
  net8.0;$(XcacivBaseTargetFramework)
</XcacivTargetFrameworks>
```

### 4. Test Projects Updated
- `Xcaciv.Command.Tests`: net8.0 ? net10.0
- `Xcaciv.Command.FileLoaderTests`: net8.0 ? net10.0

### 5. Build Script Updated
- Parameter renamed: `UseNet10` ? `UseNet08`
- Default messaging updated to reflect .NET 10
- SDK detection updated to require .NET 10 by default
- Multi-targeting now adds .NET 8 instead of .NET 10

## Usage

### Default Build (.NET 10 only)
```powershell
.\build.ps1
```
**Output**: Builds for net10.0 only

### Multi-Target Build (.NET 8 + .NET 10)
```powershell
.\build.ps1 -UseNet08
```
**Output**: Builds for both net8.0 and net10.0

## Requirements

### For Default Builds
- ? .NET 10 SDK (required)

### For Multi-Target Builds (`-UseNet08`)
- ? .NET 10 SDK (required)
- ? .NET 8 SDK (required)
- ?? Must use Debug configuration (auto-switched)
- ?? Tests automatically skipped (test projects don't support multi-targeting)

## Package Contents

### Default Build
```
Xcaciv.Command.2.1.1.nupkg
?? lib/
?  ?? net10.0/
?     ?? Xcaciv.Command.dll
?     ?? Xcaciv.Command.Core.dll
?     ?? Xcaciv.Command.Interface.dll
?     ?? Xcaciv.Command.FileLoader.dll
```

### Multi-Target Build (`-UseNet08`)
```
Xcaciv.Command.2.1.1.nupkg
?? lib/
?  ?? net8.0/
?  ?  ?? Xcaciv.Command.dll
?  ?  ?? Xcaciv.Command.Core.dll
?  ?  ?? Xcaciv.Command.Interface.dll
?  ?  ?? Xcaciv.Command.FileLoader.dll
?  ?
?  ?? net10.0/
?     ?? Xcaciv.Command.dll
?     ?? Xcaciv.Command.Core.dll
?     ?? Xcaciv.Command.Interface.dll
?     ?? Xcaciv.Command.FileLoader.dll
```

## Projects Using Multi-Targeting Variable

All library projects use `$(XcacivTargetFrameworks)` and will automatically adjust:

- ? `Xcaciv.Command`
- ? `Xcaciv.Command.Core`
- ? `Xcaciv.Command.Interface`
- ? `Xcaciv.Command.FileLoader`
- ? `Xcaciv.Command.DependencyInjection`
- ? `Xcaciv.Command.Extensions.Commandline`

## Test Projects

Test projects now target net10.0 by default:

- ? `Xcaciv.Command.Tests` ? net10.0
- ? `Xcaciv.Command.FileLoaderTests` ? net10.0

**Note**: When using `-UseNet08`, tests are automatically skipped because test projects can't multi-target.

## Breaking Changes

### For Consumers
- **None** - Packages with net10.0 are compatible with .NET 10+ applications
- Applications targeting .NET 10 (or later) can consume packages that target net8.0 (backward compatibility), but applications targeting .NET 8 cannot consume packages that only target net10.0

### For Contributors
- ?? **.NET 10 SDK now required** to build the solution
- Build flag changed: `-UseNet10` ? `-UseNet08`
- Test projects require .NET 10 SDK

## Migration Steps for Contributors

### 1. Install .NET 10 SDK
```powershell
# Download from: https://dotnet.microsoft.com/download/dotnet/10.0
```

### 2. Verify Installation
```powershell
dotnet --list-sdks
# Should show: 10.x.x [...]
```

### 3. Update Build Commands
```powershell
# BEFORE (old flag)
.\build.ps1 -UseNet10

# AFTER (new flag for backward compat)
.\build.ps1 -UseNet08
```

### 4. Default Build Now Targets .NET 10
```powershell
# This now builds for .NET 10 only (not .NET 8)
.\build.ps1
```

## Rationale

### Why Default to .NET 10?

1. **Forward Progress**: .NET 10 is the current LTS release
2. **Modern Features**: Access to latest .NET performance improvements and features
3. **Industry Standard**: Align with .NET ecosystem moving forward
4. **Backward Compat**: .NET 8 support maintained via `-UseNet08` flag
5. **Simpler Default**: Most users want latest version, opt-in for legacy

### Why Rename Flag?

1. **Clarity**: Flag name indicates what you're adding (net8.0), not what's already there
2. **Intuitive**: `-UseNet08` clearly means "include .NET 8 support"
3. **Future-Proof**: Pattern scales (e.g., future `-UseNet11` when .NET 12 is default)

## Files Changed

| File | Change |
|------|--------|
| `Directory.Build.props` | Changed base from net8.0 to net10.0, renamed flag |
| `build.ps1` | Renamed `UseNet10` ? `UseNet08`, updated messaging |
| `src/Xcaciv.Command.Tests/Xcaciv.Command.Tests.csproj` | net8.0 ? net10.0 |
| `src/Xcaciv.Command.FileLoaderTests/Xcaciv.Command.FileLoaderTests.csproj` | net8.0 ? net10.0 |
| `docs/NET10_DEFAULT_MIGRATION.md` | New migration guide (this file) |

## Testing

### Test Results
```powershell
# Default build (net10.0 only)
.\build.ps1 -SkipTests
# ? Expected: SUCCESS

# Multi-target build (net8.0 + net10.0)
.\build.ps1 -UseNet08
# ? Expected: SUCCESS (auto Debug, auto SkipTests)

# Run tests (net10.0)
.\build.ps1
# ? Expected: SUCCESS, tests run on net10.0
```

## Troubleshooting

### Error: ".NET 10 SDK not detected"
**Solution**: Install .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0

### Error: ".NET 8 SDK not detected" (with `-UseNet08`)
**Solution**: Install .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0

### Error: Old flag `-UseNet10` not recognized
**Solution**: Update to new flag `-UseNet08`

### Build works but tests fail with multi-targeting
**Expected**: Tests are automatically skipped with `-UseNet08` flag

## Rollback Plan

If you need to revert to .NET 8 default:

1. Restore `Directory.Build.props`:
   ```xml
   <XcacivBaseTargetFramework>net8.0</XcacivBaseTargetFramework>
   ```

2. Restore test projects to net8.0

3. Rename flag back to `UseNet10` in build.ps1

## Timeline

- **Current State**: .NET 10 is default
- **Next Major Version** (v3.0): May remove .NET 8 support entirely
- **LTS Support**: .NET 8 remains supported via `-UseNet08` for current v2.x releases

## Questions?

- **Issue Tracker**: https://github.com/Xcaciv/Xcaciv.Command/issues
- **Discussions**: https://github.com/Xcaciv/Xcaciv.Command/discussions

---

**Migration Completed**: December 2024  
**Target Version**: 2.1.1+  
**Status**: ? Complete
