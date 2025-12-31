#!/usr/bin/env pwsh

# Test verification script for Xcaciv.Command
Write-Host "Building solution..." -ForegroundColor Cyan
$buildResult = dotnet build Xcaciv.Command.sln -c Debug --nologo -v minimal

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build FAILED" -ForegroundColor Red
    exit 1
}

Write-Host "Build succeeded" -ForegroundColor Green

Write-Host "`nRunning tests..." -ForegroundColor Cyan
$testResult = dotnet test Xcaciv.Command.sln -c Debug --no-build --logger "console;verbosity=minimal" 

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n[PASS] ALL TESTS PASSED" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n[FAIL] TESTS FAILED" -ForegroundColor Red
    exit 1
}
