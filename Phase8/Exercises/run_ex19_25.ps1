$ErrorActionPreference = 'Stop'

$base = "http://localhost:5280"

function New-Export([string]$path, [hashtable]$body) {
  return Invoke-RestMethod -Method Post -Uri "$base$path" -ContentType "application/json" -Body ($body | ConvertTo-Json -Depth 10)
}

function Get-LineCount([string]$text) {
  if ([string]::IsNullOrWhiteSpace($text)) { return 0 }
  return ($text -split "`n").Count
}

function Get-StatusCode([string]$url, [string]$method = "GET", [string]$body = $null) {
  try {
    if ($method -eq "POST") {
      Invoke-WebRequest -Method Post -Uri $url -ContentType "application/json" -Body $body | Out-Null
    }
    elseif ($method -eq "PUT") {
      Invoke-WebRequest -Method Put -Uri $url -ContentType "application/json" -Body $body | Out-Null
    }
    elseif ($method -eq "DELETE") {
      Invoke-WebRequest -Method Delete -Uri $url | Out-Null
    }
    else {
      Invoke-WebRequest -Method Get -Uri $url | Out-Null
    }
    return 200
  }
  catch {
    if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
      return [int]$_.Exception.Response.StatusCode
    }
    return -1
  }
}

Write-Host "== Phase 8 | Exercise 19: Compare system vs group Patient NDJSON ==" -ForegroundColor Cyan
$sys19 = New-Export -path "/api/bulk/Export/`$export" -body @{ resourceTypes = @("Patient") }
Invoke-RestMethod -Method Post -Uri "$base/api/bulk/Export/$($sys19.jobId)/execute" | Out-Null
$sysNdjson = Invoke-RestMethod -Uri "$base/api/bulk/Export/$($sys19.jobId)/download/Patient"
$grp19 = New-Export -path "/api/bulk/Export/Group/GRP-DIABETES/`$export" -body @{ resourceTypes = @("Patient") }
Invoke-RestMethod -Method Post -Uri "$base/api/bulk/Export/$($grp19.jobId)/execute" | Out-Null
$grpNdjson = Invoke-RestMethod -Uri "$base/api/bulk/Export/$($grp19.jobId)/download/Patient"
[PSCustomObject]@{ SystemPatientLines = (Get-LineCount $sysNdjson); GroupPatientLines = (Get-LineCount $grpNdjson) } | Format-List

Write-Host "`n== Exercise 20: Export for non-existent group ==" -ForegroundColor Cyan
$bad20 = New-Export -path "/api/bulk/Export/Group/GRP-NONEXISTENT/`$export" -body @{}
$bad20Exec = Invoke-RestMethod -Method Post -Uri "$base/api/bulk/Export/$($bad20.jobId)/execute"
[PSCustomObject]@{ JobId = $bad20Exec.jobId; Status = $bad20Exec.status; Error = $bad20Exec.errorMessage } | Format-List

Write-Host "`n== Exercise 21: Cancel export job ==" -ForegroundColor Cyan
$cancel21 = New-Export -path "/api/bulk/Export/`$export" -body @{ resourceTypes = @("Patient", "ExplanationOfBenefit", "Encounter", "MedicationRequest", "Condition", "Procedure", "Observation") }
Invoke-RestMethod -Method Put -Uri "$base/api/bulk/Export/$($cancel21.jobId)/cancel" | Out-Null
$cancel21Status = Invoke-RestMethod -Uri "$base/api/bulk/Export/$($cancel21.jobId)/status"
$statusText21 = if ($cancel21Status.status) { $cancel21Status.status } else { "unknown" }
Write-Host "Cancelled job status: $statusText21"

Write-Host "`n== Exercise 22: Delete export job ==" -ForegroundColor Cyan
$codeDelete = Get-StatusCode -url "$base/api/bulk/Export/$($sys19.jobId)" -method "DELETE"
$codeAfterDelete = Get-StatusCode -url "$base/api/bulk/Export/$($sys19.jobId)/status" -method "GET"
[PSCustomObject]@{ DeleteStatusCode = $codeDelete; StatusAfterDelete = $codeAfterDelete } | Format-List

Write-Host "`n== Exercise 23: Download before completion ==" -ForegroundColor Cyan
$notReady23 = New-Export -path "/api/bulk/Export/`$export" -body @{}
$prematureCode = Get-StatusCode -url "$base/api/bulk/Export/$($notReady23.jobId)/download/Patient" -method "GET"
Write-Host "Download before execute -> HTTP $prematureCode"

Write-Host "`n== Exercise 24: Full population workflow ==" -ForegroundColor Cyan
$group24 = Invoke-RestMethod -Method Post -Uri "$base/api/bulk/Group" -ContentType "application/json" -Body (@{
  name = "Annual Review 2025"
  description = "All active members for 2025 annual health review"
  patientIds = @("51707", "52458", "65520")
} | ConvertTo-Json -Depth 10)
$job24 = New-Export -path "/api/bulk/Export/Group/$($group24.groupId)/`$export" -body @{ resourceTypes = @("Patient", "ExplanationOfBenefit", "Encounter", "MedicationRequest", "Condition", "Procedure", "Observation", "AllergyIntolerance") }
Invoke-RestMethod -Method Post -Uri "$base/api/bulk/Export/$($job24.jobId)/execute" | Out-Null
$status24 = Invoke-RestMethod -Uri "$base/api/bulk/Export/$($job24.jobId)/status"
$analytics24 = Invoke-RestMethod -Uri "$base/api/bulk/Export/$($job24.jobId)/analytics"
$condition24 = Invoke-RestMethod -Uri "$base/api/bulk/Export/$($job24.jobId)/download/Condition"
[PSCustomObject]@{
  GroupId = $group24.groupId
  OutputFiles = @($status24.output).Count
  TotalPatients = $analytics24.totalPatients
  ConditionLines = (Get-LineCount $condition24)
} | Format-List

Write-Host "`n== Exercise 25: Compare system vs high-risk analytics ==" -ForegroundColor Cyan
$sys25 = New-Export -path "/api/bulk/Export/`$export" -body @{ resourceTypes = @("Patient", "ExplanationOfBenefit", "Encounter", "MedicationRequest", "Condition") }
Invoke-RestMethod -Method Post -Uri "$base/api/bulk/Export/$($sys25.jobId)/execute" | Out-Null
$sysAnalytics25 = Invoke-RestMethod -Uri "$base/api/bulk/Export/$($sys25.jobId)/analytics"
$grp25 = New-Export -path "/api/bulk/Export/Group/GRP-HIGHRISK/`$export" -body @{ resourceTypes = @("Patient", "ExplanationOfBenefit", "Encounter", "MedicationRequest", "Condition") }
Invoke-RestMethod -Method Post -Uri "$base/api/bulk/Export/$($grp25.jobId)/execute" | Out-Null
$grpAnalytics25 = Invoke-RestMethod -Uri "$base/api/bulk/Export/$($grp25.jobId)/analytics"
[PSCustomObject]@{
  SystemTotalClaims = $sysAnalytics25.totalClaims
  SystemAverageClaimAmount = $sysAnalytics25.averageClaimAmount
  GroupTotalClaims = $grpAnalytics25.totalClaims
  GroupAverageClaimAmount = $grpAnalytics25.averageClaimAmount
} | Format-List

Write-Host "`nDone. Phase 8 Exercises 19-25 executed." -ForegroundColor Green
