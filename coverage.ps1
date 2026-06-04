#!/usr/bin/env pwsh
# Runs all tests with coverage collection and generates a browsable HTML report.
#
# Usage:
#   .\coverage.ps1          Run tests, build report
#   .\coverage.ps1 -Open    Run tests, build report, then open it in the browser

param(
    [switch]$Open
)

$ErrorActionPreference = "Stop"

$resultsDir = "TestResults"
$reportDir  = "CoverageReport"

# Start clean so stale results from previous runs aren't merged into the report.
if (Test-Path $resultsDir) { Remove-Item $resultsDir -Recurse -Force }

# Step 1: run tests and collect raw coverage data (coverlet.collector -> Cobertura XML).
# coverage.runsettings excludes generated source so it never enters the report data.
dotnet test --collect:"XPlat Code Coverage" --settings coverage.runsettings --results-directory $resultsDir
if ($LASTEXITCODE -ne 0) { throw "Tests failed - report not generated." }

# Step 2: turn the raw XML into an HTML report.
reportgenerator `
    -reports:"$resultsDir/**/coverage.cobertura.xml" `
    -targetdir:"$reportDir" `
    -reporttypes:"Html;TextSummary"

Write-Host ""
Get-Content "$reportDir/Summary.txt"
Write-Host ""
Write-Host "Report: $reportDir/index.html"

if ($Open) {
    $report = "$reportDir/index.html"
    # $IsMacOS / $IsLinux are defined in PowerShell 7+; on Windows (pwsh or 5.1)
    # they're absent/false, so we fall through to Start-Process.
    if ($IsMacOS)     { & open $report }
    elseif ($IsLinux) { & xdg-open $report }
    else              { Start-Process $report }
}
