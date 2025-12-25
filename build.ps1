[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$VersionSuffix,
    [string]$NuGetSource = 'https://api.nuget.org/v3/index.json',
    [string]$NuGetApiKey,
    [switch]$UseNet10,
    [string]$LocalNuGetDirectory = 'G:\NuGetPackages'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-DotNet {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

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

    Write-Host "dotnet $($sanitizedArguments -join ' ')" -ForegroundColor Cyan
    dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($sanitizedArguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

# Use the script directory as the repository root to avoid accidentally moving up a level
$repositoryRoot = $PSScriptRoot
Push-Location $repositoryRoot
try {
    $solutionPath = Join-Path $repositoryRoot 'Xcaciv.Command.sln'
    $packageProjectPath = Join-Path $repositoryRoot 'src\Xcaciv.Command\Xcaciv.Command.csproj'
    $artifactDirectory = Join-Path $repositoryRoot 'artifacts\packages'

    if (-not (Test-Path -Path $artifactDirectory)) {
        New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null
    }

    $msbuildProperties = @()
    if ($UseNet10.IsPresent) {
        $msbuildProperties += '-p:UseNet10=true'
    }

    # Discover solution if default path does not exist
    if (-not (Test-Path -Path $solutionPath)) {
        $solutions = Get-ChildItem -Path $repositoryRoot -Filter '*.sln' -Recurse -ErrorAction SilentlyContinue
        if ($solutions -and $solutions.Count -gt 0) {
            $solutionPath = $solutions[0].FullName
            Write-Host "Discovered solution: $solutionPath" -ForegroundColor Yellow
        }
    }

    if (Test-Path -Path $solutionPath) {
        $restoreArguments = @('restore', $solutionPath)
        $restoreArguments += $msbuildProperties
        Invoke-DotNet -Arguments $restoreArguments

        $buildArguments = @('build', $solutionPath, '--configuration', $Configuration, '--nologo')
        $buildArguments += $msbuildProperties
        Invoke-DotNet -Arguments $buildArguments
    }
    else {
        # Fallback to restoring and building the primary package project
        Write-Host "Solution not found. Falling back to building project: $packageProjectPath" -ForegroundColor Yellow
        $restoreArguments = @('restore', $packageProjectPath)
        $restoreArguments += $msbuildProperties
        Invoke-DotNet -Arguments $restoreArguments

        $buildArguments = @('build', $packageProjectPath, '--configuration', $Configuration, '--nologo')
        $buildArguments += $msbuildProperties
        Invoke-DotNet -Arguments $buildArguments
    }

    $packArguments = @('pack', $packageProjectPath, '--configuration', $Configuration, '--output', $artifactDirectory, '--nologo')
    if ($VersionSuffix) {
        $packArguments += "-p:VersionSuffix=$VersionSuffix"
    }
    $packArguments += $msbuildProperties

    Invoke-DotNet -Arguments $packArguments

    $packageFiles = Get-ChildItem -Path $artifactDirectory -Filter '*.nupkg' | Where-Object { $_.Name -notlike '*.snupkg' }
    $symbolPackageFiles = Get-ChildItem -Path $artifactDirectory -Filter '*.snupkg' -ErrorAction SilentlyContinue

    if (-not (Test-Path -Path $LocalNuGetDirectory)) {
        New-Item -ItemType Directory -Path $LocalNuGetDirectory -Force | Out-Null
    }

    $packagesToCopy = @($packageFiles + $symbolPackageFiles) | Where-Object { $_ }
    foreach ($package in $packagesToCopy) {
        Copy-Item -Path $package.FullName -Destination $LocalNuGetDirectory -Force
    }
    Write-Host "Copied packages to $LocalNuGetDirectory." -ForegroundColor Green

    if ($NuGetApiKey) {
        foreach ($packageFile in $packageFiles) {
            Invoke-DotNet -Arguments @('nuget', 'push', $packageFile.FullName, '--source', $NuGetSource, '--api-key', $NuGetApiKey, '--skip-duplicate')
        }
        foreach ($symbolPackageFile in $symbolPackageFiles) {
            Invoke-DotNet -Arguments @('nuget', 'push', $symbolPackageFile.FullName, '--source', $NuGetSource, '--api-key', $NuGetApiKey, '--skip-duplicate')
        }
    }
    else {
        Write-Host "Packages saved to $artifactDirectory (skipping push because no API key was provided)." -ForegroundColor Green
    }
}
finally {
    Pop-Location
}