# Phase 4: Drug Formulary API (DaVinci Drug Formulary)

## Overview

This phase implements a **Drug Formulary API** based on the [Da Vinci Drug Formulary IG](http://hl7.org/fhir/us/Davinci-drug-formulary/). CMS requires payers to publish their drug formularies so members can check medication coverage, cost-sharing, and restrictions before filling prescriptions.

## Key Concepts

### Drug Formulary Structure
A formulary is a list of drugs covered by an insurance plan, organized into:
- **Drug Tiers**: Cost levels (generic → preferred brand → non-preferred → specialty)
- **Cost-Sharing**: Copay, coinsurance percentages
- **Restrictions**: Prior authorization, step therapy, quantity limits
- **Alternatives**: Lower-cost options in the same therapeutic class

### FHIR Resources
| DaVinci Profile | FHIR Resource | Purpose |
|----------------|---------------|---------|
| PayerInsurancePlan | InsurancePlan | Plan details + tier structure |
| FormularyItem | Basic | Links drug to plan + tier + restrictions |
| FormularyDrug | MedicationKnowledge | Drug details (name, codes, dosage) |

### Real-World Use Cases
1. **"Is my drug covered?"** — Member checks if their medication is on the formulary
2. **"What will it cost?"** — Check drug tier and copay/coinsurance
3. **"Do I need prior auth?"** — Check restrictions before filling
4. **"Is there a cheaper option?"** — Find generic/lower-tier alternatives

## Architecture

```
┌─────────────────────────────────┐
│       Drug Formulary API        │  Port 5240
│        (FormularyAPI)           │
├─────────────────────────────────┤
│  Controllers:                   │
│    PlanController               │  Insurance plans + tiers
│    DrugController               │  Drug search + alternatives
│    CoverageCheckController      │  Coverage determination
│    MedicationRequestController  │  Patient prescriptions (FHIR)
├─────────────────────────────────┤
│  Services:                      │
│    FormularyService (in-memory) │  Formulary data + business logic
│    FhirFormularyService         │  HAPI FHIR MedicationRequests
├─────────────────────────────────┤
│      HAPI FHIR Server          │  Port 8082
└─────────────────────────────────┘
```

## Running the API

```bash
cd Phase4/FormularyAPI
dotnet run --launch-profile http
```

Swagger UI: http://localhost:5240/swagger

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/formulary/Plan` | List all insurance plans |
| GET | `/api/formulary/Plan/{planId}` | Get plan with tier structure |
| GET | `/api/formulary/Plan/{planId}/tiers` | Get plan's drug tiers |
| GET | `/api/formulary/Drug?name=xxx&tier=xxx&planId=xxx` | Search formulary |
| GET | `/api/formulary/Drug/{drugId}` | Get drug details |
| GET | `/api/formulary/Drug/{drugId}/alternatives` | Find cheaper alternatives |
| POST | `/api/formulary/CoverageCheck` | Check drug coverage |
| GET | `/api/formulary/CoverageCheck?drugName=xxx` | Quick coverage check |
| GET | `/api/fhir/MedicationRequest?patient={id}` | Patient prescriptions |

## Sample Data
- 2 insurance plans (PPO Gold, HMO Silver)
- 22 drugs across 8 therapeutic classes
- 4 drug tiers with different cost-sharing
- Drugs with prior auth, step therapy, quantity limits

## Next Steps
→ [Phase 4 Hands-On Guide](./Exercises/HANDS_ON_GUIDE.md) for 25 exercises
→ Phase 5: Coverage Requirements Discovery (CRD)
