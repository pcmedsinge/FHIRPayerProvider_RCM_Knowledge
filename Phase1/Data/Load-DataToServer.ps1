# Load Synthea Data into FHIR Server

Write-Host "=== Loading Data into FHIR Server ===" -ForegroundColor Cyan
Write-Host ""

$dataDir = $PSScriptRoot
$fhirServerUrl = "http://localhost:8082/fhir"
$bundlesPath = Join-Path $dataDir "output\fhir"

# Check if server is running
try {
    $response = Invoke-RestMethod -Uri "$fhirServerUrl/metadata" -Method Get -ErrorAction Stop
    Write-Host "FHIR Server is running" -ForegroundColor Green
}
catch {
    Write-Host "FHIR Server is not accessible at $fhirServerUrl" -ForegroundColor Red
    Write-Host "Please start the server with: docker-compose up -d" -ForegroundColor Yellow
    exit 1
}

# Get all bundle files, prioritizing metadata files first
$allFiles = Get-ChildItem -Path $bundlesPath -Filter "*.json"
$hospitalFiles = $allFiles | Where-Object { $_.Name -like "hospitalInformation*" }
$practitionerFiles = $allFiles | Where-Object { $_.Name -like "practitionerInformation*" }
$patientFiles = $allFiles | Where-Object { $_.Name -notlike "hospitalInformation*" -and $_.Name -notlike "practitionerInformation*" }

# Load in order: hospital, practitioner, then patients
$bundleFiles = @()
$bundleFiles += $hospitalFiles
$bundleFiles += $practitionerFiles
$bundleFiles += $patientFiles

Write-Host "Found $($bundleFiles.Count) patient bundles to load" -ForegroundColor Yellow
Write-Host "  Hospital info: $($hospitalFiles.Count)" -ForegroundColor Gray
Write-Host "  Practitioner info: $($practitionerFiles.Count)" -ForegroundColor Gray
Write-Host "  Patient records: $($patientFiles.Count)" -ForegroundColor Gray
Write-Host ""

$loaded = 0
$failed = 0
$failedFiles = @()

foreach ($file in $bundleFiles) {
    try {
        Write-Host "Loading: $($file.Name)..." -NoNewline
        
        # Read the bundle
        $bundle = Get-Content $file.FullName -Raw
        
        # POST the bundle to the server with increased timeout
        $response = Invoke-RestMethod -Uri $fhirServerUrl -Method Post -Body $bundle -ContentType "application/fhir+json" -TimeoutSec 120 -ErrorAction Stop
        
        Write-Host " Done" -ForegroundColor Green
        $loaded++
    }
    catch {
        $errorMsg = $_.Exception.Message
        if ($_.ErrorDetails.Message) {
            try {
                $errorJson = $_.ErrorDetails.Message | ConvertFrom-Json
                $errorMsg = $errorJson.issue[0].diagnostics
            } catch {}
        }
        
        Write-Host " Failed: $errorMsg" -ForegroundColor Red
        $failedFiles += $file.Name
        $failed++
    }
}

Write-Host ""
Write-Host "=== Load Complete ===" -ForegroundColor Green
Write-Host "Successfully loaded: $loaded patients" -ForegroundColor Green
if ($failed -gt 0) {
    Write-Host "Failed: $failed patients" -ForegroundColor Red
    Write-Host ""
    Write-Host "Failed files:" -ForegroundColor Yellow
    foreach ($name in $failedFiles) {
        Write-Host "  - $name" -ForegroundColor Gray
    }
}
Write-Host ""
Write-Host "Test queries:" -ForegroundColor Cyan
Write-Host "  View all patients: $fhirServerUrl/Patient" -ForegroundColor White
Write-Host "  View claims: $fhirServerUrl/ExplanationOfBenefit" -ForegroundColor White
Write-Host ""
