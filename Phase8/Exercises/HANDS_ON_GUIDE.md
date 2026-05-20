# Phase 8: Bulk Data Export (BCDA) — Hands-On Guide

## Prerequisites
- Phase 8 BulkDataAPI running on port 5280
- HAPI FHIR server running on port 8082
- Postman or similar REST client

```bash
cd Phase8/BulkDataAPI
dotnet run --launch-profile http
```

---

## Part A: Group Management (Exercises 1-6)

### Exercise 1: List Pre-configured Groups
View all patient groups available for group-level export.

**Request:**
```
GET http://localhost:5280/api/bulk/Group
```

**Expected:** 4 groups — Diabetes Care Cohort, Cardiac Risk, HEDIS Reporting, High-Risk Patients.

---

### Exercise 2: Get Diabetes Group Details
View members and details of the Diabetes Care Cohort.

**Request:**
```
GET http://localhost:5280/api/bulk/Group/GRP-DIABETES
```

**Expected:** Group with 3 patient IDs (51707, 52458, 65520) and description.

---

### Exercise 3: Create a Custom Group
Create a new group for quality reporting.

**Request:**
```
POST http://localhost:5280/api/bulk/Group
Content-Type: application/json

{
  "name": "Preventive Care Cohort",
  "description": "Patients due for preventive screening in 2025",
  "patientIds": ["51707", "65520"]
}
```

**Expected:** New group with generated groupId.

---

### Exercise 4: Add Members to Group
Add a patient to an existing group.

**Request:**
```
PUT http://localhost:5280/api/bulk/Group/GRP-CARDIAC/members/add
Content-Type: application/json

{
  "patientIds": ["65520"]
}
```

**Expected:** Group now has 3 members (51707, 52458, 65520).

---

### Exercise 5: Remove Members from Group
Remove a patient from a group.

**Request:**
```
PUT http://localhost:5280/api/bulk/Group/GRP-HEDIS/members/remove
Content-Type: application/json

{
  "patientIds": ["65520"]
}
```

**Expected:** Group now has 2 members instead of 3.

---

### Exercise 6: Validation — Create Group with Missing Fields
Test error handling.

**Request:**
```
POST http://localhost:5280/api/bulk/Group
Content-Type: application/json

{
  "name": "Empty Group"
}
```

**Expected:** 400 Bad Request — "patientIds is required and must not be empty".

---

## Part B: System-Level Export (Exercises 7-13)

### Exercise 7: Initiate System-Level Export
Start a full system export with default resource types.

**Request:**
```
POST http://localhost:5280/api/bulk/Export/$export
Content-Type: application/json

{}
```

**Expected:** 202 Accepted with jobId and Content-Location header.

**Save the jobId** — you'll need it for exercises 8-13.

---

### Exercise 8: Check Export Status (Before Execution)
Poll the job status before execution.

**Request:**
```
GET http://localhost:5280/api/bulk/Export/{jobId}/status
```

**Expected:** 202 with status "queued", 0% progress. Note the Retry-After header.

---

### Exercise 9: Execute System Export
Run the export — pulls all data from HAPI FHIR.

**Request:**
```
POST http://localhost:5280/api/bulk/Export/{jobId}/execute
```

**Expected:** Completed job with output manifest listing NDJSON files for each resource type, counts, and sizes.

**What to observe:**
- How many resource types have data?
- Total resources exported
- Time duration

---

### Exercise 10: Check Completed Export Status (Manifest)
Get the bulk data manifest (per FHIR Bulk Data spec).

**Request:**
```
GET http://localhost:5280/api/bulk/Export/{jobId}/status
```

**Expected:** 200 OK with manifest containing:
- `transactionTime` — server time at export start
- `output` — array of NDJSON file URLs with types and counts
- `summary` — total resources, files, and duration

---

### Exercise 11: Download Patient NDJSON
Download the actual Patient NDJSON file.

**Request:**
```
GET http://localhost:5280/api/bulk/Export/{jobId}/download/Patient
```

**Expected:** Content-Type `application/fhir+ndjson`, one JSON Patient resource per line.

**What to observe:** Each line is a complete FHIR Patient JSON object. This is the standard format for bulk data transfer.

---

### Exercise 12: Download ExplanationOfBenefit NDJSON
Download claims data.

**Request:**
```
GET http://localhost:5280/api/bulk/Export/{jobId}/download/ExplanationOfBenefit
```

**Expected:** NDJSON with one EOB per line. May be large — observe the count in the manifest.

---

### Exercise 13: Get Export Analytics
Analyze the exported data for population health insights.

**Request:**
```
GET http://localhost:5280/api/bulk/Export/{jobId}/analytics
```

**Expected:** Analytics including:
- Patient demographics (gender breakdown)
- Total claims and average claim amount
- Encounter class distribution (ambulatory, inpatient, etc.)
- Top conditions and medications
- Total procedures

---

## Part C: Selective & Group Exports (Exercises 14-20)

### Exercise 14: Export Specific Resource Types Only
Export only Patient and Condition resources.

**Request:**
```
POST http://localhost:5280/api/bulk/Export/$export
Content-Type: application/json

{
  "resourceTypes": ["Patient", "Condition"]
}
```

Execute:
```
POST http://localhost:5280/api/bulk/Export/{jobId}/execute
```

**Expected:** Only 2 NDJSON files — Patient and Condition.

---

### Exercise 15: Export Medications and Observations
Export pharmacology and lab data.

**Request:**
```
POST http://localhost:5280/api/bulk/Export/$export
Content-Type: application/json

{
  "resourceTypes": ["MedicationRequest", "Observation"]
}
```

Execute:
```
POST http://localhost:5280/api/bulk/Export/{jobId}/execute
```

Check analytics:
```
GET http://localhost:5280/api/bulk/Export/{jobId}/analytics
```

**Expected:** Top medications list and counts. An interesting view of the synthetic population's prescriptions.

---

### Exercise 16: Group-Level Export — Diabetes Cohort
Export data only for patients in the Diabetes Care Cohort.

**Request:**
```
POST http://localhost:5280/api/bulk/Export/Group/GRP-DIABETES/$export
Content-Type: application/json

{
  "resourceTypes": ["Patient", "Condition", "MedicationRequest", "Observation"]
}
```

Execute:
```
POST http://localhost:5280/api/bulk/Export/{jobId}/execute
```

**Expected:** Only data for patients 51707, 52458, and 65520. Compare counts with system-level export.

---

### Exercise 17: Group-Level Export — High-Risk Patient
Export all data for the single high-risk patient.

**Request:**
```
POST http://localhost:5280/api/bulk/Export/Group/GRP-HIGHRISK/$export
Content-Type: application/json

{}
```

Execute and check analytics:
```
POST http://localhost:5280/api/bulk/Export/{jobId}/execute
GET http://localhost:5280/api/bulk/Export/{jobId}/analytics
```

**Expected:** Data for patient 52458 only. Use analytics to understand this high-risk patient's profile.

---

### Exercise 18: Group-Level Export — HEDIS Reporting
Export data needed for HEDIS quality measures.

**Request:**
```
POST http://localhost:5280/api/bulk/Export/Group/GRP-HEDIS/$export
Content-Type: application/json

{
  "resourceTypes": ["Patient", "Encounter", "Condition", "Procedure", "Observation"]
}
```

Execute:
```
POST http://localhost:5280/api/bulk/Export/{jobId}/execute
```

**Expected:** Clinical data for HEDIS reporting — encounters, conditions, procedures, and observations.

---

### Exercise 19: Download and Compare NDJSON Files
Compare Patient NDJSON from system export vs. group export.

Step 1: Download system-level Patient NDJSON (from Exercise 11)
```
GET http://localhost:5280/api/bulk/Export/{system-jobId}/download/Patient
```

Step 2: Download group-level Patient NDJSON (from Exercise 16)
```
GET http://localhost:5280/api/bulk/Export/{group-jobId}/download/Patient
```

**What to compare:** System export has all patients; group export has only the 3 diabetes cohort patients. Line count should differ.

---

### Exercise 20: Export for Non-existent Group
Test error handling for invalid group.

**Request:**
```
POST http://localhost:5280/api/bulk/Export/Group/GRP-NONEXISTENT/$export
Content-Type: application/json

{}
```

Execute:
```
POST http://localhost:5280/api/bulk/Export/{jobId}/execute
```

**Expected:** Job fails — "Group GRP-NONEXISTENT not found".

---

## Part D: Advanced Operations (Exercises 21-25)

### Exercise 21: Cancel an Export Job
Cancel an export before execution.

Step 1: Initiate export
```
POST http://localhost:5280/api/bulk/Export/$export
Content-Type: application/json

{
  "resourceTypes": ["Patient", "ExplanationOfBenefit", "Encounter", "MedicationRequest", "Condition", "Procedure", "Observation"]
}
```

Step 2: Cancel before executing
```
PUT http://localhost:5280/api/bulk/Export/{jobId}/cancel
```

Step 3: Verify
```
GET http://localhost:5280/api/bulk/Export/{jobId}/status
```

**Expected:** Status "cancelled".

---

### Exercise 22: Delete an Export Job
Clean up completed export and free resources.

Step 1: Get list of all jobs
```
GET http://localhost:5280/api/bulk/Export
```

Step 2: Delete a completed job
```
DELETE http://localhost:5280/api/bulk/Export/{jobId}
```

Step 3: Verify deletion
```
GET http://localhost:5280/api/bulk/Export/{jobId}/status
```

**Expected:** 404 Not Found after deletion.

---

### Exercise 23: Download Before Completion
Try to download data from an unexecuted export.

Step 1: Initiate but don't execute
```
POST http://localhost:5280/api/bulk/Export/$export
Content-Type: application/json

{}
```

Step 2: Try to download
```
GET http://localhost:5280/api/bulk/Export/{jobId}/download/Patient
```

**Expected:** 400 Bad Request — "Export not yet completed".

---

### Exercise 24: Full Population Health Workflow
Complete end-to-end: create custom group → export → analyze.

**Step 1: Create custom group**
```
POST http://localhost:5280/api/bulk/Group
Content-Type: application/json

{
  "name": "Annual Review 2025",
  "description": "All active members for 2025 annual health review",
  "patientIds": ["51707", "52458", "65520"]
}
```

**Step 2: Initiate group export with all clinical types**
```
POST http://localhost:5280/api/bulk/Export/Group/{groupId}/$export
Content-Type: application/json

{
  "resourceTypes": ["Patient", "ExplanationOfBenefit", "Encounter", "MedicationRequest", "Condition", "Procedure", "Observation", "AllergyIntolerance"]
}
```

**Step 3: Execute**
```
POST http://localhost:5280/api/bulk/Export/{jobId}/execute
```

**Step 4: Review manifest**
```
GET http://localhost:5280/api/bulk/Export/{jobId}/status
```

**Step 5: Analyze**
```
GET http://localhost:5280/api/bulk/Export/{jobId}/analytics
```

**Step 6: Download specific data**
```
GET http://localhost:5280/api/bulk/Export/{jobId}/download/Condition
```

**Expected:** Complete pipeline from cohort definition through data extraction to analysis.

**Analysis questions:**
- What are the most common conditions in this cohort?
- Average claim amount per patient?
- Gender distribution?
- Top medications prescribed?

---

### Exercise 25: Compare System vs. Group Analytics
Compare population-level insights across different export scopes.

**Step 1:** Run a system-level export with full types
```
POST http://localhost:5280/api/bulk/Export/$export
Content-Type: application/json

{
  "resourceTypes": ["Patient", "ExplanationOfBenefit", "Encounter", "MedicationRequest", "Condition"]
}
```

Execute: `POST /api/bulk/Export/{jobId}/execute`

**Step 2:** Get system analytics
```
GET http://localhost:5280/api/bulk/Export/{jobId}/analytics
```

**Step 3:** Now run a group-level export for high-risk patient
```
POST http://localhost:5280/api/bulk/Export/Group/GRP-HIGHRISK/$export
Content-Type: application/json

{
  "resourceTypes": ["Patient", "ExplanationOfBenefit", "Encounter", "MedicationRequest", "Condition"]
}
```

Execute: `POST /api/bulk/Export/{jobId}/execute`

**Step 4:** Get group analytics
```
GET http://localhost:5280/api/bulk/Export/{jobId}/analytics
```

**Compare:**
- How does the high-risk patient's claim average compare to the population?
- Are the top conditions different for the high-risk patient vs. overall?
- What medications are unique to the high-risk patient?

---

## Optional: No-Postman Runner (Exercises 19-25)

To execute the advanced bulk export scenarios from PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File .\Phase8\Exercises\run_ex19_25.ps1
```

Notes:
- Keep Phase 8 BulkDataAPI running on `http://localhost:5280`.
- Keep HAPI FHIR running on `http://localhost:8082/fhir` for export execution.

---

## Summary of All Exercises
| # | Exercise | Endpoint | Key Learning |
|---|----------|----------|--------------|
| 1 | List groups | GET /Group | Pre-configured groups |
| 2 | Diabetes group | GET /Group/GRP-DIABETES | Group membership |
| 3 | Create group | POST /Group | Custom cohort definition |
| 4 | Add members | PUT /Group/{id}/members/add | Group management |
| 5 | Remove members | PUT /Group/{id}/members/remove | Member removal |
| 6 | Validation | POST /Group | Error handling |
| 7 | System $export | POST /Export/$export | Initiate export (202) |
| 8 | Check status | GET /Export/{id}/status | Async polling |
| 9 | Execute export | POST /Export/{id}/execute | HAPI FHIR pull |
| 10 | Export manifest | GET /Export/{id}/status | Bulk data output spec |
| 11 | Download Patient | GET /Export/{id}/download/Patient | NDJSON format |
| 12 | Download EOB | GET /Export/{id}/download/ExplanationOfBenefit | Claims NDJSON |
| 13 | Analytics | GET /Export/{id}/analytics | Population health |
| 14 | Selective types | POST /Export/$export | _type parameter |
| 15 | Meds + Obs | POST /Export/$export | Pharmacy + lab data |
| 16 | Group export | POST /Export/Group/{id}/$export | Cohort export |
| 17 | Single patient | POST /Export/Group/GRP-HIGHRISK/$export | Focused export |
| 18 | HEDIS export | POST /Export/Group/GRP-HEDIS/$export | Quality reporting |
| 19 | Compare NDJSON | GET /download/ (two jobs) | Scope comparison |
| 20 | Invalid group | POST /Export/Group/GRP-NONEXISTENT/$export | Error handling |
| 21 | Cancel export | PUT /Export/{id}/cancel | Job cancellation |
| 22 | Delete export | DELETE /Export/{id} | Cleanup |
| 23 | Premature download | GET /download before execute | State validation |
| 24 | E2E workflow | Multi-step | Full pipeline |
| 25 | Compare analytics | GET /analytics (two jobs) | Population vs. cohort |
