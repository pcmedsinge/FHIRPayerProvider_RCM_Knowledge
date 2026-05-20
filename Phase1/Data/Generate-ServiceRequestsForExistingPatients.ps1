# Add ServiceRequest resources for patients that already exist in HAPI FHIR.
# This script does not create any new Patient resources.

param(
    [int]$RequestsPerPatient = 3,
    [int]$BatchSize = 25,
    [string]$FhirBaseUrl = "http://localhost:8082/fhir"
)

Write-Host "=== Generating ServiceRequest Data For Existing Patients ===" -ForegroundColor Cyan
Write-Host ""

if ($RequestsPerPatient -lt 1) {
    Write-Host "RequestsPerPatient must be at least 1." -ForegroundColor Red
    exit 1
}

try {
    $patientBundle = Invoke-RestMethod -Uri "$FhirBaseUrl/Patient?_count=500" -Method Get -TimeoutSec 60 -ErrorAction Stop
}
catch {
    Write-Host "Failed to query Patient resources from $FhirBaseUrl" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Yellow
    exit 1
}

$patients = @($patientBundle.entry | ForEach-Object { $_.resource } | Where-Object { $_ -and $_.resourceType -eq 'Patient' })
if ($patients.Count -eq 0) {
    Write-Host "No existing patients found. Nothing to enrich." -ForegroundColor Yellow
    exit 0
}

try {
    $practitionerBundle = Invoke-RestMethod -Uri "$FhirBaseUrl/Practitioner?_count=200" -Method Get -TimeoutSec 60 -ErrorAction Stop
    $practitioners = @($practitionerBundle.entry | ForEach-Object { $_.resource } | Where-Object { $_ -and $_.resourceType -eq 'Practitioner' })
}
catch {
    $practitioners = @()
}

$catalog = @(
    @{ Category = 'laboratory'; System = 'http://loinc.org'; Code = '58410-2'; Display = 'Complete blood count (hemogram) panel'; Priority = 'routine' },
    @{ Category = 'laboratory'; System = 'http://loinc.org'; Code = '24323-8'; Display = 'Comprehensive metabolic panel'; Priority = 'routine' },
    @{ Category = 'laboratory'; System = 'http://loinc.org'; Code = '4548-4'; Display = 'Hemoglobin A1c'; Priority = 'routine' },
    @{ Category = 'laboratory'; System = 'http://loinc.org'; Code = '2093-3'; Display = 'Cholesterol [Mass/volume] in Serum or Plasma'; Priority = 'routine' },
    @{ Category = 'imaging'; System = 'http://www.ama-assn.org/go/cpt'; Code = '71046'; Display = 'Chest X-ray 2 views'; Priority = 'routine' },
    @{ Category = 'imaging'; System = 'http://www.ama-assn.org/go/cpt'; Code = '70553'; Display = 'MRI brain with and without contrast'; Priority = 'urgent' },
    @{ Category = 'procedure'; System = 'http://www.ama-assn.org/go/cpt'; Code = '45378'; Display = 'Diagnostic colonoscopy'; Priority = 'routine' },
    @{ Category = 'referral'; System = 'http://snomed.info/sct'; Code = '306206005'; Display = 'Referral to cardiology service'; Priority = 'routine' }
)

$entries = @()
$created = 0
$patientCount = $patients.Count

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

    $bundleJson = $bundleObject | ConvertTo-Json -Depth 15
    Invoke-RestMethod -Uri $BaseUrl -Method Post -Body $bundleJson -ContentType 'application/fhir+json' -TimeoutSec 120 -ErrorAction Stop | Out-Null
}

foreach ($patient in $patients) {
    for ($index = 0; $index -lt $RequestsPerPatient; $index++) {
        $template = Get-Random -InputObject $catalog
        $daysAgo = Get-Random -Minimum 7 -Maximum 730
        $authoredOn = (Get-Date).ToUniversalTime().AddDays(-$daysAgo)
        $occurrence = $authoredOn.AddDays((Get-Random -Minimum 1 -Maximum 45))
        $status = if ((Get-Random -Minimum 0 -Maximum 100) -lt 65) { 'completed' } else { 'active' }

        $serviceRequest = @{
            resourceType = 'ServiceRequest'
            status = $status
            intent = 'order'
            priority = $template.Priority
            category = @(
                @{
                    coding = @(
                        @{
                            system = 'http://terminology.hl7.org/CodeSystem/servicerequest-category'
                            code = $template.Category
                            display = (Get-Culture).TextInfo.ToTitleCase($template.Category)
                        }
                    )
                    text = (Get-Culture).TextInfo.ToTitleCase($template.Category)
                }
            )
            code = @{
                coding = @(
                    @{
                        system = $template.System
                        code = $template.Code
                        display = $template.Display
                    }
                )
                text = $template.Display
            }
            subject = @{ reference = "Patient/$($patient.id)" }
            authoredOn = $authoredOn.ToString('o')
            occurrenceDateTime = $occurrence.ToString('o')
            note = @(
                @{ text = "Supplemental synthetic ServiceRequest added for facade and workflow testing." }
            )
        }

        if ($practitioners.Count -gt 0) {
            $requester = Get-Random -InputObject $practitioners
            $serviceRequest.requester = @{ reference = "Practitioner/$($requester.id)" }
        }

        $entries += @{
            request = @{ method = 'POST'; url = 'ServiceRequest' }
            resource = $serviceRequest
        }
        $created++

        if ($entries.Count -ge $BatchSize) {
            Submit-Batch -BatchEntries $entries -BaseUrl $FhirBaseUrl
            $entries = @()
            Write-Host "Created $created ServiceRequest resources so far..." -ForegroundColor Green
        }
    }
}

if ($entries.Count -gt 0) {
    Submit-Batch -BatchEntries $entries -BaseUrl $FhirBaseUrl
}

Write-Host "" 
Write-Host "=== ServiceRequest Enrichment Complete ===" -ForegroundColor Green
Write-Host "Patients targeted: $patientCount" -ForegroundColor Green
Write-Host "ServiceRequest resources created: $created" -ForegroundColor Green
Write-Host "Verify with: $FhirBaseUrl/ServiceRequest?_summary=count" -ForegroundColor White
