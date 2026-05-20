# Phase 7: Payer-to-Payer Data Exchange (PDex) — Hands-On Guide

## Prerequisites
- Phase 7 PDexAPI running on port 5270
- HAPI FHIR server running on port 8082
- Postman or similar REST client

```bash
cd Phase7/PDexAPI
dotnet run --launch-profile http
```

---

## Part A: Member Match (Exercises 1-8)

### Exercise 1: List Known Members
View all members registered in the payer system.

**Request:**
```
GET http://localhost:5270/api/pdex/MemberMatch/members
```

**Expected:** 5 members across 3 payers (PAYER-ALPHA, PAYER-BETA, PAYER-GAMMA) with resource count summaries.

---

### Exercise 2: Get Member Data Summary
View detailed data holdings for a specific patient.

**Request:**
```
GET http://localhost:5270/api/pdex/MemberMatch/members/51707
```

**Expected:** Ramon Schulist's data summary — 85 EOBs, 42 encounters, 15 medications, coverage dates.

---

### Exercise 3: $member-match — Exact ID Match
Match a member using their known patient ID (highest confidence).

**Request:**
```
POST http://localhost:5270/api/pdex/MemberMatch
Content-Type: application/json

{
  "memberFirstName": "Ramon",
  "memberLastName": "Schulist",
  "memberDateOfBirth": "1965-03-15",
  "memberGender": "male",
  "memberId": "51707",
  "oldPayerId": "PAYER-ALPHA",
  "newPayerId": "PAYER-BETA"
}
```

**Expected:** Match with confidence "certain", unique patient identifier, and coverage IDs for both payers.

---

### Exercise 4: $member-match — Name Match
Match a member using demographics only (no ID).

**Request:**
```
POST http://localhost:5270/api/pdex/MemberMatch
Content-Type: application/json

{
  "memberFirstName": "Rasheeda",
  "memberLastName": "Heaney",
  "memberDateOfBirth": "1975-08-22",
  "memberGender": "female",
  "oldPayerId": "PAYER-ALPHA",
  "newPayerId": "PAYER-GAMMA"
}
```

**Expected:** Match with confidence "probable" and recommendation for DOB verification.

---

### Exercise 5: $member-match — Cross-Payer Match
Match a member from a different payer system.

**Request:**
```
POST http://localhost:5270/api/pdex/MemberMatch
Content-Type: application/json

{
  "memberFirstName": "Margaret",
  "memberLastName": "Johnson",
  "memberDateOfBirth": "1958-11-30",
  "memberGender": "female",
  "oldPayerId": "PAYER-BETA",
  "newPayerId": "PAYER-ALPHA"
}
```

**Expected:** Match found for Margaret Johnson at PAYER-BETA.

---

### Exercise 6: $member-match — No Match
Attempt to match a non-existent member.

**Request:**
```
POST http://localhost:5270/api/pdex/MemberMatch
Content-Type: application/json

{
  "memberFirstName": "John",
  "memberLastName": "Doe",
  "memberDateOfBirth": "1990-01-01",
  "memberGender": "male",
  "oldPayerId": "PAYER-ALPHA",
  "newPayerId": "PAYER-BETA"
}
```

**Expected:** No match found, confidence "none".

---

### Exercise 7: $member-match — Partial Match
Provide only a last name that partially matches (ambiguous).

**Request:**
```
POST http://localhost:5270/api/pdex/MemberMatch
Content-Type: application/json

{
  "memberFirstName": "R",
  "memberLastName": "Schulist",
  "memberDateOfBirth": "1965-03-15",
  "memberGender": "male",
  "oldPayerId": "PAYER-ALPHA",
  "newPayerId": "PAYER-BETA"
}
```

**Expected:** Match found (only one Schulist in system). Compare with exact match confidence.

---

### Exercise 8: $member-match — Missing Required Fields
Test validation.

**Request:**
```
POST http://localhost:5270/api/pdex/MemberMatch
Content-Type: application/json

{
  "memberFirstName": "Ramon"
}
```

**Expected:** 400 Bad Request — "memberLastName is required".

---

## Part B: Consent Management (Exercises 9-16)

### Exercise 9: Create Data Exchange Consent
Create consent for transferring data from PAYER-ALPHA to PAYER-BETA.

**Request:**
```
POST http://localhost:5270/api/pdex/Consent
Content-Type: application/json

{
  "patientId": "51707",
  "patientName": "Ramon Schulist",
  "sourcePayerId": "PAYER-ALPHA",
  "targetPayerId": "PAYER-BETA",
  "dataCategories": ["claims", "encounters", "medications", "conditions"],
  "expirationDays": 365
}
```

**Expected:** Draft consent with consentId, data categories, and expiration date.

**Save the consentId** — you'll need it for exercises 11-16 and beyond.

---

### Exercise 10: Create Broad Consent (All Categories)
Create consent with default (all) data categories.

**Request:**
```
POST http://localhost:5270/api/pdex/Consent
Content-Type: application/json

{
  "patientId": "52458",
  "patientName": "Rasheeda Heaney",
  "sourcePayerId": "PAYER-ALPHA",
  "targetPayerId": "PAYER-GAMMA",
  "expirationDays": 180
}
```

**Expected:** Consent with all default categories: claims, encounters, medications, conditions, allergies, procedures, observations.

---

### Exercise 11: Get Consent Details
Retrieve a specific consent.

**Request:**
```
GET http://localhost:5270/api/pdex/Consent/{consentId}
```
*Replace with consentId from Exercise 9.*

**Expected:** Full consent details including status "draft", categories, and parties.

---

### Exercise 12: Activate Consent
Patient signs and activates the consent (transitions from draft → active).

**Request:**
```
PUT http://localhost:5270/api/pdex/Consent/{consentId}/activate
```

**Expected:** Status changes to "active" with consentDate timestamp.

---

### Exercise 13: Try to Activate Already-Active Consent
Test idempotency — activating an already-active consent fails.

**Request:**
```
PUT http://localhost:5270/api/pdex/Consent/{consentId}/activate
```

**Expected:** 400 Bad Request — "not in draft status".

---

### Exercise 14: Revoke Consent
Patient revokes their data sharing consent.

First, activate the consent from Exercise 10:
```
PUT http://localhost:5270/api/pdex/Consent/{consentId-from-ex10}/activate
```

Then revoke it:
```
PUT http://localhost:5270/api/pdex/Consent/{consentId-from-ex10}/revoke
```

**Expected:** Status "revoked" with revokedAt timestamp.

---

### Exercise 15: Get Patient's Consent History
View all consents for a specific patient.

**Request:**
```
GET http://localhost:5270/api/pdex/Consent/patient/51707
```

**Expected:** List of consents for Ramon Schulist.

---

### Exercise 16: Validation — Same Source and Target
Try creating consent where source = target payer.

**Request:**
```
POST http://localhost:5270/api/pdex/Consent
Content-Type: application/json

{
  "patientId": "51707",
  "patientName": "Ramon Schulist",
  "sourcePayerId": "PAYER-ALPHA",
  "targetPayerId": "PAYER-ALPHA"
}
```

**Expected:** 400 Bad Request — "Source and target payers must be different".

---

## Part C: Data Exchange (Exercises 17-25)

### Exercise 17: Initiate Data Exchange
Start a data exchange using an active consent.

**Request:**
```
POST http://localhost:5270/api/pdex/Exchange
Content-Type: application/json

{
  "consentId": "{consentId-from-exercise-9}"
}
```
*Use the consentId that was activated in Exercise 12.*

**Expected:** Exchange job created with status "queued".

**Save the jobId** — you'll need it for the next exercises.

---

### Exercise 18: Execute Data Exchange
Run the exchange — pulls actual data from HAPI FHIR server.

**Request:**
```
POST http://localhost:5270/api/pdex/Exchange/{jobId}/execute
```

**Expected:** Job status "completed" with resource counts, exchanged resources, and provenance records. This actually queries your HAPI FHIR server.

**What to observe:** How many resources were found for each category? Compare with the member summary from Exercise 2.

---

### Exercise 19: Check Exchange Job Status
View the current state of an exchange job.

**Request:**
```
GET http://localhost:5270/api/pdex/Exchange/{jobId}
```

**Expected:** Full job details — status, started/completed times, resource counts, categories.

---

### Exercise 20: View Exchanged Resources
See what specific resources were transferred.

**Request:**
```
GET http://localhost:5270/api/pdex/Exchange/{jobId}/resources
```

**Expected:** List of transferred resources grouped by type with counts.

---

### Exercise 21: View Provenance Records
Examine the data origin tracking for the exchange.

**Request:**
```
GET http://localhost:5270/api/pdex/Exchange/{jobId}/provenance
```

**Expected:** Provenance records for each transferred resource — agent (source payer), activity ("transmit"), timestamp.

**What to observe:** Each resource has its own provenance record proving where it came from.

---

### Exercise 22: Exchange with Category Override
Initiate exchange requesting only specific categories (overriding consent defaults).

First create and activate a new consent:
```
POST http://localhost:5270/api/pdex/Consent
Content-Type: application/json

{
  "patientId": "65520",
  "patientName": "Karena O'Keefe",
  "sourcePayerId": "PAYER-ALPHA",
  "targetPayerId": "PAYER-BETA",
  "dataCategories": ["claims", "encounters", "medications", "conditions", "observations"]
}
```

Activate it:
```
PUT http://localhost:5270/api/pdex/Consent/{consentId}/activate
```

Initiate exchange for only medications:
```
POST http://localhost:5270/api/pdex/Exchange
Content-Type: application/json

{
  "consentId": "{consentId}",
  "dataCategories": ["medications"]
}
```

Execute:
```
POST http://localhost:5270/api/pdex/Exchange/{jobId}/execute
```

**Expected:** Only MedicationRequest resources are transferred (not the other consented categories).

---

### Exercise 23: Exchange with Inactive Consent
Try to exchange using a revoked consent.

**Request:**
```
POST http://localhost:5270/api/pdex/Exchange
Content-Type: application/json

{
  "consentId": "{consentId-revoked-from-exercise-14}"
}
```

**Expected:** 400 Bad Request — "Consent must be active before data exchange".

---

### Exercise 24: Cancel Exchange Job
Cancel a pending exchange job.

Step 1: Create a new consent and exchange:
```
POST http://localhost:5270/api/pdex/Consent
Content-Type: application/json

{
  "patientId": "51707",
  "patientName": "Ramon Schulist",
  "sourcePayerId": "PAYER-ALPHA",
  "targetPayerId": "PAYER-GAMMA"
}
```

Activate and initiate exchange:
```
PUT http://localhost:5270/api/pdex/Consent/{consentId}/activate
```
```
POST http://localhost:5270/api/pdex/Exchange
Content-Type: application/json

{
  "consentId": "{consentId}"
}
```

Cancel before execution:
```
PUT http://localhost:5270/api/pdex/Exchange/{jobId}/cancel
```

**Expected:** Job status changes to "cancelled".

---

### Exercise 25: Complete End-to-End Workflow
Full payer-to-payer exchange: match → consent → exchange → verify.

**Step 1: Member Match**
```
POST http://localhost:5270/api/pdex/MemberMatch
Content-Type: application/json

{
  "memberFirstName": "Karena",
  "memberLastName": "O'Keefe",
  "memberDateOfBirth": "1980-05-10",
  "memberGender": "female",
  "memberId": "65520",
  "oldPayerId": "PAYER-ALPHA",
  "newPayerId": "PAYER-GAMMA"
}
```

**Step 2: View Data Summary**
```
GET http://localhost:5270/api/pdex/MemberMatch/members/65520
```

**Step 3: Create Consent**
```
POST http://localhost:5270/api/pdex/Consent
Content-Type: application/json

{
  "patientId": "65520",
  "patientName": "Karena O'Keefe",
  "sourcePayerId": "PAYER-ALPHA",
  "targetPayerId": "PAYER-GAMMA",
  "dataCategories": ["claims", "encounters", "medications", "conditions", "allergies"]
}
```

**Step 4: Activate Consent**
```
PUT http://localhost:5270/api/pdex/Consent/{consentId}/activate
```

**Step 5: Initiate Exchange**
```
POST http://localhost:5270/api/pdex/Exchange
Content-Type: application/json

{
  "consentId": "{consentId}"
}
```

**Step 6: Execute Exchange**
```
POST http://localhost:5270/api/pdex/Exchange/{jobId}/execute
```

**Step 7: Review Results**
```
GET http://localhost:5270/api/pdex/Exchange/{jobId}/resources
```

**Step 8: Verify Provenance**
```
GET http://localhost:5270/api/pdex/Exchange/{jobId}/provenance
```

**Expected:** Complete data transfer with full audit trail showing origin, resource types, and quantities transferred.

---

## Optional: No-Postman Runner (Exercises 19-25)

To execute the advanced exchange exercises via script:

```powershell
powershell -ExecutionPolicy Bypass -File .\Phase7\Exercises\run_ex19_25.ps1
```

Notes:
- Keep Phase 7 PDexAPI running on `http://localhost:5270`.
- Keep HAPI FHIR running on `http://localhost:8082/fhir` for exchange execution steps.

---

## Summary of All Exercises
| # | Exercise | Endpoint | Key Learning |
|---|----------|----------|--------------|
| 1 | List members | GET /MemberMatch/members | Member registry |
| 2 | Member summary | GET /MemberMatch/members/{id} | Data holdings |
| 3 | ID match | POST /MemberMatch | Exact match (certain) |
| 4 | Name match | POST /MemberMatch | Demographic match (probable) |
| 5 | Cross-payer match | POST /MemberMatch | Multi-payer lookup |
| 6 | No match | POST /MemberMatch | Match failure handling |
| 7 | Partial match | POST /MemberMatch | Ambiguous matching |
| 8 | Validation | POST /MemberMatch | Required field checks |
| 9 | Create consent | POST /Consent | Consent creation |
| 10 | Broad consent | POST /Consent | Default categories |
| 11 | Get consent | GET /Consent/{id} | Consent details |
| 12 | Activate | PUT /Consent/{id}/activate | Consent lifecycle |
| 13 | Double activate | PUT /Consent/{id}/activate | Idempotency check |
| 14 | Revoke consent | PUT /Consent/{id}/revoke | Patient rights |
| 15 | Patient consents | GET /Consent/patient/{id} | Consent history |
| 16 | Same payer | POST /Consent | Validation rules |
| 17 | Initiate exchange | POST /Exchange | Job creation |
| 18 | Execute exchange | POST /Exchange/{id}/execute | FHIR data pull |
| 19 | Job status | GET /Exchange/{id} | Progress tracking |
| 20 | Resources | GET /Exchange/{id}/resources | Transfer details |
| 21 | Provenance | GET /Exchange/{id}/provenance | Data origin tracking |
| 22 | Category override | POST /Exchange | Selective transfer |
| 23 | Inactive consent | POST /Exchange | Consent enforcement |
| 24 | Cancel job | PUT /Exchange/{id}/cancel | Job cancellation |
| 25 | E2E workflow | Multi-step | Complete P2P exchange |
