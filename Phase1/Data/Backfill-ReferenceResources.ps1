#Requires -Version 5.1
<#
.SYNOPSIS
Extracts reference/dictionary resources from original Synthea bundles and loads them to HAPI
without modifying patient-linked data. This restores provider, organization, and terminology
context needed for clinical data interpretation.

.PARAMETER FHIRBase
FHIR server base URL (default: http://localhost:8082/fhir)

.PARAMETER SourceDir
Path to directory containing original Synthea bundles (default: .\output\fhir)
#>

param(
    [string]$FHIRBase = 'http://localhost:8082/fhir',
    [string]$SourceDir = '.\output\fhir'
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

# Reference resource types that should be extracted
$referenceTypes = @(
    'Organization',
    'Practitioner',
    'Location',
    'HealthcareService',
    'PractitionerRole',
    'Medication',
    'CodeSystem',
    'ValueSet'
)

Write-Host "Extracting reference resources from Synthea bundles..."
$referenceResources = @()
$processedFiles = 0
$totalEntries = 0

# Scan all bundles and extract reference resources
Get-ChildItem -Path $SourceDir -Filter '*.json' | ForEach-Object {
    try {
        $bundle = Get-Content $_.FullName -Raw | ConvertFrom-Json
        $processedFiles++
        
        if ($bundle.entry) {
            $bundle.entry | ForEach-Object {
                $totalEntries++
                if ($_.resource.resourceType -in $referenceTypes) {
                    # Remove circular references and patient links to reduce payload
                    $res = [PSCustomObject]@{
                        fullUrl = $_.fullUrl
                        resource = $_.resource
                    }
                    $referenceResources += $res
                }
            }
        }
    } catch {
        Write-Warning "Failed to process $($_.Name): $_"
    }
}

Write-Host "Processed $processedFiles files, $totalEntries total entries"
Write-Host "Extracted $($referenceResources.Count) reference resources for backfill"

if ($referenceResources.Count -eq 0) {
    Write-Host "No reference resources found to backfill"
    exit 0
}

# Deduplicate by resourceType/id
$seen = @{}
$uniqueResources = @()
foreach ($item in $referenceResources) {
    $key = "$($item.resource.resourceType)/$($item.resource.id)"
    if (-not $seen.ContainsKey($key)) {
        $seen[$key] = $true
        $uniqueResources += $item
    }
}

Write-Host "After deduplication: $($uniqueResources.Count) unique reference resources"

# Group by type and count
$uniqueResources | Group-Object { $_.resource.resourceType } | ForEach-Object {
    Write-Host "  $($_.Name): $($_.Count)"
}

# Create batch bundle for import
$refBundle = [PSCustomObject]@{
    resourceType = 'Bundle'
    type = 'transaction'
    entry = $uniqueResources | ForEach-Object {
        [PSCustomObject]@{
            fullUrl = $_.fullUrl
            resource = $_.resource
            request = [PSCustomObject]@{
                method = 'PUT'
                url = "$($_.resource.resourceType)/$($_.resource.id)"
            }
        }
    }
}

$bundleJson = $refBundle | ConvertTo-Json -Depth 100
$bundleSize = ([System.Text.Encoding]::UTF8.GetByteCount($bundleJson)) / 1MB
Write-Host "Bundle size: $([math]::Round($bundleSize, 2)) MB"

# Post to HAPI
Write-Host "Uploading reference resources to $FHIRBase..."
try {
    $response = Invoke-RestMethod -Uri $FHIRBase -Method Post `
        -Body $bundleJson -ContentType 'application/fhir+json' `
        -TimeoutSec 900 -ErrorAction Stop
    
    $responseJson = $response | ConvertTo-Json -Depth 5
    if ($response.entry) {
        $succeeded = 0
        $failed = 0
        $response.entry | ForEach-Object {
            if ($_.response.status -like '2*') {
                $succeeded++
            } else {
                $failed++
                if ($_.response.outcome.issue) {
                    Write-Warning "Failed: $($_.response.status) - $($_.response.outcome.issue[0].diagnostics)"
                }
            }
        }
        Write-Host "Reference resource backfill complete:"
        Write-Host "  Succeeded: $succeeded"
        Write-Host "  Failed: $failed"
    } else {
        Write-Host "Backfill response: $responseJson"
    }
} catch {
    Write-Error "Failed to backfill reference resources: $($_.Exception.Message)"
    if ($_.ErrorDetails) {
        Write-Error "Details: $($_.ErrorDetails.Message)"
    }
}
