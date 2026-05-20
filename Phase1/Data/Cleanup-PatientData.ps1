# Cleanup patient-linked FHIR data while preserving dictionary/reference resources.
# Default mode is dry-run. Use -Apply to perform deletions.

param(
    [string]$FhirBaseUrl = "http://localhost:8082/fhir",
    [int]$MinObservations = 20,
    [int]$MinEncounters = 1,
    [int]$MinConditions = 1,
    [int]$MinMedicationRequests = 1,
    [int]$MinFinancialRecords = 1,
    [string[]]$KeepPatientIds = @('51707', '52458', '65520', '55001', '55002'),
    [switch]$RemoveDeceased,
    [switch]$Apply
)

Write-Host "=== Patient Data Cleanup ===" -ForegroundColor Cyan
Write-Host "FHIR Base: $FhirBaseUrl" -ForegroundColor White
Write-Host "Mode: $(if ($Apply) { 'APPLY' } else { 'DRY RUN' })" -ForegroundColor Yellow
Write-Host ""

function Get-SearchCount {
    param(
        [string]$ResourceType,
        [string]$Query
    )

    $uri = "{0}/{1}?{2}&_summary=count" -f $FhirBaseUrl, $ResourceType, $Query
    try {
        $result = Invoke-RestMethod -Uri $uri -Method Get -TimeoutSec 60 -ErrorAction Stop
        return [int]$result.total
    }
    catch {
        return 0
    }
}

function Get-Entries {
    param(
        [string]$ResourceType,
        [string]$Query
    )

    $uri = "{0}/{1}?{2}&_count=500" -f $FhirBaseUrl, $ResourceType, $Query
    try {
        $result = Invoke-RestMethod -Uri $uri -Method Get -TimeoutSec 120 -ErrorAction Stop
        if ($null -eq $result.entry) {
            return @()
        }
        return @($result.entry)
    }
    catch {
        return @()
    }
}

function Remove-ResourceById {
    param(
        [string]$ResourceType,
        [string]$Id,
        [switch]$Cascade
    )

    $uri = "{0}/{1}/{2}" -f $FhirBaseUrl, $ResourceType, $Id
    if ($Cascade) {
        $uri = "$uri`?_cascade=delete"
    }
    Invoke-RestMethod -Uri $uri -Method Delete -TimeoutSec 120 -ErrorAction Stop | Out-Null
}

try {
    $patientBundle = Invoke-RestMethod -Uri "$FhirBaseUrl/Patient?_count=500" -Method Get -TimeoutSec 120 -ErrorAction Stop
}
catch {
    Write-Host "Failed to query patients from $FhirBaseUrl" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Yellow
    exit 1
}

$patients = @($patientBundle.entry | ForEach-Object { $_.resource } | Where-Object { $_ -and $_.resourceType -eq 'Patient' })
if ($patients.Count -eq 0) {
    Write-Host "No patients found." -ForegroundColor Yellow
    exit 0
}

$patientReports = @()

foreach ($patient in $patients) {
    $patientRef = "Patient/$($patient.id)"
    $isDeceased = ($null -ne $patient.deceasedBoolean -and $patient.deceasedBoolean) -or ($null -ne $patient.deceasedDateTime)

    $observationCount = Get-SearchCount -ResourceType 'Observation' -Query "subject=$patientRef"
    $encounterCount = Get-SearchCount -ResourceType 'Encounter' -Query "subject=$patientRef"
    $conditionCount = Get-SearchCount -ResourceType 'Condition' -Query "subject=$patientRef"
    $medicationRequestCount = Get-SearchCount -ResourceType 'MedicationRequest' -Query "subject=$patientRef"
    $eobCount = Get-SearchCount -ResourceType 'ExplanationOfBenefit' -Query "patient=$patientRef"
    $claimCount = Get-SearchCount -ResourceType 'Claim' -Query "patient=$patientRef"
    $diagnosticReportCount = Get-SearchCount -ResourceType 'DiagnosticReport' -Query "subject=$patientRef"
    $procedureCount = Get-SearchCount -ResourceType 'Procedure' -Query "subject=$patientRef"
    $serviceRequestCount = Get-SearchCount -ResourceType 'ServiceRequest' -Query "subject=$patientRef"

    $financialCount = $eobCount + $claimCount
    $hasWorkflowOrDiagnostic = ($diagnosticReportCount + $procedureCount + $serviceRequestCount) -gt 0

    $keep = $observationCount -ge $MinObservations -and
            $encounterCount -ge $MinEncounters -and
            $conditionCount -ge $MinConditions -and
            $medicationRequestCount -ge $MinMedicationRequests -and
            $financialCount -ge $MinFinancialRecords -and
            $hasWorkflowOrDiagnostic

    if ($RemoveDeceased -and $isDeceased) {
        $keep = $false
    }

    if ($KeepPatientIds -contains [string]$patient.id) {
        $keep = $true
    }

    $patientReports += [pscustomobject]@{
        PatientId = $patient.id
        Name = "$($patient.name[0].given[0]) $($patient.name[0].family)"
        Deceased = $isDeceased
        Observations = $observationCount
        Encounters = $encounterCount
        Conditions = $conditionCount
        MedicationRequests = $medicationRequestCount
        FinancialRecords = $financialCount
        DiagnosticReports = $diagnosticReportCount
        Procedures = $procedureCount
        ServiceRequests = $serviceRequestCount
        Keep = $keep
    }
}

$toKeep = @($patientReports | Where-Object { $_.Keep })
$toRemove = @($patientReports | Where-Object { -not $_.Keep })

Write-Host "Patients evaluated: $($patientReports.Count)" -ForegroundColor White
Write-Host "Patients kept: $($toKeep.Count)" -ForegroundColor Green
Write-Host "Patients flagged for removal: $($toRemove.Count)" -ForegroundColor Yellow
Write-Host ""

if ($toRemove.Count -gt 0) {
    Write-Host "Flagged for removal (first 20):" -ForegroundColor Yellow
    $toRemove | Select-Object -First 20 PatientId, Name, Deceased, Observations, Encounters, Conditions, MedicationRequests, FinancialRecords, ServiceRequests | Format-Table -AutoSize
    Write-Host ""
}

if (-not $Apply) {
    Write-Host "Dry run only. Re-run with -Apply to remove flagged patients and their patient-linked resources." -ForegroundColor Cyan
    exit 0
}

$resourceQueryMap = @(
    @{ Resource = 'Task'; Query = 'for={0}' },
    @{ Resource = 'Appointment'; Query = 'patient={0}' },
    @{ Resource = 'CarePlan'; Query = 'subject={0}' },
    @{ Resource = 'ServiceRequest'; Query = 'subject={0}' },
    @{ Resource = 'Specimen'; Query = 'subject={0}' },
    @{ Resource = 'MedicationDispense'; Query = 'subject={0}' },
    @{ Resource = 'MedicationRequest'; Query = 'subject={0}' },
    @{ Resource = 'Observation'; Query = 'subject={0}' },
    @{ Resource = 'DiagnosticReport'; Query = 'subject={0}' },
    @{ Resource = 'Procedure'; Query = 'subject={0}' },
    @{ Resource = 'DocumentReference'; Query = 'subject={0}' },
    @{ Resource = 'AllergyIntolerance'; Query = 'patient={0}' },
    @{ Resource = 'Immunization'; Query = 'patient={0}' },
    @{ Resource = 'ExplanationOfBenefit'; Query = 'patient={0}' },
    @{ Resource = 'Claim'; Query = 'patient={0}' },
    @{ Resource = 'Coverage'; Query = 'beneficiary={0}' },
    @{ Resource = 'Condition'; Query = 'subject={0}' },
    @{ Resource = 'Encounter'; Query = 'subject={0}' }
)

foreach ($report in $toRemove) {
    $patientRef = "Patient/$($report.PatientId)"
    Write-Host "Removing patient-linked data for $($report.PatientId) ..." -ForegroundColor Yellow

    foreach ($map in $resourceQueryMap) {
        $query = $map.Query -f $patientRef
        $entries = Get-Entries -ResourceType $map.Resource -Query $query
        foreach ($entry in $entries) {
            if ($null -ne $entry.resource.id) {
                try {
                    Remove-ResourceById -ResourceType $map.Resource -Id $entry.resource.id -Cascade
                }
                catch {
                    Write-Host "  Warning: could not delete $($map.Resource)/$($entry.resource.id)" -ForegroundColor DarkYellow
                }
            }
        }
    }

    try {
        Remove-ResourceById -ResourceType 'Patient' -Id $report.PatientId -Cascade
    }
    catch {
        Write-Host "  Warning: could not delete Patient/$($report.PatientId)" -ForegroundColor DarkYellow
    }
}

Write-Host "" 
Write-Host "Cleanup applied successfully." -ForegroundColor Green