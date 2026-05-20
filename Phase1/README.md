# Phase 1: FHIR Fundamentals & Environment Setup

**Duration**: 2-3 days (4-6 hours total)  
**Goal**: Establish foundational understanding of payer-specific FHIR and set up development environment

---

## 🎯 Learning Objectives

By the end of Phase 1, you will:
- ✅ Understand key FHIR concepts in payer context (refresher)
- ✅ Know the difference between provider-focused and payer-focused FHIR profiles
- ✅ Have a running FHIR server with synthetic payer data
- ✅ Understand CARIN Blue Button and Da Vinci Implementation Guides
- ✅ Grasp architectural patterns for payer systems

---

## 📚 Part 1: FHIR Refresher - Payer Perspective (30-45 min)

### 1.1 Core FHIR Concepts (Quick Review)

Since you have FHIR experience, here's what's **different** in the payer world:

#### **FHIR Resources - Payer Focus**

| Resource | Provider Context | Payer Context |
|----------|------------------|---------------|
| **Patient** | Clinical demographics | Member demographics + enrollment |
| **Encounter** | Clinical visit | Often derived from claims |
| **Observation** | Lab results, vitals | Mostly via claims, less detail |
| **Condition** | Detailed diagnosis | ICD-10 codes from claims |
| **Medication** | Prescriptions | Pharmacy claims (NCPDP) |
| **Procedure** | Clinical procedures | CPT/HCPCS from claims |

**Key Insight**: Payers work with **claims data** (administrative) rather than clinical data. FHIR bridges this gap.

---

### 1.2 Payer-Specific Resources

Resources you'll use heavily that providers rarely touch:

#### **ExplanationOfBenefit (EOB)** ⭐ Most Important
- Represents a claim (professional, institutional, pharmacy, dental)
- Contains: services, costs, adjudication, providers
- Core of Member Access API

```
ExplanationOfBenefit
├── patient (member)
├── insurer (payer)
├── provider
├── type (professional/institutional/pharmacy/dental)
├── item[] (line items)
│   ├── adjudication[] (allowed, paid, copay, etc.)
│   └── revenue code, place of service
├── total[] (submitted, allowed, paid)
└── payment (payment date, amount)
```

#### **Coverage**
- Member's insurance coverage
- Plan details, subscriber info, period
- Referenced by EOB

#### **InsurancePlan**
- Plan metadata (formulary, network)
- Benefits and cost-sharing

---

### 1.3 Implementation Guides - What Are They?

**Implementation Guides (IGs)** are profiles that constrain base FHIR resources for specific use cases.

#### **Key IGs for Payers**:

1. **US Core** (Foundation)
   - Base profiles for US healthcare
   - Patient, Practitioner, Organization, etc.
   - Required by most other IGs

2. **CARIN Blue Button** ⭐ (Member Access)
   - Profiles for EOB (4 types)
3. **Da Vinci Project** (Multiple IGs)
   - **Plan-Net**: Provider directory
   - **Drug Formulary**: Medication coverage
   - **CRD**: Coverage requirements discovery
   - **DTR**: Documentation templates
   - **PAS**: Prior authorization

#### **Why IGs Matter**:
- Base FHIR is too flexible (500+ optional fields)
- IGs define: required fields, value sets, cardinality
- Ensures interoperability between vendors

---

### 1.4 FHIR Profiles Deep Dive

**What is a Profile?**
A profile constrains a base resource:
- Makes optional fields required
- Restricts value sets
- Adds extensions
- Defines must-support elements

**Example**: CARIN Blue Button EOB Profile
```
Base FHIR EOB → Too flexible
  ↓ Profile constraints
CARIN BB Professional EOB → Specific requirements
  - type must be professional
  - patient must be US Core Patient
  - Must include adjudication amounts
  - Must support: identifier, status, type, use, patient, billablePeriod, provider, etc.
```

**Must Support**: Elements marked "must support" mean:
- Payers must populate if data exists
- Consumers must be able to process
- Critical for interoperability

---

## 🔧 Part 2: FHIR Server Setup (45-60 min)

### 2.1 FHIR Server Options for .NET

| Server | Pros | Cons | Use Case |
|--------|------|------|----------|
| **Firely Server** | .NET native, fast, commercial support | Paid for production | ✅ Best for .NET |
| **HAPI FHIR** | Free, feature-rich, popular | Java-based | Testing/learning |
| **Azure FHIR** | Managed, scalable | Azure-only, cost | Production |

**Decision**: We'll use **Firely Server** (formerly Vonk) with free tier for development.

### 2.2 Setup Steps

We'll set up Firely Server locally using their free community edition.

#### **Option A: Firely Server (Recommended)**

**Requirements**:
- .NET 8.0 SDK
- SQL Server or SQLite for storage

**Steps** (We'll do this together):
1. Download Firely Server community edition
2. Configure for SQLite storage
3. Load synthetic payer data
4. Test with Postman/REST Client

#### **Option B: Public Test Server (Quick Start)**

For immediate testing, use public sandboxes:
- **Aidbox**: `https://aidbox.app`
- **HAPI Test Server**: `http://hapi.fhir.org/baseR4`
- **SMART Health IT**: `https://r4.smarthealthit.org`

**Note**: Public servers have shared data and rate limits.

---

### 2.3 Understanding FHIR Server Capabilities

FHIR servers expose capabilities via **CapabilityStatement**:

```
GET [base]/metadata
```

Returns:
- Supported resources
- Search parameters
- Operations ($export, $everything, etc.)
- Security (OAuth endpoints)

**Key Capabilities for Payers**:
- Search (GET with query params)
- Read (GET by ID)
- Create/Update (POST/PUT) - for PAS, DTR
- $export (bulk data)
- $member-match (payer-to-payer)

---

## 📊 Part 3: Understanding Payer Data Model (45 min)

### 3.1 Claims Data vs Clinical Data

**Typical Payer Data Sources**:

```
Legacy Systems                    FHIR API
┌─────────────────┐              ┌──────────────┐
│ Claims System   │──────┐       │              │
│ (X12 837/835)   │      │       │   Facade/    │
├─────────────────┤      ├──────▶│   Mapping    │──▶ FHIR Resources
│ Eligibility     │      │       │   Layer      │
│ System          │──────┤       │              │
├─────────────────┤      │       └──────────────┘
│ Pharmacy Claims │──────┘
│ (NCPDP)         │
└─────────────────┘
```

### 3.2 Claim Types and EOB Profiles

CARIN Blue Button defines 4 EOB profiles:

#### **1. Professional EOB** (CMS 1500)
- Doctor visits, outpatient procedures
- Line items with CPT codes
- Example: Office visit, lab test

#### **2. Institutional (Inpatient) EOB** (UB-04)
- Hospital admissions
- Revenue codes
- Example: Hip replacement surgery

#### **3. Institutional (Outpatient) EOB** (UB-04)
- Hospital outpatient services
- Emergency room, surgery center
- Example: ER visit

#### **4. Pharmacy EOB** (NCPDX)
- Prescription fills
- NDC codes, days supply
- Example: 30-day supply of medication

---

### 3.3 Synthetic Test Data

We'll use **Synthea** - open-source synthetic patient generator.

**What Synthea Provides**:
- Realistic patient demographics
- Clinical history (conditions, meds, procedures)
- Claims data
- **Output**: FHIR bundles ready to load

**Data We'll Generate**:
- 100 patients with various conditions
- Insurance coverage records
- Claims (professional, institutional, pharmacy)
- Provider information

---

## 🏗️ Part 4: Architecture Patterns (45 min)

### 4.1 Facade vs Repository Pattern

Both approaches solve: "How do we expose legacy payer data as FHIR?"

#### **Facade Pattern** (Transform on-the-fly)

```
API Request                     Legacy System
    │                                │
    ↓                                │
┌────────────────┐                  │
│  FHIR Facade   │──────Query──────▶│
│   (Mapping)    │◀─────Data────────│
└────────────────┘                  │
    │                                │
    ↓                                │
FHIR Response
(Transformed in real-time)
```

**Pros**:
- Always current data
- No data duplication
- Faster implementation

**Cons**:
- Performance overhead
- Complex queries challenging
- Depends on legacy system availability

**Best for**: Real-time eligibility, current coverage

---

#### **Repository Pattern** (ETL to FHIR store)

```
Legacy System              FHIR Server
    │                          │
    │──ETL Process (nightly)──▶│
    │   (X12 → FHIR)           │
    │                          │
                               │
API Request                    │
    │                          │
    └──────────────────────────▶
                               │
                    FHIR Response
                 (Direct from store)
```

**Pros**:
- Fast queries
- Rich FHIR capabilities (search, $export)
- Decoupled from legacy

**Cons**:
- Data latency
- Storage costs
- ETL complexity

**Best for**: Member Access (historical claims), Bulk Data

---

#### **Hybrid Approach** ⭐ (Most Common)

```
                    ┌──────────────┐
Real-time Requests  │              │
(Eligibility, PA)──▶│   Facade     │──▶ Legacy Systems
                    │              │
                    └──────────────┘
                    
                    ┌──────────────┐
Historical Requests │              │
(Claims, EOB)──────▶│  Repository  │──▶ FHIR Server
                    │              │
                    └──────────────┘
```

**Strategy**:
- **Facade**: Current eligibility, active coverage, real-time PA
- **Repository**: Historical claims (EOB), analytics, bulk export

---

### 4.2 API Architecture Components

```
┌─────────────────────────────────────────────┐
│           Client (Patient Portal)           │
└─────────────────────────────────────────────┘
                    │
                    ↓ OAuth 2.0 + SMART
┌─────────────────────────────────────────────┐
│         API Gateway / Auth Layer            │
│  - OAuth token validation                   │
│  - Patient matching                         │
│  - Rate limiting                            │
└─────────────────────────────────────────────┘
                    │
                    ↓
┌─────────────────────────────────────────────┐
│         FHIR Business Logic Layer           │
│  - Authorization (patient access control)   │
│  - Validation                               │
│  - Profile enforcement                      │
└─────────────────────────────────────────────┘
                    │
        ┌───────────┴───────────┐
        ↓                       ↓
┌──────────────┐        ┌──────────────┐
│ FHIR Server  │        │   Facade     │
│ (Repository) │        │   Service    │
└──────────────┘        └──────────────┘
        │                       │
        ↓                       ↓
  Historical            Legacy Systems
    Claims             (Real-time data)
```

---

## 🛠️ Part 5: Hands-On Setup (Practical)

### 5.1 Environment Preparation

**What We'll Set Up**:
1. ✅ .NET 8.0 SDK
2. ✅ Firely Server (or test server access)
3. ✅ Synthea for data generation
4. ✅ Postman for testing
5. ✅ Visual Studio Code

### 5.2 Project Structure

```
FHIRPayerProvider/
├── README.md                    # Master plan
├── Phase1/
│   ├── README.md               # This file
│   ├── Setup/                  # Setup scripts
│   ├── Data/                   # Synthetic data
│   └── Exercises/              # Practice exercises
├── Phase2/
│   ├── MemberAccessAPI/        # C# project
│   └── Tests/
├── Phase3/
│   ├── ProviderDirectoryAPI/
│   └── Tests/
... (more phases)
```

---

## 📝 Part 6: Key Concepts to Remember

### 6.1 FHIR Payer Principles

1. **Claims as Source of Truth**: Most payer data comes from claims (X12 837)
2. **Administrative Focus**: Less clinical detail than provider systems
3. **Member-Centric**: Patient is the member with enrollment/coverage
4. **Compliance-Driven**: CMS mandates drive implementation
5. **Interoperability Goal**: Share data with members, providers, other payers

### 6.2 Important Value Sets

Payer-specific code systems you'll encounter:

- **Claim Types**: `http://terminology.hl7.org/CodeSystem/claim-type`
  - professional, institutional, pharmacy, dental
  
- **CPT/HCPCS**: Procedure codes
- **ICD-10**: Diagnosis codes  
- **NDC**: Drug codes
- **Revenue Codes**: Hospital billing
- **Place of Service**: Where care was delivered

### 6.3 Search Parameters for Payers

Critical search capabilities:

```
GET /ExplanationOfBenefit?patient=123
GET /ExplanationOfBenefit?patient=123&service-date=gt2023-01-01
GET /Coverage?patient=123
GET /Patient?identifier=memberID
```

---

## ✅ Phase 1 Deliverables Checklist

Before moving to Phase 2, ensure you have:

- [ ] FHIR server running (local or sandbox access)
- [ ] Loaded synthetic payer data (patients, coverage, EOBs)
- [ ] Successfully retrieved an ExplanationOfBenefit resource
- [ ] Understood the 4 CARIN BB EOB profiles
- [ ] Reviewed US Core Patient and Coverage profiles
- [ ] Understand facade vs repository patterns
- [ ] Postman collection with sample queries ready

---

## 🎯 Practice Exercises

### Exercise 1: Explore a Sample EOB
1. Get an ExplanationOfBenefit from the server
2. Identify: patient, type, service date, paid amount
3. Find line items with procedure codes

### Exercise 2: Compare Profiles
1. Compare base FHIR Patient vs US Core Patient
2. Find required vs optional elements
3. Understand must-support elements

### Exercise 3: Architecture Decision
For these scenarios, choose Facade vs Repository:
- Scenario A: Query last 2 years of claims for a member
- Scenario B: Check if a member is currently eligible
- Scenario C: Bulk export all member data for analytics

---

## 📚 Required Reading

Before starting hands-on implementation:

1. **CARIN BB IG** - Read "Background" and "Use Cases" sections
   - URL: http://hl7.org/fhir/us/carin-bb/

2. **Da Vinci Project Overview**
   - URL: https://www.hl7.org/about/davinci/

3. **CMS Interoperability Rule** - Executive Summary
   - URL: https://www.cms.gov/Regulations-and-Guidance/Guidance/Interoperability/index

**Time**: 30-45 minutes of reading

---

## ✅ Phase 1 Completion Status

### Environment Setup - COMPLETED ✅

**FHIR Server**:
- HAPI FHIR R4 (4.0.1) running on port 8082
- PostgreSQL 15 backend (hapi_payer_provider database)
- Persistent storage configured
- Docker containers healthy and operational

**Synthetic Data**:
- 21 synthetic patients with 2-year medical history
- 2,170 ExplanationOfBenefit records (claims)
- 1,120 Encounters
- 915 Conditions
- 754 Procedures
- 1,050 Medication Requests

**Scripts Created**:
- `Scripts/Start-FHIRServer.ps1` - Start FHIR server
- `Scripts/Stop-FHIRServer.ps1` - Stop FHIR server
- `Scripts/Check-FHIRServer.ps1` - Check server status and resource counts
- `Scripts/Restart-FHIRServer.ps1` - Restart server
- `Phase1/Data/Generate-SimpleData.ps1` - Generate synthetic payer data
- `Phase1/Data/Generate-ClinicalRichData.ps1` - Generate deeper EHR-style data (observations, orders, reports)
- `Phase1/Data/Generate-ServiceRequestsForExistingPatients.ps1` - Add order data for existing patients only
- `Phase1/Data/Generate-WorkflowDataForExistingPatients.ps1` - Add CarePlan, Task, Appointment, Specimen, and MedicationDispense for existing patients
- `Phase1/Data/Load-DataToServer.ps1` - Load data into server
- `Phase1/Data/Explore-FHIRData.ps1` - Explore loaded data

**Access Points**:
- FHIR API: http://localhost:8082/fhir
- Web UI: http://localhost:8082
- Metadata: http://localhost:8082/fhir/metadata

### Quick Commands

```powershell
# Start server
.\Scripts\Start-FHIRServer.ps1

# Check status
.\Scripts\Check-FHIRServer.ps1

# Explore data
.\Phase1\Data\Explore-FHIRData.ps1

# Generate richer clinical dataset
.\Phase1\Data\Generate-ClinicalRichData.ps1 -PopulationSize 150 -YearsOfHistory 8

# Load generated bundles
.\Phase1\Data\Load-DataToServer.ps1

# Add ServiceRequest records for existing patients already in HAPI
.\Phase1\Data\Generate-ServiceRequestsForExistingPatients.ps1 -RequestsPerPatient 3

# Add workflow resources for existing patients already in HAPI
.\Phase1\Data\Generate-WorkflowDataForExistingPatients.ps1

# Stop server
.\Scripts\Stop-FHIRServer.ps1
```

---

## 🚀 Next Steps

Once you complete Phase 1:
1. Mark Phase 1 as complete in master README ✅
2. Move to **Phase 2: Member Access API** 
3. We'll build your first payer FHIR API!

---

## 💡 Questions to Validate Understanding

Before proceeding, you should be able to answer:

1. What's the difference between ExplanationOfBenefit and Claim resources?
2. Why do payers need both facade and repository patterns?
3. What are the 4 types of CARIN BB EOB profiles?
4. What is "must support" and why does it matter?
5. How does claims data (X12) map to FHIR resources?

---

**Phase 1 Status**: ✅ **COMPLETED**  
**Next Phase**: Phase 2 - Member Access API (CARIN Blue Button)
