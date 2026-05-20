# Explore FHIR Payer Data

$fhirServer = "http://localhost:8082/fhir"

Write-Host "=== FHIR Payer Data Explorer ===" -ForegroundColor Cyan
Write-Host "Server: $fhirServer" -ForegroundColor White
Write-Host ""

# Get summary counts
Write-Host "Resource Counts:" -ForegroundColor Yellow
$resources = @("Patient", "ExplanationOfBenefit", "Coverage", "Encounter", "Condition", "Procedure", "MedicationRequest")

foreach ($resource in $resources) {
    try {
        $result = Invoke-RestMethod -Uri "$fhirServer/$resource`?_summary=count" -Method Get
        Write-Host "  $resource`: $($result.total)" -ForegroundColor White
    }
    catch {
        Write-Host "  $resource`: 0" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "=== Sample Patient ===" -ForegroundColor Cyan
$patients = Invoke-RestMethod -Uri "$fhirServer/Patient?_count=1" -Method Get
$patient = $patients.entry[0].resource

Write-Host "Name: $($patient.name[0].given[0]) $($patient.name[0].family)" -ForegroundColor Green
Write-Host "Gender: $($patient.gender)" -ForegroundColor White
Write-Host "Birth Date: $($patient.birthDate)" -ForegroundColor White
Write-Host "ID: $($patient.id)" -ForegroundColor Gray

Write-Host ""
Write-Host "=== Sample Claim (EOB) ===" -ForegroundColor Cyan
$eobs = Invoke-RestMethod -Uri "$fhirServer/ExplanationOfBenefit?_count=1" -Method Get
$eob = $eobs.entry[0].resource

Write-Host "Claim Type: $($eob.type.coding[0].display)" -ForegroundColor Green
Write-Host "Patient: $($eob.patient.reference)" -ForegroundColor White
Write-Host "Provider: $($eob.provider.display)" -ForegroundColor White
Write-Host "Service Date: $($eob.billablePeriod.start)" -ForegroundColor White
Write-Host "Total Submitted: `$$($eob.total | Where-Object {$_.category.coding.code -eq 'submitted'} | Select-Object -First 1 | %{$_.amount.value})" -ForegroundColor Yellow
Write-Host "Total Paid: `$$($eob.total | Where-Object {$_.category.coding.code -eq 'benefit'} | Select-Object -First 1 | %{$_.amount.value})" -ForegroundColor Green

Write-Host ""
Write-Host "=== Useful Queries ===" -ForegroundColor Cyan
Write-Host "Get all patients: " -NoNewline; Write-Host "$fhirServer/Patient" -ForegroundColor White
Write-Host "Get all claims: " -NoNewline; Write-Host "$fhirServer/ExplanationOfBenefit" -ForegroundColor White
Write-Host "Get claims for patient: " -NoNewline; Write-Host "$fhirServer/ExplanationOfBenefit?patient=Patient/$($patient.id)" -ForegroundColor White
Write-Host "Web UI: " -NoNewline; Write-Host "http://localhost:8082" -ForegroundColor White
Write-Host ""
