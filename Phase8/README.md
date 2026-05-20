# Phase 8: Bulk Data Export (BCDA)

## Overview
This phase implements the **FHIR Bulk Data Access** (Flat FHIR) specification. It enables large-scale data export for population health analytics, quality reporting, and data migration — generating NDJSON files from FHIR resources.

## Architecture
```
┌───────────────────────────────────────────────────────┐
│               BulkDataAPI (Port 5280)                  │
│                                                        │
│  ┌──────────────────┐  ┌───────────────────────────┐  │
│  │  Group Service    │  │  Bulk Export Service       │  │
│  │  - 4 preset groups│  │  - System-level $export    │  │
│  │  - Custom groups  │  │  - Patient-level $export   │  │
│  │  - Member mgmt    │  │  - Group-level $export     │  │
│  └──────────────────┘  │  - NDJSON generation        │  │
│                         │  - Analytics engine         │  │
│                         └───────────────────────────┘  │
│                                                        │
│             ┌──────────────────────┐                   │
│             │   HAPI FHIR (8082)   │                   │
│             │ (Data source for     │                   │
│             │  bulk export)        │                   │
│             └──────────────────────┘                   │
└───────────────────────────────────────────────────────┘
```

## Key Concepts
| Concept | Description |
|---------|-------------|
| **$export** | Async FHIR operation for bulk data extraction |
| **NDJSON** | Newline Delimited JSON — one resource per line |
| **System Export** | Export all resources of specified types |
| **Patient Export** | Export resources for specific patients |
| **Group Export** | Export resources for a defined patient group |
| **Content-Location** | HTTP header pointing to job status endpoint |

## Pre-configured Groups
| Group ID | Name | Members | Use Case |
|----------|------|---------|----------|
| GRP-DIABETES | Diabetes Care Cohort | 51707, 52458, 65520 | Quality reporting |
| GRP-CARDIAC | Cardiac Risk Group | 51707, 52458 | Risk analysis |
| GRP-HEDIS | HEDIS Reporting Population | 51707, 52458, 65520 | HEDIS measures |
| GRP-HIGHRISK | High-Risk Patients | 52458 | Care management |

## API Endpoints

### Export Endpoints
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/bulk/Export/$export | System-level export |
| POST | /api/bulk/Export/Patient/$export | Patient-level export |
| POST | /api/bulk/Export/Group/{id}/$export | Group-level export |
| POST | /api/bulk/Export/{jobId}/execute | Execute export |
| GET | /api/bulk/Export/{jobId}/status | Check job status |
| GET | /api/bulk/Export/{jobId}/download/{type} | Download NDJSON |
| GET | /api/bulk/Export/{jobId}/analytics | Get data analytics |
| DELETE | /api/bulk/Export/{jobId} | Delete export |
| PUT | /api/bulk/Export/{jobId}/cancel | Cancel export |
| GET | /api/bulk/Export | List all jobs |

### Group Endpoints
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/bulk/Group | List all groups |
| GET | /api/bulk/Group/{id} | Get group details |
| POST | /api/bulk/Group | Create group |
| PUT | /api/bulk/Group/{id}/members/add | Add members |
| PUT | /api/bulk/Group/{id}/members/remove | Remove members |
| DELETE | /api/bulk/Group/{id} | Delete group |

## Running the Service
```bash
cd Phase8/BulkDataAPI
dotnet run --launch-profile http
# Swagger: http://localhost:5280/swagger
```

## Export Flow
1. **Initiate** — POST $export → 202 Accepted + Content-Location
2. **Execute** — POST /execute → Pulls from HAPI FHIR
3. **Poll status** — GET /status → 202 (in-progress) or 200 (complete with manifest)
4. **Download** — GET /download/{type} → NDJSON content
5. **Analyze** — GET /analytics → Data insights
6. **Cleanup** — DELETE job when done
