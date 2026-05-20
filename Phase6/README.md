cleqr# Phase 6: Prior Authorization (PAS + DTR)

## Overview
This phase implements the **Da Vinci Prior Authorization Support (PAS)** and **Documentation Templates & Rules (DTR)** APIs. Together they automate the prior authorization workflow — from collecting clinical documentation via smart questionnaires to submitting and tracking PA requests.

## Architecture
```
┌─────────────────────────────────────────────────┐
│              PriorAuthAPI (Port 5260)            │
│                                                  │
│  ┌──────────────────┐  ┌──────────────────────┐ │
│  │  DTR Service      │  │  PAS Service          │ │
│  │  (Questionnaires) │  │  (Decision Engine)    │ │
│  │  - 5 templates    │  │  - Auto-approve       │ │
│  │  - Smart forms    │  │  - Conditional        │ │
│  │  - Auto-populate  │  │  - Pended for review  │ │
│  │  - Responses      │  │  - Denied (cosmetic)  │ │
│  └──────────────────┘  └──────────────────────┘ │
│                                                  │
│  Controllers:                                    │
│  - QuestionnaireController (DTR)                 │
│  - PriorAuthController (PAS)                     │
└─────────────────────────────────────────────────┘
```

## Key Concepts
| Concept | Description |
|---------|-------------|
| **PAS** | Prior Authorization Support — automated submission/tracking |
| **DTR** | Documentation Templates & Rules — smart questionnaires |
| **$submit** | FHIR operation to submit PA request (Claim) |
| **$inquiry** | FHIR operation to check PA status |
| **Questionnaire** | FHIR resource for collecting structured clinical data |
| **QuestionnaireResponse** | Completed answers to a questionnaire |

## Service Codes & Decision Logic
| Code | Procedure | Decision |
|------|-----------|----------|
| 71046 | Chest X-Ray | Auto-approve |
| 43239 | Upper GI Endoscopy | Auto-approve |
| 70553 | MRI Brain | Conditional (needs documentation) |
| 74177 | CT Abdomen | Conditional (needs documentation) |
| 72148 | MRI Lumbar Spine | Conditional |
| 27447 | Total Knee Replacement | Full surgical review |
| 29881 | Knee Arthroscopy | Full surgical review |
| 1991302 | Ozempic | Specialty pharmacy review |
| E0601 | CPAP Machine | DME with compliance check |
| 15780 | Dermabrasion | Denied (cosmetic) |

## API Endpoints

### DTR (Questionnaire) Endpoints
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/dtr/Questionnaire | List all questionnaire templates |
| GET | /api/dtr/Questionnaire/{id} | Get questionnaire by ID |
| GET | /api/dtr/Questionnaire/by-service/{code} | Get questionnaire for a service code |
| POST | /api/dtr/Questionnaire/response | Submit completed questionnaire |
| GET | /api/dtr/Questionnaire/response/{id} | Get response by ID |
| GET | /api/dtr/Questionnaire/responses/patient/{patientId} | Patient's responses |

### PAS (Prior Auth) Endpoints
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/pas/PriorAuth/submit | Submit PA request ($submit) |
| GET | /api/pas/PriorAuth/status/{authId} | Check PA status ($inquiry) |
| GET | /api/pas/PriorAuth/patient/{patientId} | Patient's PA requests |
| GET | /api/pas/PriorAuth | List all PA requests (admin) |
| PUT | /api/pas/PriorAuth/cancel/{authId} | Cancel PA request |
| PUT | /api/pas/PriorAuth/update/{authId}?status=X | Admin status update |

## Running the Service
```bash
cd Phase6/PriorAuthAPI
dotnet run --launch-profile http
# Swagger: http://localhost:5260/swagger
```

## Typical Workflow
1. **Discover questionnaire** — GET `/api/dtr/Questionnaire/by-service/70553`
2. **Fill out questionnaire** — POST `/api/dtr/Questionnaire/response`
3. **Submit PA request** — POST `/api/pas/PriorAuth/submit` (include questionnaireResponseId)
4. **Check status** — GET `/api/pas/PriorAuth/status/{authId}`
5. **Payer reviews** — PUT `/api/pas/PriorAuth/update/{authId}?status=approved`

---

## Workflow & Architecture Diagrams

### Functional Workflow (PA Journey)
This diagram shows the end-to-end PA workflow from clinician order entry through CRD guidance, DTR questionnaire collection, PAS submission, async review, and status notification back to EHR.

**Flow**: CRD (guidance) → DTR (documentation) → PAS (submission) → Async Review → Status → EHR

See: [`pa_workflow_functional.png`](pa_workflow_functional.png) | [`pa_workflow_functional.mmd`](pa_workflow_functional.mmd) (source)

### Architectural Overview
This diagram shows the technical components and integration points:
- **EHR** (order entry, task tracking, notification handling)
- **CRD Service** (Phase 5 coverage rules engine)
- **Prior Auth API** (Phase 6 DTR + PAS services)
- **Async Processing** (queue, workers, scheduler for long-running PA decisions)
- **Storage** (HAPI FHIR, PA database, questionnaire responses)
- **Payer Backend** (UM worklist, human reviewers, notification service)

See: [`pa_architecture_overview.png`](pa_architecture_overview.png) | [`pa_architecture_overview.mmd`](pa_architecture_overview.mmd) (source)
