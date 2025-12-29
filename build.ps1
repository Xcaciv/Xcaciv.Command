#Requires -Version 5.1

<#
.SYNOPSIS
    Build script for Xcaciv.Command solution

.DESCRIPTION
    Builds, tests, packs, and optionally publishes NuGet packages for Xcaciv.Command.
    
.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Release

.PARAMETER VersionSuffix
    Version suffix for pre-release packages (e.g., "beta.1")

.PARAMETER NuGetSource
    NuGet source URL for publishing. Default: https://api.nuget.org/v3/index.json

.PARAMETER NuGetApiKey
    API key for NuGet publishing. If not provided, packages won't be pushed.

.PARAMETER UseNet08
    Include .NET 8 target framework in addition to .NET 10 (multi-targeting)

.PARAMETER LocalNuGetDirectory
    Local directory to copy packages to. Default: G:\NuGetPackages

.PARAMETER SkipTests
    Skip running tests

.EXAMPLE
    .\build.ps1
    
.EXAMPLE
    .\build.ps1 -Configuration Release -NuGetApiKey $env:NUGET_API_KEY
    
.EXAMPLE
    .\build.ps1 -Configuration Debug -SkipTests
    
.EXAMPLE
    .\build.ps1 -UseNet08
#>

[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',  # Changed from 'Release' to 'Debug' for .NET 10 default
    
    [string]$VersionSuffix,
    
    [string]$NuGetSource = 'https://api.nuget.org/v3/index.json',
    
    [string]$NuGetApiKey,
    
    [switch]$UseNet08,
    
    [string]$LocalNuGetDirectory = 'G:\NuGetPackages',
    
    [switch]$SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

#region Helper Functions

function Write-BuildStep {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )
    
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
}

function Write-Success {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )
    
    Write-Host "? $Message" -ForegroundColor Green
}

function Write-ErrorMessage {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )
    
    Write-Host "? $Message" -ForegroundColor Red
}

function Invoke-DotNet {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    # Sanitize arguments for display (hide API keys)
    $sanitizedArguments = @()
    for ($argumentIndex = 0; $argumentIndex -lt $Arguments.Length; $argumentIndex++) {
        $currentArgument = $Arguments[$argumentIndex]
        if ($currentArgument -like '--api-key=*') {
            $sanitizedArguments += '--api-key=***REDACTED***'
            continue
        }

        if ($currentArgument -eq '--api-key' -and ($argumentIndex + 1) -lt $Arguments.Length) {
            $sanitizedArguments += $currentArgument
            $sanitizedArguments += '***REDACTED***'
            $argumentIndex++
            continue
        }

        $sanitizedArguments += $currentArgument
    }

    Write-Host "dotnet $($sanitizedArguments -join ' ')" -ForegroundColor DarkGray
    
    & dotnet @Arguments
    
    if ($LASTEXITCODE -ne 0) {
        Write-ErrorMessage "dotnet $($sanitizedArguments -join ' ') failed with exit code $LASTEXITCODE"
        throw "Build failed"
    }
}

function Test-DotNetInstalled {
    try {
        $dotnetVersion = & dotnet --version 2>&1
        Write-Host ".NET SDK version: $dotnetVersion" -ForegroundColor Gray
        
        # Check if .NET 10 SDK is installed (now required by default)
        $sdkList = & dotnet --list-sdks 2>&1
        $hasNet10 = $sdkList | Where-Object { $_ -match '^10\.' }
        
        if (-not $hasNet10) {
            Write-Host "`n? WARNING: .NET 10 SDK not detected" -ForegroundColor Yellow
            Write-Host ".NET 10 SDK is required (projects now default to .NET 10)." -ForegroundColor Yellow
            Write-Host "Available SDKs:" -ForegroundColor Yellow
            $sdkList | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
            Write-Host "`nDownload .NET 10 SDK from: https://dotnet.microsoft.com/download/dotnet/10.0" -ForegroundColor Yellow
            return $false
        }
        
        # Check if .NET 8 is required but not installed (when UseNet08 is specified)
        if ($UseNet08.IsPresent) {
            $hasNet08 = $sdkList | Where-Object { $_ -match '^8\.' }
            
            if (-not $hasNet08) {
                Write-Host "`n? WARNING: .NET 8 SDK not detected" -ForegroundColor Yellow
                Write-Host "The -UseNet08 flag requires .NET 8 SDK to be installed." -ForegroundColor Yellow
                Write-Host "Available SDKs:" -ForegroundColor Yellow
                $sdkList | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
                Write-Host "`nDownload .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
                Write-Host "Or remove the -UseNet08 flag to build for .NET 10 only.`n" -ForegroundColor Yellow
                return $false
            }
        }
        
        return $true
    }
    catch {
        Write-ErrorMessage ".NET SDK is not installed or not in PATH"
        Write-Host "Download from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
        return $false
    }
}

#endregion

#region Main Script

try {
    Write-BuildStep "Xcaciv.Command Build Script"
    
    # Force Debug configuration when UseNet08 is enabled
    # Release mode uses PackageReferences which may not exist for multi-targeting yet
    if ($UseNet08.IsPresent -and $Configuration -eq 'Release') {
        Write-Host "? WARNING: UseNet08 requires Debug configuration (Release uses PackageReferences)" -ForegroundColor Yellow
        Write-Host "Automatically switching to Debug configuration..." -ForegroundColor Yellow
        $Configuration = 'Debug'
    }
    
    Write-Host "Configuration: $Configuration" -ForegroundColor Gray
    
    if ($UseNet08) {
        Write-Host "Multi-targeting: .NET 8 + .NET 10" -ForegroundColor Gray
        if (-not $SkipTests) {
            Write-Host "? Auto-enabling SkipTests: Test projects don't support multi-targeting" -ForegroundColor Yellow
            $SkipTests = $true
        }
    }
    else {
        Write-Host "Target Framework: .NET 10" -ForegroundColor Gray
    }
    
    Write-Host "Skip Tests: $SkipTests" -ForegroundColor Gray
    
    # Verify .NET SDK is installed
    if (-not (Test-DotNetInstalled)) {
        exit 1
    }

    # Use the script directory as the repository root
    $repositoryRoot = $PSScriptRoot
    Write-Host "Repository root: $repositoryRoot" -ForegroundColor Gray
    
    Push-Location $repositoryRoot
    
    # Define paths
    $solutionPath = Join-Path $repositoryRoot 'Xcaciv.Command.sln'
    $packageProjectPath = Join-Path $repositoryRoot 'src\Xcaciv.Command\Xcaciv.Command.csproj'
    $artifactDirectory = Join-Path $repositoryRoot 'artifacts\packages'

    # Create artifacts directory if it doesn't exist
    if (-not (Test-Path -Path $artifactDirectory)) {
        New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null
        Write-Host "Created artifacts directory: $artifactDirectory" -ForegroundColor Gray
    }

    # Build MSBuild properties - MUST use /p: syntax for properties to work correctly
    $msbuildProperties = @()
    if ($UseNet08.IsPresent) {
        $msbuildProperties += '/p:UseNet08=true'
        Write-Host "Note: Building with multi-targeting enabled (net8.0;net10.0)" -ForegroundColor Yellow
    }

    # Discover solution if default path does not exist
    if (-not (Test-Path -Path $solutionPath)) {
        Write-Host "Default solution path not found, searching..." -ForegroundColor Yellow
        $solutions = Get-ChildItem -Path $repositoryRoot -Filter '*.sln' -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($solutions) {
            $solutionPath = $solutions.FullName
            Write-Host "Discovered solution: $solutionPath" -ForegroundColor Yellow
        }
        else {
            Write-Host "No solution file found, will build project directly" -ForegroundColor Yellow
            $solutionPath = $null
        }
    }

    # Step 1: Restore
    Write-BuildStep "Step 1: Restoring Dependencies"
    
    if ($UseNet08.IsPresent) {
        # When UseNet08 is enabled, restore the package project specifically
        # This ensures project references are available for both net8.0 and net10.0
        Write-Host "Restoring package project with multi-targeting..." -ForegroundColor Yellow
        $restoreArguments = @('restore', $packageProjectPath)
        $restoreArguments += $msbuildProperties
        Invoke-DotNet -Arguments $restoreArguments
        Write-Success "Restore completed for package project"
    }
    elseif ($solutionPath -and (Test-Path -Path $solutionPath)) {
        $restoreArguments = @('restore', $solutionPath)
        $restoreArguments += $msbuildProperties
        Invoke-DotNet -Arguments $restoreArguments
        Write-Success "Restore completed for solution"
    }
    elseif (Test-Path -Path $packageProjectPath) {
        $restoreArguments = @('restore', $packageProjectPath)
        $restoreArguments += $msbuildProperties
        Invoke-DotNet -Arguments $restoreArguments
        Write-Success "Restore completed for project"
    }
    else {
        Write-ErrorMessage "Neither solution nor package project found"
        throw "Build failed: No buildable target found"
    }

    # Step 2: Build
    Write-BuildStep "Step 2: Building Solution"
    
    # When UseNet08 is enabled, only build the package project to avoid test project issues
    if ($UseNet08.IsPresent) {
        Write-Host "Building package project only (test projects excluded)" -ForegroundColor Yellow
        $buildArguments = @('build', $packageProjectPath, '--configuration', $Configuration, '--no-restore', '--nologo')
        $buildArguments += $msbuildProperties
        Invoke-DotNet -Arguments $buildArguments
        Write-Success "Build completed for package project"
    }
    elseif ($solutionPath -and (Test-Path -Path $solutionPath)) {
        $buildArguments = @('build', $solutionPath, '--configuration', $Configuration, '--no-restore', '--nologo')
        $buildArguments += $msbuildProperties
        Invoke-DotNet -Arguments $buildArguments
        Write-Success "Build completed for solution"
    }
    else {
        $buildArguments = @('build', $packageProjectPath, '--configuration', $Configuration, '--no-restore', '--nologo')
        $buildArguments += $msbuildProperties
        Invoke-DotNet -Arguments $buildArguments
        Write-Success "Build completed for project"
    }

    # Step 3: Test (optional)
    if (-not $SkipTests -and $solutionPath -and (Test-Path -Path $solutionPath)) {
        Write-BuildStep "Step 3: Running Tests"
        
        if ($UseNet08.IsPresent) {
            Write-Host "Note: Tests will run on all target frameworks (net8.0 and net10.0)" -ForegroundColor Yellow
        }
        
        $testArguments = @('test', $solutionPath, '--configuration', $Configuration, '--no-build', '--nologo', '--verbosity', 'normal')
        $testArguments += $msbuildProperties
        
        try {
            Invoke-DotNet -Arguments $testArguments
            Write-Success "All tests passed"
        }
        catch {
            Write-ErrorMessage "Tests failed"
            throw
        }
    }
    elseif ($SkipTests) {
        Write-Host "Skipping tests (SkipTests flag set)" -ForegroundColor Yellow
    }

    # Step 4: Pack
    Write-BuildStep "Step 4: Creating NuGet Packages"
    
    if ($UseNet08.IsPresent) {
        Write-Host "Note: Package will contain assemblies for both net8.0 and net10.0" -ForegroundColor Yellow
    }
    
    # Note: Not using --no-build because of project reference structure in Xcaciv.Command
    $packArguments = @('pack', $packageProjectPath, '--configuration', $Configuration, '--output', $artifactDirectory, '--nologo')
    
    if ($VersionSuffix) {
        $packArguments += "--version-suffix", $VersionSuffix
        Write-Host "Version suffix: $VersionSuffix" -ForegroundColor Gray
    }
    
    $packArguments += $msbuildProperties
    Invoke-DotNet -Arguments $packArguments
    Write-Success "Packages created"

    # Step 5: Copy to local directory
    Write-BuildStep "Step 5: Copying Packages to Local Directory"
    
    $packageFiles = Get-ChildItem -Path $artifactDirectory -Filter '*.nupkg' | Where-Object { $_.Name -notlike '*.symbols.nupkg' }
    $symbolPackageFiles = Get-ChildItem -Path $artifactDirectory -Filter '*.snupkg' -ErrorAction SilentlyContinue

    if (-not (Test-Path -Path $LocalNuGetDirectory)) {
        New-Item -ItemType Directory -Path $LocalNuGetDirectory -Force | Out-Null
        Write-Host "Created local NuGet directory: $LocalNuGetDirectory" -ForegroundColor Gray
    }

    $packagesToCopy = @($packageFiles + $symbolPackageFiles) | Where-Object { $_ }
    
    if ($packagesToCopy.Count -eq 0) {
        Write-ErrorMessage "No packages found to copy"
    }
    else {
        foreach ($package in $packagesToCopy) {
            Copy-Item -Path $package.FullName -Destination $LocalNuGetDirectory -Force
            Write-Host "  Copied: $($package.Name)" -ForegroundColor Gray
        }
        Write-Success "Copied $($packagesToCopy.Count) package(s) to $LocalNuGetDirectory"
    }

    # Step 6: Push to NuGet (optional)
    if ($NuGetApiKey) {
        Write-BuildStep "Step 6: Publishing to NuGet"
        
        $pushCount = 0
        
        foreach ($packageFile in $packageFiles) {
            try {
                Invoke-DotNet -Arguments @('nuget', 'push', $packageFile.FullName, '--source', $NuGetSource, '--api-key', $NuGetApiKey, '--skip-duplicate')
                Write-Host "  Pushed: $($packageFile.Name)" -ForegroundColor Gray
                $pushCount++
            }
            catch {
                Write-Host "  Warning: Failed to push $($packageFile.Name)" -ForegroundColor Yellow
            }
        }
        
        foreach ($symbolPackageFile in $symbolPackageFiles) {
            try {
                Invoke-DotNet -Arguments @('nuget', 'push', $symbolPackageFile.FullName, '--source', $NuGetSource, '--api-key', $NuGetApiKey, '--skip-duplicate')
                Write-Host "  Pushed: $($symbolPackageFile.Name)" -ForegroundColor Gray
                $pushCount++
            }
            catch {
                Write-Host "  Warning: Failed to push $($symbolPackageFile.Name)" -ForegroundColor Yellow
            }
        }
        
        Write-Success "Published $pushCount package(s) to NuGet"
    }
    else {
        Write-Host "`nSkipping NuGet publish (no API key provided)" -ForegroundColor Yellow
        Write-Host "Packages are available at: $artifactDirectory" -ForegroundColor Gray
    }

    # Build summary
    Write-BuildStep "Build Summary"
    Write-Host "Configuration: $Configuration" -ForegroundColor Gray
    
    if ($UseNet08.IsPresent) {
        Write-Host "Target Frameworks: net8.0, net10.0" -ForegroundColor Gray
    }
    else {
        Write-Host "Target Framework: net10.0" -ForegroundColor Gray
    }
    
    Write-Host "Artifacts: $artifactDirectory" -ForegroundColor Gray
    Write-Host "Local copy: $LocalNuGetDirectory" -ForegroundColor Gray
    
    if ($packageFiles) {
        Write-Host "`nPackages created:" -ForegroundColor Gray
        foreach ($pkg in $packageFiles) {
            Write-Host "  - $($pkg.Name)" -ForegroundColor Gray
        }
    }
    
    Write-Success "`nBuild completed successfully!"
    
    exit 0
}
catch {
    Write-Host "`n" -NoNewline
    Write-ErrorMessage "Build failed: $($_.Exception.Message)"
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}

#endregion