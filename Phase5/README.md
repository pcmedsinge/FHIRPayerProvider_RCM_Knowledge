# Phase 5: Coverage Requirements Discovery (CRD) Service

## Overview

This phase implements a **CRD Service** based on the [Da Vinci CRD Implementation Guide](http://hl7.org/fhir/us/davinci-crd/). CRD enables real-time coverage requirements checking within the EHR workflow using the **CDS Hooks** standard. When a clinician orders a procedure, medication, or service, the payer's CRD service responds with coverage information, prior auth requirements, and documentation needs.

## Key Concepts

### CDS Hooks Architecture
```
┌──────────────┐    CDS Hook Request    ┌──────────────────┐
│     EHR      │ ────────────────────→  │   CRD Service    │
│  (Clinician) │                        │   (Payer)        │
│              │ ←────────────────────  │                  │
└──────────────┘    Cards Response      └──────────────────┘
```

### Supported Hooks
| Hook | Trigger | Purpose |
|------|---------|---------|
| `order-select` | Clinician picks an order | Early coverage check |
| `order-sign` | Clinician signs orders | Final validation before submission |
| `appointment-book` | Scheduling appointment | Network status check |

### CDS Cards Response Types
- **Info** (blue): General coverage information
- **Warning** (yellow): Prior auth required, documentation needed
- **Critical** (red): Service not covered

### Prefetch

When an EHR calls a CDS Hook it can include pre-fetched FHIR resources in the request body so the CRD service does not need to make additional queries back to the EHR.

**How it works:**
1. The CRD service advertises *prefetch templates* in the `/cds-services` discovery response — each template is a parameterised FHIR query (e.g. `Patient/{{context.patientId}}`).
2. Before sending the hook request the EHR resolves each template using the current patient/order context, fetches the matching FHIR resources, and embeds them under the `prefetch` key.
3. The CRD service reads directly from `prefetch` instead of performing round-trip calls to the EHR's FHIR server.

**Concrete example — `order-sign` hook request:**

```json
{
  "hookInstance": "a8f3b2c1-1234-5678-abcd-ef0123456789",
  "hook": "order-sign",
  "context": {
    "userId": "Practitioner/pract-001",
    "patientId": "51707",
    "encounterId": "enc-9901",
    "selections": ["ServiceRequest/sr-74177"],
    "draftOrders": {
      "resourceType": "Bundle",
      "entry": [
        {
          "resource": {
            "resourceType": "ServiceRequest",
            "id": "sr-74177",
            "status": "draft",
            "code": {
              "coding": [{ "system": "http://www.ama-assn.org/go/cpt", "code": "74177", "display": "CT Abdomen" }]
            },
            "subject": { "reference": "Patient/51707" }
          }
        }
      ]
    }
  },
  "prefetch": {
    "patient": {
      "resourceType": "Patient",
      "id": "51707",
      "name": [{ "family": "Schulist", "given": ["Ramon"] }],
      "birthDate": "1970-04-15"
    },
    "coverage": {
      "resourceType": "Coverage",
      "id": "cov-001",
      "status": "active",
      "subscriber": { "reference": "Patient/51707" },
      "payor": [{ "display": "Acme Health Plan" }]
    }
  }
}
```

> **`context` vs `prefetch`:** `context` carries lightweight identifiers and the draft order bundle (what the clinician just created). `prefetch` carries the actual FHIR resources the EHR already fetched on the CRD service's behalf — ready to use with no extra network call needed.

> **Note:** The Phase 5 `CRDService` advertises prefetch templates in `/cds-services` discovery but evaluates coverage from `context.selections` / `context.draftOrders`. Consuming `prefetch` resources (e.g. applying patient-specific plan rules from the `coverage` resource) would be a natural next enhancement.

## Architecture

Note: In this phase, both `CDS Hooks Endpoints` and `Admin Endpoints` are hosted on the **payer-side CRDService** (port `5250`). The EHR is the caller/client for CDS Hooks requests.

```
┌──────────────────────────────────┐
│        CRD Service               │  Port 5250
│        (CRDService)              │
├──────────────────────────────────┤
│  CDS Hooks Endpoints:            │
│    GET  /cds-services            │  Discovery
│    POST /cds-services/crd-*      │  Hook handlers
├──────────────────────────────────┤
│  Admin Endpoints:                │
│    GET /api/Rules                │  View all rules
│    GET /api/Rules/check/{code}   │  Quick code check
├──────────────────────────────────┤
│  Services:                       │
│    CoverageRulesService          │  13 rules engine
└──────────────────────────────────┘
```

## Running the API

```bash
cd Phase5/CRDService
dotnet run --launch-profile http
```

Swagger UI: http://localhost:5250/swagger

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/cds-services` | CDS Hooks Discovery |
| POST | `/cds-services/crd-order-select` | Order select hook handler |
| POST | `/cds-services/crd-order-sign` | Order sign hook handler |
| POST | `/cds-services/crd-appointment-book` | Appointment book handler |
| GET | `/api/Rules` | List all coverage rules |
| GET | `/api/Rules/type/{type}` | Rules by type (imaging/procedure/medication/dme) |
| GET | `/api/Rules/check/{code}` | Quick code coverage check |

## Coverage Rules (13 rules)

| Code | Type | Description | Prior Auth? |
|------|------|-------------|-------------|
| 70553 | Imaging | MRI Brain | Yes |
| 74177 | Imaging | CT Abdomen | Yes |
| 71046 | Imaging | Chest X-Ray | No |
| 72148 | Imaging | MRI Lumbar | Yes |
| 27447 | Procedure | Total Knee Replacement | Yes |
| 29881 | Procedure | Knee Arthroscopy | Yes |
| 43239 | Procedure | Upper GI Endoscopy | No (docs only) |
| 1991302 | Medication | Ozempic | Yes |
| 1657981 | Medication | Keytruda | Yes |
| 1876366 | Medication | Dupixent | Yes |
| E0601 | DME | CPAP Device | Yes |
| K0856 | DME | Power Wheelchair | Yes |
| 15780 | Procedure | Dermabrasion (cosmetic) | Not covered |

## Next Steps
→ [Phase 5 Hands-On Guide](./Exercises/HANDS_ON_GUIDE.md) for 25 exercises
→ Phase 6: DTR + PAS (Prior Authorization)
