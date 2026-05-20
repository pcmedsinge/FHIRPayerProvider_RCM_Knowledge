# Generate Synthetic Payer Data with Synthea

Write-Host "=== Generating Synthetic Payer Data ===" -ForegroundColor Cyan
Write-Host ""

$dataDir = $PSScriptRoot
$syntheaJar = Join-Path $dataDir "synthea.jar"
$outputDir = Join-Path $dataDir "output"

# Configuration
$populationSize = 10
$state = "Massachusetts"

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Population: $populationSize patients" -ForegroundColor White
Write-Host "  State: $state" -ForegroundColor White
Write-Host "  Output: $outputDir" -ForegroundColor White
Write-Host ""

# Clean previous output
if (Test-Path $outputDir) {
    Write-Host "Cleaning previous output..." -ForegroundColor Yellow
    Remove-Item -Path "$outputDir\fhir\*" -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Generating patients (this may take 2-3 minutes)..." -ForegroundColor Yellow
Write-Host ""

# Run Synthea with reduced memory settings
# -Xmx512m limits memory usage
# --exporter.fhir.export=true enables FHIR R4 export
# --exporter.baseDirectory sets output location
java -Xmx512m -jar $syntheaJar `
    -p $populationSize `
    -s 12345 `
    $state `
    --exporter.fhir.export=true `
    --exporter.baseDirectory="$outputDir"

Write-Host ""
Write-Host "=== Data Generation Complete! ===" -ForegroundColor Green
Write-Host ""

# Count generated files
$fhirFiles = Get-ChildItem -Path "$outputDir\fhir" -Filter "*.json" -ErrorAction SilentlyContinue
if ($fhirFiles) {
    Write-Host "Generated $($fhirFiles.Count) FHIR bundle files" -ForegroundColor Green
    Write-Host "Location: $outputDir\fhir" -ForegroundColor White
    Write-Host ""
    Write-Host "Each file contains:" -ForegroundColor Cyan
    Write-Host "  - Patient demographics" -ForegroundColor White
    Write-Host "  - Clinical conditions" -ForegroundColor White
    Write-Host "  - Medications" -ForegroundColor White
    Write-Host "  - Procedures" -ForegroundColor White
    Write-Host "  - Encounters (visits)" -ForegroundColor White
    Write-Host "  - Observations (labs, vitals)" -ForegroundColor White
    Write-Host "  - Claims data (ExplanationOfBenefit)" -ForegroundColor White
    Write-Host ""
    Write-Host "Next step: Load this data into FHIR server" -ForegroundColor Yellow
}
else {
    Write-Host "No FHIR files generated. Check for errors above." -ForegroundColor Red
}
