# Generate richer synthetic EHR-style FHIR data with Synthea.
# This script is aimed at facade/API testing where broader clinical depth is needed.

param(
    [int]$PopulationSize = 150,
    [string]$State = "Massachusetts",
    [int]$YearsOfHistory = 8,
    [int]$Seed = 8675309,
    [switch]$CleanOutput = $true
)

Write-Host "=== Generating Clinical-Rich Synthea Data ===" -ForegroundColor Cyan
Write-Host ""

$dataDir = $PSScriptRoot
$syntheaJar = Join-Path $dataDir "synthea.jar"
$outputDir = Join-Path $dataDir "output"
$fhirDir = Join-Path $outputDir "fhir"

if (-not (Test-Path $syntheaJar)) {
    Write-Host "Synthea jar not found at: $syntheaJar" -ForegroundColor Red
    Write-Host "Run Setup-Synthea.ps1 first." -ForegroundColor Yellow
    exit 1
}

if ($YearsOfHistory -lt 1 -or $YearsOfHistory -gt 20) {
    Write-Host "YearsOfHistory must be between 1 and 20." -ForegroundColor Red
    exit 1
}

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Population: $PopulationSize" -ForegroundColor White
Write-Host "  State: $State" -ForegroundColor White
Write-Host "  Years of history: $YearsOfHistory" -ForegroundColor White
Write-Host "  Seed: $Seed" -ForegroundColor White
Write-Host "  Output: $fhirDir" -ForegroundColor White
Write-Host ""

if ($CleanOutput -and (Test-Path $fhirDir)) {
    Write-Host "Cleaning previous generated bundles..." -ForegroundColor Yellow
    Remove-Item -Path "$fhirDir\*" -Recurse -Force -ErrorAction SilentlyContinue
}

if (-not (Test-Path $outputDir)) {
    New-Item -Path $outputDir -ItemType Directory | Out-Null
}

Write-Host "Running Synthea (this may take several minutes)..." -ForegroundColor Yellow
Write-Host ""

java -Xmx2g -jar $syntheaJar `
    -p $PopulationSize `
    -s $Seed `
    $State `
    --exporter.fhir.export=true `
    --exporter.baseDirectory="$outputDir" `
    --exporter.years_of_history=$YearsOfHistory

if ($LASTEXITCODE -ne 0) {
    Write-Host "Synthea generation failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

$fhirFiles = Get-ChildItem -Path $fhirDir -Filter "*.json" -ErrorAction SilentlyContinue

Write-Host ""
if ($null -eq $fhirFiles -or $fhirFiles.Count -eq 0) {
    Write-Host "No FHIR bundle files were generated." -ForegroundColor Red
    exit 1
}

$totalSizeMB = [math]::Round((($fhirFiles | Measure-Object Length -Sum).Sum / 1MB), 2)

Write-Host "=== Generation Complete ===" -ForegroundColor Green
Write-Host "Generated bundle files: $($fhirFiles.Count)" -ForegroundColor Green
Write-Host "Total bundle size: ${totalSizeMB} MB" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1) Load into HAPI FHIR: .\Load-DataToServer.ps1" -ForegroundColor White
Write-Host "  2) Verify high-value resources:" -ForegroundColor White
Write-Host "     http://localhost:8082/fhir/Observation?_count=5" -ForegroundColor Gray
Write-Host "     http://localhost:8082/fhir/ServiceRequest?_count=5" -ForegroundColor Gray
Write-Host "     http://localhost:8082/fhir/DiagnosticReport?_count=5" -ForegroundColor Gray
Write-Host "     http://localhost:8082/fhir/MedicationRequest?_count=5" -ForegroundColor Gray
