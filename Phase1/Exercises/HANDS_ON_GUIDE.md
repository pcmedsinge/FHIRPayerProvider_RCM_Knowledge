# Phase 1 Hands-On Testing Guide

Test your FHIR server by opening these URLs in your browser.

---

## Exercise 1: Server Metadata

View server capabilities and supported resources.

**URL**: http://localhost:8082/fhir/metadata

---

## Exercise 2: List All Patients

View first 10 patients in the system.

**URL**: http://localhost:8082/fhir/Patient?_count=10

---

## Exercise 3: Get Specific Patient

View detailed information for a single patient (replace ID with actual patient ID from Exercise 2).

**URL**: http://localhost:8082/fhir/Patient/51707

---

## Exercise 4: Search Patients by Name

Find patients with names containing "John".

**URL**: http://localhost:8082/fhir/Patient?name=John

**Try also**:
- By family name: http://localhost:8082/fhir/Patient?family=Smith
- By gender: http://localhost:8082/fhir/Patient?gender=female

---

## Exercise 5: View Claims (ExplanationOfBenefit)

View first 5 insurance claims.

**URL**: http://localhost:8082/fhir/ExplanationOfBenefit?_count=5

---

## Exercise 6: Get Claims for Specific Patient

View all claims for a single patient (replace patient ID).

**URL**: http://localhost:8082/fhir/ExplanationOfBenefit?patient=Patient/51707

---

## Exercise 7: View Patient Encounters

View all medical visits for a patient.

**URL**: http://localhost:8082/fhir/Encounter?patient=Patient/51707

---

## Exercise 8: View Patient Conditions

View all diagnoses/conditions for a patient.

**URL**: http://localhost:8082/fhir/Condition?patient=Patient/51707

---

## Exercise 9: View Patient Medications

View all medications prescribed to a patient.

**URL**: http://localhost:8082/fhir/MedicationRequest?patient=Patient/51707

---

## Exercise 10: Count Resources

View total count of each resource type.

- Patients: http://localhost:8082/fhir/Patient?_summary=count
- Claims: http://localhost:8082/fhir/ExplanationOfBenefit?_summary=count
- Encounters: http://localhost:8082/fhir/Encounter?_summary=count
- Conditions: http://localhost:8082/fhir/Condition?_summary=count
- Medications: http://localhost:8082/fhir/MedicationRequest?_summary=count

---

## Exercise 11: Search Claims by Date

Find claims created from 2024 onwards.

**URL**: http://localhost:8082/fhir/ExplanationOfBenefit?created=ge2024-01-01

**Try also**:
- Specific year: http://localhost:8082/fhir/ExplanationOfBenefit?created=ge2024-01-01&created=le2024-12-31
- Recent claims: http://localhost:8082/fhir/ExplanationOfBenefit?created=ge2025-01-01

---

## Exercise 12: View All Claim Types

View many claims to see different types (professional, institutional, pharmacy).

**URL**: http://localhost:8082/fhir/ExplanationOfBenefit?_count=100

---

## Exercise 13: View Patient Procedures

View all medical procedures performed on a patient.

**URL**: http://localhost:8082/fhir/Procedure?patient=Patient/51707

---

## Exercise 14: Search Across All Resources

View all available resource types on the server.

**URL**: http://localhost:8082/fhir

---

## Exercise 15: View Web UI

Use HAPI FHIR's built-in web interface for easier browsing.

**URL**: http://localhost:8082

---

## 🔍 Advanced FHIR Search Features

### Exercise 16: Combine Multiple Search Parameters

Search for female patients named "Maria":

**URL**: http://localhost:8082/fhir/Patient?gender=female&name=Maria

Combine patient filter with date range for claims:

**URL**: http://localhost:8082/fhir/ExplanationOfBenefit?patient=Patient/51707&created=ge2024-01-01

---

### Exercise 17: Include Related Resources

Get claims and automatically include the patient details in one response:

**URL**: http://localhost:8082/fhir/ExplanationOfBenefit?_count=5&_include=ExplanationOfBenefit:patient

Get encounters and include the patient:

**URL**: http://localhost:8082/fhir/Encounter?_count=5&_include=Encounter:patient

---

### Exercise 18: Reverse Include (Find Related Resources)

Get a patient and all their claims in one call:

**URL**: http://localhost:8082/fhir/Patient?_id=51707&_revinclude=ExplanationOfBenefit:patient

Get a patient with all encounters:

**URL**: http://localhost:8082/fhir/Patient?_id=51707&_revinclude=Encounter:patient

---

### Exercise 19: Sort Results

Get patients sorted by birth date (oldest first):

**URL**: http://localhost:8082/fhir/Patient?_sort=birthdate&_count=10

Get claims sorted by creation date (newest first):

**URL**: http://localhost:8082/fhir/ExplanationOfBenefit?_sort=-created&_count=10

---

### Exercise 20: Search with Modifiers

Exact name match:

**URL**: http://localhost:8082/fhir/Patient?name:exact=John

Contains search (case-insensitive substring):

**URL**: http://localhost:8082/fhir/Patient?name:contains=mar

Find patients missing phone numbers:

**URL**: http://localhost:8082/fhir/Patient?telecom:missing=true

---

### Exercise 21: Multiple Values (OR Logic)

Search for multiple claim statuses (active OR draft):

**URL**: http://localhost:8082/fhir/ExplanationOfBenefit?status=active,draft

Search for male OR female patients:

**URL**: http://localhost:8082/fhir/Patient?gender=male,female

---

### Exercise 22: Chained Parameters

Find all claims where patient's name contains "Smith":

**URL**: http://localhost:8082/fhir/ExplanationOfBenefit?patient.name=Smith

Find encounters where patient is female:

**URL**: http://localhost:8082/fhir/Encounter?patient.gender=female

---

### Exercise 23: Pagination

Get first page with 5 results:

**URL**: http://localhost:8082/fhir/Patient?_count=5

Use the `link` field in response for next/previous pages:
- Look for `relation: "next"` in the Bundle
- Copy the URL to get the next page

---

### Exercise 24: Complex Query - Patient Health Summary

Get patient with all claims, encounters, and conditions:

**URL**: http://localhost:8082/fhir/Patient?_id=51707&_revinclude=ExplanationOfBenefit:patient&_revinclude=Encounter:patient&_revinclude=Condition:patient

---

### Exercise 25: Search by Identifier

Find claim by its identifier:

**URL**: http://localhost:8082/fhir/ExplanationOfBenefit?identifier=31f9936d-3250-0e03-850e-5af4c5804b72

Find patient by MRN or member ID (if they have identifiers):

**URL**: http://localhost:8082/fhir/Patient?identifier=12345

---

## 🎓 Understanding FHIR Search Parameters

**Basic Search:**
- `?_count=10` - Limit to 10 results
- `?name=John` - Search by name
- `?patient=Patient/123` - Filter by patient
- `?created=ge2024-01-01` - Date greater than or equal
- `?_summary=count` - Return only count, no data

**Advanced Search:**
- `?_include=ExplanationOfBenefit:patient` - Include related resources
- `?_revinclude=ExplanationOfBenefit:patient` - Reverse include
- `?_sort=birthdate` - Sort ascending, `-created` for descending
- `?name:exact=John` - Exact match modifier
- `?name:contains=mar` - Substring search
- `?status=active,draft` - Multiple values (OR logic)
- `?patient.name=Smith` - Chained parameters
- `?identifier=12345` - Search by identifier

---

## ✅ Completion Checklist

**Basic Queries:**
- [ ] Viewed server metadata
- [ ] Listed patients
- [ ] Searched patients by name
- [ ] Viewed specific patient details
- [ ] Explored claims (ExplanationOfBenefit)
- [ ] Viewed patient's complete health record
- [ ] Counted resources
- [ ] Searched by date range
- [ ] Used the web UI

**Advanced FHIR Search:**
- [ ] Combined multiple search parameters
- [ ] Used _include to get related resources
- [ ] Used _revinclude for reverse includes
- [ ] Sorted results
- [ ] Used search modifiers (:exact, :contains)
- [ ] Searched with multiple values (OR)
- [ ] Used chained parameters
- [ ] Tested pagination
- [ ] Searched by identifier
- [ ] Built complex multi-resource queries

**Phase 1 Complete - Ready for Phase 2!** 🎉

---

## Exercise 6: Get Claims for a Specific Patient

```powershell
$patientId = "51707"
$claims = Invoke-RestMethod -Uri "http://localhost:8082/fhir/ExplanationOfBenefit?patient=Patient/$patientId"

Write-Host "Total: $($claims.total)"

$totalCost = 0
foreach ($entry in $claims.entry) {
    $eob = $entry.resource
    $submitted = $eob.total | Where-Object { $_.category.coding[0].code -eq "submitted" }
    if ($submitted -and $submitted.amount.value) {
        $totalCost += $submitted.amount.value
    }
}

Write-Host "Total Healthcare Cost: $"$totalCost
```

---

## Exercise 7: Get Patient's Encounters

```powershell
$patientId = "51707"
$encounters = Invoke-RestMethod -Uri "http://localhost:8082/fhir/Encounter?patient=Patient/$patientId"

Write-Host "Total: $($encounters.total)"
foreach ($entry in $encounters.entry) {
    $enc = $entry.resource
    Write-Host "$($enc.period.start) - $($enc.type[0].text) ($($enc.status))"
}
```

---

## Exercise 8: Get Patient's Conditions

```powershell
$patientId = "51707"
$conditions = Invoke-RestMethod -Uri "http://localhost:8082/fhir/Condition?patient=Patient/$patientId"

Write-Host "Total: $($conditions.total)"
foreach ($entry in $conditions.entry) {
    $cond = $entry.resource
    Write-Host "$($cond.code.text) - $($cond.clinicalStatus.coding[0].code)"
}
```

---

## Exercise 9: Get Patient's Medications

```powershell
$patientId = "51707"
$meds = Invoke-RestMethod -Uri "http://localhost:8082/fhir/MedicationRequest?patient=Patient/$patientId"

Write-Host "Total: $($meds.total)"
foreach ($entry in $meds.entry) {
    $med = $entry.resource
    if ($med.medicationCodeableConcept) {
        Write-Host "$($med.medicationCodeableConcept.text) - $($med.status)"
    }
}
```

---

## Exercise 10: Count All Resources

```powershell
@("Patient", "ExplanationOfBenefit", "Encounter", "Condition", "Procedure", "MedicationRequest") | ForEach-Object {
    $count = (Invoke-RestMethod -Uri "http://localhost:8082/fhir/$_`?_summary=count").total
    Write-Host "$($_.PadRight(25)): $count"
}
```

---

## Exercise 11: Search Claims by Date

```powershell
$eobs = Invoke-RestMethod -Uri "http://localhost:8082/fhir/ExplanationOfBenefit?created=ge2024-01-01"
Write-Host "Claims from 2024: $($eobs.total)"
```

---

## Exercise 12: Analyze Claim Types

```powershell
$eobs = Invoke-RestMethod -Uri "http://localhost:8082/fhir/ExplanationOfBenefit?_count=100"
$typeCount = @{}

$eobs.entry | ForEach-Object {
    if ($_.resource.type.coding) {
        $type = $_.resource.type.coding[0].code
        $typeCount[$type] = ($typeCount[$type] ?? 0) + 1
    }
}

$typeCount.GetEnumerator() | ForEach-Object { Write-Host "$($_.Key): $($_.Value)" }
```

---

## Exercise 13: Complete Patient Summary

```powershell
$patientId = "51707"
$patient = Invoke-RestMethod -Uri "http://localhost:8082/fhir/Patient/$patientId"
Write-Host "$($patient.name[0].given[0]) $($patient.name[0].family)"`n

@("ExplanationOfBenefit", "Encounter", "Condition", "MedicationRequest", "Procedure") | ForEach-Object {
    $count = (Invoke-RestMethod -Uri "http://localhost:8082/fhir/$_`?patient=Patient/$patientId&_summary=count").total
    Write-Host "$_`: $count"
}
```

---

## Exercise 14: Export Data to JSON

```powershell
$patientId = "51707"
Invoke-RestMethod -Uri "http://localhost:8082/fhir/Patient/$patientId" | ConvertTo-Json -Depth 10 | Out-File "patient.json"
Invoke-RestMethod -Uri "http://localhost:8082/fhir/ExplanationOfBenefit?patient=Patient/$patientId" | ConvertTo-Json -Depth 10 | Out-File "claims.json"
Write-Host "Exported to patient.json and claims.json"
```

---

## Exercise 15: Browser Access

- http://localhost:8082/fhir/metadata
- http://localhost:8082/fhir/Patient
- http://localhost:8082/fhir/ExplanationOfBenefit
- http://localhost:8082/fhir/Patient?name=John
- http://localhost:8082/fhir/ExplanationOfBenefit?patient=Patient/51707

---

## 🎓 Key FHIR Search Patterns

```powershell
# Limit results
?_count=10

# Search by parameter
?name=John

# Filter by reference
?patient=Patient/123

# Date comparisons
?created=ge2024-01-01

# Get counts only
?_summary=count
```

---

## ✅ Completion Checklist

- [ ] Retrieved server metadata
- [ ] Listed and searched patients
- [ ] Explored ExplanationOfBenefit (claims)
- [ ] Viewed patient encounters, conditions, medications
- [ ] Counted resources
- [ ] Searched by date range
- [ ] Analyzed claim types
- [ ] Retrieved complete patient records
- [ ] Exported data to JSON
- [ ] Tested browser access

**Ready for Phase 2: Building a Member Access API!** 🎉
