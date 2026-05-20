# Phase 3: Provider Directory API (DaVinci PDEX Plan-Net)

## Overview

This phase implements a **Provider Directory API** based on the [Da Vinci PDEX Plan-Net Implementation Guide](http://hl7.org/fhir/us/davinci-pdex-plan-net/). CMS requires payers to make provider directory information available via FHIR APIs so members can search for in-network providers.

## Key Concepts

### Da Vinci PDEX Plan-Net IG
The Plan-Net IG defines how payer provider directories should be exposed via FHIR. It standardizes:
- **Practitioner** — Individual healthcare providers (doctors, nurses, therapists)
- **PractitionerRole** — Links a practitioner to an organization + specialty + location
- **Organization** — Hospitals, clinics, insurance companies, networks
- **Location** — Physical addresses where services are delivered
- **HealthcareService** — Types of services offered at locations
- **Endpoint** — Technical connectivity details (for electronic exchange)

### Resource Relationships
```
Practitioner ──────> PractitionerRole <────── Organization
                          │
                          ├──> Location
                          └──> HealthcareService
```

### Real-World Use Cases
1. **Find a Doctor**: Member searches for a dermatologist within 10 miles
2. **Verify Network Status**: Check if Dr. Smith is in-network for Blue Cross PPO
3. **Facility Lookup**: Find the nearest hospital that accepts my insurance
4. **Specialty Search**: Find all cardiologists affiliated with Mass General

## Architecture

```
┌────────────────────────────────┐
│     Provider Directory API     │  Port 5230
│    (ProviderDirectoryAPI)      │
├────────────────────────────────┤
│  Controllers:                  │
│    PractitionerController      │
│    OrganizationController      │
│    LocationController          │
│    PractitionerRoleController  │
│    HealthcareServiceController │
│    FindDoctorController        │
├────────────────────────────────┤
│  Services:                     │
│    IProviderService            │
│    ProviderService             │
├────────────────────────────────┤
│      HAPI FHIR Server         │  Port 8082
└────────────────────────────────┘
```

## Running the API

```bash
cd Phase3/ProviderDirectoryAPI
dotnet run --launch-profile http
```

Swagger UI: http://localhost:5230/swagger

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/fhir/Practitioner/{id}` | Get practitioner by ID |
| GET | `/api/fhir/Practitioner?name=xxx` | Search practitioners |
| GET | `/api/fhir/Practitioner/{id}/details` | Full detail with roles/locations |
| GET | `/api/fhir/Organization/{id}` | Get organization by ID |
| GET | `/api/fhir/Organization?name=xxx&type=xxx` | Search organizations |
| GET | `/api/fhir/Location/{id}` | Get location by ID |
| GET | `/api/fhir/Location?city=xxx&state=xxx` | Search locations |
| GET | `/api/fhir/PractitionerRole?specialty=xxx` | Search practitioner roles |
| GET | `/api/fhir/HealthcareService?organization=xxx` | Search healthcare services |
| GET | `/api/FindDoctor?name=xxx&specialty=xxx&city=xxx` | Combined find-a-doctor |

## Key Files

| File | Purpose |
|------|---------|
| `Program.cs` | App configuration and DI |
| `Services/IProviderService.cs` | Service interface |
| `Services/ProviderService.cs` | FHIR client operations |
| `Controllers/*.cs` | REST API endpoints |
| `Middleware/FhirErrorMiddleware.cs` | Error handling |

## Next Steps
→ [Phase 3 Hands-On Guide](./Exercises/HANDS_ON_GUIDE.md) for 25 exercises
→ Phase 4: Formulary API
