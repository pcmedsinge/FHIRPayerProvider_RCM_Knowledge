# FHIR Resource Inventory (Phase 1 to Phase 8)

## Scope
This document lists FHIR resource types used across Phase 1 to Phase 8 and highlights missing or weakly documented references.

## Deduplicated Resource List (All Phases)
- Patient
- ExplanationOfBenefit
- Coverage
- Encounter
- Condition
- Procedure
- MedicationRequest
- Practitioner
- PractitionerRole
- Organization
- Location
- HealthcareService
- Questionnaire
- QuestionnaireResponse
- Consent
- Provenance
- InsurancePlan
- ServiceRequest
- Observation
- AllergyIntolerance
- DiagnosticReport
- Immunization
- MedicationKnowledge
- Medication
- Claim

## Phase-wise Coverage

### Phase 1
- Patient
- ExplanationOfBenefit
- Coverage
- Encounter
- Condition
- Procedure
- MedicationRequest
- Practitioner
- Organization
- Location
- Claim

### Phase 2
- Patient
- ExplanationOfBenefit
- Coverage
- Encounter (implied)
- Condition (implied)
- MedicationRequest (implied)
- Practitioner (implied)
- Organization (implied)

### Phase 3
- Practitioner
- PractitionerRole
- Organization
- Location
- HealthcareService

### Phase 4
- InsurancePlan
- MedicationKnowledge
- Medication (implied)
- MedicationRequest (implied)

### Phase 5
- ServiceRequest
- Patient (implied context)
- Coverage (implied context)
- Questionnaire (referenced in workflow)

### Phase 6
- Questionnaire
- QuestionnaireResponse
- Claim
- ServiceRequest (implied)
- Patient (implied)

### Phase 7
- Consent
- Provenance
- ExplanationOfBenefit
- Encounter
- MedicationRequest
- Condition
- Patient (implied)
- Coverage (implied)
- AllergyIntolerance (implied)
- Procedure (implied)
- Observation (implied)
- DiagnosticReport (implied)
- Immunization (implied)

### Phase 8
- Patient
- ExplanationOfBenefit
- Coverage
- Encounter
- MedicationRequest
- Condition
- Procedure
- Observation
- AllergyIntolerance
- DiagnosticReport
- Immunization
- Practitioner
- Organization

## Missing or Weakly Referenced Areas
1. Claim vs ExplanationOfBenefit distinction is not consistently explained in learning docs.
2. Endpoint-related directory capability is not exercised deeply in Phase 3 guides.
3. Observation, DiagnosticReport, and AllergyIntolerance appear strongly in later phases but are weaker in early hands-on coverage.
4. CoverageEligibility style standards are not central in CRD walkthroughs.
5. Medication vs MedicationRequest semantics could be clarified in formulary/member access narratives.

## Quick Metrics
- Total resource types found: 25
- Explicitly implemented or documented: 16
- Implied or indirectly referenced: 9
