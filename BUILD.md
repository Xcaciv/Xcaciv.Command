# Build Script Usage Guide

## Overview

The `build.ps1` PowerShell script automates the build, test, pack, and publish process for the Xcaciv.Command solution.

## Prerequisites

- **.NET 8 SDK** or later installed
- **PowerShell 5.1** or later
- **Git** (optional, for repository operations)

## Quick Start

### Basic Build

```powershell
.\build.ps1
```

This will:
1. Restore NuGet dependencies
2. Build the solution in Release configuration
3. Run all tests
4. Create NuGet packages
5. Copy packages to local directory (`G:\NuGetPackages`)

### Build Without Tests

```powershell
.\build.ps1 -SkipTests
```

### Debug Build

```powershell
.\build.ps1 -Configuration Debug
```

### Build and Publish to NuGet

```powershell
.\build.ps1 -NuGetApiKey $env:NUGET_API_KEY
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Configuration` | String | `Release` | Build configuration (Debug or Release) |
| `VersionSuffix` | String | (none) | Version suffix for pre-release packages (e.g., "beta.1") |
| `NuGetSource` | String | `https://api.nuget.org/v3/index.json` | NuGet source URL for publishing |
| `NuGetApiKey` | String | (none) | API key for NuGet publishing |
| `UseNet10` | Switch | `false` | Use .NET 10 target framework in addition to .NET 8 (multi-targeting) |
| `LocalNuGetDirectory` | String | `G:\NuGetPackages` | Local directory to copy packages to |
| `SkipTests` | Switch | `false` | Skip running tests |

## Common Scenarios

### Scenario 1: Local Development Build

Build and test locally without publishing:

```powershell
.\build.ps1 -Configuration Debug
```

### Scenario 2: CI/CD Build

Build, test, and publish to NuGet (typically in CI/CD pipeline):

```powershell
.\build.ps1 -Configuration Release -NuGetApiKey $env:NUGET_API_KEY
```

### Scenario 3: Pre-release Build

Build with version suffix for pre-release:

```powershell
.\build.ps1 -VersionSuffix "beta.1"
```

This creates packages like `Xcaciv.Command.2.1.1-beta.1.nupkg`

### Scenario 4: Quick Build (No Tests)

Fast build for local testing without running unit tests:

```powershell
.\build.ps1 -Configuration Debug -SkipTests
```

### Scenario 5: Custom Local Package Directory

Use a different local package directory:

```powershell
.\build.ps1 -LocalNuGetDirectory "C:\MyPackages"
```

### Scenario 6: Multi-Targeting Build

Build for both .NET 8 and .NET 10:

```powershell
.\build.ps1 -UseNet10
```

This creates packages with assemblies for both net8.0 and net10.0, providing better compatibility across .NET versions.

**Note:** Requires .NET 10 SDK to be installed.

## Build Steps

The script performs the following steps in order:

1. **Verification**: Checks that .NET SDK is installed
2. **Restore**: Restores NuGet package dependencies
3. **Build**: Compiles the solution
4. **Test**: Runs all unit tests (unless `-SkipTests` is specified)
5. **Pack**: Creates NuGet packages (.nupkg and .snupkg)
6. **Copy**: Copies packages to local directory
7. **Publish**: Pushes packages to NuGet (if API key is provided)

## Output

### Artifacts Directory

Packages are created in:
```
artifacts\packages\
```

### Local Copy

Packages are copied to the local directory (default: `G:\NuGetPackages`) for easy access.

### Package Files

The script creates:
- **Main packages**: `Xcaciv.Command.{version}.nupkg`
- **Symbol packages**: `Xcaciv.Command.{version}.snupkg`

## Troubleshooting

### Issue: "Running scripts is disabled on this system"

**Solution**: Run PowerShell as Administrator and execute:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

Or run the script with bypass:
```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

### Issue: ".NET SDK is not installed"

**Solution**: Download and install the .NET SDK from:
https://dotnet.microsoft.com/download

### Issue: "Build failed with exit code 1"

**Solution**: 
1. Check the error messages in the output
2. Ensure all dependencies are restored: `dotnet restore`
3. Try cleaning the solution: `dotnet clean`
4. Check that you're in the correct directory (repository root)

### Issue: Tests failing

**Solution**:
1. Run tests manually: `dotnet test`
2. Check test output for specific failures
3. Use `-SkipTests` to build without running tests while investigating

### Issue: "No packages found to copy"

**Solution**: 
1. Ensure the pack step completed successfully
2. Check the `artifacts\packages` directory
3. Verify the project file has `GeneratePackageOnBuild` enabled

## Environment Variables

You can set these environment variables for convenience:

```powershell
# Set NuGet API key
$env:NUGET_API_KEY = "your-api-key-here"

# Use in build
.\build.ps1 -NuGetApiKey $env:NUGET_API_KEY
```

## Integration with Visual Studio

You can run the build script from:

1. **Terminal**: Open Terminal in Visual Studio and run `.\build.ps1`
2. **Task Runner**: Configure as a Task Runner in VS Code
3. **Pre-build event**: Add as a pre-build event in project properties

## Continuous Integration

### GitHub Actions Example

```yaml
- name: Build and Test
  run: |
    pwsh ./build.ps1 -Configuration Release
  
- name: Publish to NuGet
  if: github.ref == 'refs/heads/main'
  run: |
    pwsh ./build.ps1 -Configuration Release -NuGetApiKey ${{ secrets.NUGET_API_KEY }}
```

### Azure Pipelines Example

```yaml
- task: PowerShell@2
  inputs:
    filePath: 'build.ps1'
    arguments: '-Configuration Release -NuGetApiKey $(NUGET_API_KEY)'
```

## Version Management

The script reads version from project files. To update version:

1. Edit `src/Xcaciv.Command/Xcaciv.Command.csproj`
2. Update `<Version>` element
3. Run build script

For pre-release versions, use `-VersionSuffix`:
```powershell
.\build.ps1 -VersionSuffix "alpha.1"
```

## Security Best Practices

1. **Never commit API keys**: Use environment variables or secret managers
2. **Use skip-duplicate**: The script uses `--skip-duplicate` to avoid errors when republishing
3. **Mask sensitive output**: API keys are automatically redacted in console output
4. **Use HTTPS**: Always use HTTPS for NuGet sources

## Performance Tips

1. **Skip tests for faster builds**: Use `-SkipTests` when testing build only
2. **Use Debug for local dev**: Debug builds are faster than Release
3. **Clean artifacts**: Periodically clean the `artifacts` directory to save space

## Help

For more help with the script:

```powershell
Get-Help .\build.ps1 -Detailed
```

Or view examples:

```powershell
Get-Help .\build.ps1 -Examples
```

## Support

- **Issues**: https://github.com/Xcaciv/Xcaciv.Command/issues
- **Documentation**: See `docs/` directory
- **Discussions**: https://github.com/Xcaciv/Xcaciv.Command/discussions
