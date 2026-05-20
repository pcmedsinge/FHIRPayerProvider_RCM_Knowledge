# Synthea Setup Script for Windows

Write-Host "=== Synthea Data Generator Setup ===" -ForegroundColor Cyan
Write-Host ""

$dataDir = $PSScriptRoot
$syntheaVersion = "3.2.0"
$syntheaUrl = "https://github.com/synthetichealth/synthea/releases/download/v$syntheaVersion/synthea-with-dependencies.jar"
$syntheaJar = Join-Path $dataDir "synthea.jar"
$outputDir = Join-Path $dataDir "output"

# Check Java
Write-Host "Checking Java installation..." -ForegroundColor Yellow
$javaCommand = Get-Command java -ErrorAction SilentlyContinue

if ($null -eq $javaCommand) {
    Write-Host "X Java is not installed" -ForegroundColor Red
    Write-Host ""
    Write-Host "Installing Java via winget..." -ForegroundColor Yellow
    winget install EclipseAdoptium.Temurin.11.JDK --accept-package-agreements --accept-source-agreements
    Write-Host ""
    Write-Host "Java installed. Please close and reopen PowerShell, then run this script again." -ForegroundColor Green
    exit 0
}

$javaVersion = java -version 2>&1 | Select-Object -First 1
Write-Host "Java is installed: $javaVersion" -ForegroundColor Green

# Download Synthea
if (-not (Test-Path $syntheaJar)) {
    Write-Host ""
    Write-Host "Downloading Synthea v$syntheaVersion..." -ForegroundColor Yellow
    Invoke-WebRequest -Uri $syntheaUrl -OutFile $syntheaJar -UseBasicParsing
    Write-Host "Synthea downloaded successfully" -ForegroundColor Green
}
else {
    Write-Host "Synthea already downloaded" -ForegroundColor Green
}

# Create output directory
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

Write-Host ""
Write-Host "=== Synthea is ready! ===" -ForegroundColor Green
Write-Host ""
