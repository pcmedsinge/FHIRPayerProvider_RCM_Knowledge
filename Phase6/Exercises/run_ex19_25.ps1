$ErrorActionPreference = 'Stop'

$base = "http://localhost:5260"

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

Write-Host "== Phase 6 setup: Create baseline pended PA for status/history exercises ==" -ForegroundColor Cyan
$baselineBody = @{
  patientId = "51707"
  serviceCode = "70553"
  serviceDescription = "MRI Brain"
  providerId = "PRACT-SETUP"
  providerName = "Dr. Setup"
  diagnosis = "Persistent headache"
  urgency = "routine"
} | ConvertTo-Json
$baseline = Invoke-RestMethod -Method Post -Uri "$base/api/pas/PriorAuth/submit" -ContentType "application/json" -Body $baselineBody
$baselineAuthId = $baseline.authorizationId
Write-Host "Baseline authorizationId: $baselineAuthId (status=$($baseline.status))"

Write-Host "`n== Exercise 19: Check PA status ==" -ForegroundColor Cyan
$status19 = Invoke-RestMethod -Uri "$base/api/pas/PriorAuth/status/$baselineAuthId"
$status19 | Select-Object authorizationId, status, reviewOutcome, decisionDate | Format-List

Write-Host "`n== Exercise 20: Patient PA history ==" -ForegroundColor Cyan
$patientHistory = Invoke-RestMethod -Uri "$base/api/pas/PriorAuth/patient/51707"
[PSCustomObject]@{ PatientId = "51707"; TotalRequests = $patientHistory.total } | Format-List

Write-Host "`n== Exercise 21: Complete DTR-to-PAS workflow (74177) ==" -ForegroundColor Cyan
$q74177 = Invoke-RestMethod -Uri "$base/api/dtr/Questionnaire/by-service/74177"
$responseBody = @{
  questionnaireId = "DTR-Q-74177"
  patientId = "65520"
  answers = @{
    indication = "Acute abdominal pain - RLQ tenderness"
    "lab-results" = "Elevated WBC, CRP"
    "previous-imaging" = "Ultrasound inconclusive"
    "ultrasound-first" = "Yes"
    emergency = "No"
  }
} | ConvertTo-Json -Depth 10
$qResponse = Invoke-RestMethod -Method Post -Uri "$base/api/dtr/Questionnaire/response" -ContentType "application/json" -Body $responseBody
$pa74177Body = @{
  patientId = "65520"
  serviceCode = "74177"
  serviceDescription = "CT Abdomen with Contrast"
  providerId = "PRACT-030"
  providerName = "Dr. Radiologist"
  diagnosis = "Acute abdominal pain - rule out appendicitis"
  urgency = "urgent"
  questionnaireResponseId = $qResponse.responseId
  supportingDocuments = @("Lab results", "Ultrasound report")
} | ConvertTo-Json -Depth 10
$pa74177 = Invoke-RestMethod -Method Post -Uri "$base/api/pas/PriorAuth/submit" -ContentType "application/json" -Body $pa74177Body
[PSCustomObject]@{
  QuestionnaireFound = $q74177.questionnaireId
  QuestionnaireResponseId = $qResponse.responseId
  AuthorizationId = $pa74177.authorizationId
  Status = $pa74177.status
} | Format-List

Write-Host "`n== Exercise 22: Admin list all PA requests ==" -ForegroundColor Cyan
$allPa = Invoke-RestMethod -Uri "$base/api/pas/PriorAuth"
$statusSummary = @($allPa.requests) | Group-Object status | Select-Object Name, Count
[PSCustomObject]@{ TotalRequests = $allPa.total } | Format-List
$statusSummary | Format-Table -AutoSize

Write-Host "`n== Exercise 23: Admin update pended request ==" -ForegroundColor Cyan
$updateUrl = "$base/api/pas/PriorAuth/update/$baselineAuthId?status=approved&notes=Medical%20director%20approved%20after%20peer-to-peer%20review"
$updated = Invoke-RestMethod -Method Put -Uri $updateUrl
$verifyUpdated = Invoke-RestMethod -Uri "$base/api/pas/PriorAuth/status/$baselineAuthId"
[PSCustomObject]@{
  AuthorizationId = $updated.authorizationId
  UpdatedStatus = $updated.status
  HistoryEntries = @($verifyUpdated.statusHistory).Count
} | Format-List

Write-Host "`n== Exercise 24: Cancel a pending PA request ==" -ForegroundColor Cyan
$cancelBody = @{
  patientId = "51707"
  serviceCode = "72148"
  serviceDescription = "MRI Lumbar Spine"
  providerId = "PRACT-003"
  providerName = "Dr. Neurologist"
  diagnosis = "Lower back pain"
  urgency = "routine"
} | ConvertTo-Json
$toCancel = Invoke-RestMethod -Method Post -Uri "$base/api/pas/PriorAuth/submit" -ContentType "application/json" -Body $cancelBody
$cancelled = Invoke-RestMethod -Method Put -Uri "$base/api/pas/PriorAuth/cancel/$($toCancel.authorizationId)"
[PSCustomObject]@{ AuthorizationId = $toCancel.authorizationId; CancelStatus = $cancelled.status } | Format-List

Write-Host "`n== Exercise 25: Edge cases & validation ==" -ForegroundColor Cyan
$codeMissingPatient = Get-StatusCode -url "$base/api/pas/PriorAuth/submit" -method "POST" -body '{"serviceCode":"71046","serviceDescription":"Chest X-Ray"}'
$codeMissingService = Get-StatusCode -url "$base/api/pas/PriorAuth/submit" -method "POST" -body '{"patientId":"51707"}'
$codeMissingAuth = Get-StatusCode -url "$base/api/pas/PriorAuth/status/NONEXISTENT-ID" -method "GET"
$codeInvalidStatus = Get-StatusCode -url "$base/api/pas/PriorAuth/update/$baselineAuthId?status=invalid-status" -method "PUT" -body ""
$codeMissingQ = Get-StatusCode -url "$base/api/dtr/Questionnaire/DOES-NOT-EXIST" -method "GET"
[PSCustomObject]@{
  MissingPatient = $codeMissingPatient
  MissingServiceCode = $codeMissingService
  NonExistentAuthorization = $codeMissingAuth
  InvalidStatusUpdate = $codeInvalidStatus
  NonExistentQuestionnaire = $codeMissingQ
} | Format-List

Write-Host "`nDone. Phase 6 Exercises 19-25 executed." -ForegroundColor Green
