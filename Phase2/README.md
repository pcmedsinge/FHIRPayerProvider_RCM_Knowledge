# Phase 2: Member Access API (CARIN Blue Button)

**Duration**: 3-4 days (2 hours/day)  
**Goal**: Build a .NET 8 Member Access API that exposes patient claims, coverage, and health data per CMS mandate

---

## 🎯 What We're Building

A **Member Access API** — the CMS-mandated API that every US health plan must provide. Members (patients) log into their insurance portal and can view:
- Their demographics
- Insurance coverage details
- Claims history (professional, institutional, pharmacy)
- Health data (conditions, medications, encounters)

### Architecture

```
Postman (Member)  →  .NET 8 Web API (localhost:5220)  →  HAPI FHIR (localhost:8082)
                     ├── Auth: JWT token with patientId
                     ├── Access Control: Member sees ONLY their data
                     ├── CARIN BB: Responses follow Blue Button profiles
                     └── FHIR: Firely SDK (Hl7.Fhir.R4)
```

---

## 📚 Key Concepts

### CARIN Blue Button Implementation Guide

The [CARIN Consumer Directed Payer Data Exchange](http://hl7.org/fhir/us/carin-bb/) (CARIN BB) IG standardizes how health plans expose claims data to members.

**Core Profiles:**

| Profile | Base Resource | Purpose |
|---------|--------------|---------|
| C4BB Patient | Patient | Member demographics with MemberID |
| C4BB Coverage | Coverage | Insurance plan, subscriber, period |
| C4BB EOB Professional | ExplanationOfBenefit | Doctor/outpatient claims (CMS 1500) |
| C4BB EOB Institutional | ExplanationOfBenefit | Hospital claims (UB-04) |
| C4BB EOB Pharmacy | ExplanationOfBenefit | Prescription drug claims (NCPDP) |
| C4BB EOB Oral | ExplanationOfBenefit | Dental claims |

### ExplanationOfBenefit (EOB) — The Core Resource

EOB is the **most important** resource in payer FHIR. It represents a processed claim:

```
ExplanationOfBenefit
├── patient          → Who is the member
├── type             → professional | institutional | pharmacy
├── provider         → Who provided the service
├── facility         → Where service was provided
├── billablePeriod   → Service date range
├── created          → When claim was processed
├── diagnosis[]      → ICD-10 codes
├── careTeam[]       → Providers involved
├── item[]           → Line items (individual services)
│   ├── productOrService  → CPT/HCPCS/NDC code
│   ├── servicedPeriod    → When service happened
│   └── adjudication[]    → Financial breakdown per line
├── total[]          → Claim totals
│   ├── submitted    → Amount billed
│   ├── eligible     → Amount allowed
│   └── benefit      → Amount paid by insurer
├── insurance        → Coverage reference
└── payment          → Payment details
```

### Adjudication — The Financial Story

Every claim line item has adjudication showing how the payer processed the charge:

| Code | Meaning | Example |
|------|---------|---------|
| `submitted` | Amount provider billed | $500 |
| `eligible` | Amount allowed per contract | $350 |
| `benefit` | Amount insurer pays | $280 |
| `deductible` | Applied to deductible | $50 |
| `copay` | Member copay | $20 |
| `coinsurance` | Member coinsurance | $0 |

### Coverage Resource

Represents a member's insurance enrollment:

```
Coverage
├── status          → active | cancelled
├── type            → Plan type (HMO, PPO, etc.)
├── subscriber      → Primary policyholder
├── beneficiary     → Who is covered (the patient)
├── period          → Coverage start/end dates
├── payor           → Insurance company
└── class[]         → Plan, group, subgroup details
```

### SMART on FHIR Authorization

The standard for securing FHIR APIs in healthcare:

```
① Member opens app / portal
② App redirects to authorization server
③ Member logs in, grants consent
④ App receives access token with scopes:
   - patient/Patient.read
   - patient/ExplanationOfBenefit.read
   - patient/Coverage.read
⑤ App calls FHIR API with token
⑥ API validates token, scopes patient data to that member ONLY
```

For our exercises, we'll **simulate** this with JWT tokens containing the patientId.

---

## 🗂️ Data Available in HAPI FHIR (from Phase 1)

| Resource | Count | Notes |
|----------|-------|-------|
| Patient | 27 | Synthetic members |
| ExplanationOfBenefit | 2,170 | Claims (professional, institutional, pharmacy) |
| Encounter | 1,120 | Medical visits |
| Condition | 915 | Diagnoses |
| Procedure | 754 | Medical procedures |
| MedicationRequest | 1,050 | Prescriptions |
| Practitioner | 110 | Providers |
| Organization | 110 | Healthcare organizations |
| Location | 111 | Facilities |
| Claim | 2,170 | Original claims |
| Coverage | 0 | Contained inside EOBs (we'll create standalone) |

**EOB Claim Types:**
- Professional: ~251+ (doctor visits, outpatient)
- Pharmacy: ~242+ (prescriptions)
- Institutional: ~7+ (hospital)

---

## 📋 Exercise Plan

### Part A: Explore CARIN BB Data (Exercises 1–8)
Postman/Browser only — understand the data before coding

### Part B: Build .NET 8 Member Access API (Exercises 9–19)
Hands-on coding with Firely SDK + Postman testing

### Part C: Real-World Scenarios (Exercises 20–25)
End-to-end flows, security testing, compliance

See [Exercises/HANDS_ON_GUIDE.md](./Exercises/HANDS_ON_GUIDE.md) for detailed step-by-step exercises.

---

## 🏗️ Project Structure

```
Phase2/
├── README.md                          # This file
├── Exercises/
│   └── HANDS_ON_GUIDE.md             # 25 hands-on exercises
└── MemberAccessAPI/                   # .NET 8 Web API (built during exercises)
    ├── MemberAccessAPI.csproj
    ├── Program.cs
    ├── appsettings.json
    ├── Controllers/
    │   ├── PatientController.cs
    │   ├── CoverageController.cs
    │   └── EOBController.cs
    ├── Services/
    │   ├── IFhirService.cs
    │   └── FhirService.cs
    ├── Auth/
    │   └── MemberAuthHandler.cs
    └── Models/
        └── AuthModels.cs
```

---

## ✅ Phase 2 Deliverables

- [ ] Understand CARIN BB EOB profiles (4 types)
- [ ] Understand Coverage and Patient profiles
- [ ] Working .NET 8 API with Patient, Coverage, EOB endpoints
- [ ] JWT-based member authentication
- [ ] Patient-scoped access control (member sees only own data)
- [ ] Search by date range, claim type
- [ ] _include for related resources
- [ ] Proper FHIR error responses (OperationOutcome)
- [ ] Postman collection testing all endpoints

---

## 🚀 Getting Started

1. Ensure HAPI FHIR is running: http://localhost:8082/fhir/metadata
2. Open [Exercises/HANDS_ON_GUIDE.md](./Exercises/HANDS_ON_GUIDE.md)
3. Start with Exercise 1
