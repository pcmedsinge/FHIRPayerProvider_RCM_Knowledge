# Phase 4 — Worked Solutions (Exercises 19–25)

This file gives executable solutions for Exercises 19 onward without relying on Postman UI.

## Prerequisites
- FormularyAPI running on `http://localhost:5240`
- HAPI FHIR running on `http://localhost:8082/fhir`
- PowerShell available

---

## Exercise 19 — Cross-Reference Prescription with Formulary

### Goal
Take a real patient medication and check payer formulary coverage.

### Working approach
Use patient `51707` and pick RxNorm/medication from MedicationRequest list.

### Commands
```powershell
# 1) Get patient meds
$bundle = Invoke-RestMethod -Uri "http://localhost:5240/api/fhir/MedicationRequest?patient=51707&_count=10"

# 2) Extract first medication with RxNorm
$med = $bundle.entry | ForEach-Object { $_.resource } |
  Where-Object { $_.medicationCodeableConcept.coding } |
  Select-Object -First 1

$rx = ($med.medicationCodeableConcept.coding | Where-Object { $_.system -match 'rxnorm' } | Select-Object -First 1).code
$name = $med.medicationCodeableConcept.text

# 3) Check formulary by name (PLAN-PPO-2026)
Invoke-RestMethod -Uri ("http://localhost:5240/api/formulary/CoverageCheck?drugName=" + [uri]::EscapeDataString($name) + "&planId=PLAN-PPO-2026")

# 4) If name doesn't match, check by RxNorm via POST
Invoke-RestMethod -Method Post -Uri "http://localhost:5240/api/formulary/CoverageCheck" -ContentType "application/json" -Body (@{
  drugName = ""
  rxNormCode = $rx
  planId = "PLAN-PPO-2026"
} | ConvertTo-Json)
```

### Expected behavior
- If med is in formulary: `isCovered: true`, tier/copay/PA flags returned.
- If not in formulary: `isCovered: false` with message.
- For patient `51707`, one sampled med is Lisinopril (`rxNormCode: 314076`) which is covered.

---

## Exercise 20 — Cost Comparison Report

### Goal
Compare monthly patient out-of-pocket between PPO vs HMO.

### Commands
```powershell
$drugs = "Lisinopril","Metformin","Sertraline"
$plans = "PLAN-PPO-2026","PLAN-HMO-2026"

$result = foreach($p in $plans){
  $rows = foreach($d in $drugs){
    $r = Invoke-RestMethod -Uri "http://localhost:5240/api/formulary/CoverageCheck?drugName=$d&planId=$p"
    [PSCustomObject]@{ Plan=$p; Drug=$d; Covered=$r.isCovered; Copay=$r.estimatedCopay }
  }
  [PSCustomObject]@{
    Plan = $p
    MonthlyTotal = ($rows | Measure-Object -Property Copay -Sum).Sum
    Detail = $rows
  }
}
$result | ConvertTo-Json -Depth 6
```

### Expected
- PPO total ≈ `$30`
- HMO total ≈ `$15`

---

## Exercise 21 — Tier Migration (Crestor → Atorvastatin)

### Commands
```powershell
$crestor = Invoke-RestMethod -Uri "http://localhost:5240/api/formulary/Drug/DRUG-005"
$alts = Invoke-RestMethod -Uri "http://localhost:5240/api/formulary/Drug/DRUG-005/alternatives"

$best = $alts.alternatives | Sort-Object potentialSavings -Descending | Select-Object -First 1

[PSCustomObject]@{
  OriginalDrug = $crestor.drugName
  OriginalCopay = $crestor.copay
  BestAlternative = $best.drugName
  AltCopay = $best.copay
  MonthlySavings = $best.potentialSavings
  AnnualSavings = $best.potentialSavings * 12
} | ConvertTo-Json
```

### Expected
- Crestor copay `$30`
- Best generic alternative around `$10`
- Savings ≈ `$20/month`, `$240/year`

---

## Exercise 22 — Invalid Inputs

### Commands
```powershell
Invoke-WebRequest -Uri "http://localhost:5240/api/formulary/Drug/INVALID-ID" -SkipHttpErrorCheck
Invoke-WebRequest -Uri "http://localhost:5240/api/formulary/Plan/NONEXISTENT" -SkipHttpErrorCheck
Invoke-WebRequest -Method Post -Uri "http://localhost:5240/api/formulary/CoverageCheck" -ContentType "application/json" -Body "{}" -SkipHttpErrorCheck
Invoke-WebRequest -Uri "http://localhost:5240/api/formulary/CoverageCheck?drugName=" -SkipHttpErrorCheck
```

### Expected
- Proper 400/404 behavior and descriptive error payloads.

---

## Exercise 23 — Multi-Drug Regimen Coverage

### Commands
```powershell
$regimen = "Metformin","Jardiance","Ozempic"
$analysis = foreach($d in $regimen){
  $r = Invoke-RestMethod -Uri "http://localhost:5240/api/formulary/CoverageCheck?drugName=$d&planId=PLAN-PPO-2026"
  [PSCustomObject]@{
    Drug = $d
    Covered = $r.isCovered
    Tier = $r.drugTier
    Copay = $r.estimatedCopay
    Coinsurance = $r.coinsurancePercent
    RequiresPriorAuth = $r.requiresPriorAuth
    StepTherapy = $r.stepTherapy
  }
}
$analysis | ConvertTo-Json -Depth 4
```

### Expected
- Metformin: covered generic, low cost, no PA
- Jardiance: covered preferred brand, step therapy likely true
- Ozempic: covered specialty, PA true, step therapy true

---

## Exercise 24 — Therapeutic Substitution (Celebrex)

### Commands
```powershell
$cov = Invoke-RestMethod -Method Post -Uri "http://localhost:5240/api/formulary/CoverageCheck" -ContentType "application/json" -Body '{"drugName":"Celebrex","planId":"PLAN-PPO-2026"}'
$alts = Invoke-RestMethod -Uri "http://localhost:5240/api/formulary/Drug/DRUG-010/alternatives"

[PSCustomObject]@{
  Original = $cov.drugName
  OriginalCopay = $cov.estimatedCopay
  RequiresPA = $cov.requiresPriorAuth
  StepTherapy = $cov.stepTherapy
  SuggestedAlternative = ($alts.alternatives | Sort-Object potentialSavings -Descending | Select-Object -First 1).drugName
} | ConvertTo-Json -Depth 4
```

### Expected
- Celebrex: higher cost + PA + step therapy
- Ibuprofen/Naproxen alternatives lower cost with fewer restrictions

---

## Exercise 25 — Dashboard Data Pack

### Commands
```powershell
$plans = Invoke-RestMethod -Uri "http://localhost:5240/api/formulary/Plan"
$tiers = Invoke-RestMethod -Uri "http://localhost:5240/api/formulary/Plan/PLAN-PPO-2026/tiers"
$generic = Invoke-RestMethod -Uri "http://localhost:5240/api/formulary/Drug?tier=generic&planId=PLAN-PPO-2026"
$specialty = Invoke-RestMethod -Uri "http://localhost:5240/api/formulary/Drug?tier=specialty&planId=PLAN-PPO-2026"
$pa = Invoke-RestMethod -Uri "http://localhost:5240/api/formulary/Drug?planId=PLAN-PPO-2026&requiresPriorAuth=true"
$meds = Invoke-RestMethod -Uri "http://localhost:5240/api/fhir/MedicationRequest?patient=51707&_count=20"

[PSCustomObject]@{
  PlanCount = $plans.Count
  TierCount = $tiers.Count
  GenericDrugCount = $generic.total
  SpecialtyDrugCount = $specialty.total
  PriorAuthDrugCount = $pa.total
  PatientMedicationEntries = $meds.entry.Count
} | ConvertTo-Json
```

### Expected
- Clean summary object ready for dashboard widgets/cards.

---

## Optional one-shot runner
You can run all these in one go using:
- `Phase4/Exercises/run_ex19_25.ps1`
