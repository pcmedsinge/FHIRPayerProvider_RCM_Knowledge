# Add workflow-oriented FHIR resources for patients that already exist in HAPI FHIR.
# This script does not create any new Patient resources.

param(
    [int]$BatchSize = 30,
    [string]$FhirBaseUrl = "http://localhost:8082/fhir"
)

Write-Host "=== Generating Workflow Data For Existing Patients ===" -ForegroundColor Cyan
Write-Host ""

function Submit-Batch {
    param(
        [array]$BatchEntries,
        [string]$BaseUrl
    )

    if ($BatchEntries.Count -eq 0) {
        return
    }

    $bundleObject = @{
        resourceType = 'Bundle'
        type = 'transaction'
        entry = $BatchEntries
    }

    $bundleJson = $bundleObject | ConvertTo-Json -Depth 20
    Invoke-RestMethod -Uri $BaseUrl -Method Post -Body $bundleJson -ContentType 'application/fhir+json' -TimeoutSec 120 -ErrorAction Stop | Out-Null
}

try {
    $patientBundle = Invoke-RestMethod -Uri "$FhirBaseUrl/Patient?_count=500" -Method Get -TimeoutSec 60 -ErrorAction Stop
}
catch {
    Write-Host "Failed to query existing patients from $FhirBaseUrl" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Yellow
    exit 1
}

$patients = @($patientBundle.entry | ForEach-Object { $_.resource } | Where-Object { $_ -and $_.resourceType -eq 'Patient' })
if ($patients.Count -eq 0) {
    Write-Host "No existing patients found. Nothing to enrich." -ForegroundColor Yellow
    exit 0
}

try {
    $practitionerBundle = Invoke-RestMethod -Uri "$FhirBaseUrl/Practitioner?_count=300" -Method Get -TimeoutSec 60 -ErrorAction Stop
    $practitioners = @($practitionerBundle.entry | ForEach-Object { $_.resource } | Where-Object { $_ -and $_.resourceType -eq 'Practitioner' })
}
catch {
    $practitioners = @()
}

$medicationCatalog = @(
    @{ Code = '860975'; Display = '24 HR metformin hydrochloride 500 MG Extended Release Oral Tablet'; System = 'http://www.nlm.nih.gov/research/umls/rxnorm' },
    @{ Code = '197361'; Display = 'Amlodipine 5 MG Oral Tablet'; System = 'http://www.nlm.nih.gov/research/umls/rxnorm' },
    @{ Code = '617314'; Display = 'Atorvastatin 20 MG Oral Tablet'; System = 'http://www.nlm.nih.gov/research/umls/rxnorm' },
    @{ Code = '849574'; Display = 'Albuterol 0.09 MG/ACTUAT inhaler'; System = 'http://www.nlm.nih.gov/research/umls/rxnorm' }
)

$specimenCatalog = @(
    @{ Code = '119297000'; Display = 'Blood specimen'; System = 'http://snomed.info/sct' },
    @{ Code = '122555007'; Display = 'Venous blood specimen'; System = 'http://snomed.info/sct' },
    @{ Code = '258580003'; Display = 'Urine specimen'; System = 'http://snomed.info/sct' }
)

$carePlanTitles = @(
    'Diabetes longitudinal management plan',
    'Hypertension follow-up plan',
    'Preventive wellness care plan',
    'Cardiometabolic risk reduction plan'
)

$taskCodes = @(
    @{ Code = 'complete-questionnaire'; Display = 'Complete intake questionnaire' },
    @{ Code = 'schedule-followup'; Display = 'Schedule follow-up visit' },
    @{ Code = 'review-labs'; Display = 'Review lab results' }
)

$entries = @()
$created = @{ CarePlan = 0; Task = 0; Appointment = 0; Specimen = 0; MedicationDispense = 0 }

foreach ($patient in $patients) {
    $patientRef = "Patient/$($patient.id)"
    $practitioner = if ($practitioners.Count -gt 0) { Get-Random -InputObject $practitioners } else { $null }
    $practitionerRef = if ($practitioner) { "Practitioner/$($practitioner.id)" } else { $null }
    $daysAgo = Get-Random -Minimum 5 -Maximum 540
    $baseDate = (Get-Date).ToUniversalTime().AddDays(-$daysAgo)

    $carePlan = @{
        resourceType = 'CarePlan'
        status = 'active'
        intent = 'plan'
        title = (Get-Random -InputObject $carePlanTitles)
        subject = @{ reference = $patientRef }
        created = $baseDate.ToString('o')
        description = 'Supplemental synthetic care plan for workflow testing.'
    }
    if ($practitionerRef) {
        $carePlan.author = @{ reference = $practitionerRef }
    }
    $entries += @{ request = @{ method = 'POST'; url = 'CarePlan' }; resource = $carePlan }
    $created.CarePlan++

    $taskTemplate = Get-Random -InputObject $taskCodes
    $task = @{
        resourceType = 'Task'
        status = 'requested'
        intent = 'order'
        code = @{ text = $taskTemplate.Display }
        description = $taskTemplate.Display
        for = @{ reference = $patientRef }
        authoredOn = $baseDate.AddDays(1).ToString('o')
        executionPeriod = @{ start = $baseDate.AddDays(2).ToString('o') }
    }
    if ($practitionerRef) {
        $task.requester = @{ reference = $practitionerRef }
        $task.owner = @{ reference = $practitionerRef }
    }
    $entries += @{ request = @{ method = 'POST'; url = 'Task' }; resource = $task }
    $created.Task++

    $appointmentStart = $baseDate.AddDays((Get-Random -Minimum 3 -Maximum 30))
    $appointmentEnd = $appointmentStart.AddMinutes(30)
    $appointmentParticipants = @(
        @{ actor = @{ reference = $patientRef }; status = 'accepted' }
    )
    if ($practitionerRef) {
        $appointmentParticipants += @{ actor = @{ reference = $practitionerRef }; status = 'accepted' }
    }
    $appointment = @{
        resourceType = 'Appointment'
        status = 'booked'
        description = 'Synthetic follow-up appointment'
        start = $appointmentStart.ToString('o')
        end = $appointmentEnd.ToString('o')
        participant = $appointmentParticipants
    }
    $entries += @{ request = @{ method = 'POST'; url = 'Appointment' }; resource = $appointment }
    $created.Appointment++

    $specimenTemplate = Get-Random -InputObject $specimenCatalog
    $specimen = @{
        resourceType = 'Specimen'
        status = 'available'
        subject = @{ reference = $patientRef }
        type = @{
            coding = @(
                @{
                    system = $specimenTemplate.System
                    code = $specimenTemplate.Code
                    display = $specimenTemplate.Display
                }
            )
            text = $specimenTemplate.Display
        }
        receivedTime = $baseDate.AddDays(1).AddHours(2).ToString('o')
    }
    $entries += @{ request = @{ method = 'POST'; url = 'Specimen' }; resource = $specimen }
    $created.Specimen++

    $medTemplate = Get-Random -InputObject $medicationCatalog
    $dispense = @{
        resourceType = 'MedicationDispense'
        status = 'completed'
        medicationCodeableConcept = @{
            coding = @(
                @{
                    system = $medTemplate.System
                    code = $medTemplate.Code
                    display = $medTemplate.Display
                }
            )
            text = $medTemplate.Display
        }
        subject = @{ reference = $patientRef }
        whenHandedOver = $baseDate.AddDays(2).ToString('o')
        quantity = @{ value = (Get-Random -Minimum 15 -Maximum 91); unit = 'tablet' }
    }
    if ($practitionerRef) {
        $dispense.performer = @(
            @{ actor = @{ reference = $practitionerRef } }
        )
    }
    $entries += @{ request = @{ method = 'POST'; url = 'MedicationDispense' }; resource = $dispense }
    $created.MedicationDispense++

    if ($entries.Count -ge $BatchSize) {
        Submit-Batch -BatchEntries $entries -BaseUrl $FhirBaseUrl
        $entries = @()
        Write-Host "Created resources so far: CarePlan=$($created.CarePlan), Task=$($created.Task), Appointment=$($created.Appointment), Specimen=$($created.Specimen), MedicationDispense=$($created.MedicationDispense)" -ForegroundColor Green
    }
}

if ($entries.Count -gt 0) {
    Submit-Batch -BatchEntries $entries -BaseUrl $FhirBaseUrl
}

Write-Host ""
Write-Host "=== Workflow Enrichment Complete ===" -ForegroundColor Green
Write-Host "Patients targeted: $($patients.Count)" -ForegroundColor Green
Write-Host "CarePlan created: $($created.CarePlan)" -ForegroundColor Green
Write-Host "Task created: $($created.Task)" -ForegroundColor Green
Write-Host "Appointment created: $($created.Appointment)" -ForegroundColor Green
Write-Host "Specimen created: $($created.Specimen)" -ForegroundColor Green
Write-Host "MedicationDispense created: $($created.MedicationDispense)" -ForegroundColor Green
