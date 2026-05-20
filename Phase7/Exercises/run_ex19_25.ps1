$ErrorActionPreference = 'Stop'

$base = "http://localhost:5270"

function Get-StatusCode([string]$url, [string]$method = "GET", [string]$body = $null) {
  try {
    if ($method -eq "POST") {
      Invoke-WebRequest -Method Post -Uri $url -ContentType "application/json" -Body $body | Out-Null
    }
    elseif ($method -eq "PUT") {
      Invoke-WebRequest -Method Put -Uri $url -ContentType "application/json" -Body $body | Out-Null
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

Write-Host "== Phase 7 setup: Create active consent + baseline completed exchange ==" -ForegroundColor Cyan
$consentSetupBody = @{
  patientId = "51707"
  patientName = "Ramon Schulist"
  sourcePayerId = "PAYER-ALPHA"
  targetPayerId = "PAYER-BETA"
  dataCategories = @("claims", "encounters", "medications", "conditions")
} | ConvertTo-Json -Depth 10
$consentSetup = Invoke-RestMethod -Method Post -Uri "$base/api/pdex/Consent" -ContentType "application/json" -Body $consentSetupBody
$activeConsent = Invoke-RestMethod -Method Put -Uri "$base/api/pdex/Consent/$($consentSetup.consentId)/activate"
$exchangeSetup = Invoke-RestMethod -Method Post -Uri "$base/api/pdex/Exchange" -ContentType "application/json" -Body (@{ consentId = $activeConsent.consentId } | ConvertTo-Json)
$baselineJob = Invoke-RestMethod -Method Post -Uri "$base/api/pdex/Exchange/$($exchangeSetup.jobId)/execute"
$baselineJobId = $baselineJob.jobId
Write-Host "Baseline jobId: $baselineJobId (status=$($baselineJob.status))"

Write-Host "`n== Exercise 19: Check exchange job status ==" -ForegroundColor Cyan
$jobStatus = Invoke-RestMethod -Uri "$base/api/pdex/Exchange/$baselineJobId"
$jobStatus | Select-Object jobId, status, totalResourcesFound, totalResourcesTransferred | Format-List

Write-Host "`n== Exercise 20: View exchanged resources ==" -ForegroundColor Cyan
$resources = Invoke-RestMethod -Uri "$base/api/pdex/Exchange/$baselineJobId/resources"
$resources.byType | Format-Table -AutoSize

Write-Host "`n== Exercise 21: View provenance records ==" -ForegroundColor Cyan
$provenance = Invoke-RestMethod -Uri "$base/api/pdex/Exchange/$baselineJobId/provenance"
[PSCustomObject]@{ JobId = $provenance.jobId; ProvenanceRecords = $provenance.total } | Format-List

Write-Host "`n== Exercise 22: Category override (medications only) ==" -ForegroundColor Cyan
$consent22 = Invoke-RestMethod -Method Post -Uri "$base/api/pdex/Consent" -ContentType "application/json" -Body (@{
  patientId = "65520"
  patientName = "Karena O'Keefe"
  sourcePayerId = "PAYER-ALPHA"
  targetPayerId = "PAYER-BETA"
  dataCategories = @("claims", "encounters", "medications", "conditions", "observations")
} | ConvertTo-Json -Depth 10)
Invoke-RestMethod -Method Put -Uri "$base/api/pdex/Consent/$($consent22.consentId)/activate" | Out-Null
$job22 = Invoke-RestMethod -Method Post -Uri "$base/api/pdex/Exchange" -ContentType "application/json" -Body (@{ consentId = $consent22.consentId; dataCategories = @("medications") } | ConvertTo-Json -Depth 10)
$result22 = Invoke-RestMethod -Method Post -Uri "$base/api/pdex/Exchange/$($job22.jobId)/execute"
$summary22 = Invoke-RestMethod -Uri "$base/api/pdex/Exchange/$($job22.jobId)/resources"
[PSCustomObject]@{
  JobId = $result22.jobId
  Status = $result22.status
  ResourceTypesReturned = (@($summary22.byType.resourceType) -join ", ")
} | Format-List

Write-Host "`n== Exercise 23: Exchange with inactive consent ==" -ForegroundColor Cyan
$consent23 = Invoke-RestMethod -Method Post -Uri "$base/api/pdex/Consent" -ContentType "application/json" -Body (@{
  patientId = "51707"
  patientName = "Ramon Schulist"
  sourcePayerId = "PAYER-ALPHA"
  targetPayerId = "PAYER-GAMMA"
} | ConvertTo-Json)
Invoke-RestMethod -Method Put -Uri "$base/api/pdex/Consent/$($consent23.consentId)/revoke" | Out-Null
$inactiveCode = Get-StatusCode -url "$base/api/pdex/Exchange" -method "POST" -body (@{ consentId = $consent23.consentId } | ConvertTo-Json)
Write-Host "Initiate exchange with revoked consent -> HTTP $inactiveCode"

Write-Host "`n== Exercise 24: Cancel exchange job ==" -ForegroundColor Cyan
$consent24 = Invoke-RestMethod -Method Post -Uri "$base/api/pdex/Consent" -ContentType "application/json" -Body (@{
  patientId = "51707"
  patientName = "Ramon Schulist"
  sourcePayerId = "PAYER-ALPHA"
  targetPayerId = "PAYER-GAMMA"
} | ConvertTo-Json)
Invoke-RestMethod -Method Put -Uri "$base/api/pdex/Consent/$($consent24.consentId)/activate" | Out-Null
$job24 = Invoke-RestMethod -Method Post -Uri "$base/api/pdex/Exchange" -ContentType "application/json" -Body (@{ consentId = $consent24.consentId } | ConvertTo-Json)
$cancel24 = Invoke-RestMethod -Method Put -Uri "$base/api/pdex/Exchange/$($job24.jobId)/cancel"
[PSCustomObject]@{ JobId = $cancel24.jobId; Status = $cancel24.status } | Format-List

Write-Host "`n== Exercise 25: Complete end-to-end workflow ==" -ForegroundColor Cyan
$match25 = Invoke-RestMethod -Method Post -Uri "$base/api/pdex/MemberMatch" -ContentType "application/json" -Body (@{
  memberFirstName = "Karena"
  memberLastName = "O'Keefe"
  memberDateOfBirth = "1980-05-10"
  memberGender = "female"
  memberId = "65520"
  oldPayerId = "PAYER-ALPHA"
  newPayerId = "PAYER-GAMMA"
} | ConvertTo-Json -Depth 10)
$summary25 = Invoke-RestMethod -Uri "$base/api/pdex/MemberMatch/members/65520"
$consent25 = Invoke-RestMethod -Method Post -Uri "$base/api/pdex/Consent" -ContentType "application/json" -Body (@{
  patientId = "65520"
  patientName = "Karena O'Keefe"
  sourcePayerId = "PAYER-ALPHA"
  targetPayerId = "PAYER-GAMMA"
  dataCategories = @("claims", "encounters", "medications", "conditions", "allergies")
} | ConvertTo-Json -Depth 10)
Invoke-RestMethod -Method Put -Uri "$base/api/pdex/Consent/$($consent25.consentId)/activate" | Out-Null
$job25 = Invoke-RestMethod -Method Post -Uri "$base/api/pdex/Exchange" -ContentType "application/json" -Body (@{ consentId = $consent25.consentId } | ConvertTo-Json)
Invoke-RestMethod -Method Post -Uri "$base/api/pdex/Exchange/$($job25.jobId)/execute" | Out-Null
$res25 = Invoke-RestMethod -Uri "$base/api/pdex/Exchange/$($job25.jobId)/resources"
$prov25 = Invoke-RestMethod -Uri "$base/api/pdex/Exchange/$($job25.jobId)/provenance"
[PSCustomObject]@{
  MatchConfidence = $match25.matchConfidence
  PatientName = $summary25.patientName
  JobId = $job25.jobId
  TotalTransferred = $res25.totalResources
  ProvenanceRecords = $prov25.total
} | Format-List

Write-Host "`nDone. Phase 7 Exercises 19-25 executed." -ForegroundColor Green
