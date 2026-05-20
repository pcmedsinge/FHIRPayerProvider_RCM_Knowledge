param(
    [string]$KeepIdsPath = ".\output\keep-patient-ids.txt",
    [string]$SourceBundlesPath = ".\output\fhir",
    [string]$CuratedBundlesPath = ".\output\curated-fhir",
    [string]$ComposePath = "..\Setup",
    [switch]$ApplyRebuild
)

Write-Host "=== Rebuild Curated Data From Keep List ===" -ForegroundColor Cyan

if (-not (Test-Path $KeepIdsPath)) {
    Write-Host "Keep list not found: $KeepIdsPath" -ForegroundColor Red
    exit 1
}
if (-not (Test-Path $SourceBundlesPath)) {
    Write-Host "Source bundle folder not found: $SourceBundlesPath" -ForegroundColor Red
    exit 1
}

$keepIds = Get-Content $KeepIdsPath | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim() }
$keepSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$keepIds | ForEach-Object { [void]$keepSet.Add($_) }

Write-Host "Keep IDs loaded: $($keepSet.Count)" -ForegroundColor Yellow

if (Test-Path $CuratedBundlesPath) {
    Remove-Item -Path "$CuratedBundlesPath\*" -Recurse -Force -ErrorAction SilentlyContinue
} else {
    New-Item -ItemType Directory -Path $CuratedBundlesPath | Out-Null
}

$allFiles = Get-ChildItem -Path $SourceBundlesPath -Filter "*.json"
$hospitalFiles = $allFiles | Where-Object { $_.Name -like "hospitalInformation*" }
$practitionerFiles = $allFiles | Where-Object { $_.Name -like "practitionerInformation*" }
$patientFiles = $allFiles | Where-Object { $_.Name -notlike "hospitalInformation*" -and $_.Name -notlike "practitionerInformation*" }

$metaFiles = @($hospitalFiles) + @($practitionerFiles)
foreach ($f in $metaFiles) {
    Copy-Item -Path $f.FullName -Destination (Join-Path $CuratedBundlesPath $f.Name) -Force
}

$matchedPatientFiles = 0
$matchedPatientIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

foreach ($file in $patientFiles) {
    try {
        $bundle = Get-Content $file.FullName -Raw | ConvertFrom-Json -ErrorAction Stop
        $patientEntry = $bundle.entry | Where-Object { $_.resource.resourceType -eq 'Patient' } | Select-Object -First 1
        if ($null -eq $patientEntry) { continue }
        $patientId = [string]$patientEntry.resource.id
        if ($keepSet.Contains($patientId)) {
            Copy-Item -Path $file.FullName -Destination (Join-Path $CuratedBundlesPath $file.Name) -Force
            $matchedPatientFiles++
            [void]$matchedPatientIds.Add($patientId)
        }
    }
    catch {
        Write-Host "Skipping unreadable bundle: $($file.Name)" -ForegroundColor DarkYellow
    }
}

$missingKeepIds = @($keepSet | Where-Object { -not $matchedPatientIds.Contains($_) })

Write-Host ""
Write-Host "Curated bundle creation complete." -ForegroundColor Green
Write-Host "  Hospital bundles copied: $($hospitalFiles.Count)" -ForegroundColor White
Write-Host "  Practitioner bundles copied: $($practitionerFiles.Count)" -ForegroundColor White
Write-Host "  Patient bundles copied: $matchedPatientFiles" -ForegroundColor White
Write-Host "  Keep IDs not found in source bundles: $($missingKeepIds.Count)" -ForegroundColor Yellow
if ($missingKeepIds.Count -gt 0) {
    $missingPreview = (($missingKeepIds | Select-Object -First 20) -join ', ')
    Write-Host "  Missing IDs (first 20): $missingPreview" -ForegroundColor DarkYellow
}

if (-not $ApplyRebuild) {
    Write-Host ""
    Write-Host "Dry run complete. Re-run with -ApplyRebuild to reset and reload HAPI from curated bundles." -ForegroundColor Cyan
    exit 0
}

Write-Host ""
Write-Host "Applying rebuild: resetting volumes and reloading curated bundles..." -ForegroundColor Yellow

Push-Location $ComposePath
try {
    docker compose down -v | Out-Null
    docker compose up -d | Out-Null
}
finally {
    Pop-Location
}

$fhirServerUrl = "http://localhost:8082/fhir"
$maxAttempts = 60
$ready = $false
for ($i = 1; $i -le $maxAttempts; $i++) {
    try {
        Invoke-RestMethod -Uri "$fhirServerUrl/metadata" -Method Get -TimeoutSec 10 -ErrorAction Stop | Out-Null
        $ready = $true
        break
    }
    catch {
        Start-Sleep -Seconds 3
    }
}

if (-not $ready) {
    Write-Host "FHIR server did not become ready in time." -ForegroundColor Red
    exit 1
}

$curatedFiles = Get-ChildItem -Path $CuratedBundlesPath -Filter "*.json"
$curHospital = $curatedFiles | Where-Object { $_.Name -like "hospitalInformation*" }
$curPract = $curatedFiles | Where-Object { $_.Name -like "practitionerInformation*" }
$curPatient = $curatedFiles | Where-Object { $_.Name -notlike "hospitalInformation*" -and $_.Name -notlike "practitionerInformation*" }
$ordered = @($curHospital + $curPract + $curPatient)

$loaded = 0
$failed = 0
foreach ($f in $ordered) {
    try {
        $body = Get-Content $f.FullName -Raw
        Invoke-RestMethod -Uri $fhirServerUrl -Method Post -Body $body -ContentType "application/fhir+json" -TimeoutSec 180 -ErrorAction Stop | Out-Null
        $loaded++
    }
    catch {
        $failed++
        Write-Host "Failed to load: $($f.Name)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Rebuild complete." -ForegroundColor Green
Write-Host "  Bundles loaded: $loaded" -ForegroundColor White
Write-Host "  Bundles failed: $failed" -ForegroundColor White
Write-Host "  Verify: $fhirServerUrl/Patient?_summary=count" -ForegroundColor White
