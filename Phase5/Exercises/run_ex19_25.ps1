$ErrorActionPreference = 'Stop'

$baseCrd = "http://localhost:5250"
$baseFormulary = "http://localhost:5240"

function New-HookRequest([string]$hook, [array]$selections) {
  return @{
    hookInstance = [guid]::NewGuid().ToString()
    fhirServer = "http://localhost:8082/fhir"
    hook = $hook
    context = @{
      userId = "Practitioner/123"
      patientId = "51707"
      selections = $selections
    }
  }
}

Write-Host "== Phase 5 | Exercise 19: Compare order-select vs order-sign cards ==" -ForegroundColor Cyan
$mriSel = @(@{ code = "70553"; display = "MRI Brain with and without contrast" })
$orderSelectReq = New-HookRequest -hook "order-select" -selections $mriSel
$orderSignReq = New-HookRequest -hook "order-sign" -selections $mriSel
$orderSelectResp = Invoke-RestMethod -Method Post -Uri "$baseCrd/cds-services/crd-order-select" -ContentType "application/json" -Body ($orderSelectReq | ConvertTo-Json -Depth 10)
$orderSignResp = Invoke-RestMethod -Method Post -Uri "$baseCrd/cds-services/crd-order-sign" -ContentType "application/json" -Body ($orderSignReq | ConvertTo-Json -Depth 10)
$selectCount = @($orderSelectResp.cards).Count
$signCount = @($orderSignResp.cards).Count
$hasReviewCard = @($orderSignResp.cards | Where-Object { $_.summary -like "*Review Required Before Signing*" }).Count -gt 0
[PSCustomObject]@{ OrderSelectCards = $selectCount; OrderSignCards = $signCount; OrderSignHasReviewCard = $hasReviewCard } | Format-List

Write-Host "`n== Exercise 20: End-to-end CRD workflow sample ==" -ForegroundColor Cyan
$discovery = Invoke-RestMethod -Uri "$baseCrd/cds-services"
$workflowReq = New-HookRequest -hook "order-select" -selections @(
  @{ code = "70553"; display = "MRI Brain" },
  @{ code = "71046"; display = "Chest X-Ray" }
)
$workflowSignReq = New-HookRequest -hook "order-sign" -selections @(
  @{ code = "70553"; display = "MRI Brain" },
  @{ code = "71046"; display = "Chest X-Ray" }
)
$workflowSelect = Invoke-RestMethod -Method Post -Uri "$baseCrd/cds-services/crd-order-select" -ContentType "application/json" -Body ($workflowReq | ConvertTo-Json -Depth 10)
$workflowSign = Invoke-RestMethod -Method Post -Uri "$baseCrd/cds-services/crd-order-sign" -ContentType "application/json" -Body ($workflowSignReq | ConvertTo-Json -Depth 10)
[PSCustomObject]@{
  DiscoveryServices = @($discovery.services).Count
  OrderSelectCards = @($workflowSelect.cards).Count
  OrderSignCards = @($workflowSign.cards).Count
} | Format-List

Write-Host "`n== Exercise 21: CRD + Formulary integration (Ozempic) ==" -ForegroundColor Cyan
$ozRule = Invoke-RestMethod -Uri "$baseCrd/api/Rules/check/1991302"
$formularyOk = $true
try {
  $ozCoverage = Invoke-RestMethod -Uri "$baseFormulary/api/formulary/CoverageCheck?drugName=Ozempic&planId=PLAN-PPO-2026"
  $ozAlternatives = Invoke-RestMethod -Uri "$baseFormulary/api/formulary/Drug/DRUG-008/alternatives"
} catch {
  $formularyOk = $false
  Write-Host "Phase 4 Formulary API not reachable; skipping cross-phase calls." -ForegroundColor Yellow
}
if ($formularyOk) {
  [PSCustomObject]@{
    CrdRequiresPA = $ozRule.requiresPriorAuth
    FormularyRequiresPA = $ozCoverage.requiresPriorAuth
    FormularyStepTherapy = $ozCoverage.stepTherapy
    AlternativeCount = @($ozAlternatives.alternatives).Count
  } | Format-List
}

Write-Host "`n== Exercise 22: Prior auth documentation checklist (27447) ==" -ForegroundColor Cyan
$kneeRule = Invoke-RestMethod -Uri "$baseCrd/api/Rules/check/27447"
Write-Host "Required documents:"
$kneeRule.requiredDocuments | ForEach-Object { Write-Host (" - [ ] " + $_) }

Write-Host "`n== Exercise 23: Complex patient visit ==" -ForegroundColor Cyan
$complexReq = New-HookRequest -hook "order-select" -selections @(
  @{ code = "72148"; display = "MRI Lumbar Spine" },
  @{ code = "1991302"; display = "Ozempic" },
  @{ code = "E0601"; display = "CPAP Device" }
)
$complexResp = Invoke-RestMethod -Method Post -Uri "$baseCrd/cds-services/crd-order-select" -ContentType "application/json" -Body ($complexReq | ConvertTo-Json -Depth 10)
$complexResp.cards | Select-Object indicator, summary | Format-Table -AutoSize

Write-Host "`n== Exercise 24: Card indicator analysis ==" -ForegroundColor Cyan
$codes = @("70553", "71046", "15780", "43239")
$indicatorRows = foreach ($code in $codes) {
  $rule = Invoke-RestMethod -Uri "$baseCrd/api/Rules/check/$code"
  $indicator = if ($rule.coverageStatus -eq "not-covered") { "critical" } elseif ($rule.requiresPriorAuth) { "warning" } else { "info" }
  [PSCustomObject]@{ Code = $code; RequiresPA = $rule.requiresPriorAuth; CoverageStatus = $rule.coverageStatus; DerivedIndicator = $indicator }
}
$indicatorRows | Format-Table -AutoSize

Write-Host "`n== Exercise 25: CRD -> DTR -> PAS preview (link inspection) ==" -ForegroundColor Cyan
$previewReq = New-HookRequest -hook "order-select" -selections @(@{ code = "27447"; display = "Total Knee Replacement" })
$previewResp = Invoke-RestMethod -Method Post -Uri "$baseCrd/cds-services/crd-order-select" -ContentType "application/json" -Body ($previewReq | ConvertTo-Json -Depth 10)
$firstCard = @($previewResp.cards)[0]
[PSCustomObject]@{
  CardSummary = $firstCard.summary
  SuggestionCount = @($firstCard.suggestions).Count
  LinkLabels = (@($firstCard.links | ForEach-Object { $_.label }) -join ", ")
} | Format-List

Write-Host "`nDone. Phase 5 Exercises 19-25 executed." -ForegroundColor Green
