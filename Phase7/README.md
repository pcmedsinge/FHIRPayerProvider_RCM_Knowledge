# Phase 7: Payer-to-Payer Data Exchange (PDex)

## Overview
This phase implements the **Da Vinci Payer Data Exchange (PDex)** API. It enables payer-to-payer data transfer when a member switches health plans, ensuring continuity of care through consent-driven data sharing.

## Architecture
```
┌──────────────────────────────────────────────────────┐
│                PDexAPI (Port 5270)                     │
│                                                        │
│  ┌─────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │  Consent     │  │ Member Match │  │ Data Exchange│ │
│  │  Service     │  │ Service      │  │ Service      │ │
│  │  - Create    │  │ - $member-   │  │ - Initiate   │ │
│  │  - Activate  │  │   match      │  │ - Execute    │ │
│  │  - Revoke    │  │ - ID match   │  │ - Provenance │ │
│  │  - Validate  │  │ - Name match │  │ - HAPI pull  │ │
│  └─────────────┘  └──────────────┘  └──────────────┘ │
│                                                        │
│             ┌──────────────────────┐                   │
│             │   HAPI FHIR (8082)   │                   │
│             │ (Simulates old payer │                   │
│             │  data source)        │                   │
│             └──────────────────────┘                   │
└──────────────────────────────────────────────────────┘
```

## Key Concepts
| Concept | Description |
|---------|-------------|
| **PDex** | Payer Data Exchange — CMS mandate for data sharing |
| **$member-match** | FHIR operation to identify a patient across payers |
| **Consent** | Patient authorization for data transfer |
| **Provenance** | Tracking data origin and transfer chain |
| **P2P Exchange** | Payer-to-payer bidirectional data sharing |
| **UPI** | Unique Patient Identifier across systems |

## Simulated Payers
| Payer ID | Name | Endpoint |
|----------|------|----------|
| PAYER-ALPHA | Alpha Health Plan | localhost:5270 |
| PAYER-BETA | Beta Insurance Co | localhost:5271 |
| PAYER-GAMMA | Gamma Medicare Advantage | localhost:5272 |

## Known Members
| Patient ID | Name | Payer | EOBs | Encounters | Meds |
|-----------|------|-------|------|------------|------|
| 51707 | Ramon Schulist | PAYER-ALPHA | 85 | 42 | 15 |
| 52458 | Rasheeda Heaney | PAYER-ALPHA | 120 | 55 | 22 |
| 65520 | Karena O'Keefe | PAYER-ALPHA | 45 | 28 | 10 |
| 55001 | Margaret Johnson | PAYER-BETA | 200 | 95 | 35 |
| 55002 | Robert Williams | PAYER-GAMMA | 30 | 15 | 8 |

## API Endpoints

### Consent Endpoints
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/pdex/Consent | Create consent |
| GET | /api/pdex/Consent/{id} | Get consent |
| PUT | /api/pdex/Consent/{id}/activate | Activate consent |
| PUT | /api/pdex/Consent/{id}/revoke | Revoke consent |
| GET | /api/pdex/Consent/patient/{patientId} | Patient's consents |
| GET | /api/pdex/Consent | List all consents |

### Member Match Endpoints
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/pdex/MemberMatch | $member-match operation |
| GET | /api/pdex/MemberMatch/members | List known members |
| GET | /api/pdex/MemberMatch/members/{id} | Get member summary |

### Exchange Endpoints
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/pdex/Exchange | Initiate exchange |
| POST | /api/pdex/Exchange/{jobId}/execute | Execute exchange |
| GET | /api/pdex/Exchange/{jobId} | Get job status |
| GET | /api/pdex/Exchange | List all jobs |
| GET | /api/pdex/Exchange/patient/{patientId} | Patient's jobs |
| PUT | /api/pdex/Exchange/{jobId}/cancel | Cancel job |
| GET | /api/pdex/Exchange/{jobId}/provenance | Get provenance |
| GET | /api/pdex/Exchange/{jobId}/resources | Get exchanged resources |

## Running the Service
```bash
cd Phase7/PDexAPI
dotnet run --launch-profile http
# Swagger: http://localhost:5270/swagger
```

## Typical Workflow
1. **$member-match** — Identify patient at old payer
2. **Create consent** — Patient authorizes data transfer
3. **Activate consent** — Patient signs consent
4. **Initiate exchange** — Create exchange job with consent
5. **Execute exchange** — Pull data from old payer (HAPI FHIR)
6. **Review provenance** — Verify data origin and transfer chain
