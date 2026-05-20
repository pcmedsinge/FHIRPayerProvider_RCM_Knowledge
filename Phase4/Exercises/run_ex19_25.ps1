$ErrorActionPreference = 'Stop'

$base = "http://localhost:5240"
$planPpo = "PLAN-PPO-2026"
$planHmo = "PLAN-HMO-2026"
$patientId = "51707"

Write-Host "== Exercise 19: Cross-reference patient med with formulary ==" -ForegroundColor Cyan
$bundle = Invoke-RestMethod -Uri "$base/api/fhir/MedicationRequest?patient=$patientId&_count=10"
$preferredRx = @("314076","861004","1991302","312940")
$allMeds = $bundle.entry | ForEach-Object { $_.resource } | Where-Object { $_.medicationCodeableConcept }
$med = $allMeds | Where-Object {
  $rx = ($_.medicationCodeableConcept.coding | Where-Object { $_.system -match 'rxnorm' } | Select-Object -First 1).code
  $preferredRx -contains $rx
} | Select-Object -First 1
if (-not $med) { $med = $allMeds | Select-Object -First 1 }
$medName = $med.medicationCodeableConcept.text
$rx = ($med.medicationCodeableConcept.coding | Where-Object { $_.system -match 'rxnorm' } | Select-Object -First 1).code
Write-Host "Picked med: $medName (RxNorm: $rx)"
try {
  $covByName = Invoke-RestMethod -Uri ($base + "/api/formulary/CoverageCheck?drugName=" + [uri]::EscapeDataString($medName) + "&planId=$planPpo")
  $covByName | ConvertTo-Json -Depth 4
} catch {
  Write-Host "Name check failed; continuing with RxNorm..." -ForegroundColor Yellow
}
$body = @{ drugName = ""; rxNormCode = $rx; planId = $planPpo } | ConvertTo-Json
$covByRx = Invoke-RestMethod -Method Post -Uri "$base/api/formulary/CoverageCheck" -ContentType "application/json" -Body $body
$covByRx | ConvertTo-Json -Depth 4

Write-Host "`n== Exercise 20: PPO vs HMO cost comparison ==" -ForegroundColor Cyan
$drugs = "Lisinopril","Metformin","Sertraline"
$rows = foreach($plan in @($planPpo, $planHmo)){
  foreach($drug in $drugs){
    $r = Invoke-RestMethod -Uri "$base/api/formulary/CoverageCheck?drugName=$drug&planId=$plan"
    [PSCustomObject]@{ Plan=$plan; Drug=$drug; Copay=$r.estimatedCopay }
  }
}
$rows | Format-Table -AutoSize
$rows | Group-Object Plan | ForEach-Object {
  [PSCustomObject]@{ Plan=$_.Name; MonthlyTotal=($_.Group | Measure-Object Copay -Sum).Sum }
} | Format-Table -AutoSize

Write-Host "`n== Exercise 21: Crestor migration analysis ==" -ForegroundColor Cyan
$crestor = Invoke-RestMethod -Uri "$base/api/formulary/Drug/DRUG-005"
$alts = Invoke-RestMethod -Uri "$base/api/formulary/Drug/DRUG-005/alternatives"
$best = $alts.alternatives | Sort-Object potentialSavings -Descending | Select-Object -First 1
[PSCustomObject]@{
  Original = $crestor.drugName
  OriginalCopay = $crestor.copay
  BestAlternative = $best.drugName
  AltCopay = $best.copay
  MonthlySavings = $best.potentialSavings
  AnnualSavings = $best.potentialSavings * 12
} | Format-List

Write-Host "`n== Exercise 22: Invalid input checks ==" -ForegroundColor Cyan
$invalidUrls = @(
  "$base/api/formulary/Drug/INVALID-ID",
  "$base/api/formulary/Plan/NONEXISTENT",
  "$base/api/formulary/CoverageCheck?drugName="
)

function Get-StatusCode($url, $method = "GET", $body = $null) {
  try {
    if ($method -eq "POST") {
      Invoke-WebRequest -Method Post -Uri $url -ContentType "application/json" -Body $body | Out-Null
    }
    else {
      Invoke-WebRequest -Uri $url | Out-Null
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

foreach($u in $invalidUrls){
  $code = Get-StatusCode -url $u
  Write-Host "$u -> $code"
}
$postCode = Get-StatusCode -url "$base/api/formulary/CoverageCheck" -method "POST" -body "{}"
Write-Host "POST /CoverageCheck {} -> $postCode"

Write-Host "`n== Exercise 23: Multi-drug regimen ==" -ForegroundColor Cyan
foreach($d in "Metformin","Jardiance","Ozempic"){
  $r = Invoke-RestMethod -Uri "$base/api/formulary/CoverageCheck?drugName=$d&planId=$planPpo"
  [PSCustomObject]@{
    Drug=$d; Tier=$r.drugTier; Copay=$r.estimatedCopay; PA=$r.requiresPriorAuth; StepTherapy=$r.stepTherapy
  } | Format-Table -AutoSize
}

Write-Host "`n== Exercise 24: Therapeutic substitution (Celebrex) ==" -ForegroundColor Cyan
$celebrexCov = Invoke-RestMethod -Method Post -Uri "$base/api/formulary/CoverageCheck" -ContentType "application/json" -Body '{"drugName":"Celebrex","planId":"PLAN-PPO-2026"}'
$celebrexAlts = Invoke-RestMethod -Uri "$base/api/formulary/Drug/DRUG-010/alternatives"
$bestAlt = $celebrexAlts.alternatives | Sort-Object potentialSavings -Descending | Select-Object -First 1
[PSCustomObject]@{
  OriginalDrug = $celebrexCov.drugName
  OriginalCopay = $celebrexCov.estimatedCopay
  RequiresPA = $celebrexCov.requiresPriorAuth
  StepTherapy = $celebrexCov.stepTherapy
  SuggestedAlternative = $bestAlt.drugName
  AlternativeCopay = $bestAlt.copay
  PotentialSavings = $bestAlt.potentialSavings
} | Format-List

Write-Host "`n== Exercise 25: Dashboard summary pack ==" -ForegroundColor Cyan
$plans = Invoke-RestMethod -Uri "$base/api/formulary/Plan"
$tiers = Invoke-RestMethod -Uri "$base/api/formulary/Plan/$planPpo/tiers"
$generic = Invoke-RestMethod -Uri "$base/api/formulary/Drug?tier=generic&planId=$planPpo"
$specialty = Invoke-RestMethod -Uri "$base/api/formulary/Drug?tier=specialty&planId=$planPpo"
$pa = Invoke-RestMethod -Uri "$base/api/formulary/Drug?planId=$planPpo&requiresPriorAuth=true"
$meds = Invoke-RestMethod -Uri "$base/api/fhir/MedicationRequest?patient=$patientId&_count=20"

[PSCustomObject]@{
  PlanCount = $plans.Count
  TierCount = $tiers.Count
  GenericDrugCount = $generic.total
  SpecialtyDrugCount = $specialty.total
  PriorAuthDrugCount = $pa.total
  PatientMedicationEntries = $meds.entry.Count
} | Format-List

Write-Host "`nDone. Exercises 19-25 executed." -ForegroundColor Green
