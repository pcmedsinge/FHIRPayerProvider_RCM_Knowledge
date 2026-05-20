# Phase 4: Drug Formulary API — Hands-On Guide (25 Exercises)

## Pre-requisites
- HAPI FHIR server running on `http://localhost:8082/fhir`
- Phase 4 API running: `cd Phase4/FormularyAPI && dotnet run --launch-profile http`
- API available at `http://localhost:5240`

---

## Part A: Understanding Drug Data (Exercises 1-8)

### Exercise 1: Explore MedicationRequests in HAPI
**Goal**: See what medication data Synthea generated.

**Postman**: `GET http://localhost:8082/fhir/MedicationRequest?_count=10`

**What to observe**: Each MedicationRequest contains `medicationCodeableConcept` with RxNorm codes, `subject` (patient reference), `status`, `dosageInstruction`.

---

### Exercise 2: Patient's Medications
**Goal**: Find all medications for a specific patient.

**Postman**: `GET http://localhost:8082/fhir/MedicationRequest?patient=Patient/51707&_count=20`

Replace `51707` with a valid patient ID from your HAPI server.

---

### Exercise 3: Count Medication Resources
**Goal**: Understand the medication data volume.

**Postman**:
```
GET http://localhost:8082/fhir/MedicationRequest?_summary=count
GET http://localhost:8082/fhir/Medication?_summary=count
```

**Note**: These counts can differ. `MedicationRequest` is patient/order data, while `Medication` is drug definition data.

---

### Exercise 4: Explore Insurance Plans
**Goal**: View the available insurance plans.

**Postman**: `GET http://localhost:5240/api/formulary/Plan`

**Expected**: Two plans — Blue Cross PPO Gold 2026 and Aetna HMO Silver 2026.

---

### Exercise 5: Plan Tier Structure
**Goal**: Understand how drug tiers work.

**Postman**: `GET http://localhost:5240/api/formulary/Plan/PLAN-PPO-2026/tiers`

**What to observe**: Four tiers (generic, preferred-brand, non-preferred-brand, specialty) with increasing copay/coinsurance.

**Compare**: `GET http://localhost:5240/api/formulary/Plan/PLAN-HMO-2026/tiers`

**Key insight**: Same tiers but different cost-sharing — HMO has lower generic copay ($5 vs $10) but higher specialty coinsurance (35% vs 30%).

---

### Exercise 6: Search All Formulary Drugs
**Goal**: See the complete formulary.

**Postman**: `GET http://localhost:5240/api/formulary/Drug`

**Expected**: All drugs in the formulary across both plans.

---

### Exercise 7: Search by Drug Name
**Goal**: Find a specific drug.

**Postman queries**:
```
GET http://localhost:5240/api/formulary/Drug?name=Lisinopril
GET http://localhost:5240/api/formulary/Drug?name=Ozempic
GET http://localhost:5240/api/formulary/Drug?name=metformin
```

**What to observe**: Same drug may appear on multiple plans with different costs.

---

### Exercise 8: Search by Therapeutic Class
**Goal**: Find all drugs in a therapeutic category.

**Postman**:
```
GET http://localhost:5240/api/formulary/Drug?therapeuticClass=Cardiovascular
GET http://localhost:5240/api/formulary/Drug?therapeuticClass=Diabetes
GET http://localhost:5240/api/formulary/Drug?therapeuticClass=Oncology
GET http://localhost:5240/api/formulary/Drug?therapeuticClass=Mental Health
```

---

## Part B: Coverage & Cost Operations (Exercises 9-19)

### Exercise 9: Coverage Check — Generic Drug
**Goal**: Check coverage for a low-cost generic.

**Postman**: `POST http://localhost:5240/api/formulary/CoverageCheck`
**Body** (JSON):
```json
{
  "drugName": "Lisinopril",
  "planId": "PLAN-PPO-2026"
}
```

**Expected**: Covered, Tier 1 (generic), $10 copay, no prior auth.

---

### Exercise 10: Coverage Check — Specialty Drug with Prior Auth
**Goal**: Check a specialty drug requiring prior authorization.

**Postman**: `POST http://localhost:5240/api/formulary/CoverageCheck`
**Body**:
```json
{
  "drugName": "Ozempic",
  "planId": "PLAN-PPO-2026"
}
```

**Expected**: Covered, specialty tier, 30% coinsurance, **requires prior auth**, quantity limit, step therapy.

---

### Exercise 11: Coverage Check — Drug Not on Formulary
**Goal**: Test behavior for uncovered drugs.

**Postman**: `POST http://localhost:5240/api/formulary/CoverageCheck`
**Body**:
```json
{
  "drugName": "Wegovy",
  "planId": "PLAN-PPO-2026"
}
```

**Expected**: `isCovered: false` with message about contacting plan.

---

### Exercise 12: Compare Cost Across Plans
**Goal**: See how the same drug costs different amounts on different plans.

**Execute both**:
```
GET http://localhost:5240/api/formulary/CoverageCheck?drugName=Lisinopril&planId=PLAN-PPO-2026
GET http://localhost:5240/api/formulary/CoverageCheck?drugName=Lisinopril&planId=PLAN-HMO-2026
```

**Key insight**: PPO copay = $10, HMO copay = $5. Plan choice affects drug costs.

---

### Exercise 13: Find Drug Alternatives
**Goal**: Discover lower-cost options.

**Postman**: `GET http://localhost:5240/api/formulary/Drug/DRUG-005/alternatives`

**Expected**: Crestor (preferred-brand, $30) alternatives include Atorvastatin (generic, $10) — saving $20/month.

---

### Exercise 14: Alternatives for Specialty Drugs
**Goal**: Explore alternatives for expensive specialty drugs.

**Postman**:
```
GET http://localhost:5240/api/formulary/Drug/DRUG-008/alternatives
GET http://localhost:5240/api/formulary/Drug/DRUG-018/alternatives
```

**What to observe**: Specialty drugs may have limited alternatives, all within specialty tier.

---

### Exercise 15: Search by Drug Tier
**Goal**: View all drugs in a specific tier.

**Postman**:
```
GET http://localhost:5240/api/formulary/Drug?tier=generic&planId=PLAN-PPO-2026
GET http://localhost:5240/api/formulary/Drug?tier=preferred-brand&planId=PLAN-PPO-2026
GET http://localhost:5240/api/formulary/Drug?tier=specialty&planId=PLAN-PPO-2026
```

**Key insight**: Generic tier has the most drugs and lowest cost — payers incentivize generic use.

---

### Exercise 16: Step Therapy Drugs
**Goal**: Identify drugs requiring step therapy.

**Postman**: `GET http://localhost:5240/api/formulary/Drug?planId=PLAN-PPO-2026&stepTherapy=true`

This endpoint now supports direct filtering by `stepTherapy=true`.

**Examples**: Crestor (try generics first), Ozempic (try Metformin first), Lexapro (try Sertraline first).

---

### Exercise 17: Prior Auth Required Drugs
**Goal**: Identify all drugs needing prior authorization.

**Postman**: `GET http://localhost:5240/api/formulary/Drug?planId=PLAN-PPO-2026&requiresPriorAuth=true`

This endpoint now supports direct filtering by `requiresPriorAuth=true`.

Expected list includes:
- Ozempic (specialty diabetes)
- Celebrex (non-preferred NSAID)
- Dupixent (specialty biologic)
- Keytruda (specialty oncology)
- Ibrance (specialty oncology)

---

### Exercise 18: Patient Prescriptions via FHIR
**Goal**: Access patient medication data from HAPI through the API.

**Postman**: `GET http://localhost:5240/api/fhir/MedicationRequest?patient=51707&_count=10`

This shows what a patient is actually prescribed — you can then cross-reference with formulary coverage.

---

### Exercise 19: Cross-Reference Prescription with Formulary
**Goal**: Take a patient's prescription and check formulary coverage.

**Steps**:
1. Get patient's meds: `GET http://localhost:5240/api/fhir/MedicationRequest?patient=51707&_count=5`
2. Note the medication name from `medicationCodeableConcept.text`
3. Check formulary: `GET http://localhost:5240/api/formulary/CoverageCheck?drugName={medicationName}&planId=PLAN-PPO-2026`
4. If name-based check fails, fallback to RxNorm via POST:

```json
POST http://localhost:5240/api/formulary/CoverageCheck
{
  "drugName": "",
  "rxNormCode": "{rxNormFromMedicationRequest}",
  "planId": "PLAN-PPO-2026"
}
```

5. If still not covered, search alternatives by class: `GET http://localhost:5240/api/formulary/Drug?therapeuticClass={class}&planId=PLAN-PPO-2026`

---

## Part C: Advanced Scenarios (Exercises 20-25)

### Exercise 20: Build a Cost Comparison Report
**Goal**: Compare total monthly drug costs across plans.

For each plan, calculate total monthly out-of-pocket for these drugs:
- Lisinopril (cardiovascular)
- Metformin (diabetes)
- Sertraline (mental health)

**Steps**:
1. Check each drug on PPO: `GET http://localhost:5240/api/formulary/CoverageCheck?drugName=Lisinopril&planId=PLAN-PPO-2026`
2. Check each drug on HMO: `GET http://localhost:5240/api/formulary/CoverageCheck?drugName=Lisinopril&planId=PLAN-HMO-2026`
3. Sum the copays for each plan

**Expected (current sample data)**: PPO total = `$30/month`; HMO total may be lower (e.g., `$10/month`) if a drug in the comparison set is not covered on HMO.

Tip: Use drugs that exist on both plans for strict apples-to-apples comparison.

---

### Exercise 21: Drug Tier Migration Analysis
**Goal**: Understand the financial impact of tier changes.

**Scenario**: A doctor prescribes Crestor (preferred-brand, $30/month). What if the patient switches to generic Atorvastatin?

**Steps**:
1. Get Crestor: `GET http://localhost:5240/api/formulary/Drug/DRUG-005`
2. Get alternatives: `GET http://localhost:5240/api/formulary/Drug/DRUG-005/alternatives`
3. Calculate savings: $30 - $10 = $20/month = $240/year

---

### Exercise 22: Handle Invalid Inputs
**Goal**: Test API robustness.

**Postman**:
```
GET http://localhost:5240/api/formulary/Drug/INVALID-ID
GET http://localhost:5240/api/formulary/Plan/NONEXISTENT
POST http://localhost:5240/api/formulary/CoverageCheck  (body: {})
GET http://localhost:5240/api/formulary/CoverageCheck?drugName=
```

**Expected**: Appropriate 400/404 responses.

---

### Exercise 23: Multi-Drug Coverage Analysis
**Goal**: Check coverage for a complex medication regimen.

**Scenario**: Diabetes patient on 3 medications:

1. `GET http://localhost:5240/api/formulary/CoverageCheck?drugName=Metformin&planId=PLAN-PPO-2026`
2. `GET http://localhost:5240/api/formulary/CoverageCheck?drugName=Jardiance&planId=PLAN-PPO-2026`
3. `GET http://localhost:5240/api/formulary/CoverageCheck?drugName=Ozempic&planId=PLAN-PPO-2026`

**Analysis**: Metformin ($10, no restrictions) + Jardiance ($30, step therapy) + Ozempic (30% coinsurance, PA required).
The patient should start with Metformin, may add Jardiance, and Ozempic requires prior auth + step therapy completion.

---

### Exercise 24: Therapeutic Substitution Workflow
**Goal**: Simulate a pharmacist finding a covered alternative.

**Scenario**: Prescription for Celebrex (non-preferred, $50, needs PA).

**Steps**:
1. Check coverage: `POST /api/formulary/CoverageCheck` with `drugName=Celebrex`
2. Note: requires PA, $50 copay, step therapy
3. Find alternatives: `GET /api/formulary/Drug/DRUG-010/alternatives`
4. Check Ibuprofen: same class, generic tier, $5, no restrictions
5. **Clinical decision**: Pharmacist recommends Ibuprofen to avoid PA delay and save $45/month

---

### Exercise 25: Full Formulary Dashboard Data
**Goal**: Gather all data needed for a member-facing formulary dashboard.

**Execute this sequence**:
1. List plans: `GET http://localhost:5240/api/formulary/Plan`
2. Get PPO tiers: `GET http://localhost:5240/api/formulary/Plan/PLAN-PPO-2026/tiers`
3. Count drugs by tier: `GET http://localhost:5240/api/formulary/Drug?tier=generic&planId=PLAN-PPO-2026`
4. Count specialty drugs: `GET http://localhost:5240/api/formulary/Drug?tier=specialty&planId=PLAN-PPO-2026`
5. Prior auth drugs: Filter from full list for `requiresPriorAuth: true`
6. Patient meds: `GET http://localhost:5240/api/fhir/MedicationRequest?patient=51707&_count=20`

**Build a mental model**: Plan → Tiers → Drugs → Patient's actual prescriptions → Coverage gaps.

---

## Optional: Run Exercises 19-25 Without Postman

If Postman testing becomes tedious, run the prepared script:

```powershell
powershell -ExecutionPolicy Bypass -File Phase4/Exercises/run_ex19_25.ps1
```

Worked solutions and expected outputs are documented in:
- `Phase4/Exercises/SOLUTIONS_19_25.md`

---

## ✅ Completion Checklist

### Part A: Understanding Drug Data (8 exercises)
- [ ] Exercise 1: MedicationRequests in HAPI
- [ ] Exercise 2: Patient medications
- [ ] Exercise 3: Medication counts
- [ ] Exercise 4: Insurance plans
- [ ] Exercise 5: Tier structure comparison
- [ ] Exercise 6: Full formulary
- [ ] Exercise 7: Drug name search
- [ ] Exercise 8: Therapeutic class search

### Part B: Coverage & Cost (11 exercises)
- [ ] Exercise 9: Generic coverage check
- [ ] Exercise 10: Specialty drug with PA
- [ ] Exercise 11: Uncovered drug
- [ ] Exercise 12: Cross-plan cost comparison
- [ ] Exercise 13: Drug alternatives
- [ ] Exercise 14: Specialty alternatives
- [ ] Exercise 15: Tier-based search
- [ ] Exercise 16: Step therapy drugs
- [ ] Exercise 17: Prior auth drugs
- [ ] Exercise 18: Patient prescriptions (FHIR)
- [ ] Exercise 19: Cross-reference with formulary

### Part C: Advanced Scenarios (6 exercises)
- [ ] Exercise 20: Cost comparison report
- [ ] Exercise 21: Tier migration analysis
- [ ] Exercise 22: Invalid input handling
- [ ] Exercise 23: Multi-drug analysis
- [ ] Exercise 24: Therapeutic substitution
- [ ] Exercise 25: Formulary dashboard
