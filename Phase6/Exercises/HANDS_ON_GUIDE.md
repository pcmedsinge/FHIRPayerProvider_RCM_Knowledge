# Phase 6: Prior Authorization (PAS + DTR) — Hands-On Guide

## Prerequisites
- Phase 6 PriorAuthAPI running on port 5260
- Postman or similar REST client

```bash
cd Phase6/PriorAuthAPI
dotnet run --launch-profile http
```

---

## Part A: DTR — Documentation Templates & Rules (Exercises 1-10)

### Exercise 1: List All Questionnaire Templates
Discover what questionnaires are available for documentation collection.

**Request:**
```
GET http://localhost:5260/api/dtr/Questionnaire
```

**Expected:** JSON object with `total = 5` and a `questionnaires` array (MRI Brain, Total Knee, Ozempic, CPAP, CT Abdomen).

**What to observe:** Each item includes `questionnaireId`, `title`, `serviceCode`, `description`, and `questionCount` (the full question list is returned when you call Exercise 2/3/4 endpoints).

---

### Exercise 2: Get MRI Brain Questionnaire
Retrieve the specific questionnaire for MRI Brain (CPT 70553).

**Request:**
```
GET http://localhost:5260/api/dtr/Questionnaire/DTR-Q-70553
```

**Expected:** Questionnaire with 7 clinical questions about indications, symptoms, previous imaging, neurological findings, etc.

**What to observe:** Questions have `autoPopulateFrom` hints (e.g., "Condition.code") indicating CQL/FHIR auto-population.

---

### Exercise 3: Find Questionnaire by Service Code
Look up which questionnaire is needed for a Total Knee Replacement.

**Request:**
```
GET http://localhost:5260/api/dtr/Questionnaire/by-service/27447
```

**Expected:** DTR-Q-27447 questionnaire with 10 questions covering diagnosis, conservative therapy, X-ray findings, BMI, etc.

---

### Exercise 4: Find Questionnaire for Ozempic
Look up documentation requirements for GLP-1 receptor agonist.

**Request:**
```
GET http://localhost:5260/api/dtr/Questionnaire/by-service/1991302
```

**Expected:** DTR-Q-1991302 with 8 questions about diabetes diagnosis, HbA1c, metformin trial, BMI, contraindications.

---

### Exercise 5: Submit MRI Brain Questionnaire Response
Complete the MRI Brain questionnaire with clinical documentation.

**Request:**
```
POST http://localhost:5260/api/dtr/Questionnaire/response
Content-Type: application/json

{
  "questionnaireId": "DTR-Q-70553",
  "patientId": "51707",
  "answers": {
    "indication": "Persistent severe headaches with neurological symptoms",
    "symptom-duration": "Greater than 6 weeks",
    "previous-imaging": "CT scan performed — inconclusive",
    "neurological-findings": "Yes — focal deficits noted",
    "red-flags": "Yes"
  }
}
```

**Expected:** A response with a generated responseId and status "completed".

**What to observe:** The system timestamps the response and links it to the patient and questionnaire.

---

### Exercise 6: Submit Total Knee Questionnaire Response
Document conservative therapy completion for total knee replacement.

**Request:**
```
POST http://localhost:5260/api/dtr/Questionnaire/response
Content-Type: application/json

{
  "questionnaireId": "DTR-Q-27447",
  "patientId": "51707",
  "answers": {
    "primary-diagnosis": "Severe osteoarthritis, right knee",
    "symptom-duration": "18 months",
    "xray-kl-grade": "Grade 4 — severe",
    "physical-therapy-completed": "Yes — 12 weeks completed",
    "bmi": "28.5",
    "steroid-injections": "3 injections over 6 months",
    "nsaid-trial": "Yes — failed ibuprofen and naproxen",
    "functional-limitation": "Unable to walk more than 1 block, requires assistive device"
  }
}
```

**Expected:** Completed response with responseId for use in PA submission.

---

### Exercise 7: Submit Ozempic Questionnaire Response
Document diabetes management history for GLP-1 request.

**Request:**
```
POST http://localhost:5260/api/dtr/Questionnaire/response
Content-Type: application/json

{
  "questionnaireId": "DTR-Q-1991302",
  "patientId": "52458",
  "answers": {
    "diabetes-diagnosis": "Type 2 Diabetes Mellitus",
    "current-hba1c": "8.5",
    "metformin-trial": "Yes — 6 months at maximum dose",
    "metformin-result": "HbA1c remained above 7.5 despite adherence",
    "current-bmi": "32.1",
    "contraindications": "None"
  }
}
```

**Expected:** Completed response to support Ozempic prior auth.

---

### Exercise 8: Submit CPAP Questionnaire Response
Document sleep study results for CPAP DME request.

**Request:**
```
POST http://localhost:5260/api/dtr/Questionnaire/response
Content-Type: application/json

{
  "questionnaireId": "DTR-Q-E0601",
  "patientId": "65520",
  "answers": {
    "sleep-study-type": "In-lab polysomnography",
    "ahi-score": "22",
    "face-to-face-eval": "Yes — completed with sleep specialist",
    "compliance-plan": "Patient education completed, mask fitting scheduled"
  }
}
```

**Expected:** Completed response for CPAP prior auth.

---

### Exercise 9: Retrieve a Questionnaire Response
Fetch a previously submitted response by ID.

**Request:**
```
GET http://localhost:5260/api/dtr/Questionnaire/response/{responseId}
```
*Replace `{responseId}` with the ID returned from Exercise 5.*

**Expected:** Full response with questionnaireId, patientId, answers, and timestamp.

---

### Exercise 10: Get All Responses for a Patient
List all documentation completed for a specific patient.

**Request:**
```
GET http://localhost:5260/api/dtr/Questionnaire/responses/patient/51707
```

**Expected:** Array of questionnaire responses for patient 51707 (should include MRI Brain and Total Knee from exercises 5-6).

---

## Part B: PAS — Prior Authorization Submission (Exercises 11-20)

### Exercise 11: Submit PA — Auto-Approve (Chest X-Ray)
Submit a PA request for a service that gets automatically approved.

**Request:**
```
POST http://localhost:5260/api/pas/PriorAuth/submit
Content-Type: application/json

{
  "patientId": "51707",
  "serviceCode": "71046",
  "serviceDescription": "Chest X-Ray",
  "providerId": "PRACT-001",
  "providerName": "Dr. Smith",
  "diagnosis": "Persistent cough",
  "urgency": "routine"
}
```

**Expected:** Immediately approved with status "approved" and a generated authorizationId.

**What to observe:** No documentation required — auto-approve codes bypass the questionnaire requirement.

---

### Exercise 12: Submit PA — Denied (Cosmetic Procedure)
Submit a PA for a cosmetic procedure that is excluded from coverage.

**Request:**
```
POST http://localhost:5260/api/pas/PriorAuth/submit
Content-Type: application/json

{
  "patientId": "51707",
  "serviceCode": "15780",
  "serviceDescription": "Dermabrasion",
  "providerId": "PRACT-002",
  "providerName": "Dr. Jones",
  "diagnosis": "Cosmetic skin improvement",
  "urgency": "routine"
}
```

**Expected:** Status "denied" with denialReason "Cosmetic procedures are not covered under this plan."

---

### Exercise 13: Submit PA — MRI Brain with Documentation
Submit MRI Brain PA with the questionnaire response from Exercise 5.

**Request:**
```
POST http://localhost:5260/api/pas/PriorAuth/submit
Content-Type: application/json

{
  "patientId": "51707",
  "serviceCode": "70553",
  "serviceDescription": "MRI Brain without and with Contrast",
  "providerId": "PRACT-003",
  "providerName": "Dr. Neurologist",
  "diagnosis": "Persistent headaches with focal deficits",
  "urgency": "routine",
  "questionnaireResponseId": "{responseId-from-exercise-5}",
  "supportingDocuments": ["Clinical notes", "CT scan results"]
}
```

**Expected:** Status "approved" with conditions — documentation supports medical necessity.

---

### Exercise 14: Submit PA — MRI Brain WITHOUT Documentation
See what happens when imaging is requested without supporting documentation.

**Request:**
```
POST http://localhost:5260/api/pas/PriorAuth/submit
Content-Type: application/json

{
  "patientId": "52458",
  "serviceCode": "70553",
  "serviceDescription": "MRI Brain",
  "providerId": "PRACT-003",
  "providerName": "Dr. Neurologist",
  "diagnosis": "Headache",
  "urgency": "routine"
}
```

**Expected:** Status "pended-for-review" — documentation required but not provided.

**What to observe:** Compare with Exercise 13 to see how documentation affects approval.

---

### Exercise 15: Submit PA — Total Knee (Urgent with Docs)
Submit urgent surgical PA with complete documentation.

**Request:**
```
POST http://localhost:5260/api/pas/PriorAuth/submit
Content-Type: application/json

{
  "patientId": "51707",
  "serviceCode": "27447",
  "serviceDescription": "Total Knee Replacement",
  "providerId": "PRACT-005",
  "providerName": "Dr. Surgeon",
  "diagnosis": "Severe osteoarthritis right knee — Grade 4",
  "urgency": "urgent",
  "questionnaireResponseId": "{responseId-from-exercise-6}",
  "supportingDocuments": ["X-ray report", "PT completion letter", "Orthopedic evaluation"]
}
```

**Expected:** Status "approved" — urgent surgical requests with documentation are expedited.

---

### Exercise 16: Submit PA — Total Knee (Routine, Docs Only)
Submit non-urgent surgical PA — requires medical director review.

**Request:**
```
POST http://localhost:5260/api/pas/PriorAuth/submit
Content-Type: application/json

{
  "patientId": "52458",
  "serviceCode": "27447",
  "serviceDescription": "Total Knee Replacement",
  "providerId": "PRACT-005",
  "providerName": "Dr. Surgeon",
  "diagnosis": "Moderate osteoarthritis left knee",
  "urgency": "routine",
  "supportingDocuments": ["X-ray report", "PT notes"]
}
```

**Expected:** Status "pended-for-review" — surgical cases without urgency go to medical director.

---

### Exercise 17: Submit PA — Ozempic (Specialty Pharmacy)
Submit specialty medication PA with questionnaire documentation.

**Request:**
```
POST http://localhost:5260/api/pas/PriorAuth/submit
Content-Type: application/json

{
  "patientId": "52458",
  "serviceCode": "1991302",
  "serviceDescription": "Ozempic (semaglutide) injection",
  "providerId": "PRACT-010",
  "providerName": "Dr. Endocrinologist",
  "diagnosis": "Type 2 Diabetes, uncontrolled on metformin",
  "urgency": "routine",
  "questionnaireResponseId": "{responseId-from-exercise-7}",
  "supportingDocuments": ["Lab results — HbA1c 8.5", "Medication history"]
}
```

**Expected:** Status with specialty pharmacy review notes.

---

### Exercise 18: Submit PA — CPAP Machine (DME)
Submit DME request with sleep study documentation.

**Request:**
```
POST http://localhost:5260/api/pas/PriorAuth/submit
Content-Type: application/json

{
  "patientId": "65520",
  "serviceCode": "E0601",
  "serviceDescription": "CPAP Machine",
  "providerId": "PRACT-020",
  "providerName": "Dr. Pulmonologist",
  "diagnosis": "Obstructive Sleep Apnea",
  "urgency": "routine",
  "questionnaireResponseId": "{responseId-from-exercise-8}",
  "supportingDocuments": ["Sleep study report", "Face-to-face evaluation"]
}
```

**Expected:** Status "approved" with compliance monitoring conditions.

---

### Exercise 19: Check PA Status
Query the status of a previously submitted PA request.

**Request:**
```
GET http://localhost:5260/api/pas/PriorAuth/status/{authorizationId}
```
*Replace `{authorizationId}` with the ID from Exercise 13.*

**Expected:** Full PA response with current status, authorization details, timestamps, and status history.

---

### Exercise 20: Get Patient's PA History
Retrieve all PA requests for a patient.

**Request:**
```
GET http://localhost:5260/api/pas/PriorAuth/patient/51707
```

**Expected:** List of all PA requests submitted for patient 51707 (should include Chest X-Ray, MRI, Total Knee from earlier exercises).

---

## Part C: Advanced Workflows (Exercises 21-25)

### Exercise 21: Complete DTR-to-PAS Workflow
End-to-end workflow: discover questionnaire → complete → submit PA.

**Step 1:** Find questionnaire for CT Abdomen
```
GET http://localhost:5260/api/dtr/Questionnaire/by-service/74177
```

**Step 2:** Complete the questionnaire
```
POST http://localhost:5260/api/dtr/Questionnaire/response
Content-Type: application/json

{
  "questionnaireId": "DTR-Q-74177",
  "patientId": "65520",
  "answers": {
    "indication": "Acute abdominal pain — RLQ tenderness",
    "lab-results": "Elevated WBC, CRP",
    "previous-imaging": "Ultrasound inconclusive",
    "ultrasound-first": "Yes",
    "emergency": "No"
  }
}
```

**Step 3:** Submit PA with questionnaire response
```
POST http://localhost:5260/api/pas/PriorAuth/submit
Content-Type: application/json

{
  "patientId": "65520",
  "serviceCode": "74177",
  "serviceDescription": "CT Abdomen with Contrast",
  "providerId": "PRACT-030",
  "providerName": "Dr. Radiologist",
  "diagnosis": "Acute abdominal pain — rule out appendicitis",
  "urgency": "urgent",
  "questionnaireResponseId": "{responseId-from-step-2}",
  "supportingDocuments": ["Lab results", "Ultrasound report"]
}
```

**Expected:** The documented, urgent imaging request should get approved.

---

### Exercise 22: Admin — List All PA Requests
View all PA requests across the system (payer admin view).

**Request:**
```
GET http://localhost:5260/api/pas/PriorAuth
```

**Expected:** Full list of all PA requests with statuses, showing the mix of approved, denied, and pended.

**What to analyze:**
- How many auto-approved vs. pended?
- Average by urgency level?
- Which service codes trigger the most reviews?

---

### Exercise 23: Admin — Update Pended Request
Simulate a payer reviewer approving a pended request.

**Step 1:** Find a pended request from Exercise 14 or 16

**Step 2:** Approve it
```
PUT http://localhost:5260/api/pas/PriorAuth/update/{authorizationId}?status=approved&notes=Medical%20director%20approved%20after%20peer-to-peer%20review
```

**Expected:** Status updated to "approved" with reviewer notes.

**Step 3:** Verify the update
```
GET http://localhost:5260/api/pas/PriorAuth/status/{authorizationId}
```

**Expected:** Status history shows the progression from pended → approved with timestamps.

---

### Exercise 24: Cancel a PA Request
Cancel a pending PA request (provider decides not to proceed).

**Step 1:** Submit a new PA request
```
POST http://localhost:5260/api/pas/PriorAuth/submit
Content-Type: application/json

{
  "patientId": "51707",
  "serviceCode": "72148",
  "serviceDescription": "MRI Lumbar Spine",
  "providerId": "PRACT-003",
  "providerName": "Dr. Neurologist",
  "diagnosis": "Lower back pain",
  "urgency": "routine"
}
```

**Step 2:** Cancel the request
```
PUT http://localhost:5260/api/pas/PriorAuth/cancel/{authorizationId}
```

**Expected:** Status changes to "cancelled". Cannot cancel already-finalized (approved/denied) requests.

**Step 3:** Try to cancel an already-approved request and observe the error.

---

### Exercise 25: Edge Cases & Validation
Test boundary conditions and error handling.

**Test 1: Missing patient ID**
```
POST http://localhost:5260/api/pas/PriorAuth/submit
Content-Type: application/json

{
  "serviceCode": "71046",
  "serviceDescription": "Chest X-Ray"
}
```
Expected: 400 Bad Request — "patientId is required"

**Test 2: Missing service code**
```
POST http://localhost:5260/api/pas/PriorAuth/submit
Content-Type: application/json

{
  "patientId": "51707"
}
```
Expected: 400 Bad Request — "serviceCode is required"

**Test 3: Non-existent authorization**
```
GET http://localhost:5260/api/pas/PriorAuth/status/NONEXISTENT-ID
```
Expected: 404 Not Found

**Test 4: Invalid status update**
```
PUT http://localhost:5260/api/pas/PriorAuth/update/{authId}?status=invalid-status
```
Expected: 400 Bad Request with list of valid statuses

**Test 5: Non-existent questionnaire**
```
GET http://localhost:5260/api/dtr/Questionnaire/DOES-NOT-EXIST
```
Expected: 404 Not Found

---

## Optional: No-Postman Runner (Exercises 19-25)

To run the advanced PAS/DTR flow exercises directly:

```powershell
powershell -ExecutionPolicy Bypass -File .\Phase6\Exercises\run_ex19_25.ps1
```

Notes:
- Keep Phase 6 PriorAuthAPI running on `http://localhost:5260`.
- The script creates its own test records (questionnaire responses + PA requests) for reproducible runs.

---

## Summary of All Exercises
| # | Exercise | Endpoint | Key Learning |
|---|----------|----------|--------------|
| 1 | List questionnaires | GET /api/dtr/Questionnaire | DTR template discovery |
| 2 | MRI Brain questionnaire | GET /api/dtr/Questionnaire/DTR-Q-70553 | Question structure & types |
| 3 | Find by service code | GET /api/dtr/Questionnaire/by-service/27447 | Code-to-questionnaire mapping |
| 4 | Ozempic questionnaire | GET /api/dtr/Questionnaire/by-service/1991302 | Medication-specific docs |
| 5 | Submit MRI response | POST /api/dtr/Questionnaire/response | Completing DTR forms |
| 6 | Submit knee response | POST /api/dtr/Questionnaire/response | Surgical documentation |
| 7 | Submit Ozempic response | POST /api/dtr/Questionnaire/response | Pharmacy documentation |
| 8 | Submit CPAP response | POST /api/dtr/Questionnaire/response | DME documentation |
| 9 | Get response by ID | GET /api/dtr/Questionnaire/response/{id} | Response retrieval |
| 10 | Patient responses | GET /api/dtr/Questionnaire/responses/patient/{id} | Patient documentation history |
| 11 | PA auto-approve | POST /api/pas/PriorAuth/submit | Auto-approval rules |
| 12 | PA denied | POST /api/pas/PriorAuth/submit | Coverage exclusions |
| 13 | PA with docs (approved) | POST /api/pas/PriorAuth/submit | Documentation → approval |
| 14 | PA without docs (pended) | POST /api/pas/PriorAuth/submit | Missing docs → pended |
| 15 | Urgent surgical PA | POST /api/pas/PriorAuth/submit | Urgency expediting |
| 16 | Routine surgical PA | POST /api/pas/PriorAuth/submit | Medical director review |
| 17 | Specialty medication | POST /api/pas/PriorAuth/submit | Pharmacy PA pathway |
| 18 | DME PA | POST /api/pas/PriorAuth/submit | DME with compliance |
| 19 | Check PA status | GET /api/pas/PriorAuth/status/{id} | Status inquiry |
| 20 | Patient PA history | GET /api/pas/PriorAuth/patient/{id} | Patient-level view |
| 21 | DTR-to-PAS workflow | Multi-step | End-to-end integration |
| 22 | Admin list all PAs | GET /api/pas/PriorAuth | Payer admin view |
| 23 | Admin update status | PUT /api/pas/PriorAuth/update/{id} | Payer review simulation |
| 24 | Cancel PA request | PUT /api/pas/PriorAuth/cancel/{id} | Cancellation workflow |
| 25 | Edge cases | Various | Validation & error handling |
