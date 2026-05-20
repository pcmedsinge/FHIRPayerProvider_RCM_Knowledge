param(
    [string]$FhirBaseUrl = "http://localhost:8082/fhir",
    [string]$KeepIdsPath = ".\output\keep-patient-ids.txt",
    [string]$ExportPath = ".\output\curated-from-server",
    [int]$PageCount = 500
)

Write-Host "=== Export Kept Patient Bundles From Server ===" -ForegroundColor Cyan

if (-not (Test-Path $KeepIdsPath)) {
    Write-Host "Keep list not found: $KeepIdsPath" -ForegroundColor Red
    exit 1
}

$keepIds = Get-Content $KeepIdsPath | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim() }
if ($keepIds.Count -eq 0) {
    Write-Host "No keep IDs found." -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $ExportPath)) {
    New-Item -ItemType Directory -Path $ExportPath | Out-Null
}

function Get-NextLink {
    param($Bundle)
    if ($null -eq $Bundle.link) { return $null }
    $next = $Bundle.link | Where-Object { $_.relation -eq 'next' } | Select-Object -First 1
    if ($next) { return [string]$next.url }
    return $null
}

function Get-AllResources {
    param(
        [string]$InitialUrl
    )

    $resources = @()
    $url = $InitialUrl
    $safety = 0

    while ($url -and $safety -lt 1000) {
        $safety++
        try {
            $bundle = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec 120 -ErrorAction Stop
        }
        catch {
            break
        }

        if ($bundle.entry) {
            $resources += @($bundle.entry | ForEach-Object { $_.resource } | Where-Object { $_ })
        }

        $url = Get-NextLink -Bundle $bundle
    }

    return $resources
}

$resourceQueries = @(
    @{ Type = 'Observation'; Query = 'subject=Patient/{0}' },
    @{ Type = 'Encounter'; Query = 'subject=Patient/{0}' },
    @{ Type = 'Condition'; Query = 'subject=Patient/{0}' },
    @{ Type = 'Procedure'; Query = 'subject=Patient/{0}' },
    @{ Type = 'MedicationRequest'; Query = 'subject=Patient/{0}' },
    @{ Type = 'DiagnosticReport'; Query = 'subject=Patient/{0}' },
    @{ Type = 'DocumentReference'; Query = 'subject=Patient/{0}' },
    @{ Type = 'ServiceRequest'; Query = 'subject=Patient/{0}' },
    @{ Type = 'CarePlan'; Query = 'subject=Patient/{0}' },
    @{ Type = 'Specimen'; Query = 'subject=Patient/{0}' },
    @{ Type = 'MedicationDispense'; Query = 'subject=Patient/{0}' },
    @{ Type = 'AllergyIntolerance'; Query = 'patient=Patient/{0}' },
    @{ Type = 'Immunization'; Query = 'patient=Patient/{0}' },
    @{ Type = 'ExplanationOfBenefit'; Query = 'patient=Patient/{0}' },
    @{ Type = 'Claim'; Query = 'patient=Patient/{0}' },
    @{ Type = 'Coverage'; Query = 'beneficiary=Patient/{0}' },
    @{ Type = 'Task'; Query = 'for=Patient/{0}' },
    @{ Type = 'Appointment'; Query = 'patient=Patient/{0}' }
)

$exported = 0
$failed = 0

foreach ($patientId in $keepIds) {
    try {
        $all = @()
        $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

        $patient = Invoke-RestMethod -Uri "$FhirBaseUrl/Patient/$patientId" -Method Get -TimeoutSec 60 -ErrorAction Stop
        if ($patient -and $patient.id) {
            $key = "Patient/$($patient.id)"
            if ($seen.Add($key)) { $all += $patient }
        }

        foreach ($rq in $resourceQueries) {
            $q = [string]::Format($rq.Query, $patientId)
            $url = "$FhirBaseUrl/$($rq.Type)?$q&_count=$PageCount"
            $resources = Get-AllResources -InitialUrl $url
            foreach ($r in $resources) {
                if ($null -eq $r -or $null -eq $r.resourceType -or $null -eq $r.id) { continue }
                $key = "$($r.resourceType)/$($r.id)"
                if ($seen.Add($key)) {
                    $all += $r
                }
            }
        }

        $entries = @()
        foreach ($r in $all) {
            $entries += @{
                request = @{ method = 'POST'; url = $r.resourceType }
                resource = $r
            }
        }

        $bundle = @{
            resourceType = 'Bundle'
            type = 'transaction'
            entry = $entries
        }

        $safePid = $patientId -replace '[^A-Za-z0-9_-]', '_'
        $outFile = Join-Path $ExportPath ("patient-{0}.json" -f $safePid)
        $bundle | ConvertTo-Json -Depth 25 | Set-Content -Path $outFile -Encoding UTF8
        $exported++
        Write-Host "Exported patient $patientId with $($entries.Count) resources" -ForegroundColor Green
    }
    catch {
        $failed++
        Write-Host "Failed to export patient $patientId" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Export complete." -ForegroundColor Cyan
Write-Host "  Exported: $exported" -ForegroundColor White
Write-Host "  Failed:   $failed" -ForegroundColor White
Write-Host "  Output:   $ExportPath" -ForegroundColor White
