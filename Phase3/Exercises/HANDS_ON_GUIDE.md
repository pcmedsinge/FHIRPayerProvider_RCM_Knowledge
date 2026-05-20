# Phase 3: Provider Directory API — Hands-On Guide (25 Exercises)

## Pre-requisites
- HAPI FHIR server running on `http://localhost:8082/fhir`
- Phase 3 API running: `cd Phase3/ProviderDirectoryAPI && dotnet run --launch-profile http`
- API available at `http://localhost:5230`
- Postman or curl ready

---

## Part A: FHIR Provider Resources — Direct HAPI Queries (Exercises 1-8)

### Exercise 1: Explore Practitioners in HAPI
**Goal**: See what practitioner data exists from Synthea.

**Postman**: `GET http://localhost:8082/fhir/Practitioner?_count=10`

**Expected**: Bundle with Practitioner resources containing name, gender, address, telecom.

**What to observe**:
- Each Practitioner typically has `name`, `gender`, `address`, `telecom`
- In this Synthea/HAPI dataset, `qualification` is often not populated on `Practitioner`
- Provider specialty is mainly represented in `PractitionerRole.specialty` (Exercise 5/6)
- Note the IDs — you'll use them in later exercises

---

### Exercise 2: Search Practitioners by Name
**Goal**: Find a specific practitioner by name.

**Postman**: `GET http://localhost:8082/fhir/Practitioner?name=Smith&_count=5`

**Try also**:
- `GET http://localhost:8082/fhir/Practitioner?name:contains=john&_count=5`
- `GET http://localhost:8082/fhir/Practitioner?family=Smith`

**What to observe**: FHIR name search is flexible — `name` matches given OR family names.

---

### Exercise 3: Explore Organizations
**Goal**: Understand organization types in payer data.

**Postman**: `GET http://localhost:8082/fhir/Organization?_count=10`

**What to observe**:
- Organizations represent hospitals, clinics, payer companies
- Look at `type` coding (e.g., `prov` for provider, `pay` for payer)
- Note `address`, `telecom` for contact info
- `partOf` may reference parent organizations

---

### Exercise 4: Explore Locations
**Goal**: Understand location resources from Synthea.

**Postman**: `GET http://localhost:8082/fhir/Location?_count=10`

**Try also**:
- `GET http://localhost:8082/fhir/Location?address-state=MA&_count=10`
- `GET http://localhost:8082/fhir/Location?address-city=Boston&_count=5`

**What to observe**:
- Locations have `address` with full street/city/state/zip
- `position` with lat/long (if available from Synthea)
- `managingOrganization` links to the org that runs this facility

---

### Exercise 5: PractitionerRole — The Key Linking Resource
**Goal**: Understand how PractitionerRole connects practitioners to organizations.

**Postman**: `GET http://localhost:8082/fhir/PractitionerRole?_count=10&_include=PractitionerRole:practitioner&_include=PractitionerRole:organization`

**What to observe**:
- PractitionerRole is the **bridge** between Practitioner, Organization, and Location
- `practitioner` reference points to a Practitioner
- `organization` reference points to an Organization
- `location` references where they practice
- `specialty` contains their clinical specialty codes
- `_include` brings the referenced resources into the same Bundle

---

### Exercise 6: Search PractitionerRoles by Specialty
**Goal**: Find all providers with a specific specialty.

**Postman**: `GET http://localhost:8082/fhir/PractitionerRole?specialty=http://nucc.org/provider-taxonomy|208D00000X&_count=10`

**Common NUCC Taxonomy Codes**:
| Code | Specialty |
|------|-----------|
| 208600000X | Surgery |
| 208D00000X | General Practice |
| 207R00000X | Internal Medicine |
| 208000000X | Pediatrics |
| 2084N0400X | Neurology |

**Try finding all specialties**: `GET http://localhost:8082/fhir/PractitionerRole?_summary=count`

---

### Exercise 7: Healthcare Services
**Goal**: Explore what healthcare services exist.

**Postman**: `GET http://localhost:8082/fhir/HealthcareService?_count=10`

**What to observe**:
- In some Synthea/HAPI datasets (including yours), `HealthcareService` may be empty (`total: 0`)
- If present, services describe what is offered at a location
- They are typically connected via `providedBy`, `location`, `serviceType`, and `specialty`
- If empty, continue using `PractitionerRole + Organization + Location` as your provider directory backbone

---

### Exercise 8: Resource Counts Summary
**Goal**: Get a count of all provider directory resources.

**Execute each in Postman**:
```
GET http://localhost:8082/fhir/Practitioner?_summary=count
GET http://localhost:8082/fhir/Organization?_summary=count
GET http://localhost:8082/fhir/Location?_summary=count
GET http://localhost:8082/fhir/PractitionerRole?_summary=count
GET http://localhost:8082/fhir/HealthcareService?_summary=count
```

**Record your counts** — you'll compare with API results.

---

## Part B: Provider Directory API Endpoints (Exercises 9-19)

### Exercise 9: Get a Practitioner via API
**Goal**: Use the Provider Directory API to fetch a practitioner.

First, get a practitioner ID from Exercise 1, then:

**Postman**: `GET http://localhost:5230/api/fhir/Practitioner/{id}`

Replace `{id}` with an actual ID from your HAPI server (e.g., the first practitioner ID from Exercise 1).

**Expected**: Same practitioner data, served through your API.

---

### Exercise 10: Search Practitioners via API
**Goal**: Search practitioners by name through your API.

**Postman**: `GET http://localhost:5230/api/fhir/Practitioner?name=Smith&_count=5`

**Compare**: Results should match Exercise 2's direct HAPI query.

---

### Exercise 11: Get Organization via API
**Goal**: Fetch an organization by ID.

**Postman**: `GET http://localhost:5230/api/fhir/Organization/{id}`

Replace `{id}` with an organization ID from Exercise 3.

---

### Exercise 12: Search Organizations via API
**Goal**: Search organizations by name and type.

**Postman requests**:
```
GET http://localhost:5230/api/fhir/Organization?name=Hospital&_count=10
GET http://localhost:5230/api/fhir/Organization?type=prov&_count=10
GET http://localhost:5230/api/fhir/Organization?address=Boston&_count=5
```

---

### Exercise 13: Get Location via API
**Goal**: Fetch a specific location.

**Postman**: `GET http://localhost:5230/api/fhir/Location/{id}`

---

### Exercise 14: Search Locations by Geography
**Goal**: Search locations by city and state.

**Postman requests**:
```
GET http://localhost:5230/api/fhir/Location?state=MA&_count=10
GET http://localhost:5230/api/fhir/Location?city=Boston&_count=5
GET http://localhost:5230/api/fhir/Location?city=Boston&state=MA&_count=5
```

---

### Exercise 15: Search PractitionerRoles via API
**Goal**: Find practitioner roles with includes.

**Postman requests**:
```
GET http://localhost:5230/api/fhir/PractitionerRole?_count=10
GET http://localhost:5230/api/fhir/PractitionerRole?practitioner={practitionerId}&_count=10
GET http://localhost:5230/api/fhir/PractitionerRole?specialty=208D00000X&_count=10
```

**What to observe**: The response includes referenced Practitioner, Organization, and Location resources thanks to `_include` in the service layer.

---

### Exercise 16: Search Healthcare Services via API
**Goal**: Find services offered by an organization.

**Postman**: `GET http://localhost:5230/api/fhir/HealthcareService?organization={orgId}&_count=10`

---

### Exercise 17: Get Practitioner Full Details
**Goal**: Use the detail endpoint to get everything about a practitioner.

**Postman**: `GET http://localhost:5230/api/fhir/Practitioner/{id}/details`

**What to observe**: Returns a collection Bundle containing:
- The Practitioner resource
- All PractitionerRole resources
- Included Organization and Location resources

This is the **"provider profile page"** — everything about one doctor in a single call.

---

### Exercise 18: Find a Doctor — Combined Search
**Goal**: Use the Find-a-Doctor endpoint for a real-world search experience.

**Postman requests**:
```
GET http://localhost:5230/api/FindDoctor?_count=10
GET http://localhost:5230/api/FindDoctor?name=Smith&_count=10
GET http://localhost:5230/api/FindDoctor?state=MA&_count=10
GET http://localhost:5230/api/FindDoctor?city=Boston&state=MA&_count=10
GET http://localhost:5230/api/FindDoctor?specialty=208D00000X&city=Boston&_count=10
```

**What to observe**: This endpoint combines PractitionerRole search with client-side filtering for name/location — simulating a real provider directory search experience.

---

### Exercise 19: Verify Data Consistency
**Goal**: Confirm that API results match direct HAPI queries.

1. Pick a practitioner ID from HAPI: `GET http://localhost:8082/fhir/Practitioner/{id}`
2. Get same via API: `GET http://localhost:5230/api/fhir/Practitioner/{id}`
3. Compare the JSON — they should be identical

4. Count practitioners from HAPI: `GET http://localhost:8082/fhir/Practitioner?_summary=count`
5. Search all via API: `GET http://localhost:5230/api/fhir/Practitioner?_count=200`
6. Compare the totals

---

## Part C: Advanced Scenarios & Error Handling (Exercises 20-25)

### Exercise 20: Handle Non-Existent Resources
**Goal**: Test error handling for missing resources.

**Postman**:
```
GET http://localhost:5230/api/fhir/Practitioner/99999999
GET http://localhost:5230/api/fhir/Organization/99999999
GET http://localhost:5230/api/fhir/Location/99999999
```

**Expected**: 404 responses with meaningful error messages.

---

### Exercise 21: Empty Search Results
**Goal**: Test behavior when search returns no results.

**Postman**:
```
GET http://localhost:5230/api/fhir/Practitioner?name=XYZNONEXISTENT
GET http://localhost:5230/api/fhir/Location?city=Atlantis
GET http://localhost:5230/api/FindDoctor?name=XYZNONEXISTENT&city=Atlantis
```

**Expected**: Empty Bundle with `total: 0` — NOT a 404.

---

### Exercise 22: Pagination with _count
**Goal**: Test pagination control.

**Postman**:
```
GET http://localhost:5230/api/fhir/Practitioner?_count=2
GET http://localhost:5230/api/fhir/Practitioner?_count=5
GET http://localhost:5230/api/fhir/Practitioner?_count=50
```

**What to observe**: The Bundle's `link` array may contain `next` URLs for pagination.

---

### Exercise 23: Cross-Resource Analysis
**Goal**: Trace the relationship chain from Practitioner through PractitionerRole to Organization and Location.

**Steps**:
1. Get practitioner details: `GET http://localhost:5230/api/fhir/Practitioner/{id}/details`
2. From the response, identify the PractitionerRole entries
3. Note the `organization` and `location` references
4. Fetch each: `GET http://localhost:5230/api/fhir/Organization/{orgId}`
5. Fetch each: `GET http://localhost:5230/api/fhir/Location/{locId}`

**This mirrors the Plan-Net relationship model**: Practitioner → PractitionerRole → Organization + Location

---

### Exercise 24: Build a Provider Card
**Goal**: Using API responses, assemble all data needed for a provider directory card.

A typical provider card shows:
- Doctor's name and photo
- Specialty
- Office address and phone
- Affiliated hospital/clinic
- Accepting new patients status

**Steps**:
1. `GET http://localhost:5230/api/fhir/Practitioner/{id}/details`
2. From the Bundle, extract:
   - `Practitioner.name` → Doctor's name
   - `PractitionerRole.specialty` → Specialty
   - `Location.address` → Office address
   - `Location.telecom` → Phone number
   - `Organization.name` → Hospital/clinic name
   - `PractitionerRole.availableTime` → Hours (if available)

---

### Exercise 25: Compare Direct HAPI vs API for All Resource Types
**Goal**: End-to-end validation of all provider directory endpoints.

**Execute this comparison matrix**:

| Resource | HAPI Direct | API Endpoint | Match? |
|----------|-------------|--------------|--------|
| Practitioner search | `http://localhost:8082/fhir/Practitioner?_count=5` | `http://localhost:5230/api/fhir/Practitioner?_count=5` | ☐ |
| Organization search | `http://localhost:8082/fhir/Organization?_count=5` | `http://localhost:5230/api/fhir/Organization?_count=5` | ☐ |
| Location search | `http://localhost:8082/fhir/Location?_count=5` | `http://localhost:5230/api/fhir/Location?_count=5` | ☐ |
| PractitionerRole | `http://localhost:8082/fhir/PractitionerRole?_count=5` | `http://localhost:5230/api/fhir/PractitionerRole?_count=5` | ☐ |

---

## ✅ Completion Checklist

### Part A: Direct HAPI Queries (8 exercises)
- [ ] Exercise 1: Explore Practitioners
- [ ] Exercise 2: Search by Name
- [ ] Exercise 3: Explore Organizations
- [ ] Exercise 4: Explore Locations
- [ ] Exercise 5: PractitionerRole linking
- [ ] Exercise 6: Specialty search
- [ ] Exercise 7: Healthcare Services
- [ ] Exercise 8: Resource counts

### Part B: API Endpoints (11 exercises)
- [ ] Exercise 9: Get Practitioner via API
- [ ] Exercise 10: Search Practitioners
- [ ] Exercise 11: Get Organization
- [ ] Exercise 12: Search Organizations
- [ ] Exercise 13: Get Location
- [ ] Exercise 14: Search Locations by geography
- [ ] Exercise 15: PractitionerRoles via API
- [ ] Exercise 16: Healthcare Services via API
- [ ] Exercise 17: Practitioner full details
- [ ] Exercise 18: Find a Doctor
- [ ] Exercise 19: Data consistency check

### Part C: Advanced Scenarios (6 exercises)
- [ ] Exercise 20: Error handling
- [ ] Exercise 21: Empty results
- [ ] Exercise 22: Pagination
- [ ] Exercise 23: Cross-resource tracing
- [ ] Exercise 24: Provider card assembly
- [ ] Exercise 25: Full comparison matrix
