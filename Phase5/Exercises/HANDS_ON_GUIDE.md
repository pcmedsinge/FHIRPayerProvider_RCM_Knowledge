# Phase 5: CRD Service — Hands-On Guide (25 Exercises)

## Pre-requisites
- Phase 5 API running: `cd Phase5/CRDService && dotnet run --launch-profile http`
- API available at `http://localhost:5250`

---

## Part A: CDS Hooks Discovery & Rules (Exercises 1-8)

### Exercise 1: CDS Hooks Discovery
**Goal**: Discover what hooks this CRD service supports.

**Postman**: `GET http://localhost:5250/cds-services`

**Expected**: JSON with `services` array containing three hook definitions (order-select, order-sign, appointment-book) with prefetch templates.

**What to observe**: Each service has `hook`, `title`, `description`, `id`, and `prefetch` — the EHR uses prefetch templates to send relevant patient data with hook invocations.

---

### Exercise 2: View All Coverage Rules
**Goal**: See the complete rules engine.

**Postman**: `GET http://localhost:5250/api/Rules`

**Expected**: 13 coverage rules across imaging, procedure, medication, and DME categories.

---

### Exercise 3: Rules by Service Type — Imaging
**Goal**: See imaging-specific rules.

**Postman**: `GET http://localhost:5250/api/Rules/type/imaging`

**Expected**: 4 rules — MRI Brain (PA), CT Abdomen (PA), Chest X-Ray (no PA), MRI Lumbar (PA).

---

### Exercise 4: Rules by Service Type — All Types
**Goal**: Explore all rule categories.

**Postman**:
```
GET http://localhost:5250/api/Rules/type/procedure
GET http://localhost:5250/api/Rules/type/medication
GET http://localhost:5250/api/Rules/type/dme
```

---

### Exercise 5: Quick Code Check — MRI Brain
**Goal**: Check if a specific CPT code requires prior auth.

**Postman**: `GET http://localhost:5250/api/Rules/check/70553`

**Expected**: Found = true, requiresPriorAuth = true, with required documents list (clinical indication, previous imaging, neuro exam).

---

### Exercise 6: Quick Code Check — Chest X-Ray (No PA)
**Goal**: Check a code that doesn't need prior auth.

**Postman**: `GET http://localhost:5250/api/Rules/check/71046`

**Expected**: Found = true, requiresPriorAuth = false, coverageStatus = "covered".

---

### Exercise 7: Quick Code Check — Unknown Code
**Goal**: Test behavior with an unrecognized code.

**Postman**: `GET http://localhost:5250/api/Rules/check/99999`

**Expected**: Found = false, message about standard coverage applying.

---

### Exercise 8: Quick Code Check — Not Covered
**Goal**: Check a cosmetic procedure.

**Postman**: `GET http://localhost:5250/api/Rules/check/15780`

**Expected**: Found = true, coverageStatus = "not-covered", message about cosmetic exclusion.

---

## Part B: CDS Hook Invocations (Exercises 9-19)

### Exercise 9: Order-Select — MRI Brain Order
**Goal**: Simulate a clinician selecting an MRI order in the EHR.

**Postman**: `POST http://localhost:5250/cds-services/crd-order-select`
**Headers**: `Content-Type: application/json`
**Body**:
```json
{
  "hookInstance": "d1577c69-dfbe-44ad-bd63-3d3145dc3e65",
  "fhirServer": "http://localhost:8082/fhir",
  "hook": "order-select",
  "context": {
    "userId": "Practitioner/123",
    "patientId": "51707",
    "selections": [
      {
        "resourceType": "ServiceRequest",
        "code": "70553",
        "display": "MRI Brain with and without contrast"
      }
    ]
  }
}
```

**Expected**: Warning card: "Prior Authorization Required: MRI Brain" with required documents and link to start PA.

---

### Exercise 10: Order-Select — Chest X-Ray (Covered)
**Goal**: Order something that doesn't need prior auth.

**Postman**: `POST http://localhost:5250/cds-services/crd-order-select`
**Body**:
```json
{
  "hookInstance": "a1234567-0000-0000-0000-000000000001",
  "fhirServer": "http://localhost:8082/fhir",
  "hook": "order-select",
  "context": {
    "userId": "Practitioner/123",
    "patientId": "51707",
    "selections": [
      {
        "resourceType": "ServiceRequest",
        "code": "71046",
        "display": "Chest X-Ray 2 views"
      }
    ]
  }
}
```

**Expected**: Info card: "No coverage requirements identified."

---

### Exercise 11: Order-Select — CT with Alternative Suggestion
**Goal**: See how CRD suggests cheaper alternatives.

**Postman**: `POST http://localhost:5250/cds-services/crd-order-select`
**Body**:
```json
{
  "hookInstance": "a1234567-0000-0000-0000-000000000002",
  "fhirServer": "http://localhost:8082/fhir",
  "hook": "order-select",
  "context": {
    "userId": "Practitioner/123",
    "patientId": "51707",
    "selections": [
      {
        "resourceType": "ServiceRequest",
        "code": "74177",
        "display": "CT Abdomen/Pelvis with contrast"
      }
    ]
  }
}
```

**Expected**: Two cards — Warning for PA required + Info card suggesting ultrasound as cheaper alternative.

---

### Exercise 12: Order-Select — Specialty Medication (Ozempic)
**Goal**: Check coverage for a specialty drug order.

**Postman**: `POST http://localhost:5250/cds-services/crd-order-select`
**Body**:
```json
{
  "hookInstance": "a1234567-0000-0000-0000-000000000003",
  "fhirServer": "http://localhost:8082/fhir",
  "hook": "order-select",
  "context": {
    "userId": "Practitioner/123",
    "patientId": "51707",
    "selections": [
      {
        "resourceType": "MedicationRequest",
        "code": "1991302",
        "display": "Semaglutide (Ozempic)"
      }
    ]
  }
}
```

**Expected**: Warning for PA + alternative suggestion (Metformin, Jardiance).

---

### Exercise 13: Order-Select — Total Knee Replacement
**Goal**: Check a major surgical procedure.

**Postman**: `POST http://localhost:5250/cds-services/crd-order-select`
**Body**:
```json
{
  "hookInstance": "a1234567-0000-0000-0000-000000000004",
  "fhirServer": "http://localhost:8082/fhir",
  "hook": "order-select",
  "context": {
    "userId": "Practitioner/123",
    "patientId": "51707",
    "selections": [
      {
        "resourceType": "ServiceRequest",
        "code": "27447",
        "display": "Total Knee Replacement"
      }
    ]
  }
}
```

**Expected**: Warning for PA (requires 6 months conservative treatment) + alternative suggestion (arthroscopy or injection).

---

### Exercise 14: Order-Select — Cosmetic (Not Covered)
**Goal**: See how CRD handles non-covered services.

**Postman**: `POST http://localhost:5250/cds-services/crd-order-select`
**Body**:
```json
{
  "hookInstance": "a1234567-0000-0000-0000-000000000005",
  "fhirServer": "http://localhost:8082/fhir",
  "hook": "order-select",
  "context": {
    "userId": "Practitioner/123",
    "patientId": "51707",
    "selections": [
      {
        "resourceType": "ServiceRequest",
        "code": "15780",
        "display": "Dermabrasion (cosmetic)"
      }
    ]
  }
}
```

**Expected**: Critical card: "Not Covered: Dermabrasion (cosmetic)."

---

### Exercise 15: Order-Select — DME (CPAP)
**Goal**: Check DME coverage requirements.

**Postman**: `POST http://localhost:5250/cds-services/crd-order-select`
**Body**:
```json
{
  "hookInstance": "a1234567-0000-0000-0000-000000000006",
  "fhirServer": "http://localhost:8082/fhir",
  "hook": "order-select",
  "context": {
    "userId": "Practitioner/123",
    "patientId": "51707",
    "selections": [
      {
        "resourceType": "DeviceRequest",
        "code": "E0601",
        "display": "CPAP Device"
      }
    ]
  }
}
```

**Expected**: Warning card with required docs (sleep study, AHI score, compliance plan).

---

### Exercise 16: Order-Select — Multiple Items
**Goal**: Submit multiple order selections in one hook call.

**Postman**: `POST http://localhost:5250/cds-services/crd-order-select`
**Body**:
```json
{
  "hookInstance": "a1234567-0000-0000-0000-000000000007",
  "fhirServer": "http://localhost:8082/fhir",
  "hook": "order-select",
  "context": {
    "userId": "Practitioner/123",
    "patientId": "51707",
    "selections": [
      { "code": "70553", "display": "MRI Brain" },
      { "code": "71046", "display": "Chest X-Ray" },
      { "code": "1991302", "display": "Ozempic" }
    ]
  }
}
```

**Expected**: Multiple cards — PA warning for MRI, PA+alternative for Ozempic, no requirements for X-Ray.

---

### Exercise 17: Order-Sign Hook
**Goal**: Test the order-sign hook (final validation before signing).

**Postman**: `POST http://localhost:5250/cds-services/crd-order-sign`
**Body**:
```json
{
  "hookInstance": "b2345678-0000-0000-0000-000000000001",
  "fhirServer": "http://localhost:8082/fhir",
  "hook": "order-sign",
  "context": {
    "userId": "Practitioner/123",
    "patientId": "51707",
    "selections": [
      { "code": "27447", "display": "Total Knee Replacement" }
    ]
  }
}
```

**Expected**: Cards from order evaluation PLUS an additional "Review Required Before Signing" warning.

---

### Exercise 18: Appointment-Book Hook
**Goal**: Test the appointment scheduling hook.

**Postman**: `POST http://localhost:5250/cds-services/crd-appointment-book`
**Body**:
```json
{
  "hookInstance": "c3456789-0000-0000-0000-000000000001",
  "fhirServer": "http://localhost:8082/fhir",
  "hook": "appointment-book",
  "context": {
    "userId": "Practitioner/123",
    "patientId": "51707",
    "encounterId": "12345"
  }
}
```

**Expected**: Info card reminding to verify network status with link to Provider Directory.

---

### Exercise 19: Compare Cards Across Hooks
**Goal**: See how the same order produces different cards in order-select vs order-sign.

1. Submit MRI Brain to order-select (Exercise 9)
2. Submit same to order-sign (replace `crd-order-select` with `crd-order-sign`)
3. **Compare**: order-sign adds an extra "Review Required" card — it's more cautious at sign time.

---

## Part C: Integration & Real-World Scenarios (Exercises 20-25)

### Exercise 20: End-to-End CRD Workflow
**Goal**: Simulate the full CRD workflow as it happens in an EHR.

**Steps**:
1. Discovery: `GET http://localhost:5250/cds-services`
2. Clinician selects MRI Brain: `POST .../crd-order-select` with code 70553
3. CRD returns: PA required, documents needed, DTR link
4. Clinician also orders Chest X-Ray (no issues)
5. Clinician signs: `POST .../crd-order-sign` with both codes
6. System shows final review cards

---

### Exercise 21: Integrate with Phase 4 (Formulary Check)
**Goal**: Cross-reference CRD medication rules with the Formulary API.

1. CRD says Ozempic needs PA: `GET http://localhost:5250/api/Rules/check/1991302`
2. Check Formulary: `GET http://localhost:5240/api/formulary/CoverageCheck?drugName=Ozempic&planId=PLAN-PPO-2026`
3. Find alternatives: `GET http://localhost:5240/api/formulary/Drug/DRUG-008/alternatives`

**Both** CRD and Formulary agree: Ozempic requires PA + step therapy. Metformin is the first-line alternative.

---

### Exercise 22: Prior Auth Documentation Checklist
**Goal**: Build a documentation checklist from CRD output.

For Total Knee Replacement (27447):
1. Check CRD: `GET http://localhost:5250/api/Rules/check/27447`
2. Extract `requiredDocuments`:
   - [ ] X-ray evidence of degenerative changes
   - [ ] 6 months conservative treatment
   - [ ] BMI documentation
   - [ ] Physical therapy records

This checklist would be pre-populated in the DTR questionnaire (Phase 6).

---

### Exercise 23: CRD for a Complex Patient Visit
**Goal**: Simulate a complex visit with multiple orders.

**Scenario**: Patient with knee pain + diabetes + sleep apnea. Doctor orders:
- MRI Lumbar Spine (72148)
- Ozempic refill (1991302)
- CPAP device (E0601)

**Postman**: `POST http://localhost:5250/cds-services/crd-order-select`
**Body**:
```json
{
  "hookInstance": "complex-visit-001",
  "fhirServer": "http://localhost:8082/fhir",
  "hook": "order-select",
  "context": {
    "userId": "Practitioner/123",
    "patientId": "51707",
    "selections": [
      { "code": "72148", "display": "MRI Lumbar Spine" },
      { "code": "1991302", "display": "Ozempic" },
      { "code": "E0601", "display": "CPAP Device" }
    ]
  }
}
```

**Expected**: Multiple warning cards — ALL three require prior auth with different documentation.

---

### Exercise 24: Card Indicator Analysis
**Goal**: Categorize all rules by their card indicator level.

Execute for each code and categorize:
```
GET http://localhost:5250/api/Rules/check/70553    → warning (PA)
GET http://localhost:5250/api/Rules/check/71046    → info (covered)
GET http://localhost:5250/api/Rules/check/15780    → critical (not covered)
GET http://localhost:5250/api/Rules/check/43239    → info (docs only)
```

**Summary**:
- Info cards: Covered services, documentation-only items
- Warning cards: Prior auth required
- Critical cards: Not covered

---

### Exercise 25: CRD → DTR → PAS Workflow Preview
**Goal**: Understand how CRD connects to DTR and PAS (Phases 6).

1. CRD identifies PA need: `POST .../crd-order-select` with 27447
2. Card contains suggestion: "Launch DTR to collect documentation"
3. Card contains link: "Start Prior Auth (PAS)" pointing to Phase 6 API
4. **Next phase**: DTR will serve the Questionnaire, PAS will submit the PA request

**This is the Da Vinci Burden Reduction workflow**: CRD → DTR → PAS

---

## Optional: No-Postman Runner (Exercises 19-25)

If you want to execute the advanced Phase 5 exercises from PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File .\Phase5\Exercises\run_ex19_25.ps1
```

Notes:
- Keep Phase 5 CRD API running on `http://localhost:5250`.
- Exercise 21 also calls Phase 4 Formulary API on `http://localhost:5240` (the script skips that step if unavailable).

---

## ✅ Completion Checklist

### Part A: Discovery & Rules (8 exercises)
- [ ] Exercise 1: CDS Hooks Discovery
- [ ] Exercise 2: All coverage rules
- [ ] Exercise 3: Imaging rules
- [ ] Exercise 4: All rule types
- [ ] Exercise 5: Code check — MRI Brain
- [ ] Exercise 6: Code check — Chest X-Ray
- [ ] Exercise 7: Code check — unknown
- [ ] Exercise 8: Code check — not covered

### Part B: Hook Invocations (11 exercises)
- [ ] Exercise 9: MRI Brain order-select
- [ ] Exercise 10: Chest X-Ray (covered)
- [ ] Exercise 11: CT with alternatives
- [ ] Exercise 12: Ozempic (specialty med)
- [ ] Exercise 13: Total Knee Replacement
- [ ] Exercise 14: Cosmetic (not covered)
- [ ] Exercise 15: DME (CPAP)
- [ ] Exercise 16: Multiple items
- [ ] Exercise 17: Order-sign hook
- [ ] Exercise 18: Appointment-book hook
- [ ] Exercise 19: Compare hooks

### Part C: Integration (6 exercises)
- [ ] Exercise 20: End-to-end workflow
- [ ] Exercise 21: Formulary integration
- [ ] Exercise 22: Documentation checklist
- [ ] Exercise 23: Complex patient visit
- [ ] Exercise 24: Card indicator analysis
- [ ] Exercise 25: CRD → DTR → PAS preview
