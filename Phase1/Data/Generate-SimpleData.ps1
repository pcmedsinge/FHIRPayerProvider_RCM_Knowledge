# Generate Simple Synthetic Payer Data with Synthea
# This generates patients with shorter medical histories to keep files small

Write-Host "=== Generating Simple Synthetic Payer Data ===" -ForegroundColor Cyan
Write-Host ""

$dataDir = $PSScriptRoot
$syntheaJar = Join-Path $dataDir "synthea.jar"
$outputDir = Join-Path $dataDir "output"

# Configuration for simpler data
$populationSize = 20
$state = "Massachusetts"
$startDate = "2024-01-01"  # Only 2 years of history

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Population: $populationSize patients" -ForegroundColor White
Write-Host "  State: $state" -ForegroundColor White
Write-Host "  Start Date: $startDate (shorter history = smaller files)" -ForegroundColor White
Write-Host "  Output: $outputDir" -ForegroundColor White
Write-Host ""

# Clean previous output
if (Test-Path $outputDir) {
    Write-Host "Cleaning previous output..." -ForegroundColor Yellow
    Remove-Item -Path "$outputDir\fhir\*" -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Generating patients (this may take 1-2 minutes)..." -ForegroundColor Yellow
Write-Host ""

# Run Synthea with reduced memory and shorter history
java -Xmx512m -jar $syntheaJar `
    -p $populationSize `
    -s 54321 `
    $state `
    --exporter.fhir.export=true `
    --exporter.baseDirectory="$outputDir" `
    --exporter.years_of_history=2

Write-Host ""
Write-Host "=== Data Generation Complete! ===" -ForegroundColor Green
Write-Host ""

# Count generated files
$fhirFiles = Get-ChildItem -Path "$outputDir\fhir" -Filter "*.json" -ErrorAction SilentlyContinue
if ($fhirFiles) {
    Write-Host "Generated $($fhirFiles.Count) FHIR bundle files" -ForegroundColor Green
    
    # Show file sizes
    $fhirFiles | ForEach-Object {
        $sizeMB = [math]::Round($_.Length / 1MB, 2)
        if ($sizeMB -gt 5) {
            Write-Host "  $($_.Name): ${sizeMB}MB" -ForegroundColor Yellow
        } else {
            Write-Host "  $($_.Name): ${sizeMB}MB" -ForegroundColor Green
        }
    }
    
    Write-Host ""
    Write-Host "Next step: Load this data into FHIR server" -ForegroundColor Yellow
    Write-Host "Run: .\Load-DataToServer.ps1" -ForegroundColor White
}
else {
    Write-Host "No FHIR files generated. Check for errors above." -ForegroundColor Red
}
