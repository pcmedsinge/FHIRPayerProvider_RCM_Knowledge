# Payer/Provider PM — Gap Document & Study Reference

**Purpose**: Single reference doc covering the 4 business-domain gaps not deeply covered in the FHIR workspace. Use this as the starting point for quiz sessions and a quick-reference before the interview.

**Audience for the role**: Product/Project Manager — Healthcare Payer/Provider Consulting
**Code required for these gaps**: Almost none. Conceptual fluency, vocabulary, flow diagrams, and business impact are what matter.

---

## How to Use This Document

1. Read top-to-bottom once for context (60-90 min).
2. Then ask in chat: *"Quiz me on Gap 1"* (or 2/3/4) for interactive practice.
3. Before the interview, re-read the **"Key Terms"** and **"Likely Interview Questions"** sections only — those are your fast-reference cheat sheets.

---

## The Master Workflow Sequence — Read This First

Before diving into any gap, understand the **end-to-end sequence** of how the entire payer-provider ecosystem flows. Everything in this document is a detail within one of these steps.

```
STEP 1 — ENROLLMENT
   Employer / Marketplace / CMS sends 834 (or equivalent)
   → Payer creates member record
   → Member receives ID card
   ↓

STEP 2 — ELIGIBILITY VERIFICATION  ← happens BEFORE service, not after
   Provider checks 270/271 before rendering service
   "Is this patient covered today? What are their benefits/copay?"
   ↓

STEP 3 — SERVICE RENDERED + PRIOR AUTH (if needed)
   Doctor sees patient, documents in EHR
   If high-cost service → prior auth required first (CRD → DTR → PAS)
   ↓

STEP 4 — CLAIM SUBMISSION
   Provider generates EDI 837 → Clearinghouse → Payer
   Acknowledgement: TA1 / 999 / 277CA
   ↓

STEP 5 — ADJUDICATION
   Payer runs edits, pricing, benefit application
   Decision: Paid / Denied / Pended
   ↓

STEP 6 — REMITTANCE
   Payer sends EDI 835 (ERA) back to provider
   EOB generated for member (CARIN BB FHIR EOB — your Phase 2)
   ↓

STEP 7 — CLAIM STATUS INQUIRY (on-demand, not a fixed step)
   Provider queries 276 → Payer responds 277
   Can happen any time after Step 4 while waiting for 835
```

**Common mistake to avoid**: Eligibility (Step 2) happens BEFORE claim submission, not after. Submitting a claim for an ineligible member is a guaranteed denial. Always: enroll → verify → treat → bill → collect.

---

### FHIR Role at Each Step — Where Your Workspace Connects

> **Key principle**: FHIR does NOT replace EDI for claims (837/835 stay). It adds a real-time, API-based layer on top. The one active replacement: prior authorization (Step 3), fax/portal → FHIR PAS, mandated by CMS by 2027.

**Step 1 — Enrollment** *(EDI: 834 — no FHIR replacement)*
- FHIR role: Member views their own **Coverage resource** via Patient Access API — the FHIR read of what the 834 created
- Your phase: Phase 2 MemberAccessAPI

**Step 2 — Eligibility** *(EDI: 270/271 dominant)*
- FHIR role: **Coverage resource** emerging as real-time alternative to 270/271 in some payer APIs
- FHIR role: CRD (Phase 5) reads Coverage at point-of-order to determine whether prior auth is needed
- Your phases: Phase 2 (Coverage exposure), Phase 5 (CRD uses Coverage)

**Step 3 — Prior Authorization** *(EDI: was fax/portal — actively being replaced)*
- FHIR role: **CDS Hooks fires inside EHR** (order-select / order-sign) — your Phase 5 CRDService
- FHIR role: **DTR** auto-populates prior auth form from EHR data — your Phase 6
- FHIR role: **PAS** submits structured prior auth request to payer — your Phase 6 PriorAuthAPI
- Mandate: **CMS-0057-F requires this by Jan 2027**
- Your phases: Phase 5 CRDService + Phase 6 PriorAuthAPI

**Step 3 — Drug Formulary** *(EDI: PBM proprietary — no standard)*
- FHIR role: **Da Vinci Formulary IG** — query drug coverage, tier placement, step therapy requirements
- Your phase: Phase 4 FormularyAPI

**Step 3 — Provider Lookup** *(EDI: no standard)*
- FHIR role: **FHIR Plan-Net** — provider directory, in-network check at point of referral
- Your phase: Phase 3 ProviderDirectoryAPI

**Step 4 — Claim Submission** *(EDI: 837 — not changing)*
- FHIR role: None. 837 stays.

**Step 5 — Adjudication** *(EDI: internal payer system)*
- FHIR role: None in the core adjudication engine.

**Step 6 — Remittance** *(EDI: 835 — not changing)*
- FHIR role: **CARIN BB EOB** — member-facing FHIR view of the adjudicated 835, mandated by CMS-9115-F
- Your phase: Phase 2 MemberAccessAPI

**Step 6 — Payer-to-Payer Exchange** *(EDI: no equivalent)*
- FHIR role: **PDex** — when member switches payers, clinical + claims history transferred in FHIR
- Your phase: Phase 7 PDexAPI

**Step 7 — Claim Status** *(EDI: 276/277)*
- FHIR role: **ClaimResponse resource** — emerging FHIR alternative to 276/277

**Cross-cutting — Population Health** *(EDI: no equivalent)*
- FHIR role: **Bulk Data ($export)** — population-level clinical + claims export at scale; BCDA pattern for CMS
- Your phase: Phase 8 BulkDataAPI

---

### AI / Agentic AI / MCP Role at Each Step — Detailed Use Cases & Tech Stack

---

#### Step 1 — Enrollment

**Use Case 1: 834 File Anomaly Detection**
> Before the 834 file hits the payer's enrollment system, an ML model scans it for errors that will cause downstream claim denials — wrong effective dates, missing Member IDs, duplicate enrollments, impossible date combinations (term date before effective date), plan codes that don't exist in the payer's benefit catalog.
- **ML approach**: Rule-based pre-validation + XGBoost classification model trained on historical 834 error patterns
- **Output**: Error report with severity ranking — critical errors flagged before file processing begins
- **Vendors**: Edifecs Smart Testing, Cognizant EDI validation tools
- **Business value**: Prevents the "member shows up at provider with no coverage" scenario 2 weeks after enrollment

**Use Case 2: Dependent Eligibility Audit**
> ML model runs periodic audits on all enrolled dependents, flagging those who are likely no longer eligible — a "child" dependent who has aged past 26, a spouse after a divorce event, an employee who left but whose dependents are still active.
- **ML approach**: Classification model on demographic data + life event triggers + claims pattern analysis
- **Output**: List of dependents for human verification; confirmed ineligible = 834 termination generated
- **Vendors**: Businessolver, Aon Hewitt Health, Conduent
- **Business value**: Large payers recover $300–500 PMPM per incorrectly enrolled dependent; typical audit recovers millions annually

**Use Case 3: Enrollment Fraud Detection**
> Anomaly detection identifies fraudulent enrollment patterns — ghost employees (fictitious persons enrolled to collect claims), fake dependents, and duplicate member records across plans.
- **ML approach**: Isolation Forest / autoencoders on enrollment attributes; graph analysis to find shared addresses, SSNs, or phone numbers across unrelated members
- **Vendors**: NICE Actimize, SAS Fraud Management, Optum Fraud analytics
- **Business value**: Medicare and Medicaid enrollment fraud costs billions annually; commercial plans face ghost employee schemes

---

#### Step 2 — Eligibility Verification

**Use Case 1: Automated Batch Eligibility — Agentic Workflow**
> The night before clinic, an agentic workflow automatically pulls the next day's appointment schedule from the EHR, fires 270 eligibility checks for every patient, and writes the 271 responses back into the scheduling system. The front desk arrives the next morning to a pre-verified worklist — no manual eligibility calls.
- **Tech stack**: RPA (UiPath / Automation Anywhere) or native EHR integration + 270/271 EDI via clearinghouse API (Availity, Change Healthcare)
- **Agentic pattern**: Scheduled trigger → EHR API pull → batch 270 generation → 271 parse → EHR writeback → exception flagging
- **Vendors**: Waystar Patient Access, Change Healthcare Patient Access Advisor, Epic/Cerner native integrations
- **Business value**: Eliminates ~20 minutes per patient of front-desk eligibility work; flags problems before the patient walks in

**Use Case 2: LLM Member Benefits Chatbot**
> A conversational AI answers member questions about their own coverage in natural language: "What's my deductible remaining?" / "Is Dr. Smith in-network?" / "Do I need a referral for a dermatologist?" — by reading the FHIR Coverage resource in real-time.
- **Tech stack**: LLM (GPT-4 / Claude) + MCP tool-use + your `fhir-mcp-suite` FHIR server. The LLM calls MCP tools: `get_coverage(memberId)`, `get_provider_directory(npi)`, `get_formulary(planId, drug)`
- **Your connection**: This is exactly your `fhir-mcp-suite` in production — the LLM uses FHIR APIs as tools
- **Vendors**: Pager Health, Gyant, League, payer-built on Azure OpenAI / AWS Bedrock
- **Business value**: Reduces member services call volume by 30–40%; 24/7 availability

**Use Case 3: Predictive Eligibility Risk Flagging**
> Before a high-cost scheduled procedure (surgery, chemo, imaging), ML predicts which patients are at elevated risk of an eligibility problem on the date of service — recently changed employers, plan year ending soon, COBRA about to expire.
- **ML approach**: Logistic regression / gradient boosting on enrollment history, event triggers (employer change, recent 834 transactions), days until plan year end
- **Output**: "High risk" flag in scheduling system → patient access team proactively verifies with the patient
- **Business value**: Avoids uncompensated care; reduces day-of-service surprises that lead to patient complaints

---

#### Step 3 — Prior Authorization

**Use Case 1: NLP Auto-Population of DTR Questionnaire**
> When a prior auth is triggered (CRD fires a "PA Required" card), the LLM reads the patient's clinical notes in the EHR and automatically answers the DTR (Documentation Templates and Rules) questionnaire — extracting the diagnosis, prior treatments tried, relevant lab values, and clinical indicators.
- **Tech stack**: NLP / LLM (GPT-4, Claude, Llama fine-tuned) + FHIR DTR IG + your `fhir-mapping-agent`
- **Your connection**: This is precisely what your `fhir-mapping-agent` does — it maps unstructured clinical text to structured FHIR data elements
- **Vendors**: Nuance DAX for PA, Cohere Health, Myndshft
- **Business value**: Reduces authorization coordinator time from 20 min to 2 min per request; 88% of DTR answers can be auto-populated from existing EHR data (Da Vinci research)

**Use Case 2: ML Prior Auth Approval Prediction (Sync Path)**
> At PAS submission time, an ML model scores the request: what is the probability this PA will be approved given this diagnosis, procedure, member plan, provider, and clinical criteria? High-confidence approvals are returned synchronously (auto-approve). Low-confidence are pended for human review.
- **ML approach**: Gradient boosting (XGBoost / LightGBM) trained on millions of historical PA decisions. Features: CPT code, ICD-10, member plan, provider NPI, prior treatment history, InterQual criteria match score
- **Tech stack**: ML model → served via REST API → called by PAS intake service → routes to sync path (auto-approve) or async path (Kafka queue + human review)
- **Vendors**: Cohere Health, Olive AI (acquired by Waystar), Myndshft, payer-built models
- **Business value**: 30–40% of PA requests can be auto-approved with > 95% accuracy; eliminates days of delay for routine approvals

**Use Case 3: Full Agentic Prior Auth Workflow**
> The complete end-to-end agentic loop — zero human touchpoints for routine cases:
```
Doctor writes order in Epic
        ↓
CDS Hook fires → your CRDService evaluates CQL rules
        ↓
"PA Required" card returned to Epic (< 5 seconds)
        ↓
DTR questionnaire launched in Epic → fhir-mapping-agent reads
EHR and auto-answers 90% of questions
        ↓
Provider reviews + confirms in 2 minutes (not 20)
        ↓
PAS bundle submitted → ML model scores → auto-approve
        ↓
Decision returned to Epic inline — no portal, no fax, no phone call
```
- **Tech stack**: CDS Hooks + FHIR R4 + CQL rules engine (Drools) + LLM (DTR auto-population) + XGBoost (approval scoring) + FHIR Subscription (async notifications)
- **Your connection**: CRDService (Phase 5) + PriorAuthAPI (Phase 6) is the FHIR substrate that makes this workflow possible
- **Business value**: Reduces PA turnaround from 3 days (phone/fax) to minutes; CMS-0057-F mandates this by Jan 2027

**Use Case 4: Payer-Side AI Clinical Reviewer**
> For pended PA requests, an LLM retrieves the submitted clinical documentation, cross-references it against InterQual criteria, generates a structured clinical summary with evidence for and against approval. The payer's clinical reviewer reads the AI summary (2 min) rather than raw documents (15 min).
- **Tech stack**: LLM + RAG (Retrieval Augmented Generation) — clinical notes + InterQual criteria loaded as context. Output: structured summary with key clinical facts highlighted
- **Vendors**: Navina, MDPortals, Aidoc for PA, payer-built on Azure OpenAI
- **Business value**: Clinical reviewer capacity increases 5–8x; faster response time improves CMS compliance on PA turnaround timelines

---

#### Step 4 — Claim Submission

**Use Case 1: Pre-Submission Denial Prediction**
> Before the 837 leaves the provider's billing system, an ML model scores each claim line for denial probability and predicts the most likely denial reason code. The billing team sees a "Risk: High — likely CARC 197 (auth required)" alert and can fix the claim before it goes to the payer.
- **ML approach**: XGBoost / LightGBM trained on millions of historical claims + denial outcomes. Features: CPT code, ICD-10, payer ID, NPI, place of service, prior auth status, member plan, billed amount
- **Vendors**: Waystar Claim Confidence, Change Healthcare Assurance (Optum), Availity Claim Status Prediction, Infinx
- **Business value**: Reducing denial rate by 5% on $500M annual billed charges = $25M recovered revenue; ROI measured in weeks

**Use Case 2: Ambient Medical Coding (AI-Assisted)**
> NLP/LLM listens to or reads the clinical encounter (doctor's notes, dictation, or ambient recording), then suggests ICD-10-CM diagnoses, CPT procedure codes, E/M level (office visit complexity), and HCPCS codes. The human coder reviews and confirms rather than creating from scratch.
- **Tech stack**: Ambient AI (microphone in exam room) → ASR (automatic speech recognition) → LLM → code suggestion. OR: clinical note NLP → code suggestion
- **Vendors**: Nuance DAX Copilot (Microsoft), AWS HealthScribe, Suki AI, Abridge, Ambience Healthcare
- **Business value**: Coder productivity increases 30–50%; coding accuracy improves; documentation burden on physicians reduced (major burnout driver)

**Use Case 3: Clinical Documentation Improvement (CDI)**
> AI reviews clinical documentation before it reaches the coding team, flagging specificity gaps that will lead to less specific (lower-reimbursed) codes or missed HCC opportunities: "Note mentions 'heart failure' — specify systolic vs diastolic for correct DRG." "Diabetes documented — add neuropathy/nephropathy if present for HCC capture."
- **Tech stack**: NLP (AWS Comprehend Medical, Google Healthcare NLP, spaCy) + knowledge graph of ICD-10 hierarchy + HCC mapping rules
- **Vendors**: 3M CDI, Dolbey Fusion CDI, Optum CDI Engage, Nuance CDI
- **Business value**: Improves DRG accuracy (inpatient revenue), HCC capture for MA plans, and reduces post-payment audit risk

---

#### Step 5 — Adjudication

**Use Case 1: AI-Assisted Pend Review**
> Instead of a human claims examiner reading every pended claim from scratch, an ML model pre-classifies each pend: likely approve / likely deny / needs clinical review. An LLM summarizes the relevant clinical documentation in 3 bullets. The examiner reviews the summary, not the raw 80-page medical record.
- **ML approach**: Multi-class classification (approve/deny/escalate) + NLP summarization. Trained on historical pend outcomes + clinical documentation
- **Tech stack**: ML classification model + LLM (GPT-4 / Claude) for summarization + RAG on clinical documents
- **Vendors**: MultiPlan, Cotiviti AI, payer-built on Azure OpenAI / AWS Bedrock
- **Business value**: Examiner handles 3–5x more pends per day; auto-resolution of low-complexity pends reduces queue by 30–40%

**Use Case 2: Real-Time Fraud Detection**
> ML analyzes the incoming claim stream in real-time for fraud patterns — unbundling (billing separately for services that should be bundled), upcoding (billing a higher code than rendered), phantom billing (services never rendered), and provider network fraud (kickback rings).
- **ML approach**: Multiple models in parallel:
  - **Isolation Forest** — flags statistical outliers (provider billing pattern suddenly spikes)
  - **Graph Neural Networks** — detects provider networks that route patients between each other for unnecessary services
  - **LSTM (Long Short-Term Memory)** — temporal pattern detection (provider's billing pattern changes over time)
- **Vendors**: Cotiviti Fraud & Abuse, Optum Payment Integrity, Equian (now part of MultiPlan), SAS Health Analytics
- **Business value**: Healthcare fraud costs the US $100B+ annually; ML-based detection recovers 3–5x more than rule-based systems

**Use Case 3: Payment Integrity / Overpayment Detection**
> Post-adjudication, AI scans paid claims for overpayments — duplicate payments, payments above contracted rate, payments for excluded procedures, COB failures where secondary paid when primary should have.
- **ML approach**: Rules + ML anomaly detection on payment amounts vs fee schedule. NLP on clinical records to verify billed procedures were actually documented.
- **Vendors**: Cotiviti, Equian/MultiPlan, Optum, nThrive
- **Business value**: Large payers recover $500M–$1B+ annually through payment integrity programs

---

#### Step 6 — Remittance

**Use Case 1: EOB Plain Language Explanation (LLM + MCP)**
> Member opens their payer app and sees their Explanation of Benefits. Instead of raw CARC codes and dollar amounts, an LLM reads their FHIR EOB resource and explains it in plain English: *"Your insurance paid $450 for your MRI on May 5. You owe $80. This applied to your deductible — you have $300 remaining before insurance covers 100%."*
- **Tech stack**: LLM (GPT-4 / Claude) + MCP tool-use + your `fhir-mcp-suite`. The LLM calls `get_eob(memberId, claimId)` as an MCP tool, reads the structured FHIR EOB, generates plain English
- **Your connection**: Exactly what your `fhir-mcp-suite` enables — structured FHIR data as LLM tool input
- **Vendors**: Pager Health, League, Accolade, payer portals built on Azure OpenAI
- **Business value**: Reduces member services call volume ("what does this bill mean?"); improves member satisfaction / Star Ratings

**Use Case 2: Automated Appeal Letter Generation**
> When a claim is denied, the RCM platform reads the CARC/RARC denial codes from the 835, retrieves the relevant clinical documentation from the EHR, and drafts an appeal letter tailored to the specific denial reason — with the relevant clinical evidence already cited.
- **Tech stack**: LLM + RAG (clinical notes retrieved from EHR FHIR API as context) + CARC/RARC code knowledge base. Your `fhir-mcp-suite` can be the retrieval layer.
- **Vendors**: Waystar Appeals AI, Infinx, Omega Healthcare, several RCM-focused startups
- **Business value**: Appeal letter drafting time drops from 45 min to 5 min; higher appeal overturn rates because evidence is better cited

**Use Case 3: Underpayment Detection**
> AI compares the paid amount on every 835 line against the provider's contracted rate in the fee schedule. Flags lines where the payer paid less than contracted — systematic underpayment is common, especially after contract renegotiations where fee schedules aren't updated in the payer system.
- **ML approach**: Rule lookup (paid amount vs fee schedule) + ML to detect systematic patterns across payers or service lines
- **Vendors**: nThrive, Cotiviti, Optum360
- **Business value**: Underpayments are estimated at 1–3% of revenue; at a $1B health system that's $10–30M/year left uncollected

---

#### Step 7 — Claim Status

**Use Case 1: Proactive AR Monitoring Agent**
> Instead of RCM staff manually checking claim status daily, an agentic AI continuously monitors all open AR records, fires 276 status inquiries at optimal intervals, parses 277 responses, and only alerts a human when action is required: "Claim #XYZ pended — additional documentation needed by June 1. Timely filing expires June 15."
- **Tech stack**: Workflow automation engine (Apache Airflow, AWS Step Functions) + 276/277 EDI integration + FHIR ClaimResponse polling + FHIR Subscription (push when available) + notification system
- **Agentic pattern**: Monitor → evaluate → alert only on exceptions (not routine status checks)
- **Vendors**: Waystar, Availity, R1 RCM platform, nThrive
- **Business value**: RCM staff handles 3–4x more accounts; nothing falls through the cracks to timely filing write-off

**Use Case 2: AR Prioritization ML Model**
> ML model scores every open claim by recovery probability and urgency, producing a daily prioritized worklist for the RCM team: work these 50 claims first, in this order. Factors in: days in AR, denial history for this payer/code combination, timely filing proximity, dollar amount, prior follow-up attempts.
- **ML approach**: Multi-factor scoring model (logistic regression + business rules). Trained on historical AR resolution outcomes.
- **Vendors**: Waystar Insights, Netsmart, Health Catalyst
- **Business value**: RCM teams working the highest-value, highest-risk claims first instead of oldest-first or random order

---

#### Cross-cutting — Population Health & Analytics

**Use Case 1: Risk Stratification for Care Management**
> ML model scores every member in the population on likelihood of high-cost events in the next 12 months: hospitalization, ED visit, readmission. High-risk members are automatically assigned to care managers for outreach.
- **ML approach**: Gradient boosting (XGBoost) on FHIR Bulk Data export — diagnoses, medications, claims history, utilization patterns, SDOH signals. Your Phase 8 BulkDataAPI feeds this pipeline.
- **Vendors**: Optum One, Arcadia, Health Catalyst, Innovaccer, IBM Merative
- **Business value**: Proactive care management reduces hospitalization rates 15–25% for high-risk members; key to Medicare Advantage Star Ratings

**Use Case 2: HCC Risk Capture via NLP**
> NLP reads clinical notes from the FHIR Bulk Data export to find undocumented or undercoded HCC conditions — e.g., a doctor mentioned "diabetic retinopathy" in a note but didn't code it on the claim. The system flags it for the coding team to add on the next encounter.
- **Tech stack**: NLP (AWS Comprehend Medical, Google Healthcare NLP API, spaCy + custom models) + FHIR Bulk Data ($export) + HCC mapping tables
- **Vendors**: Cognizant HCC Risk Capture, Optum HCC Coding, Datavant, Ciox Health (IOD)
- **Business value**: MA plans receive higher CMS capitation for accurately coded sicker members; undercoding = lost revenue of $500–$2,000 per missed HCC per member per year

**Use Case 3: HEDIS Gap-in-Care Detection**
> AI identifies members who are overdue for preventive services required by HEDIS measures — mammogram, colorectal screening, A1c testing for diabetics, well-child visits. Triggers automated outreach (text, phone, portal message).
- **Tech stack**: HEDIS measure specifications encoded as rules/ML + FHIR Bulk Data (claims + clinical) + outreach workflow automation
- **Vendors**: Arcadia, Cotiviti HEDIS, Innovaccer, Lightbeam Health
- **Business value**: Closing care gaps improves HEDIS scores → improves CMS Star Ratings → increases MA bonus payments (up to 5% revenue bonus for 5-star plans)

**Use Case 4: SDOH Prediction and Intervention**
> ML identifies members at risk due to Social Determinants of Health — food insecurity, housing instability, transportation barriers — from claims patterns (frequent ED visits for non-emergency issues, missed appointments) combined with external data sources. Triggers referral to community resources.
- **ML approach**: Multi-source model: claims patterns + census/zip code data + external SDOH databases. Gradient boosting or neural net.
- **Vendors**: Findhelp (formerly Aunt Bertha), Unite Us, NowPow, payer analytics platforms
- **Business value**: SDOH interventions reduce downstream medical costs; increasingly required by CMS value-based contracts

---

#### Quick Tech Stack Reference

| AI Technique | Healthcare Application | Example Vendors/Tools |
|---|---|---|
| **XGBoost / LightGBM** | Denial prediction, PA approval scoring, risk stratification | Waystar, Cohere Health, Optum |
| **NLP (spaCy, Comprehend Medical)** | HCC capture, CDI, PA documentation extraction | AWS, Google, Ciox/IOD |
| **LLM (GPT-4, Claude, Llama)** | EOB explanation, appeal drafting, DTR auto-population | Azure OpenAI, AWS Bedrock |
| **Ambient ASR + LLM** | Medical coding from clinical encounter audio | Nuance DAX, AWS HealthScribe, Suki |
| **Graph Neural Networks** | Fraud network detection | Cotiviti, SAS |
| **LSTM / time-series** | Temporal billing anomaly detection | Optum Payment Integrity |
| **Isolation Forest** | Statistical outlier fraud flags | SAS, Cotiviti |
| **RAG (Retrieval Augmented Generation)** | Appeal letters, clinical review summaries | Azure OpenAI + FHIR as vector store |
| **Agentic AI (tool-use)** | Prior auth end-to-end, AR monitoring, eligibility orchestration | Your fhir-mcp-suite, LangChain, AutoGen |
| **CQL + rules engine (Drools)** | PA criteria evaluation in CRD | Your CRDService Phase 5 |
| **FHIR Subscription** | Push notifications for async PA status | Your PriorAuthAPI Phase 6 |

#### The MCP Layer — Why It Matters

**MCP (Model Context Protocol)** is the standard that lets LLMs (like GPT-4, Claude) call structured tools — including FHIR API endpoints — as part of a reasoning chain. Your `fhir-mcp-suite` is a server that exposes FHIR queries as MCP tools.

```
User asks: "What medications is patient John covered for and what are the prior auth requirements?"
        ↓
LLM receives question
        ↓
LLM calls MCP tool: search_patient(name="John")
        ↓
LLM calls MCP tool: get_coverage(patientId="123")
        ↓
LLM calls MCP tool: get_formulary(planId="XYZ", drug="Humira")
        ↓
LLM synthesizes: "Humira is covered at Tier 4. Step therapy required:
  must try methotrexate first. Prior auth needed after step therapy."
```

This is the **agentic AI pattern** applied to the payer-provider workflow — the LLM orchestrates multiple FHIR calls across Steps 2, 3, and 4 and produces a unified answer. This is your `fhir-mcp-suite` + `fhir-mapping-agent` in action.

**Interview framing**: *"I've built the FHIR substrate that makes agentic AI in healthcare possible. The mandated APIs — Coverage, Formulary, Prior Auth, EOB — are now exposed as structured MCP tools. An LLM can orchestrate across all of them in a single reasoning chain to answer questions that previously required a clinician to log into 3 different portals."*

---

## Bridge to Your Existing Strengths

Always connect a gap back to what you've already built. Cheat sheet of connections:

| Gap Domain | Your Existing Work That Connects |
|---|---|
| Claims adjudication | CARIN Blue Button / EOB API — the member-facing surface of adjudicated claims |
| Enrollment | Coverage resource, Member Access API — depends on enrollment being right |
| Medicare Advantage | Bulk Data API (BCDA is literally CMS for MA plans), risk adjustment, Star Ratings tie to HEDIS |
| HEDIS / Care Mgmt / UM | **Your entire CRD → DTR → PAS workflow IS modern Utilization Management** |
| AI on top of mandates | `fhir-mcp-suite`, `fhir-mapping--agent` — applied AI on CMS-mandated FHIR APIs |

---

# GAP 1 — Claims Management & Adjudication Lifecycle

## The End-to-End Flow (Memorize)

1. **Provider submits claim** → EDI **837** transaction
   - **837P** = Professional (physician office)
   - **837I** = Institutional (hospital)
   - **837D** = Dental
2. **Clearinghouse** (Change Healthcare, Availity, Waystar) — scrubs format, validates, routes to correct payer
3. **Acknowledgement** → clearinghouse sends **999/997 TA1** back to provider (see Acknowledgement section below)
4. **Payer intake** → claim enters core adjudication system (Facets, QNXT, HealthRules, HealthEdge)
5. **Eligibility check** — was member active on date of service?
6. **Clinical / coding edits** — CPT, ICD-10, HCPCS codes valid? Medical necessity? Bundling rules (NCCI edits)?
7. **Pricing** — applies contracted rate, fee schedule, or DRG (Diagnosis-Related Group for inpatient)
8. **Benefit application** — deductible, copay, coinsurance, OOP max
9. **Adjudication decision** — Paid / Denied / Pended / Partial pay
10. **Payment + Remittance** → EDI **835 (ERA — Electronic Remittance Advice)** back to provider
11. **EOB** generated for member (this is where your CARIN Blue Button EOB resource lives)

---

## Acknowledgement & Fallback Mechanism in 837 Workflow

This is the error-handling and confirmation layer that most people don't talk about but every payer operations team deals with daily.

### Three Levels of Acknowledgement

```
Provider sends 837
        ↓
[CLEARINGHOUSE]
        ↓ immediately sends back:
   TA1 — Interchange Acknowledgement
   "I received your file. The envelope (ISA/IEA) is readable."
        ↓ then sends:
   999 (or older 997) — Functional Acknowledgement
   "Your transaction set passed/failed format validation."
        ↓ if passed, forwards to payer
[PAYER]
        ↓ payer sends back:
   277CA — Claim Acknowledgement
   "I received the claim. Here is each claim's status: Accepted / Rejected"
```

| Acknowledgement | Who Sends It | What It Confirms |
|---|---|---|
| **TA1** | Clearinghouse | File envelope is readable (ISA/IEA level) |
| **999 / 997** | Clearinghouse | Transaction format is valid (ST/SE level) |
| **277CA** | Payer | Individual claim accepted or rejected by payer |

### Fallback / Error Handling

- **Rejected at clearinghouse (999 reject)**: Provider's billing system must fix the format error and resubmit
- **Rejected at payer (277CA reject)**: Claim failed payer-specific edits — wrong NPI, invalid member ID, missing required field. Must be corrected and resubmitted.
- **No acknowledgement received**: Provider billing system detects timeout → triggers resubmission or manual follow-up
- **Timely filing rule**: If provider doesn't resubmit within the payer's timely filing window (typically 90-180 days from date of service), the claim is automatically denied — no appeal possible
- **Duplicate claim detection**: Payers run duplicate edits; accidental resubmission gets rejected with a duplicate denial code

### Who Monitors This?
The **Revenue Cycle Management (RCM) team** at the provider side. They run daily exception reports from the billing system to catch all 999/277CA rejects and work them before timely filing expires.

---

## Who Is the Payer? (Not Always Who You Think)

Yes — the payer is the insurance company (UnitedHealth, Aetna, BCBS, Cigna, Humana). But "payer" is often more complex than a single entity:

| Scenario | Who Acts as Payer |
|---|---|
| Fully-insured commercial plan | Insurance company bears all risk and adjudicates |
| **Self-funded employer plan (ASO)** | **Employer bears the financial risk. Insurance company (e.g., Aetna) acts as administrator only** — processes claims, but employer money pays them |
| Medicare Advantage | Private insurer (Humana, UHC) receives CMS capitation, adjudicates, pays providers |
| Medicare FFS | CMS directly — through MACs (Medicare Administrative Contractors) |
| Medicaid MCO | State contracts with MCO (Centene, Molina) — MCO adjudicates |

**The most important nuance**: The majority of large employer plans in the US are **self-funded (ASO — Administrative Services Only)**. The insurer just processes and manages; the employer writes the check. This is why an employer can customize benefits — it's their money. The insurance brand name you see on the card may just be the administrator, not the financial risk bearer.

### Who Actually Writes the Check?

| Plan Type | Who Bears Financial Risk | Who Physically Pays the Provider |
|---|---|---|
| Fully-insured commercial | Insurance company | Insurance company |
| **Self-funded / ASO** | **Employer** | Insurance company draws from employer's account |
| Medicare FFS | US Federal Government | MACs (Medicare Admin Contractors) on behalf of CMS |
| Medicare Advantage | MA plan (private insurer) — within CMS capitation | MA plan pays providers |
| Medicaid MCO | State + Federal government fund it | MCO pays providers |

> In a self-funded plan, the insurance company's name is on the card but it is acting purely as an administrator. If an employee has a $100,000 hospital claim, it comes out of the **employer's** bank account — not Aetna's. Aetna just processes the paperwork. This is why large self-funded employers have enormous leverage over plan design and benefits.

### The Two Models — How They Actually Work

**Model 1: Fully-Insured (What Most People Assume)**
```
Employer pays PREMIUM to Insurance Company every month
        ↓
Insurance Company pools premiums from thousands of employers
into ONE risk pool
        ↓
Claim comes in → Insurance Company pays from that pooled money
        ↓
Insurance Company bears the risk
If claims exceed premiums → Insurance Company loses money
```
The employer's obligation ends when they pay the premium. After that it is the insurance company's money.

**Model 2: Self-Funded / ASO (Majority of Large Employers)**
```
Employer pays small ADMIN FEE to insurance company (~$50 PMPM)
just for administration services — NOT a premium
        ↓
Insurance Company adjudicates the claim exactly as normal
        ↓
But draws the payment from the EMPLOYER'S OWN bank account
        ↓
Employer bears the risk — high claims = employer pays more
```
The insurance company never owned the money. They are purely a processing agent.

**The Simple Analogy**
> **Fully-insured** = You hire a contractor who buys all materials AND builds the house. You pay one fixed price. If materials cost more, that's their problem.
>
> **Self-funded** = You hire a contractor for labor only. YOU buy all the materials. If materials cost more, that comes out of YOUR pocket.
>
> The insurance brand (Aetna, UHC) is the contractor. The employer buys the materials (funds the claims) in a self-funded plan.

### Why Large Employers Choose Self-Funded
- **Cost savings** — no insurance company profit margin or state premium taxes
- **Data ownership** — they see every claim their employees make
- **Plan customization** — design benefits exactly as they want
- **Stop-loss protection** — buy separate stop-loss insurance to cap catastrophic exposure (e.g., self-pay up to $500K per member, stop-loss covers above that)

### Scale of Self-Funding
- Almost all Fortune 500 companies are self-funded
- Most companies with 500+ employees
- ~65% of covered workers in the US are in self-funded plans
- Small companies almost always fully-insured (cannot absorb claim risk)

### The 835 Remittance — Precise Answer
The provider doesn't know or care which model it is — they just receive the 835 and the payment either way.

| Model | Who Sends 835 to Provider | Whose Money Funds the Payment |
|---|---|---|
| Fully-insured | Insurance company | Insurance company's pooled funds |
| Self-funded (ASO) | Insurance company (still processes it) | **Employer's bank account** |
| Medicare FFS | MAC (Medicare Admin Contractor) | US Federal Government (CMS) |
| Medicare Advantage | MA Plan | CMS capitation funds |

---

## The Clearinghouse — More Than Just a Forwarder

Your instinct is mostly right — but they do more than just map and route:

```
Provider Billing System → CLEARINGHOUSE → Payer
```

What the clearinghouse actually does:
1. **Format validation** — checks the 837 conforms to X12 spec
2. **Companion guide compliance** — applies payer-specific rules (each payer publishes its own companion guide with extra requirements)
3. **Routing** — determines which payer to send to based on payer ID in the file
4. **Translation** — if provider sends one version, payer expects another, clearinghouse translates
5. **Acknowledgement management** — sends 999/TA1 back to provider, collects 277CA from payer
6. **Reporting / dashboard** — providers see claim status, rejection reports
7. **Value-add services** — eligibility checking (270/271), claim status inquiry (276/277), ERA delivery (835)

Think of clearinghouses as the **postal + customs service** for healthcare claims — they don't open or change the content, but they ensure it's in the right envelope, addressed correctly, and conforming to the destination country's rules.

---

## What FHIR Handles in This Workflow (2026)

FHIR does NOT replace EDI claim submission. But it touches several parts of the surrounding workflow:

| Workflow Step | Still EDI | FHIR Role in 2026 |
|---|---|---|
| Claim submission | 837 — not changing | No role |
| Remittance | 835 — not changing | No role |
| **Prior authorization** | Was fax/portal | **FHIR PAS actively replacing** (CMS-0057 deadline 2027) |
| Eligibility check | 270/271 dominant | FHIR Coverage resource emerging in some systems |
| Member views their EOB | Not possible in EDI | **FHIR CARIN BB EOB — mandated, live** |
| Claim status inquiry | 276/277 EDI | FHIR ClaimResponse emerging |
| Payer-to-payer clinical exchange | No EDI equivalent | **FHIR PDex — mandated, live** |
| Clinical data for prior auth docs | Fax / manual | **FHIR DTR auto-population — your Phase 5/6** |

**Bottom line for 2026**: FHIR surrounds the claims transaction but doesn't replace the core 837/835. The biggest active replacement is prior authorization.

---

## HCC Codes in the Claims Workflow

Yes — HCC (Hierarchical Condition Categories) codes are directly connected to claims, but they operate at the **analytics layer above adjudication**, not inside the 837 transaction itself.

```
Provider submits 837 → contains ICD-10 diagnosis codes
                                    ↓
              Payer adjudicates and stores the claim
                                    ↓
              Risk Adjustment engine reads ICD-10 codes
              Maps them to HCC categories
                                    ↓
              HCC score calculated for member
                                    ↓
              Submitted to CMS (via RAPS/EDPS) for MA plans
                                    ↓
              CMS adjusts monthly capitation payment to payer
```

- The **837 carries ICD-10 codes** — those are the raw clinical codes
- **HCC mapping** happens AFTER adjudication — a separate process

### RAPS and EDPS — What They Are

These are the two submission systems MA plans use to send risk adjustment data to CMS:

**RAPS (Risk Adjustment Processing System)**
- The **legacy** system — has been in use since MA began
- MA plan extracts diagnosis codes from adjudicated claims
- Submits a RAPS file to CMS: member ID + diagnosis codes + dates of service
- CMS processes it, maps ICD-10 codes to HCC categories, calculates risk score
- Being phased out in favor of EDPS

**EDPS (Encounter Data Processing System)**
- The **modern replacement** — CMS's shift to full encounter data
- Instead of just sending diagnosis codes (RAPS), the MA plan sends the **entire encounter record** — all fields from the adjudicated claim
- Much more granular — CMS can see the full clinical and billing picture
- CMS can audit more easily because they have the complete encounter, not just the codes
- **RADV audits** (Risk Adjustment Data Validation) use EDPS data to verify HCC codes are supported by actual medical records

**Why This Matters**
```
RAPS:  "Member 12345 had Diabetes Type 2 (ICD E11.9) on Jan 15"
EDPS:  Full claim record — provider, facility, dates, all codes,
       all line items, procedure codes, everything
```
EDPS gives CMS much more power to detect inaccurate or inflated HCC coding — which is why payers have had to tighten their risk adjustment practices significantly.

### Why Risk Adjustment Only Applies to Certain Programs

- **Medicare Advantage**: CMS pays MA plans a capitated PMPM. Sicker members = higher payment. Without risk adjustment, plans would avoid enrolling sick people (adverse selection). HCC scoring ensures CMS pays fairly based on actual member health.

- **Medicaid Managed Care**: State pays MCO a capitated rate per member. Same logic — risk adjustment ensures the MCO is paid fairly for managing a sicker-than-average population.

- **NOT applicable to commercial fully-insured FFS**: Insurer collects premiums from a large pool and pays claims as they come. No capitation, no per-member government payment, no need for CMS risk scoring. Denials and medical necessity rules govern cost, not capitation adjustment.

- **NOT applicable to commercial self-funded (ASO)**: Employer pays actual claims as incurred. No capitation at all — they pay what was billed (minus contracted rates). HCC is irrelevant.

**Simple rule**: If CMS or a state is paying a flat per-member fee to a private plan → risk adjustment and HCC coding matter enormously. If it's straight fee-for-service or employer-funded → HCC is irrelevant to the payment model.

---

## Who Does the Laborious Provider-Side Work?

You are absolutely correct — providers have a massive administrative burden beyond clinical care. Here's who handles it:

### At a Large Hospital / Health System
```
Clinical Team (Doctors, Nurses)
        ↓ document in EHR (Epic, Cerner, Oracle Health)
Medical Coders (HIM — Health Information Management)
        ↓ translate clinical documentation to ICD-10, CPT codes
Charge Capture Team
        ↓ ensure all billable services are captured
Patient Access / Front Desk
        ↓ collect insurance info, verify eligibility (270/271)
Billing Team / RCM Team
        ↓ generate 837, submit to clearinghouse, work rejections
Denial Management Team
        ↓ appeal denied claims, track underpayments
```

### At a Small Practice (1-5 Doctors)
They often outsource the entire billing/RCM function to a **third-party RCM company** (Optum, R1 RCM, Conifer Health, nThrive). The doctor just sees patients and signs off. The RCM company does everything else for a % of collections (typically 4-8%).

### The Key Roles
- **Medical Coder** — translates clinical documentation to billing codes (ICD-10, CPT, HCPCS)
- **RCM Specialist / Biller** — submits claims, works rejections and denials
- **Authorization Coordinator** — handles prior auth requests (your CRD/DTR/PAS world)
- **Denial Management Specialist** — appeals denied claims

---

## How AI Helps in 2026 — Claims Workflow

AI is actively being used across the claims workflow:

| Area | AI Application | Status in 2026 |
|---|---|---|
| **Prior auth automation** | NLP extracts clinical criteria from notes, auto-populates FHIR DTR forms | Active — your `fhir-mapping-agent` is exactly this |
| **Medical coding** | AI suggests ICD-10/CPT codes from clinical notes (ambient AI) | Active — Nuance DAX, Suki, AWS HealthScribe |
| **Denial prediction** | ML model predicts which claims will be denied before submission, fix first | Active — Change Healthcare, Availity, Waystar |
| **Denial appeals** | AI drafts appeal letters from clinical documentation | Active — several startups |
| **Claim scrubbing** | AI-enhanced edits beyond rule-based — catches errors rules miss | Active |
| **HCC risk capture** | NLP reads clinical notes to find undocumented HCC conditions | Active — Cognizant, Optum, Datavant |
| **Fraud detection** | ML on claim patterns to detect anomalies | Active — payers invest heavily here |
| **EOB explanation** | LLM chatbot explains member's EOB in plain language | Emerging — connects to your FHIR MCP work |

**Your differentiator**: Most PM candidates know the workflow. You can speak to the AI layer on top of it, especially the FHIR-native AI tooling (`fhir-mcp-suite`, `fhir-mapping-agent`).

---

## Vendor Landscape — Expanded

### Core Admin Processing Systems (CAPS)
The main adjudication engine a payer runs. Everything flows through here.

**Important clarification on naming**: TriZetto is the **company name** (owned by Cognizant). **Facets** and **QNXT** are the two **product names** under that brand — like Microsoft is the company and Word/Excel are products. When someone says "we run on Facets" they mean the TriZetto Facets product, implemented and supported by Cognizant.

| Vendor | Product | Notes |
|---|---|---|
| **TriZetto (Cognizant)** | **Facets** (large payers), **QNXT** (mid-market) | Dominant in commercial market. Cognizant both sells the product AND implements it. |
| **HealthEdge** | HealthRules Payor | Modern cloud-native, growing challenger to TriZetto |
| **Plexis Healthcare Systems** | Plexis Claims | Mid-market payers |
| **Inovalon** | ONE Platform | Cloud-based, analytics-heavy |
| **Majesco** | Majesco Health | Smaller/regional payers |

### What Does TriZetto Facets Actually Do? (Not Just Claims)

Facets is a **full Core Administrative Processing System (CAPS)** — not just a claims submission tool. It is the payer's central operational platform:

| Module | What It Does |
|---|---|
| **Membership & Enrollment** | Stores member records, processes 834 enrollment files, manages effective dates |
| **Benefits Configuration** | Defines plan benefits — deductibles, copays, coinsurance, coverage rules |
| **Provider Data Management** | Provider contracts, fee schedules, network affiliations |
| **Claims Adjudication** | Ingests 837 from clearinghouse, runs edits, prices, applies benefits, pays or denies |
| **Premium Billing** | Bills employers for group coverage |
| **Reporting & Analytics** | Operational reports, regulatory filings |

The 837 EDI arrives from the clearinghouse and **Facets takes over from there** — the clearinghouse is the delivery mechanism, Facets is the processing engine. They are completely different systems doing different jobs.

---

## Healthcare Payer Systems — Deep Dive

### The Full Payer Technology Ecosystem

A payer's IT landscape is NOT just one system. It is a stack of specialized platforms that each own a slice of the member/claim lifecycle. A CAPS like Facets is the center of gravity, but it connects to many surrounding systems.

```
                    ┌─────────────────────────────────────┐
                    │         MEMBER-FACING LAYER          │
                    │  Member Portal / Mobile App          │
                    │  Member Services CRM                 │
                    │  FHIR Patient Access API (Phase 2)   │
                    └──────────────┬──────────────────────┘
                                   │
                    ┌──────────────▼──────────────────────┐
                    │        CORE ADMIN (CAPS)             │
                    │  TriZetto Facets / QNXT              │
                    │  HealthEdge HealthRules Payor        │
                    │  (Enrollment, Benefits, Claims,      │
                    │   Provider Data, Premium Billing)    │
                    └──┬──────────┬──────────┬────────────┘
                       │          │          │
          ┌────────────▼─┐  ┌─────▼──────┐  ┌▼────────────────┐
          │  UTILIZATION │  │    PBM     │  │  CARE MGMT /    │
          │  MANAGEMENT  │  │  Platform  │  │  POPULATION     │
          │  (PA, UM,    │  │ (Pharmacy  │  │  HEALTH         │
          │   Case Mgmt) │  │  Benefits) │  │  Platform       │
          └──────────────┘  └────────────┘  └─────────────────┘
                       │          │          │
                    ┌──▼──────────▼──────────▼────────────┐
                    │      DATA / ANALYTICS LAYER          │
                    │  Data Warehouse / EDW                │
                    │  Risk Adjustment Engine              │
                    │  HEDIS / Quality Reporting           │
                    │  Fraud, Waste & Abuse (FWA) Engine   │
                    └─────────────────────────────────────┘
```

---

### Legacy vs. Modern Payer Architecture

This is the key tension every payer IT project deals with. Most large payers run on **legacy CAPS** built in the 1990s–2000s. Modernization is the dominant transformation program at every major payer right now.

| Dimension | Legacy (Facets / QNXT) | Modern (HealthEdge / Cloud-native) |
|---|---|---|
| **Architecture** | Monolithic, on-premise | Microservices, cloud-native (AWS/Azure) |
| **Configuration** | Complex, brittle — benefit config changes take weeks | Low-code rules engine — days |
| **Real-time capability** | Batch-oriented — nightly processing cycles | Event-driven, real-time APIs |
| **FHIR readiness** | Requires a FHIR facade layer bolted on | FHIR-native APIs built in |
| **Upgrade cycles** | Major releases every 1–2 years; risky migrations | Continuous deployment |
| **Scalability** | Fixed infrastructure, seasonal spikes = outages | Auto-scaling cloud |
| **Total cost** | High — licensing + infrastructure + large IT teams | Lower long-term OpEx |
| **Risk** | Mature, well-understood — payers afraid to replace it | New, less battle-tested at scale |

**The PM reality**: Most payers are NOT replacing Facets — they are wrapping it. They build FHIR APIs, event streaming (Kafka), and modern UX on top of legacy CAPS. Full replacement projects (called "core modernization") take 5–10 years and cost hundreds of millions. Your FHIR workspace is the exact pattern — building a modern API layer over legacy payer data.

---

### The Surrounding Systems — What Connects to CAPS

#### 1. Utilization Management (UM) / Prior Authorization System
Separate from CAPS. Handles the PA workflow — intake, clinical review, approval/denial, tracking.

| Vendor | Product | Notes |
|---|---|---|
| **Utilization Management (payer-built or vendor)** | eviCore (Evernorth/Cigna), AIM Specialty Health (Optum) | Manage high-cost specialty PA |
| **InterQual (Optum/CHC)** | Clinical decision support criteria | Licensed by most payers for medical necessity rules |
| **MCG / Milliman Care Guidelines** | Clinical criteria | InterQual's main competitor |
| **Jiva (Cognizant)** | UM platform | Integrated with Facets ecosystem |

**FHIR connection**: Your CRD → DTR → PAS (Phases 5–6) is modernizing exactly this layer — replacing the phone/fax/portal UM workflow with FHIR APIs.

#### 2. PBM (Pharmacy Benefit Manager)
Manages prescription drug benefits — formulary management, pharmacy network, point-of-sale adjudication at the pharmacy counter. Often a separate company from the medical payer.

| Payer | Owned PBM |
|---|---|
| UnitedHealth Group | OptumRx |
| CVS Health / Aetna | CVS Caremark |
| Cigna | Express Scripts (Evernorth) |
| Humana | Humana Pharmacy / CenterWell |

**Key distinction**: Medical claims flow through CAPS (Facets). Pharmacy claims flow through the **PBM platform** — completely separate adjudication system using **NCPDP D.0** transactions, not X12 837. The FHIR Formulary API (your Phase 4) is the modern layer on top of PBM data.

#### 3. Care Management / Population Health Platform
Manages high-risk member programs — disease management, case management, complex case coordination, HEDIS gap closure. Reads from CAPS + clinical data sources.

| Vendor | Product | Notes |
|---|---|---|
| **Innovaccer** | Health Cloud | Modern, FHIR-native, AI-driven |
| **Arcadia** | Analytics platform | Strong HEDIS, risk stratification |
| **Health Catalyst** | Data Operating System | Analytics + care management |
| **Lightbeam Health** | Population Health | Care gap automation |
| **Jiva (Cognizant)** | Care Management | Tight Facets integration |
| **Payer-built** | Many large payers build internally | UHC, Anthem, BCBS have proprietary platforms |

#### 4. Member 360 / CRM
A unified view of the member across all payer systems — claims history, care gaps, calls to member services, enrollment history. Customer service reps use this when a member calls.

| Vendor | Notes |
|---|---|
| **Salesforce Health Cloud** | Dominant in member CRM; used by Aetna, BCBS plans |
| **Microsoft Dynamics 365** | Growing footprint in payer CRM |
| **Pega** | Workflow + CRM; strong in payer operations |

**FHIR connection**: FHIR PDex (your Phase 7) feeds the Member 360 when a member switches payers — it is the mechanism that populates the new payer's view with historical data.

#### 5. Data Warehouse / Analytics Platform
Aggregates data from CAPS, PBM, care management, and external sources for reporting, risk adjustment, HEDIS, and fraud analytics.

| Layer | Common Tech |
|---|---|
| **EDW / Data Lake** | Snowflake, Databricks, Azure Synapse, AWS Redshift |
| **ETL / Integration** | MuleSoft, Azure Data Factory, Informatica |
| **BI / Reporting** | Tableau, Power BI, MicroStrategy |
| **FHIR Bulk Data** | $export (your Phase 8) feeds analytics pipelines from FHIR server |

#### 6. Risk Adjustment Engine
Runs HCC coding and scoring for MA plans. Separate from the claims adjudication path — reads adjudicated claims after the fact.

| Vendor | Notes |
|---|---|
| **Cognizant / TriZetto Risk Adjustment** | Integrated with Facets |
| **Optum Risk Adjustment** | HCC prospective coding, RAPS/EDPS submission |
| **Inovalon** | Analytics-driven risk capture |

---

### How It All Connects — The Data Flow

```
Member enrolls (834)
        ↓
CAPS stores member record (Facets Membership module)
        ↓
Member sees doctor → EHR generates encounter
        ↓
PA check → UM system queries CAPS for member benefits
        ↓ (your CRD fires here against CAPS data via FHIR)
Claim generated (837) → Clearinghouse → CAPS adjudicates
        ↓
835 remittance → provider paid
        ↓
Member views EOB (FHIR CARIN BB — your Phase 2 reads from CAPS)
        ↓
Adjudicated claim → Data Warehouse
        ↓
Risk Adjustment engine reads ICD-10 codes → submits HCC to CMS (RAPS/EDPS)
        ↓
Care Management platform reads high-risk members → triggers outreach
        ↓
HEDIS engine reads claims + clinical → computes quality measures → Star Ratings
```

---

### The FHIR Layer — Where Your Work Sits

Your 8-phase workspace is the **modern API layer on top of this entire ecosystem**. Each phase maps to a specific payer system:

| Your Phase | Payer System It Surfaces | What It Replaces / Modernizes |
|---|---|---|
| Phase 2 — Member Access API | CAPS Membership + Claims modules | Proprietary member portal → open FHIR API |
| Phase 3 — Provider Directory | Provider Data Management in CAPS | Internal directory lookup → FHIR Plan-Net |
| Phase 4 — Formulary API | PBM platform formulary data | PBM proprietary API → FHIR Formulary IG |
| Phase 5 — CRD | UM / PA intake system | Fax/phone PA initiation → CDS Hooks in EHR |
| Phase 6 — DTR/PAS | UM criteria + CAPS authorization module | Fax/portal PA forms → FHIR DTR/PAS |
| Phase 7 — PDex | CAPS + Care Mgmt historical data | No mechanism for member data portability → FHIR PDex |
| Phase 8 — Bulk Data | Data Warehouse / EDW | Batch FTP exports → FHIR $export at scale |

---

### Interview One-Liners — Payer Systems

- *"CAPS like Facets are the core — but they're legacy monoliths. The FHIR mandate is forcing payers to build modern API facades over them. That's exactly the architecture my 8-phase workspace demonstrates."*

- *"A payer's tech stack has 6 layers: CAPS, UM/PA, PBM, care management, member CRM, and analytics. FHIR connects them all with a standard API layer instead of point-to-point integrations."*

- *"The biggest risk in any payer modernization program isn't technology — it's change management. Facets has 20+ years of benefit configuration logic baked in. You can't just swap it out overnight."*

---

### Clearinghouses
| Vendor | Notes |
|---|---|
| **Change Healthcare (Optum)** | Largest in US; acquired by UnitedHealth/Optum (after major 2024 cyberattack — know this story) |
| **Availity** | Owned by consortium of BCBS plans; large footprint |
| **Waystar** | Strong in hospital/health system billing |
| **Optum EDI** | Part of UnitedHealth Group ecosystem |

**Know the Change Healthcare 2024 cyberattack**: A ransomware attack took down Change Healthcare for weeks, affecting billions in claims processing across the US. Massive business continuity event. Shows how concentrated and fragile clearinghouse infrastructure is. Interviewers may bring this up.

### Clinical Editing / Claims Audit Engines
These sit inside or alongside the adjudication system and apply medical necessity, coding, and billing rules:

| Vendor | Product | Notes |
|---|---|---|
| **Optum** | ClaimCheck / CES | Industry standard for outpatient editing |
| **Cotiviti** | Cotiviti Claims | Payment accuracy, fraud/waste/abuse |
| **Multiplan** | Cost management | Out-of-network repricing |
| **Equian (nThrive)** | Payment integrity | Overpayment detection |

### RCM Outsourcing Companies (Provider Side)
| Vendor | Notes |
|---|---|
| **R1 RCM** | Large hospital system RCM outsourcer |
| **Optum360** | Hospital billing outsourcing |
| **Conifer Health Solutions** (Tenet) | Hospital RCM |
| **nThrive** | Mid-market provider RCM |
| **Cognizant / Infosys / Wipro BPO** | IT services companies also run RCM operations as BPO — not just build software |

### How RCM Outsourcing Vendors Actually Work — They Are NOT Clearinghouses

This is a common confusion. RCM outsourcing vendors do NOT sit between provider and payer the way clearinghouses do. Instead, they **operate inside the provider's own environment**:

```
Hospital / Health System
        │
        ├── EHR (Epic, Cerner) ──────────────────┐
        ├── Practice Management / Billing System ─┤← RCM Vendor gets
        └── Clearinghouse account (Availity etc.) ─┘   login access to ALL of these
                                                        and works AS the billing team
```

**What the RCM vendor does with that access**:
1. Logs into the hospital's Epic/Cerner to pull charge data and clinical documentation
2. Uses the hospital's billing/PM system to generate and scrub claims
3. Submits 837 through the hospital's existing clearinghouse account using the hospital's NPI
4. Receives 835 remittance back into the hospital's system
5. Works denial queues inside the hospital's system
6. Posts payments back to the hospital's accounting system

**The provider never needs a new connection** — the vendor works in the provider's name, with the provider's credentials, through the provider's existing clearinghouse relationship.

**Commercial model**: RCM vendors typically charge a **percentage of net collections** (4-8%) or a flat per-claim fee. They are incentivized to collect more — their revenue goes up when the provider's revenue goes up.

---

## IT Services Companies (Infosys, Cognizant, TCS, Wipro, Accenture) — Their Role in the Ecosystem

These companies are NOT just software vendors. They play up to four simultaneous roles in healthcare — and the PM job you are interviewing for is almost certainly inside one of these practices.

### Role 1: IT Services / System Integrator (Most Common)
Build, implement, and maintain the systems payers and providers run.
- Implement TriZetto Facets or QNXT (**Cognizant owns TriZetto** — they sell the product AND implement it)
- Build FHIR APIs on top of legacy claims systems (this is your workspace)
- Integrate clearinghouse connections
- Build member portals, provider portals
- Migrate data, run application support 24/7

### Role 2: RCM Outsourcing (Provider Side)
Run the entire billing operation FOR providers. They get access to the hospital's systems and work as the billing team (explained above in the RCM section).

### Role 3: Payer Operations BPO (Business Process Outsourcing)
Run operations FOR payers — not just build the software, but operate it:
- Claims processing (working pend queues)
- Member services call center
- Enrollment processing
- Prior authorization handling
- Provider credentialing

### Role 4: Consulting / Advisory (Most Relevant to Your Interview)
Advise payers and providers on strategy, regulatory compliance, and transformation:
- CMS issues a mandate (e.g., CMS-0057 FHIR prior auth by 2027)
- Payer doesn't know how to comply
- Hires Infosys/Cognizant consulting arm for gap assessment, architecture, implementation roadmap, vendor selection, and program management
- **This is the role the PM/Product Manager position sits in**

### The Same Company, Multiple Relationships
Example — Infosys:
- **As an employer**: Buys health insurance for 300,000+ employees → sends 834 to payer
- **As an IT vendor**: Implements the claims system for that same payer
- **As BPO**: May run the call center for that payer

### Why This Matters for Your Interview
Position yourself as: *"I understand the client's pain (claims denials, CMS mandates), know the product/solution landscape (TriZetto, FHIR APIs, clearinghouses), can manage delivery from discovery to go-live, and speak both business and technology."* That is exactly what these consulting practices need from a healthcare PM.

---

## Denial Management — Why It's a Major Cost Center

This is one of the biggest operational problems in US healthcare. Understand it deeply.

### The Numbers
- ~15-20% of claims are denied on first submission
- Providers spend **$19.7 billion annually** just working denials (CAQH estimate)
- Providers write off **50-65% of denied claims** without appealing — that's revenue left on the table
- Payers also spend heavily — managing the appeals, reopenings, and dispute resolution

### The Denial Lifecycle

```
Claim Denied (835 comes back with denial CARC code)
        ↓
Denial Management Team reviews reason code
        ↓
   ├── Coding error → Recode and resubmit (corrected claim)
   ├── Missing info → Add documentation and resubmit
   ├── Auth missing → Get retroactive auth (sometimes possible)
   ├── Medical necessity → Write appeal letter with clinical evidence
   ├── Timely filing expired → Write off (unrecoverable)
   └── Payer error → Peer-to-peer with payer MD or formal appeal
```

### Common Denial Categories
| Denial Type | % of Denials (approx) | Who Fixes It |
|---|---|---|
| Missing / invalid information | ~25% | Billing team corrects and resubmits |
| Authorization / pre-cert missing | ~20% | Authorization coordinator gets retroactive auth |
| Medical necessity | ~15% | Clinical team writes appeal with supporting docs |
| Duplicate claim | ~10% | Billing team verifies and resolves |
| Coding errors (CPT/ICD mismatch) | ~15% | Coder corrects |
| Timely filing | ~5% | Usually written off |
| Eligibility / coverage issues | ~10% | Patient access resolves coverage |

### Why Payers Are Also a Cost Center Here
Payers spend money too:
- Running the denial process, reviews, and appeals takes staffing
- **Peer-to-peer review** requires a payer MD's time
- CMS mandates strict appeal response timelines (especially for MA)
- **Overturn rate** — if payer denies too aggressively and loses most appeals, it signals bad policy

### The PM Opportunity
> Reducing denial rate by even 5% saves a mid-size payer or health system tens of millions annually. This is a prime area for AI, better prior auth workflows (your CRD/DTR/PAS work), and FHIR-based real-time eligibility checking.

---

## Who Decides Prior Authorization Criteria?

The payer decides — but they don't invent criteria from scratch, and they operate within external constraints.

### The Layered Decision Stack

```
CMS (for Medicare/Medicaid)
  └── Publishes NCDs (National Coverage Determinations)
      and LCDs (Local Coverage Determinations)
      ↓
      For Medicare Advantage: 2024 CMS final rule — payer criteria
      CANNOT be more restrictive than Traditional Medicare FFS
      ↓
Clinical Criteria Vendors (payer licenses one of these)
  ├── InterQual (Optum/Change Healthcare) ← most widely used
  └── MCG Criteria (formerly Milliman Care Guidelines)
      ↓
      Evidence-based clinical benchmarks for medical necessity
      (e.g., "inpatient admission for X requires criteria A, B, C")
      ↓
PAYER layers on plan-specific rules on top of licensed criteria
  ├── Which CPT/HCPCS codes require PA
  ├── Quantity limits, duration limits
  ├── Site-of-care requirements
  └── Step therapy requirements
      ↓
Rules encoded in the payer's rules engine (Drools / IBM ODM)
      ↓
OUTPUT: "Service X requires PA — approval criteria: ..."
```

### Who Controls What

| Source | What They Control |
|---|---|
| **Payer** | Final authority — which services require PA, approval/denial decision, plan-specific rules |
| **InterQual / MCG** | Clinical criteria the payer licenses — evidence-based benchmarks for medical necessity |
| **CMS** | Hard floor for MA plans — payer criteria cannot be more restrictive than Traditional Medicare (2024 CMS final rule) |
| **State law** | Some states ban or limit PA for specific services (emergency care, mental health parity, step therapy overrides) |
| **Gold carding** | High-performing providers exempted from PA for certain services — many states now mandate this by law |

### CQL — How Criteria Become Machine-Executable in FHIR

When criteria live in an InterQual document or a clinical PDF, they can't be evaluated programmatically. The FHIR ecosystem uses **CQL (Clinical Quality Language)** to make them machine-readable:

```
InterQual criteria (human-readable):
"MRI of lumbar spine requires: documented 6 weeks of conservative
 treatment (PT/chiro), failure to improve, no red flag symptoms"

↓ converted to CQL ↓

define "ConservativeTreatmentDocumented":
  exists ([Procedure: "PhysicalTherapy"] P
    where P.performed.start >= Today() - 6 weeks)

define "MRIAuthorizationRequired":
  not "ConservativeTreatmentDocumented"
```

Your CRDService in Phase 5 evaluates CQL expressions against the patient's FHIR data to determine in < 5 seconds whether PA is required — replacing the fax/phone process entirely.

### The Provider Pushback — Why PA Lists Are Shrinking

Physicians and provider groups have lobbied heavily against excessive PA:
- **AMA surveys**: 94% of physicians say PA delays necessary care; 33% report it has led to serious adverse events
- **Gold Carding laws**: Texas, Arkansas, and other states require payers to exempt providers with > 90% approval rates from PA for those services
- **CMS 2024 MA rule**: Tightened requirements — payers must document clinical basis for all PA denials, respond faster, and cannot use criteria more restrictive than Medicare FFS
- **CMS-0057-F (2027)**: Mandates electronic PA (FHIR PAS) — which naturally creates a data trail making it harder for payers to apply criteria inconsistently

### Interview One-Liner
*"The payer sets PA criteria, but they largely license them from InterQual or MCG, and for Medicare Advantage they're now capped by CMS — they can't be more restrictive than Traditional Medicare. My CRD work is about taking those criteria and making them machine-executable in FHIR so the doctor gets the answer in real-time instead of faxing for 3 days."*

---

## Automation in the Clearinghouse → Payer Flow

Yes — significant automation exists, though much is rule-based (not yet AI-driven):

```
Provider submits 837 to clearinghouse
        ↓
[CLEARINGHOUSE — Automated Steps]
1. Format validation (automated, real-time, seconds)
2. Companion guide rules check (automated)
3. Routing to correct payer (automated — payer ID lookup)
4. 999/TA1 acknowledgement generation (automated, within minutes)
        ↓
[PAYER — Automated Steps]
5. Intake and ingestion (automated)
6. Duplicate check (automated)
7. Eligibility verification (automated — queries enrollment system)
8. Clinical/coding edits — NCCI, LCD/NCD (automated rule engine)
9. Pricing — fee schedule, DRG grouper (automated)
10. Benefit application — deductible, OOP tracker (automated)
11. Auto-adjudication decision (automated for clean claims — ~85% target)
12. 835 ERA generation and delivery (automated)
        ↓
[HUMAN TOUCHPOINTS — Where Automation Breaks Down]
- Pended claims → claims examiner manually reviews
- Complex medical necessity → clinical reviewer
- Appeals → denial management specialist
- Peer-to-peer reviews → payer MD
```

**Key insight**: The goal of every payer's IT and operations team is to push as many claims through the automated path as possible (high auto-adjudication rate) and minimize the human queue. AI is now being applied to the human touchpoints — especially pended claims and medical necessity reviews.

---

## The Concept of "Pend" — Explained

A **pended claim** is one that the system cannot automatically adjudicate and puts in a queue for a human to review. Think of it as the claims system raising its hand and saying *"I don't know what to do with this — a person needs to look at it."*

### Why a Claim Gets Pended

| Reason | Example |
|---|---|
| **Medical necessity review needed** | High-cost procedure (MRI, surgery) — system flags for clinical review |
| **Missing clinical documentation** | Prior auth exists but clinical notes not attached |
| **COB (Coordination of Benefits)** | Member has 2 insurances — system needs to determine primary/secondary |
| **New provider / unknown contract** | Provider NPI not yet loaded into payer's system |
| **Unusual billing pattern** | Unusual code combination that triggers audit flag |
| **High dollar threshold** | Claim over $X automatically goes to review |
| **Experimental procedure** | Procedure on investigational/exclusion list |

### Pend vs Deny — The Key Difference

| | Pend | Deny |
|---|---|---|
| **Meaning** | Temporarily held for human review | Decisively rejected |
| **Outcome** | Could still be paid after review | Requires appeal or resubmission to recover |
| **Clock** | Payer has regulatory timeline to resolve (e.g., 30 days for commercial, strict for MA) |  |
| **Provider action needed?** | Sometimes must supply more info | Must appeal or resubmit |

### Why Pend Rate Matters to a PM
- High pend rate = high operational cost (human reviewers are expensive)
- Pended claims slow cash flow for providers → provider abrasion
- Reducing pend rate through better rules, AI-assisted review, or prior auth automation is a key payer IT initiative

---

## Accounts Receivable (AR) — The Financial Layer on Top of Claims

AR is the **provider-side financial tracking** of money owed but not yet collected. It runs in parallel with the entire claims workflow — every outstanding claim is an open AR record.

### Where AR Opens and Closes

```
Service rendered
        ↓
837 submitted to clearinghouse
        ↓
AR OPENS HERE — provider is owed money; it's a receivable
        ↓
999 / 277CA received — AR status: "in process"
        ↓
835 remittance received:
  ├── Paid in full        →  AR closes (payment posted)
  ├── Paid partially      →  AR reduced for paid amount; remainder stays open
  │                          → bill secondary insurance (COB)
  │                          → or bill patient for their cost-share
  └── Denied              →  AR stays open; denial team works it
                              → appeal / resubmit: AR may eventually close
                              → timely filing expired: AR written off (loss)
        ↓
Claw back / recoupment:  AR that was CLOSED gets REOPENED as a liability
(payer takes back a payment already received — payer offset or check demand)
```

### Key AR Metrics

- **Days in AR** (also called Days Sales Outstanding / DSO) — average number of days from service rendered to payment received. **Industry benchmark: < 45 days**. High days in AR = cash flow problem. Payers in value-based contracts watch this closely from the provider's side too.

- **AR aging buckets** — outstanding AR is categorized by age: 0–30, 31–60, 61–90, 91–120, 120+ days. Anything past 90 days is a red flag — the older a claim gets, the less likely it is to be collected. Claims approaching 90–180 days are at timely filing risk.

- **Net collection rate** — percentage of collectible revenue actually received. Target: > 95%. Anything below 90% signals significant revenue leakage.

- **Gross collection rate** — payments received as a percentage of billed charges. Always lower than net (because allowed amount < billed amount). Less meaningful than net collection rate.

- **Write-off rate** — AR permanently removed from the books as uncollectable. Categories: contractual write-offs (difference between billed and allowed — expected), bad debt (patient couldn't pay), and timely filing write-offs (unrecoverable — avoidable).

### AR Aging in Practice

| Age Bucket | Status | RCM Action |
|---|---|---|
| 0–30 days | Normal — waiting for 835 | No action needed; monitor |
| 31–60 days | Slightly aged — check claim status (276/277) | Query status if no 835 yet |
| 61–90 days | Aging — investigate | Call payer / portal follow-up |
| 91–120 days | High risk — timely filing approaching | Escalate; resubmit if denied |
| 120+ days | Critical — most payers' timely filing window closing | Last chance to recover; likely write-off |

### How AR Connects to Everything Else in Gap 1

| Claims Event | AR Impact |
|---|---|
| Clean claim submitted | AR opens; likely to close fast (auto-adjudicated in days) |
| Claim pended | AR stays open longer — waiting for human reviewer |
| Claim denied | AR stays open; must be worked or written off |
| Timely filing expires | AR written off — 100% unrecoverable revenue loss |
| Retroactive termination / claw back | Closed AR reopened as a liability |
| Prior auth missing (denial) | AR stays open; auth coordinator must fix |
| COB — secondary insurance | AR partially open after primary pays; bill secondary |

### The RCM Team's Job in One Sentence
The RCM team's entire purpose is to **minimize days in AR and maximize net collection rate** — by submitting clean claims, catching rejections fast, working denials aggressively, and ensuring nothing ages past timely filing.

### AR on the Payer Side
Payers have the mirror image: **Accounts Payable (AP)** — what they owe to providers. Payers also track **claims liability** (reserves for claims incurred but not yet paid — called IBNR: Incurred But Not Reported). This is a major actuarial and financial reporting concept for MA plans and self-funded employers.

---

## Key Terms — With Explanations

- **Clean claim** — A claim that passes all format, eligibility, and coding edits on the first submission with no errors, allowing it to be auto-adjudicated without human review. Higher clean claim rate = lower operational cost.

- **Pend / Pended** — A claim placed in a manual review queue because the automated system could not make a final pay/deny decision. A human claims examiner or clinician must review it. Not yet denied — could still be paid.

- **Auto-adjudication rate** — The percentage of claims processed and decided entirely by the automated system without human intervention. Industry target is 85%+. Lower rate means higher staffing costs. A key payer operations KPI.

- **First-pass resolution rate** — Percentage of claims paid on the first submission without rework, resubmission, or appeal. High rate = clean data from providers and tight payer rules alignment.

- **COB — Coordination of Benefits** — When a member has two insurance plans (e.g., covered by both their own employer and their spouse's employer), rules determine which plan pays first (primary) and which pays second (secondary). The secondary pays a portion of what the primary didn't cover.

- **Subrogation** — When a payer pays a claim and later recovers that money from a third party who was liable. Example: a member injured in a car accident — the health plan pays, then pursues the auto insurer to recover the payment.

- **Allowed amount** — The maximum the payer will pay for a given service per the provider's contracted rate. The provider can only bill the member for cost-sharing (deductible/copay/coinsurance) on top of this — not the difference between billed and allowed (for in-network).

- **Billed amount** — What the provider charges. Almost always higher than the allowed amount.

- **Paid amount** — What the payer actually sends to the provider: Allowed amount minus any member cost-sharing.

- **CARC (Claim Adjustment Reason Code)** — Standard code on the 835 remittance explaining WHY a claim line was adjusted or denied. E.g., CARC 4 = "The service/supply/drug is not covered by the plan." Providers use these to decide whether to appeal or write off.

- **RARC (Remittance Advice Remark Code)** — Supplemental code on the 835 providing additional detail about the CARC. Often used together — CARC tells you the category, RARC gives the specifics.

- **Timely filing** — Each payer sets a deadline (typically 90-180 days from date of service) within which a provider must submit a claim. Missed deadline = automatic denial with no appeal right. One of the most painful write-offs in RCM.

- **DRG (Diagnosis-Related Group)** — A payment bundling system for inpatient hospital stays. Instead of paying for each service separately, CMS and payers pay a single flat rate for the entire stay based on the patient's diagnosis and procedure. E.g., knee replacement = one DRG payment regardless of how many X-rays were taken.

- **Capitation** — A payment model where the payer pays the provider a fixed amount per member per month (PMPM) regardless of how much care the member uses. Provider assumes financial risk. Common in value-based contracts.

- **Fee-for-Service (FFS)** — The traditional model: provider bills for each service, payer pays per claim. More volume = more revenue. Incentivizes quantity over quality.

- **Value-based contracts** — Arrangements where provider payment is tied to quality and outcomes, not just volume. Includes shared savings (ACO), bundled payments, and full capitation. The direction CMS and commercial payers are pushing.

- **Appeal / Reconsideration** — Formal process for a provider to challenge a payer's denial decision. First level is usually reconsideration (informal). If denied again, formal appeal. For Medicare Advantage, there are 5 levels of appeal including ALJ hearing and federal court.

---

## Interview Questions — With Model Answers

**1. "Walk me through what happens when a provider submits a claim."**

> "The provider's billing system generates an EDI 837 — professional, institutional, or dental depending on the service. That file goes to a clearinghouse like Availity or Change Healthcare, which validates the format and routes it to the correct payer using the payer ID. The clearinghouse sends back a 999 acknowledgement confirming receipt. The payer then runs the claim through eligibility verification, clinical and coding edits, pricing against the contracted fee schedule, and benefit application for deductibles and copays. For a clean claim, this is fully automated and results in an adjudication decision — paid, denied, or pended for review. The payer sends back an EDI 835 remittance advice to the provider with the payment details and any denial reason codes. A member-facing EOB is also generated — that's the CARIN Blue Button FHIR EOB resource I've built in my work."

**2. "What's the difference between an 837 and an 835?"**

> "The 837 is the claim transaction — it flows from provider to payer and contains all the billing information: who was treated, what service, what diagnosis codes, what was charged. The 835 is the remittance advice that flows back from payer to provider after adjudication — it explains what was paid, what was adjusted, and why. The 835 is also called an ERA — Electronic Remittance Advice. Together they represent the full claim cycle: the ask and the answer."

**3. "What is auto-adjudication and why is it a key payer metric?"**

> "Auto-adjudication is the percentage of claims that go through the full payment process and get a final decision — pay or deny — without any human touching them. The higher this rate, the lower the operational cost for the payer. Industry target is around 85%. Claims that fall out of auto-adjudication go into a pend queue for human review, which is expensive and creates cash flow delays for providers. Improving auto-adjudication rate is one of the biggest levers a payer's IT and operations leadership pulls — better rules engines, cleaner provider data, and now AI-assisted review of pended claims."

**4. "How would you reduce claim denials as a PM?"**

> "I'd attack it at three points. First, prevent denials at source — better prior authorization tooling so providers know what's covered before they render service. The CRD/DTR/PAS workflow I've built does exactly this — real-time coverage determination at the point of order. Second, improve first-pass clean claim rate by giving providers better real-time eligibility checks and coding feedback before submission. Third, for denials that do happen, use AI to classify denial reason codes, auto-draft appeal letters, and prioritize the denials most likely to be overturned. Each of these reduces cost on both sides — the provider's RCM team and the payer's claims operations team."

**5. "How does Coordination of Benefits work?"**

> "When a member is covered by two insurance plans, COB rules determine the order of payment. The primary plan pays first up to its allowed amount. The secondary plan then pays some or all of the remainder. The combined payment typically cannot exceed the billed amount. There are federal and NAIC rules determining which plan is primary — for example, the plan covering the member as an employee is primary over the plan covering them as a dependent. COB is operationally complex because both payers need to communicate and cross-reference, and data is often stale or incomplete."

**6. "What's the difference between a denial and a pend?"**

> "A denial is a final decision — the payer has adjudicated the claim and decided not to pay it, for a specific reason captured in a CARC code on the 835. The provider must appeal or resubmit. A pend is not a decision — it's a hold. The system couldn't auto-adjudicate and put the claim in a queue for a human reviewer. The claim could still be paid after review, or denied at that point. From a cash flow perspective, both create delays for the provider, but a pend has a better chance of eventual payment if the documentation supports it."

---

# GAP 2 — Enrollment & Eligibility

## The Flow

1. **Source** — Employer HR system, ACA Marketplace, or CMS (for Medicare/Medicaid) sends member changes
2. **EDI 834** file sent to payer (add / change / terminate transactions)
3. **Payer enrollment system** processes file — creates/updates member record
4. **ID card** generated and mailed
5. Once enrolled, providers verify coverage at point of service via:
   - **EDI 270** — Eligibility & Benefit Inquiry (from provider)
   - **EDI 271** — Eligibility & Benefit Response (from payer)

## Key Concepts

- **Open Enrollment Period (OEP)** — annual window (e.g., Nov 1 - Jan 15 for ACA, Oct 15 - Dec 7 for Medicare)
- **Special Enrollment Period (SEP)** — triggered by qualifying life events (marriage, birth, job loss, divorce)
- **Effective date** vs **enrollment processing date** — often different; causes confusion
- **Retroactive enrollment** — coverage starts in the past; major operational pain
- **Term date / Termination** — when coverage ends
- **Reinstatement** — re-activating a terminated member
- **Dependent management** — adding/removing spouse, children

## Enrollment Channels

| Channel | Source | Standard |
|---|---|---|
| Group/Employer | HR system, broker | 834 EDI |
| Individual (ACA) | Federal/State Marketplace | 834 EDI |
| Medicare | CMS (MMR, TRR files) | CMS-specific formats |
| Medicaid | State agency | State-specific feeds |

---

## Life Event Scenarios — What Actually Happens

### Scenario 1: Employee Changes Employer

```
Employee leaves Employer A
        ↓
Employer A's HR system sends 834 TERMINATION to Payer A
Coverage ends (usually end of month or last day worked)
        ↓
Employee has 3 options during the gap:
  ├── COBRA — continue Employer A's plan for up to 18 months,
  │           but employee pays FULL premium (expensive)
  ├── ACA Marketplace SEP — job loss is a qualifying life event,
  │           has 60 days to enroll in marketplace plan
  └── New employer coverage — if new job starts immediately
        ↓
Employee joins Employer B
Employer B's HR system sends 834 ENROLLMENT to Payer B
(could be same insurer or different)
        ↓
New member record created, new ID card issued
New deductible resets on effective date
```

**Key operational pain**: The gap between Employer A coverage ending and Employer B coverage starting. Employer B may have a **waiting period** (up to 90 days under ACA rules) before new coverage kicks in. During this gap the employee is uninsured unless they use COBRA or marketplace.

**Pre-existing conditions**: Since ACA 2010, insurers cannot deny coverage or charge more for pre-existing conditions. This was a massive change — previously employees were afraid to switch jobs because of this.

---

### Scenario 2: Employee Moves to a Different Insurance (Same Employer, Different Plan)

```
During Open Enrollment (typically Oct-Nov for Jan 1 effective)
        ↓
Employee logs into employer's benefits portal (Workday, SAP, ADP)
Selects new plan
        ↓
Benefits system sends 834 to OLD payer: TERMINATION (Dec 31)
Benefits system sends 834 to NEW payer: ENROLLMENT (Jan 1)
        ↓
Old payer terminates member record
New payer creates member record, issues new ID card
        ↓
Any in-progress prior auths, care management do NOT transfer
Member starts fresh with new deductible
```

**If staying with same insurer but switching plan** (e.g., HMO to PPO, both Aetna):
- Same payer processes the plan change internally
- Member gets new ID card with new plan details
- Still resets deductible and OOP

---

## Clarification: "Payers Run Reconciliations With Employers" — Who Are These Two Entities?

**Important**: Even in self-funded (ASO) plans, the employer and the insurance company (payer/administrator) are ALWAYS two separate entities. The 65% self-funded stat means the employer FUNDS the claims — it does NOT mean the employer IS the insurance company.

```
Employer A (e.g., TCS)          Insurance Company (e.g., Aetna)
─────────────────────           ───────────────────────────────
Has 50,000 employees            Administers the plan for TCS
HR system has the               Enrollment system has what
master list of who              was received via 834 files
should be covered
        │                               │
        └─────── RECONCILIATION ────────┘
                 Compare the two lists:
                 - Is everyone in HR also in Aetna's system?
                 - Any terminated employees still active in Aetna?
                 - Any dependents that should have been removed?
```

**Why reconciliation is needed**: The 834 file is sent periodically (daily, weekly, monthly depending on the employer). Errors happen — file transmission failures, timing gaps, data mismatches. The reconciliation report catches these before they cause claims denials or fraud (paying for someone no longer employed).

**Who runs it**: Both sides. Aetna sends a discrepancy report to TCS's HR team. TCS reviews and resolves. This is a recurring operational process, regardless of fully-insured or self-funded.

---

## Eligibility (270/271) — Deep Dive

This is the second half of Gap 2 that is just as important as enrollment.

### What Is Eligibility Verification?

Before rendering service, every provider should verify the patient is covered and understand their benefit details. This prevents claim denials downstream.

```
Patient calls to schedule appointment
        ↓
Provider's scheduling system sends EDI 270
"Is Jane Smith (Member ID 12345) covered under Plan XYZ
 for services on June 15, 2026?"
        ↓
Payer's eligibility system responds EDI 271
"Yes, active. Plan: PPO. Deductible: $1,500 ($800 remaining).
 Copay: $40 PCP, $80 Specialist. OOP max: $5,000 ($3,200 remaining).
 Prior auth required for: MRI, PT > 10 visits."
        ↓
Provider knows exactly what to collect from patient
and whether prior auth is needed before the visit
```

### What 271 Returns (Key Fields)

| Field | Meaning | Why It Matters |
|---|---|---|
| **Active/Inactive** | Is coverage in force on date of service? | Inactive = guaranteed denial |
| **Plan name / type** | HMO, PPO, HDHP | Determines network rules |
| **Deductible / remaining** | How much patient owes before insurance pays | Collect from patient upfront |
| **Copay / coinsurance** | Patient's share per visit | Set patient expectations |
| **OOP max / remaining** | When patient stops paying | High OOP remaining = collect more |
| **Network status** | Is this provider in-network for this patient? | Out-of-network = different cost sharing |
| **Prior auth requirements** | Which services need approval first | Triggers auth workflow |
| **COB indicator** | Does patient have other insurance? | Determine primary/secondary |

### Real-Time vs Batch Eligibility

| Mode | When Used | How |
|---|---|---|
| **Real-time** | Point of scheduling, point of service | Single 270 sent, 271 back in seconds |
| **Batch** | Night before, for next day's full schedule | Hospital sends file of all tomorrow's patients, gets back file of all 271 responses |
| **Automated inline** | EHR checks eligibility automatically at check-in | EHR integrated with clearinghouse eligibility service |

### Who Actually Performs the Eligibility Check?

This is a common confusion — it is NOT exclusively the clearinghouse.

| Path | Who Does It | When Used |
|---|---|---|
| **Direct to payer** | Provider system connects directly to payer's eligibility endpoint | Large EHRs (Epic, Cerner) have direct real-time connections to major payers |
| **Via clearinghouse** | Clearinghouse acts as pass-through — provider sends 270 to clearinghouse, clearinghouse routes to payer, returns 271 | Smaller providers, or when using clearinghouse for claims anyway |
| **PBM separately** | Pharmacy Benefit Manager handles pharmacy eligibility independently from the medical payer | At the pharmacy counter — completely separate system |

**Key point**: Clearinghouses offer eligibility as a **value-add service**, not a requirement. A provider with a large volume and direct payer relationships (like a major hospital system) often connects directly. The clearinghouse is mandatory for claims (837/835) but optional for eligibility (270/271).

**The payer's Core Admin System (Facets/QNXT) is always the source of truth** — it holds the enrollment data. Whether the 270 arrives via clearinghouse or direct, it is answered by the payer's eligibility engine sitting on top of that enrollment database.

### Step Therapy — Does It Come Into Eligibility?

Yes — **step therapy** (also called **fail-first protocol**) is a utilization management rule that surfaces during the eligibility and prior auth process, primarily for **medications**.

**What it is**: The payer requires the patient to try a less expensive drug first (usually a generic or lower-tier drug) before they will approve the more expensive drug the doctor prescribed.

```
Doctor prescribes Brand Drug X
        ↓
Pharmacy runs eligibility / formulary check with PBM
        ↓
PBM responds: "Brand Drug X requires step therapy.
  Patient must first try Generic Drug Y for 30 days.
  If ineffective, prior auth for Brand X will be considered."
        ↓
Pharmacist tells patient → patient calls doctor
→ Doctor either switches prescription or requests PA with clinical justification
```

**Where it appears in the 271 / eligibility response**:
- The 271 can include prior auth requirements
- For pharmacy, the PBM's formulary response (separate from 271) includes step therapy flags
- Your CRD (Coverage Requirements Discovery) work in Phase 5 is designed to surface exactly these requirements — step therapy, prior auth, quantity limits — at the point the doctor writes the order in the EHR, BEFORE the patient gets to the pharmacy

**Step therapy applies to**: High-cost branded medications, specialty drugs, biologics
**Does NOT apply to**: Medical procedures (those use prior auth and medical necessity criteria instead)

### Why Eligibility Errors Are So Costly

- **Member shows up, insurance is inactive** → provider renders care, submits claim → denied → must bill patient directly → collections nightmare
- **Wrong plan checked** (e.g., patient has two insurances, provider checks secondary first) → COB confusion
- **Eligibility verified, then member terminated retroactively** → payer claws back payment → provider must refund or rebill
  > **"Claw back" (Recoupment)** = Payer has already paid the claim, then discovers the member was not eligible at time of service. Payer issues a recoupment notice to the provider demanding the money back — either by offsetting against future payments (most common) or demanding a check. The provider can dispute it but if the member was genuinely ineligible, the provider typically loses and absorbs the loss or must bill the patient directly.
- **Prior auth requirements missed** → service rendered without auth → medical necessity denial

---

## Pain Points — Expanded

- **834 reconciliation** — employer HR file vs payer enrollment system mismatch; ongoing daily ops problem. Errors cause members to be active in HR but not in payer system or vice versa.
- **Mid-month adds/terms** — proration issues; billing systems must calculate partial-month premiums
- **Retroactive termination** — employer terminates employee in HR but forgets to send 834 for weeks. Payer keeps paying claims. When the 834 finally arrives, payer must **claw back** (recoup) those payments from providers. Extremely disruptive.
  > **Claw back / Recoupment**: Payer paid a claim that it legally should not have paid (because the member was not eligible). Payer issues a recoupment demand — takes the money back by deducting it from future claim payments to the provider. Provider had already posted the payment as revenue; now that revenue disappears. At scale (thousands of retroactively termed members) this is a major cash flow crisis for providers and a massive operational headache for payers.
- **Member at pharmacy rejected** — enrollment didn't flow to PBM (pharmacy benefit manager) in time. PBM is often a separate system from the medical payer.
- **Discrepancy reports** — payers run daily/weekly reconciliations with employers to catch mismatches before they become claim problems

---

## FHIR Role in 2026 for Enrollment & Eligibility

EDI still dominates this space, but FHIR is making inroads:

| Function | Still EDI | FHIR Role in 2026 |
|---|---|---|
| 834 Enrollment | Dominant — not changing | No FHIR standard for enrollment transactions |
| 270/271 Eligibility | Dominant | **Coverage resource** — FHIR equivalent, emerging in some payer APIs |
| Member views own coverage | Not possible in EDI | **FHIR Coverage resource via Patient Access API — mandated, live** |
| Provider checks patient coverage | 270/271 | **CRD uses Coverage + prior auth data in real-time** — your Phase 5 |
| Benefit details for prior auth | Fax/portal | **CRD returns coverage requirements including auth needs** |

**Key FHIR connection to your work**: Your Phase 5 (CRD) uses the FHIR Coverage resource to check in real-time whether a service needs prior authorization — this is FHIR replacing the eligibility + prior auth inquiry that used to require a 270/271 plus a phone call.

**CMS Patient Access API** (mandated under CMS-9115-F): Members can retrieve their own Coverage resource via FHIR — they see plan, effective dates, benefits — the FHIR version of what a 271 returns. Your Phase 2 (Member Access API) implements this.

---

## AI Role in 2026 for Enrollment & Eligibility

| Area | AI Application | Status in 2026 |
|---|---|---|
| **834 reconciliation** | ML detects patterns in discrepancies between employer file and payer system, flags likely errors before they cause denials | Active — several payer ops platforms |
| **Dependent eligibility audit** | AI reviews dependent eligibility (are dependents still actually dependents?) — major cost savings | Active — Conduent, Businessolver |
| **Enrollment fraud detection** | ML detects fraudulent enrollment patterns (fake dependents, ghost employees) | Active |
| **Eligibility prediction** | Predict which members are likely to have eligibility issues at point of service before they arrive | Emerging |
| **Benefits chatbot** | LLM-powered member chatbot answers "what's my deductible?" "is this doctor in-network?" — reads from FHIR Coverage resource | Active — many payer portals, connects directly to your FHIR MCP work |
| **Automated eligibility workflow** | AI orchestrates real-time 270/271 checks integrated into EHR scheduling workflow without human touchpoint | Active — Epic/Cerner integrations |
| **Retroactive term detection** | AI flags when a claim payment may be at risk due to pending retroactive termination | Emerging |

---

## Interview Questions — With Model Answers

**1. "Explain the 834 enrollment process."**

> "The 834 is the EDI transaction that carries membership enrollment data from a plan sponsor to the payer. For a group employer plan, the company's HR system — or a benefits administration platform like Workday or ADP — generates an 834 file whenever an employee is added, changes coverage, or is terminated. That file goes directly to the payer, who processes it to create or update the member record. The payer then issues an ID card. The challenge is that this process is batch-based and timing-sensitive — if the 834 is delayed or contains errors, the member may show up at a provider without active coverage, which causes claim denials and poor member experience."

**2. "What's the difference between 270 and 271?"**

> "The 270 is the eligibility inquiry — sent by a provider to a payer asking whether a specific patient is covered on a given date and what their benefits are. The 271 is the response — it comes back from the payer and tells the provider whether the member is active, what plan they're on, their deductible and remaining balance, copays, prior auth requirements, and COB indicators. This transaction is often real-time — the provider sends the 270, gets the 271 back within seconds, and knows exactly what to collect from the patient before rendering service."

**3. "What happens during open enrollment vs special enrollment?"**

> "Open enrollment is the annual window — typically October through November for employer plans, with coverage starting January 1 — when all employees can make changes to their coverage: switching plans, adding or removing dependents, or opting out. It's a fixed window everyone goes through on the same schedule. Special enrollment periods are triggered by qualifying life events: marriage, birth of a child, divorce, loss of other coverage, or change of employment. SEPs give the member a 30-60 day window to make changes outside the normal annual cycle. From an operations standpoint, open enrollment is a massive coordinated effort — thousands of 834 transactions, new ID cards, benefits reconfigurations — while SEPs are continuous, lower-volume, but require faster turnaround."

**4. "How would you reduce eligibility errors at the pharmacy counter?"**

> "The root cause is usually a timing gap — enrollment was processed in the medical payer's system but hasn't propagated to the PBM, which is often a separate platform with its own member file. I'd attack this at three points: first, improve the 834 feed latency to the PBM so changes propagate within hours, not days. Second, implement real-time eligibility checking at the pharmacy point-of-sale so the pharmacist can see live status rather than a cached file. Third, give members a self-service digital ID card that refreshes in real-time from the payer's system, so they have proof of coverage even before the physical card arrives. All three of these are much more achievable now that payers have FHIR Coverage APIs that can serve real-time data."

**5. "What's retroactive enrollment and why is it hard?"**

> "Retroactive enrollment means the member's coverage is backdated to a date in the past — for example, a baby is born on June 1 but the parents don't notify HR until June 20. The payer creates coverage retroactive to June 1. The problem is that claims may have already been submitted and adjudicated during that gap — the newborn's hospital stay was billed to self-pay. Now the payer must reprocess those claims, and providers need to rebill. Retroactive terminations are even more painful — the payer has already paid claims for a member who should have been terminated weeks ago, and must claw those payments back from providers. Both scenarios create significant administrative rework, provider abrasion, and member dissatisfaction."

---

# GAP 3 — Medicare / Medicaid (especially Medicare Advantage)

## Medicare Structure (Memorize Cold)

- **Part A** — Hospital insurance (mostly free, payroll-tax funded)
- **Part B** — Physician/outpatient (premium-based)
- **Part C** — **Medicare Advantage (MA)** — private plans bundle A+B, often include D
- **Part D** — Prescription drug coverage

## How Medicare Advantage Makes Money (Critical to Understand)

1. CMS pays MA plan a **monthly capitated PMPM** (per member per month)
2. PMPM is **risk-adjusted** using member's diagnosis codes → **HCC (Hierarchical Condition Categories)**
3. **Sicker member = higher HCC score = higher CMS payment**
4. This drives massive payer investment in:
   - **Risk adjustment coding accuracy** (capturing all conditions)
   - **HEDIS quality measures** (drives Star Ratings)
   - **Care management** (keeping members healthy to control costs)

## Star Ratings (You MUST Know This)

- CMS publishes annual ratings for each MA plan, 1-5 stars
- ~40 measures across clinical quality (HEDIS), member experience (CAHPS), health outcomes (HOS), and operations
- **4+ stars = quality bonus payment** (~5% bonus on capitation)
- **5 stars** = year-round enrollment, marketing advantage
- Drops below 3 stars repeatedly = CMS can terminate the contract
- **Cut points change every year** — moving target

## Key Calendar (Operational Beats)

- **AEP — Annual Enrollment Period**: Oct 15 - Dec 7 (members pick plan for next year)
- **OEP — MA Open Enrollment**: Jan 1 - Mar 31 (one switch allowed)
- **Star Ratings released**: October (for plan year starting next Jan)
- **Bid submission to CMS**: June (for next plan year)

## Risk Adjustment Mechanics

- **HCC coding** — providers document conditions in claims/encounters
- **RAPS** (Risk Adjustment Processing System) — legacy submission format
- **EDPS** (Encounter Data Processing System) — newer, more granular
- **RADV audits** — CMS audits to verify HCC codes are supported by medical records
- **Chart review programs** — payers review records to find missed HCC codes (legal but scrutinized)

## Medicaid Basics

- **State-administered**, federally co-funded
- **Managed Medicaid** — states contract with MCOs (Centene, Molina, Anthem) to manage members
- **CHIP** — Children's Health Insurance Program
- **Dual Eligibles (D-SNP)** — qualify for both Medicare + Medicaid; very complex coordination
- **Waiver programs** (1115, 1915) — state-specific innovation

---

## Remittance Workflow — Is It Different for Medicare and Medicaid?

Yes — same EDI standard (835), but the **who sends it and who the money comes from** differs significantly. This matters for PM interviews.

### Medicare FFS (Traditional Medicare)

```
Provider submits 837 → goes to MAC (Medicare Administrative Contractor)
        ↓
MAC adjudicates (not a private insurer — CMS-contracted entity)
        ↓
MAC sends 835 remittance back to provider
        ↓
Payment comes from CMS trust funds (Part A = Hospital Insurance Trust Fund,
Part B = Supplementary Medical Insurance Trust Fund)
```

- There are **12 MACs** nationally — each covers a geographic jurisdiction
- **Noridian, Novitas, CGS, WPS, NGS** are examples of MACs
- Providers must enroll with the correct MAC for their region
- **PECOS** (Provider Enrollment, Chain and Ownership System) is how providers enroll to bill Medicare — separate enrollment from commercial payers
- **Medicare timely filing is strict: 1 year from date of service** (commercial payers allow 90-180 days)
- **No clearinghouse required** — MACs accept 837 directly, though many providers still use clearinghouses for workflow consistency

### Medicare Advantage (Part C)

```
Provider submits 837 → to MA Plan (Humana, UHC, BCBS MA plan)
        ↓
MA Plan adjudicates using their own CAPS (Facets/QNXT/HealthRules)
        ↓
MA Plan sends 835 remittance back to provider
        ↓
Payment comes from CMS capitation funds held by the MA plan
```

- Same 837/835 EDI as commercial — **no difference in format**
- MA plans set their OWN timely filing windows (typically 90-180 days like commercial)
- **Key difference from Medicare FFS**: MA plans can have their own prior auth requirements, networks, and formularies. Providers must learn each MA plan's rules separately — not just "Medicare rules."
- MA plans pay providers via **contracted rates** — not Medicare fee schedule (unless they choose to align)

### Medicaid (Managed Medicaid)

```
Provider submits 837 → to MCO (Centene/Molina/Anthem Medicaid plan)
        ↓
MCO adjudicates
        ↓
MCO sends 835 remittance
        ↓
Payment comes from state + federal capitation funds held by the MCO
```

- **Each state has different rules** — Medicaid is state-administered, so 835 content, timely filing windows, and prior auth rules all vary by state
- **Medicaid FFS** (in states that don't use MCOs): State Medicaid agency processes directly — similar to how MACs work for Medicare FFS
- **MMIS** (Medicaid Management Information System) — the state's own claims system, similar to Facets but state-built; providers submit 837 to this

### Summary Table — Remittance Differences

| Program | Who Sends 835 | Money Source | Timely Filing | Key Nuance |
|---|---|---|---|---|
| **Commercial** | Insurance company or clearinghouse | Insurer pool or employer account | 90-180 days | Companion guides vary by payer |
| **Medicare FFS** | MAC (12 regional contractors) | CMS trust funds | **1 year** (strict) | PECOS enrollment required |
| **Medicare Advantage** | MA Plan (private insurer) | CMS capitation at plan level | 90-180 days (plan-set) | Plan has own prior auth, network, formulary |
| **Medicaid MCO** | MCO (Centene, Molina, etc.) | State + federal capitation | Varies by state | State-specific rules — not uniform |
| **Medicaid FFS** | State MMIS | State + federal general funds | Varies by state | State system, not private payer |

---

## FHIR Role in 2026 — Medicare/Medicaid Specific

This is where your workspace directly connects. FHIR mandates in this space are **government-driven**, not just commercial payer-driven.

| Function | Legacy | FHIR Role in 2026 | Your Work |
|---|---|---|---|
| **Member accesses their Medicare claims data** | Paper EOB, MyMedicare.gov | **BCDA (Blue Button 2.0 API)** — CMS's own FHIR API for MA plan members | Your Phase 8 Bulk Data API mirrors the BCDA pattern |
| **MA plan receives encounter data from CMS** | EDPS/RAPS files | FHIR Bulk Data export (CMS moving toward FHIR-based encounter exchange) | Your Phase 8 BulkDataAPI |
| **Prior auth for MA members** | Fax / portal | **CMS-0057 mandates FHIR PAS by 2027** — MA plans are in scope | Your Phase 6 PriorAuthAPI |
| **Payer-to-payer when member switches MA plan** | Phone / fax / nothing | **FHIR PDex mandated** — when member moves between MA plans, clinical data must transfer | Your Phase 7 PDexAPI |
| **Provider checks MA member eligibility** | 270/271 | FHIR Coverage resource via CRD — your Phase 5 | Your Phase 5 CRDService |
| **Medicaid member accesses their data** | Nothing | **CMS-9115-F extends to Medicaid MCOs** — they must expose FHIR Patient Access API | Your Phase 2 MemberAccessAPI pattern |
| **Risk adjustment / HCC capture from clinical data** | Chart review programs, EDPS | FHIR-based clinical data feeds (diagnostic codes in FHIR Condition resource) | Emerging — not yet mandated |

### CDS Hooks — Your CRDService Directly Serves This

Your **Phase 5 CRDService** uses **CDS Hooks** — a HL7 standard that allows a payer's clinical rules engine to fire **inside the clinician's EHR workflow** in real time. This is the most sophisticated FHIR integration in your workspace.

```
Doctor opens order entry screen in Epic (or any CDS Hooks-capable EHR)
        ↓
EHR fires a CDS Hook event ("order-select" or "order-sign")
        ↓
Your CRDService receives the hook + FHIR context (patient, order, coverage)
        ↓
CRDService checks: Is this covered? Does it need prior auth?
        ↓
Returns a "card" directly into the EHR: "Prior auth required. Launch DTR."
        ↓
Doctor sees this IN THEIR WORKFLOW without leaving Epic
```

**Why this matters for MA/Medicaid**: Prior authorization burden is the #1 complaint from providers treating MA and Medicaid members. Each MA plan has different auth rules. CDS Hooks + your CRDService is the solution CMS mandated in CMS-0057. When an interviewer asks about reducing administrative burden for providers, this is your answer.

### BCDA and Bulk Data — Your Phase 8 Connection

**BCDA (Beneficiary Claims Data API)** is CMS's own FHIR Bulk Data API. MA plans use it to:
- Pull their own members' Part A/B claims from CMS (to supplement their own adjudication data)
- Feed risk adjustment and care management programs
- Support population health analytics

Your Phase 8 BulkDataAPI implements the same FHIR Bulk Data IG (`$export` operation, ndjson output, async pattern). If an interviewer asks about large-scale data exchange in Medicare, you can say: *"I've built the server-side of exactly that pattern — the FHIR Bulk Data export that BCDA uses."*

---

## AI Role in 2026 — Medicare/Medicaid

| Area | AI Application | Status in 2026 |
|---|---|---|
| **HCC risk capture** | NLP reads clinical notes to find undocumented or under-coded HCC conditions — massive revenue impact for MA plans | Active — Cognizant, Optum, Datavant, Inovalon |
| **Star Ratings prediction** | ML models predict which HEDIS measures an MA plan is at risk of dropping, trigger interventions early | Active — most large MA plans |
| **Care gap closure** | AI identifies members overdue for screenings (mammograms, A1c checks) and triggers automated outreach | Active — connects to FHIR Bulk Data + population health |
| **Prior auth automation (MA)** | LLM extracts clinical criteria from EHR notes, auto-populates PAS requests — your CRDService + DTR workflow | Active — CMS-0057 driving adoption |
| **RADV audit defense** | AI assists in chart review to identify documentation gaps before CMS audits | Active — payer compliance teams |
| **Dual eligible coordination** | AI surfaces relevant clinical history from both Medicare and Medicaid records to care coordinators | Emerging — requires PDex / TEFCA data sharing |
| **Fraud detection** | ML detects aberrant billing in Medicaid (high fraud rate vs commercial) | Active — OIG, state Medicaid agencies, MCOs |
| **Member chatbot for MA** | LLM-powered chatbot answers "what's my plan cover?" "who's in my network?" using FHIR Coverage resource | Active — directly connects to your MemberAccessAPI + FHIR MCP work |

**Your CDS Hooks differentiator**: Most PM candidates don't know what CDS Hooks is. You built it. When the question is "how does AI fit into care management or prior auth in an MA plan?" you can say: *"I've implemented a CDS Hooks service that fires inside the EHR workflow — the doctor writes an order, our service returns coverage determination in real time, eliminating the administrative back-and-forth that costs MA plans and providers millions."*

---

## Interview Questions — With Model Answers

**1. "Explain Medicare Advantage in simple terms."**

> "Medicare Advantage is the private-plan alternative to traditional Medicare. Instead of CMS paying doctors and hospitals directly, CMS pays a fixed monthly amount to a private insurer — Humana, UHC, BCBS — to cover the member. That payment is risk-adjusted based on how sick the member is, using HCC codes derived from their diagnosis history. The private plan then adjudicates claims, manages the network, and can offer extra benefits — dental, vision, gym memberships — that traditional Medicare doesn't cover. From a business model perspective, the plan profits when the capitation it receives from CMS exceeds the claims it pays out, which is why care management and keeping members healthy is a financial priority, not just a clinical one."

**2. "How do Star Ratings work and why do they matter to a payer?"**

> "CMS publishes annual 1-5 star ratings for every Medicare Advantage plan based on about 40 measures — clinical quality measures from HEDIS like diabetes control and cancer screenings, member experience from CAHPS surveys, operational measures like appeals resolution, and health outcomes. A plan that achieves 4 or more stars gets a quality bonus payment — roughly 5% more capitation from CMS. 5-star plans also get year-round enrollment, which is a huge marketing advantage since every other plan can only enroll during the Annual Enrollment Period. Dropping below 3 stars for multiple years triggers CMS enforcement. So Star Ratings are not just a quality metric — they directly affect revenue. A PM at a health plan gets pulled into Star Ratings improvement programs because improving HEDIS scores, reducing prior auth denials, and improving member satisfaction all contribute to the rating."

**3. "What is risk adjustment and HCC coding?"**

> "Risk adjustment is the mechanism CMS uses to make sure MA plans are paid fairly for the health status of their members, not just the count of members. A plan with sicker members costs more to serve, so CMS pays them more. The way health status is measured is through HCC — Hierarchical Condition Categories. These are groupings of ICD-10 diagnosis codes that represent costly chronic conditions: diabetes with complications, heart failure, COPD, cancer. Each HCC has a risk factor. The sum of a member's HCCs produces a risk score. A risk score of 1.0 is average — a score of 1.5 means CMS pays 50% more per month for that member. This is why MA plans invest heavily in coding accuracy — finding and properly documenting every HCC condition a member has can mean tens of millions in additional CMS payment at scale. RAPS and EDPS are the submission systems that feed this data to CMS."

**4. "Why is the Annual Enrollment Period such a big operational event?"**

> "AEP — October 15 to December 7 — is when all Medicare beneficiaries can switch MA plans or move back to traditional Medicare for the following year. For a health plan, this is both an opportunity and a threat. You're trying to retain your existing members while capturing members switching away from competitors. From an operations standpoint, it's an enormous wave: new enrollments coming in via CMS, member ID cards being generated, care management records being set up, prior auth programs restarted for new members, provider network validations running. And because Star Ratings are released in October, right at the start of AEP, a plan that just dropped a star is defending against mass member attrition simultaneously. It's the single most intense operational period of the year for an MA plan."

**5. "How do MA plans differ from traditional Medicare?"**

> "Traditional Medicare is fee-for-service, administered directly by CMS through regional MACs. A provider submits an 837 to the MAC, gets paid per service at the Medicare fee schedule, no network restrictions. Medicare Advantage is capitated — CMS pays a private plan monthly, and that plan manages everything: network, prior auth, formulary, and adjudication. MA plans can restrict network (HMO vs PPO), require prior authorizations that traditional Medicare doesn't require, and offer supplemental benefits. From a provider's perspective, billing an MA plan is more like billing a commercial payer — each plan has its own rules, companion guides, and timely filing windows — whereas traditional Medicare has one national standard through the MACs. For a PM, the complexity in MA is an opportunity: prior auth workflows, provider portal integrations, member engagement tools — all of these are needed because MA is a privately-administered product, not a government-administered one."

**6. "What's a Dual Eligible and why is it complex?"**

> "A Dual Eligible is a person who qualifies for both Medicare and Medicaid simultaneously — typically elderly or disabled individuals with very low income. Medicare is their primary coverage, Medicaid fills in the gaps — copays, deductibles, services Medicare doesn't cover like long-term care. The complexity comes from coordination: two separate programs, two separate benefit structures, two separate eligibility and enrollment systems. There are D-SNPs — Dual-Eligible Special Needs Plans — which are MA plans specifically designed to coordinate Medicare and Medicaid for these members. From a data perspective, their records live in both CMS systems and state Medicaid MMIS systems. Getting a complete clinical picture requires exchanging data across both — which is exactly the problem FHIR PDex and TEFCA are designed to solve. Dual eligibles also tend to be the sickest, most complex members with the highest cost, so managing their care well has enormous financial impact."

**7. "What's the difference between Medicare and Medicaid?"**

> "Medicare and Medicaid are both government health programs administered by CMS, but they serve completely different populations, are funded differently, and operate through different mechanisms. Medicare is a federal program for people 65 and older, or younger people with certain disabilities or end-stage renal disease. It doesn't consider income — you qualify based on age or disability, not how much money you have. Medicaid is income-based — it covers people below certain income thresholds, primarily low-income adults, children, pregnant women, and people with disabilities. The funding difference is equally important: Medicare is funded entirely by the federal government through payroll taxes and premiums. Medicaid is jointly funded — the federal government pays a percentage called FMAP (Federal Medical Assistance Percentage), which varies by state, and states fund the rest. That shared funding means states have significant flexibility in how they run Medicaid — eligibility rules, covered services, and payment rates all vary by state. Medicare is much more nationally uniform. On the delivery side, traditional Medicare is administered directly by CMS through regional MACs — Medicare Administrative Contractors — and has a national fee schedule. Medicaid is administered by each state, most of which contract with private managed care organizations — Centene, Molina, Anthem — to run Managed Medicaid. From a payer IT perspective, Medicare Advantage works like a commercial payer: prior auth, network management, HEDIS, Star Ratings. Medicaid MCOs also work like commercial payers but with state-specific rules, lower reimbursement rates, and populations that tend to have more social determinants of health complexity."

| Dimension | Medicare | Medicaid |
|---|---|---|
| **Who qualifies** | Age 65+, or disabled under 65, or ESRD | Low income — adults, children, pregnant women, disabled |
| **Income test** | No — age/disability based | Yes — income and asset limits |
| **Funded by** | Federal government (payroll tax + premiums) | Federal (FMAP) + state (shared funding) |
| **Administered by** | CMS (FFS via MACs) or private MA plans | Each state — most use MCOs (Managed Medicaid) |
| **Uniformity** | National standard (FFS) | State-by-state variation |
| **Private plan option** | Part C = Medicare Advantage | Managed Medicaid MCOs (Centene, Molina, Anthem) |
| **Drug coverage** | Part D (separate) | Included in Medicaid benefit (state-managed) |
| **Prior auth (payer)** | FFS: limited / MA: full prior auth | MCO: full prior auth, state-specific rules |
| **Timely filing** | FFS: 1 year (strict) / MA: plan-set | MCO: varies by state |
| **Claims system** | MACs (FFS) / MA plan CAPS | State MMIS (FFS) / MCO CAPS |
| **FHIR mandate scope** | MA plans — CMS-9115-F and CMS-0057-F | Medicaid MCOs — same mandates apply |
| **Star Ratings** | Yes — drives QBP bonuses | No (HEDIS tracked but no Star/QBP equivalent) |
| **Dual eligible** | Primary payer | Secondary — fills in Medicare gaps |

**8. "Can you explain what Medicare and Medicaid are?"**

> "Medicare and Medicaid are both federal health programs but they serve entirely different populations and work very differently. Medicare is an entitlement program — you earn it. You qualify at 65 if you've paid Medicare payroll taxes for at least 10 years, or at any age if you have a qualifying disability, end-stage renal disease, or ALS. Income doesn't matter at all — a billionaire turns 65 and gets Medicare the same as anyone else. It has four parts: Part A covers hospital stays and is premium-free for most people; Part B covers physician and outpatient services and has a monthly premium; Part C is Medicare Advantage — the private plan option where CMS pays a private insurer like Humana or UHC a monthly capitated amount to manage the member's care; and Part D covers prescription drugs.
>
> Medicaid is completely different — it's a needs-based program funded jointly by the federal government and each state. You qualify based on income, not age or work history. The ACA expanded Medicaid in most states to cover all adults earning up to 138% of the Federal Poverty Level — about $20,800 a year for an individual in 2026. Children, pregnant women, and people with disabilities qualify at higher income thresholds. About 10 states haven't expanded, so in those states coverage can be much more restricted.
>
> The key structural difference is who runs it: Medicare is nationally uniform — the same rules everywhere, administered by CMS through regional contractors called MACs. Medicaid is administered by each state, most of which contract with private managed care organizations like Centene, Molina, and Anthem to run Managed Medicaid. That means every state has different rules, different covered benefits, and different payment rates — which is a major complexity for any PM working on Medicaid IT.
>
> Where they intersect is dual eligibles — about 12 million people who qualify for both. Medicare is primary, Medicaid wraps around it to cover what Medicare doesn't — copays, deductibles, and long-term care like nursing homes. These members are the most complex and highest-cost population in the country. From a FHIR perspective, both programs are in scope for CMS mandates — MA plans and Medicaid MCOs both must implement FHIR Patient Access APIs and FHIR prior authorization APIs by 2027 under CMS-9115-F and CMS-0057-F."

### Medicare Eligibility — Detail

| Qualifying Path | Who | How |
|---|---|---|
| **Age 65+, standard** | Anyone who paid Medicare payroll taxes 40+ quarters (10 years) | Part A free; Part B ~$185/month premium |
| **Age 65+, fewer quarters** | Fewer than 40 quarters paid | Can buy into Part A ($278–$506/month in 2026) |
| **Disability under 65** | Receives SSDI (Social Security Disability Insurance) | Must receive SSDI for **24 months** before Medicare activates — the coverage gap |
| **ESRD (any age)** | End-Stage Renal Disease requiring dialysis or transplant | Eligible immediately — no waiting period |
| **ALS (any age)** | Amyotrophic Lateral Sclerosis (Lou Gehrig's disease) | Eligible immediately — no 24-month wait, unique exception |

**The 24-month SSDI wait** is one of the most criticized gaps in US healthcare — someone becomes permanently disabled at 40 and has to wait 2 years for Medicare coverage. They often fall back on Medicaid during that gap if income-eligible.

### Medicaid Eligibility — Detail

Medicaid uses **FPL (Federal Poverty Level)** as the threshold. In 2026: ~$15,060/year for an individual, ~$31,200 for a family of 4.

| Population Group | Income Threshold | Notes |
|---|---|---|
| **Adults (ACA expansion states)** | Up to **138% FPL** (~$20,780/year individual) | Expanded by ACA 2010 — ~40 states |
| **Adults (non-expansion states)** | Very narrow — often only parents with dependent children | TX, FL, and ~8 others have not expanded |
| **Children** | Up to 133–200% FPL (varies by state) | CHIP extends to higher income in some states (up to 300% FPL) |
| **Pregnant women** | Up to 133–200% FPL | Coverage extends 12 months postpartum (ARP 2021) |
| **Elderly / disabled (SSI recipients)** | SSI income limits (~$967/month individual in 2026) | SSI recipients are **automatically** enrolled in Medicaid in most states |
| **Medically needy / spend-down** | Higher income but high medical bills | Some states allow "spending down" to qualify |

**ACA Medicaid expansion** (2010): The single biggest change to Medicaid — extended coverage to non-disabled, non-pregnant, childless adults for the first time. States that didn't expand have a **coverage gap**: people earning too much for pre-ACA Medicaid but too little for ACA marketplace subsidies (subsidies start at 100% FPL).

### The One-Line Mnemonics

> **Medicare** = *"Medi-CARE for those who EARNED it"* — age/disability, paid into it through work  
> **Medicaid** = *"Medi-CAID for those who need AID"* — income-based, welfare program

---

### Medicare Parts A / B / C / D — The Structure (Memorize Cold)

Every Medicare interview question eventually touches the four parts. Know them cold.

| Part | Name | What It Covers | Premium | Who Administers |
|---|---|---|---|---|
| **Part A** | Hospital Insurance | Inpatient hospital, skilled nursing facility (SNF), hospice, some home health | **$0** for most (need 40 quarters payroll tax) | CMS / Traditional Medicare |
| **Part B** | Medical Insurance | Doctor visits, outpatient services, preventive care, durable medical equipment (DME), some home health | **~$185/month** (2026, income-adjusted via IRMAA for high earners) | CMS / Traditional Medicare |
| **Part C** | Medicare Advantage (MA) | **Replaces** A + B + usually D. Private plan manages all care. Must cover all A/B benefits, often adds dental/vision/hearing | Varies — often $0 premium plans (payer subsidizes from CMS capitation) | **Private insurers** (Humana, UHC, Aetna, BCBS) under CMS contract |
| **Part D** | Prescription Drug | Outpatient prescription drugs | Varies — standalone PDP or included in MA-PD plan | **Private PDPs** contracted with CMS |

**The A+B vs C distinction** — this is a common interview confusion:
- **Traditional Medicare (FFS)**: Parts A + B + standalone Part D. CMS pays providers directly on a fee-for-service basis. No prior auth. Member pays 20% coinsurance with no cap (hence Medigap).
- **Medicare Advantage (Part C)**: Private insurer takes over — receives monthly capitation from CMS per member, manages ALL care under A+B+D. Can add prior auth, has networks, can have $0 premiums but more restrictions. This is the commercial payer world where your FHIR work lives.

**Medigap (Medicare Supplement)**: Sold alongside Traditional Medicare A+B to cover the 20% coinsurance gap. Does NOT work with Medicare Advantage — members pick one or the other.

**IRMAA** (Income-Related Monthly Adjustment Amount): Part B and Part D premiums increase for individuals earning over ~$106,000/year. A common gotcha — the "free" Medicare isn't truly free for high earners.

---

### Medicaid Program Types — FFS vs MCO vs Waiver

Medicaid is not one program — it is a **collection of state programs** that share federal matching funds (FMAP). States choose how to deliver it.

| Delivery Model | How It Works | States Using It | PM Relevance |
|---|---|---|---|
| **Fee-for-Service (FFS)** | State pays providers directly per service, like traditional Medicare. No managed care middleman. | Rare now — mostly rural areas or legacy states | Legacy EDI flows direct to state; no MCO layer |
| **Managed Care Organization (MCO)** | State contracts with private MCOs (Centene, Molina, Anthem, UHC Community Plan) and pays capitated PMPM. MCO manages the member. | ~75% of all Medicaid members are in MCOs | Your FHIR mandates apply here — CMS-9115-F + CMS-0057-F cover Medicaid MCOs |
| **Primary Care Case Management (PCCM)** | FFS but PCPs are assigned as gatekeepers and paid a small per-member-per-month care coordination fee | Some rural states as transition model | Hybrid — not full managed care |
| **1115 Waiver** | CMS grants a state waiver to run a demonstration program with different rules (work requirements, expansion conditions, special populations) | Many states — e.g., Indiana, Arkansas, Tennessee | High regulatory complexity — rules differ significantly from standard Medicaid |
| **HCBS Waiver (1915c)** | Home- and Community-Based Services — alternative to nursing home placement for elderly/disabled | All states have some form | LTC population, very complex care coordination, FHIR CarePlan is highly relevant |

**Why MCO structure matters for your FHIR work**: When CMS-9115-F says "Medicaid payers must expose a Patient Access API," it means the **MCOs** (Centene's Sunshine Health, Molina Healthcare, Anthem's Medicaid plans, etc.) — not the state itself. The state operates the eligibility determination system; the MCO operates the claims and care management. Your Phase 2–7 work would be built and sold to these MCOs, not to state Medicaid agencies directly.

**FMAP (Federal Medical Assistance Percentage)**: CMS reimburses states a percentage of their Medicaid costs. Ranges from 50% (wealthier states like CT, NJ) to ~83% (poorer states like MS, WV). ACA expansion states get a special 90% FMAP for the expansion population — this is what makes expansion financially attractive despite state politics.

---

# GAP 4 — HEDIS, CAHPS, Care Management, Utilization Management

## HEDIS (Healthcare Effectiveness Data and Information Set)

- Managed by **NCQA** (National Committee for Quality Assurance)
- ~90+ measures across **6 domains**:
  1. **Effectiveness of Care** — diabetes A1c control, breast cancer screening, controlling blood pressure, statin therapy
  2. **Access / Availability of Care**
  3. **Experience of Care**
  4. **Utilization & Risk-Adjusted Utilization**
  5. **Health Plan Descriptive Information**
  6. **Measures Collected Using Electronic Clinical Data Systems (ECDS)** — FHIR-based future

## Data Collection Methods

- **Administrative** — claims data only (cheap, low rates)
- **Hybrid** — claims + medical record review (expensive, higher rates)
- **ECDS** — electronic clinical data (where FHIR is going — connect to your work!)

## Why HEDIS Matters

- Feeds Star Ratings (drives MA bonus revenue)
- Required for NCQA accreditation
- Drives **care gap closure** initiatives — outbound outreach to members for missing screenings

## CAHPS (Consumer Assessment of Healthcare Providers and Systems)

- Annual member satisfaction survey
- Heavily weighted in Star Ratings
- Measures: getting needed care, customer service, plan info, doctor/provider ratings, care coordination

## Care Management — The Four Buckets

> **Why it matters**: Care management is how a payer justifies its existence beyond just paying claims. For Medicare Advantage, care management quality directly drives Star Ratings, capitation bonuses, and member retention. Every bucket below has a financial return model behind it.

| Bucket | What It Does | Who Does It | Platform / Tool | Financial Return |
|---|---|---|---|---|
| **Utilization Management (UM)** | Prior auth, concurrent review, retrospective review | UM nurses, Medical Directors | eviCore, AIM, InterQual/MCG rules | Prevents medically unnecessary spend |
| **Case Management (CM)** | Intensive coordination for high-cost complex members | RN Case Managers (1:1 caseloads) | Jiva, Guiding Care, QNXT CM module | Prevents readmissions, reduces catastrophic episodes |
| **Disease Management (DM)** | Structured programs for chronic conditions | DM coaches, pharmacists, nurses (telephonic) | Hinge Health, Omada, Livongo (now Teladoc) | Improves HEDIS measures, prevents complications |
| **Population Health** | Risk stratification, proactive outreach, gap closure | Analytics team + care coordinators | Innovaccer, Arcadia, Health Catalyst | Star Ratings improvement, risk adjustment accuracy |

---

### Bucket 1 — Utilization Management (UM)

**The core function**: UM controls what services get paid before, during, and after they occur. It is the primary cost-containment lever for a payer.

**Three review types**:

| Review Type | When It Happens | Who Initiates | Decision |
|---|---|---|---|
| **Pre-authorization (Pre-auth)** | Before service is rendered | Provider submits PA request | Approve / Deny / Pend for clinical review |
| **Concurrent review** | During an inpatient stay (ongoing) | Payer UM nurse reviews daily | Approve continued days / Initiate discharge planning |
| **Retrospective review** | After service is rendered | Triggered by claim submission | Pay / Deny / Downcode |

**Medical necessity criteria engines**:
- **InterQual** (Change Healthcare / Optum): evidence-based criteria sets used for inpatient admission, continued stay, procedure authorization. Widely used by commercial and MA plans.
- **MCG (Milliman Care Guidelines)**: competing criteria set, used by some Blues plans and regional payers. Similar function — structured decision criteria for authorization.
- **Payer-developed criteria**: some large payers (UHC, Aetna) develop their own criteria for certain service lines. These are filed with state DOIs and must be disclosed on request.

**UM staffing model**:
- **UM nurses**: front-line reviewers — apply criteria, make initial determinations, handle routine approvals
- **Medical Director (MD)**: all denials must be reviewed and signed by a physician Medical Director — this is a CMS and state regulatory requirement, not optional
- **Peer-to-peer (P2P) review**: when a provider disagrees with a UM denial, they can request a call with the payer's Medical Director to discuss clinical rationale — often results in overturn if the provider presents additional clinical context

**CMS UM compliance requirements for MA**:
- Cannot use proprietary criteria that are more restrictive than Traditional Medicare without clinical basis
- Must offer expedited PA (72-hour decision) when standard timeline would risk member health
- 2024 CMS Final Rule: AI/algorithms cannot be the sole basis for a coverage denial — a physician must review

**Your connection**: Your CRD → DTR → PAS workflow is the modern digital implementation of UM. CRD is the pre-auth request, PAS is the submission, DTR is the criteria collection. You have built the technical infrastructure that replaces fax-based UM.

---

### Bucket 2 — Case Management (CM)

**The core function**: Case management provides intensive, personalized care coordination for the highest-risk, highest-cost members. While DM covers populations (100s to 1000s of members per coordinator), CM is 1:1 — one RN case manager owns a relationship with a complex member.

**Who gets case management**:

| Trigger | Example | Why CM Activates |
|---|---|---|
| **High-cost diagnosis** | Organ transplant, cancer, NICU admission | These cases can run $500K–$2M+ — intensive coordination prevents complications |
| **Multiple comorbidities** | CHF + CKD + Diabetes + Depression | Fragmented care → preventable hospitalizations |
| **Frequent ER/inpatient** | 3+ ER visits in 6 months, 2+ hospitalizations/year | "Frequent flier" pattern signals unmanaged condition |
| **Transition of care** | Discharged from SNF/hospital | 30-day post-discharge window has highest readmission risk |
| **Social determinants (SDOH)** | Housing insecurity, food insecurity, no transportation | SDOH drives 30–40% of health outcomes — CM connects members to community resources |

**What a case manager actually does**:
- Conducts comprehensive assessment (telephonic or in-home)
- Develops an individualized care plan (FHIR CarePlan resource)
- Coordinates across all providers — PCP, specialist, hospital, home health
- Monitors medication adherence, specialist follow-up, lab results
- Removes barriers: arranges transportation, connects to social services, follows up on missed appointments
- Documents everything in the CM platform (Jiva, Guiding Care)

**Financial model**: A heavy CM program costs $500–$1,500 per member per year in coordinator time. If it prevents one $80,000 hospitalization, the ROI is 50:1. Case management is one of the highest-ROI investments a payer can make — but it requires identifying the right members (risk stratification) and reaching them (member engagement).

**FHIR connection**: FHIR CarePlan resource is the standard container for a member's care management plan. Phase 2 MemberAccessAPI exposes CarePlan data — a member can see their care plan, upcoming tasks, and assigned case manager via the Patient Access API. This is a CMS interoperability requirement.

---

### Bucket 3 — Disease Management (DM)

**The core function**: DM delivers structured, evidence-based programs to members with specific chronic conditions — at population scale. Unlike CM (1:1), DM uses standardized protocols delivered telephonically, digitally, or in-person to large cohorts.

**Primary target conditions**:

| Condition | Why DM Focus | Typical DM Interventions | HEDIS Measure Connection |
|---|---|---|---|
| **Diabetes (Type 2)** | Highest cost chronic condition — $16K+ per member/year | A1C monitoring coaching, foot care education, medication adherence | HbA1c Control (< 8%), Diabetes Eye Exam |
| **Congestive Heart Failure (CHF)** | #1 cause of hospital readmissions | Daily weight monitoring, medication adherence, sodium restriction education | — |
| **COPD / Asthma** | Preventable ER visits, medication non-adherence | Inhaler technique, trigger avoidance, action plan | — |
| **Hypertension** | Silent — poor adherence leads to stroke, CKD | Blood pressure monitoring, medication education | Controlling High Blood Pressure (CBP) |
| **Depression / Behavioral Health** | Comorbidity amplifier — worsens all other conditions | PHQ-9 screening, therapy referral, medication follow-up | Antidepressant Medication Management |
| **Obesity** | Root driver of diabetes, CHF, hypertension | Weight loss programs, behavioral coaching | BMI assessment, weight counseling |

**Digital health integration** (the modern DM landscape):
- **Livongo (now Teladoc Health)**: connected glucose meters + real-time coaching for diabetes — sends data directly to DM platform
- **Omada Health**: digital behavioral change program for diabetes prevention (CDC-recognized DPP)
- **Hinge Health**: digital physical therapy for MSK (musculoskeletal) — addresses back pain, which is a top cost driver
- Payers contract with these vendors, pay per enrolled member per month (PMPM), track outcomes via claims and biometric data

**DM staffing model**: DM nurses/coaches carry 200–500 member caseloads (vs. 25–50 for case management). Outreach is telephonic + app-based. Enrollment is voluntary — member engagement rate (how many accept the program) is a key program KPI.

**Financial model**: DM programs target 10–15% reduction in hospitalization and ER visits for the managed population. For a diabetes DM program, successful A1C control avoids complications (retinopathy, nephropathy, amputation) that cost $50K–$300K. Payers typically track medical cost trend for DM enrollees vs. a matched control group.

---

### Bucket 4 — Population Health Management

**The core function**: Population health is the upstream function that feeds all three buckets above. It answers the question: *"Of our 500,000 members, which ones need UM oversight, CM enrollment, or DM outreach — and who is going to get expensive if we don't act now?"*

**The core workflow**:

```
Data Ingestion
├── Claims data (diagnoses, procedures, medications, costs)
├── EHR / clinical data (FHIR Bulk Data — labs, vitals, conditions)
├── Pharmacy data (PBM — medication adherence, fills, gaps)
├── SDOH data (census, community surveys, address-based risk scores)
└── Biometric data (wearables, connected devices — growing)
        ↓
Risk Stratification Engine
├── HCC (Hierarchical Condition Category) risk scores — CMS model
├── Proprietary payer risk models (gradient boosting, logistic regression)
├── Episode-based risk (likelihood of hospitalization in next 12 months)
└── SDOH risk layering
        ↓
Segmentation — assigns members to buckets:
├── Rising risk → Disease Management outreach
├── High risk → Case Management referral
├── Imminent crisis → UM watchlist (concurrent review flag)
└── Healthy → Preventive outreach (gap closure, wellness)
        ↓
Outreach & Engagement
├── Care coordinator calls
├── App / portal notifications
├── PCP alerts (care gap notifications in EHR)
└── Community health worker (CHW) programs
        ↓
Measure → HEDIS, Star Ratings, cost trend, readmission rate
```

**Platforms used**:
- **Innovaccer**: Data activation platform — aggregates clinical + claims + SDOH, surfaces gaps in care, integrates with Epic/Cerner for PCP alerting
- **Arcadia**: Analytics platform — strong HEDIS reporting, ACO management, risk stratification
- **Health Catalyst**: Data Operating System (DOS) — ETL + analytics + care management workflow
- **Lightbeam**: Population health for risk-bearing entities (ACOs, MA plans)

**Gap closure — the HEDIS connection**: Population health platforms identify members who are *due* for preventive services (mammogram, colonoscopy, A1C test, diabetic eye exam) but haven't received them. These are HEDIS gaps. Closing them:
1. Improves HEDIS measure rates → improves Star Ratings → triggers CMS Quality Bonus Payments (QBP)
2. Improves risk adjustment accuracy (more diagnoses documented)
3. Reduces downstream costs (preventive care is cheaper than treating complications)

**FHIR / Bulk Data connection**: Phase 8 (Bulk Data API) is the infrastructure that makes population health work. Instead of payer analysts running SQL queries against a stale data warehouse, FHIR Bulk Data provides a standardized, near-real-time feed of clinical data from every EHR that the payer's members use. This is what enables meaningful risk stratification across a network.

---

### Care Management Economics — How It All Ties Together

```
Total Payer Medical Cost = (Claims Paid) + (Admin Cost) - (Care Mgmt Savings)

Care Mgmt Savings decomposed:
├── UM savings:           Prevented medically unnecessary services
├── CM savings:           Prevented readmissions, prevented complications
├── DM savings:           Reduced hospitalizations, reduced ER visits
└── Population Health:    Earlier intervention → lower-acuity treatment
```

**MA Quality Bonus Payments (QBP)**: Plans rated 4+ Stars receive a 5% bonus on their capitation rate from CMS. Plans rated 5 Stars receive a 5% bonus PLUS the right to enroll members year-round (instead of only during AEP). For a plan with 200,000 members at $1,200 PMPM, 5% = **$144M/year in bonus payments**. This is why care management is not a cost center — it is a direct revenue driver.

---

### PM Interview Answer — Care Management

> *"Care management has four buckets: UM prevents unnecessary services through prior auth and clinical review criteria; case management provides intensive 1:1 coordination for the 2–3% of members who drive 30% of costs; disease management delivers structured chronic condition programs at scale using evidence-based protocols and digital tools like Livongo and Omada; and population health is the upstream analytics layer that identifies which members belong in each program. For Medicare Advantage, all four connect directly to Star Ratings — and Stars determine whether a plan gets CMS quality bonus payments, which can be worth hundreds of millions of dollars annually. That financial linkage is why care management is the highest-ROI investment a payer can make."*

---

## Utilization Management Deep Dive (Your Sweet Spot)

- **Prior Authorization** — approval before service rendered
- **Concurrent review** — during inpatient stay, deciding continued necessity
- **Retrospective review** — after service, paying or denying
- **Peer-to-peer review** — payer MD discusses with provider MD
- **Medical necessity criteria** — InterQual, MCG (Milliman Care Guidelines)
- **Appeals process** — when UM denies

## Grievances & Appeals (G&A) — How It Actually Works

**CMS-regulated**, especially for Medicare Advantage and Medicaid. These are two distinct processes that interviewers often mix up.

### Grievance vs Appeal — The Core Distinction

| | Grievance | Appeal |
|---|---|---|
| **What triggers it** | Complaint about experience, service quality, or access | Challenge to a coverage or payment decision |
| **Examples** | Rude staff, long wait time, provider not returning calls, can't get an appointment | Claim denied, service not authorized, drug not covered, prior auth denied |
| **Is a clinical decision being reversed?** | No | Yes — you're asking the payer to reverse their determination |
| **CMS timeline (MA)** | 30 days to resolve | Standard: 30 days / Expedited: 72 hours |
| **Who reviews** | Member services / quality team | Medical director or independent reviewer |

### Grievance Workflow

```
Member / Provider calls or writes to file a complaint
        ↓
Payer logs it in G&A tracking system
        ↓
Member services team investigates (calls provider, reviews records)
        ↓
Payer sends written resolution to member within regulatory timeline
        ↓
If member unsatisfied → escalates to CMS if unresolved (MA)
```

- Grievances feed into **CAHPS survey scores** — if members report poor experiences at scale, Star Ratings drop
- Payers track grievance volume by category to identify operational failures

### Appeal Workflow (Clinical / Coverage Appeals)

```
Coverage decision made (Prior auth denied, claim denied)
        ↓
Member or Provider files Level 1 Appeal (Reconsideration)
Payer's own medical director reviews
        ↓
   If upheld → Level 2 Appeal
   (MA: Independent Review Entity — IRE, contracted by CMS)
        ↓
   If upheld → Level 3 (ALJ — Administrative Law Judge)
        ↓
   If upheld → Level 4 (Medicare Appeals Council — MAC)
        ↓
   If upheld → Level 5 (Federal District Court)
```

**For commercial plans**: Typically 2-3 internal levels, then state insurance commissioner, then external independent review.

**For Medicaid**: State-specific, but federally required — typically 2 internal levels then state fair hearing.

### Why Appeal Overturn Rates Matter

- If a payer denies aggressively and loses 60%+ of appeals → signals poor clinical criteria or bad policy
- **CMS tracks MA appeal overturn rates** — high overturn rate = Star Rating hit
- From a PM perspective, high overturn rate is a signal to fix the upstream prior auth process — better criteria, better CDS Hooks decision support, less over-denial

### Key Timelines (Memorize for MA)

| Type | Standard Decision | Expedited Decision |
|---|---|---|
| Prior auth request | 14 days | 72 hours (if delay = risk to health) |
| Appeal (Level 1) | 30 days | 72 hours |
| Grievance | 30 days | 24 hours (urgent) |
| Organization determination | 14 days | 72 hours |

**Expedited** = member or provider certifies that standard timeline would seriously jeopardize member's health. Payer must grant expedited review if they agree.

---

## FHIR Role in 2026 — HEDIS / Care Management / UM

| Function | Legacy | FHIR Role in 2026 | Your Work |
|---|---|---|---|
| **Prior authorization (UM)** | Fax, portal, phone | **FHIR PAS (CMS-0057 mandate 2027)** — PA request/response in FHIR | Your Phase 6 PriorAuthAPI |
| **Coverage determination at point of care** | None (provider had to call) | **CDS Hooks + CRD** — payer rules fire inside EHR at order entry | Your Phase 5 CRDService |
| **DTR — populate prior auth forms** | Manual fax / PDF forms | **FHIR DTR auto-populates** from EHR data, no manual entry | Your Phase 5 DTR capability |
| **Population health / care gap closure** | Claims data batch extract | **FHIR Bulk Data ($export)** — payer pulls clinical data at scale | Your Phase 8 BulkDataAPI |
| **HEDIS ECDS measures** | Claims + hybrid chart pull | **FHIR-based ECDS measures** — structured clinical data directly from EHR via FHIR | Emerging — NCQA roadmap |
| **Member accesses care management plan** | Paper, phone call | FHIR CarePlan resource via Patient Access API | Your Phase 2 MemberAccessAPI |
| **Provider gets clinical data for concurrent review** | Phone / fax | FHIR DocumentReference / clinical notes via PDex | Your Phase 7 PDexAPI |
| **Appeals — clinical evidence submission** | Fax clinical notes | FHIR DocumentReference / DiagnosticReport attached to appeal | Emerging |

### The CRD → DTR → PAS Chain — Your Full UM Workflow

This is the single most important thing to explain in an interview when asked about Utilization Management:

```
[STEP 1 — CRD: Coverage Requirements Discovery]
Doctor selects "Order MRI — lumbar spine" in Epic
        ↓
Epic fires CDS Hook "order-select" to your CRDService
CRDService queries Coverage + formulary + prior auth rules
        ↓
Card returned inside Epic: "Prior auth required. Click to launch DTR."

[STEP 2 — DTR: Documentation Templates & Rules]
Doctor clicks the card → FHIR DTR questionnaire launches
Your DTR service auto-fills patient data from EHR (diagnosis, history, meds)
Doctor reviews and adds clinical justification (2 minutes, not 20)
        ↓
Completed FHIR QuestionnaireResponse returned

[STEP 3 — PAS: Prior Authorization Support]
Your PriorAuthAPI submits FHIR PAS request to payer
Payer's rules engine evaluates
        ↓
   ├── Real-time approval → doctor proceeds
   └── Pended → goes to payer UM nurse for review (within 72h)
```

Without this workflow: phone tag, fax, 2-5 days delay, $11B/year in administrative waste.
With this workflow: real-time or near-real-time, fully within EHR, no separate portal login.

---

## AI Role in 2026 — HEDIS / Care Management / UM

| Area | AI Application | Status in 2026 |
|---|---|---|
| **Care gap closure** | AI identifies members overdue for preventive care (mammogram, A1c, colorectal screening), auto-generates outreach (phone, SMS, mail) | Active — Arcadia, Innovaccer, Health Catalyst |
| **Prior auth auto-approval** | ML model predicts approval probability from clinical data — high-confidence cases auto-approved without human review | Active — several MA plans and Medicaid MCOs |
| **UM denial prediction** | Predicts which prior auth requests are likely to be denied → prompts clinician to add supporting documentation before submission | Active — |
| **Concurrent review assistance** | AI monitors inpatient stays and flags when clinical criteria for continued stay are no longer met | Active — reduces inappropriate inpatient days |
| **HEDIS measure tracking** | AI tracks each member's HEDIS measure status in real-time from claims + FHIR clinical data, flags who is at risk of becoming a care gap | Active — major MA plans |
| **Grievance NLP classification** | NLP classifies incoming member complaints by category and urgency, routes to the right team | Active — reduces triage time |
| **Appeal letter drafting** | LLM generates clinical appeal letters from medical record data, clinical criteria sources (InterQual), and denial reason | Active — several RCM vendors and startups |
| **Disease management outreach** | AI generates personalized care plans and outreach messages for CHF, diabetes, COPD members | Active — connects to FHIR Bulk Data + CDS Hooks |
| **Population risk stratification** | ML scores each member on likelihood of high-cost event (hospitalization, ER), prioritizes case management outreach | Active — your BulkDataAPI enables this |

**Your differentiator**: When asked about AI in UM, you can say: *"I've implemented the infrastructure layer that makes AI-assisted UM possible — CDS Hooks for real-time decision support at the point of care, PAS for structured prior auth data exchange, and Bulk Data export for population-level analytics. The AI models plug into this infrastructure."*

---

## Interview Questions — With Model Answers

**1. "What is HEDIS and why does it matter to payers?"**

> "HEDIS is the Healthcare Effectiveness Data and Information Set — a set of about 90 standardized quality measures managed by NCQA. It measures whether health plans are delivering effective care to their members: are diabetic members getting A1c tests? Are women getting mammograms? Are blood pressure levels being controlled? Payers care deeply about HEDIS because it feeds directly into Star Ratings for Medicare Advantage — which determines whether the plan gets a quality bonus payment from CMS. A single star rating point up or down can mean hundreds of millions of dollars for a large MA plan. HEDIS also drives NCQA accreditation, which commercial employers require before they'll offer a plan to their employees. The operational implication is that payers run active care gap closure programs — identifying members who haven't received a specific screening and reaching out to get them in."

**2. "How would you close care gaps for diabetic members?"**

> "A diabetic care gap means a member with diabetes is missing a specific HEDIS measure — most commonly an A1c test in the past year, or an eye exam, or LDL cholesterol screening. The closure workflow starts with identification — using claims data and, increasingly, FHIR Bulk Data to pull clinical data from EHRs and identify who's missing what. Once you have the list, you run a multi-channel outreach campaign: automated calls, SMS reminders, letters, and provider-facing alerts in the EHR. For the EHR piece, CDS Hooks is the right mechanism — when a diabetic patient comes in for any visit, a card fires reminding the provider that the A1c is overdue. On the member side, a chatbot or patient portal message tells the member they're due for a test and links them to scheduling. The PM's job is to build and run this pipeline: data identification → outreach → scheduling → documentation — and measure closure rate monthly against the HEDIS denominator."

**3. "What's the difference between Case Management and Disease Management?"**

> "Both are care management programs but they target different populations at different intensity levels. Disease Management is population-level — it runs chronic condition programs for all members with diabetes, heart failure, COPD, or asthma. The programs are largely automated: educational materials, nurse check-in calls, medication adherence outreach. The goal is to prevent those conditions from worsening. Case Management is individual-level and high-touch — it's for the most complex, highest-cost members: transplant patients, NICU newborns, members with cancer, or someone who just had a catastrophic injury. A case manager is personally assigned and coordinates the entire care plan across multiple providers, facilities, and services. The business logic is different: Disease Management manages at scale across thousands of members with low per-member cost. Case Management invests heavily in a small number of members because their potential cost is enormous — managing a transplant member well can save millions."

**4. "Explain Utilization Management."**

> "Utilization Management is the set of processes a payer uses to ensure that the services their members receive are medically necessary, appropriate, and cost-effective. It operates in three timeframes: prior authorization — approval before a service is rendered; concurrent review — ongoing review during an inpatient stay to confirm the continued admission is necessary; and retrospective review — looking at services after the fact and deciding whether to pay. The clinical criteria used are typically InterQual or Milliman Care Guidelines — published evidence-based criteria that define what clinical conditions justify a given service. UM is one of the most contested areas in healthcare because it sits at the intersection of clinical judgment and cost control. When a payer denies a service as not medically necessary, the provider or member can appeal through a structured process. My FHIR work in Phases 5 and 6 is directly modernizing UM — replacing fax-based prior auth with real-time CDS Hooks and FHIR PAS."

**5. "How does your prior authorization FHIR work fit into UM?"**

> "My Phase 5 and 6 work implements the three-part FHIR prior auth workflow: CRD, DTR, and PAS — which maps exactly to the three UM touchpoints. CRD is the Coverage Requirements Discovery service — it uses CDS Hooks to fire inside the EHR at the moment a physician writes an order, telling them in real time whether prior auth is required. This prevents the most common UM failure: service rendered without knowing auth was needed. DTR — Documentation Templates and Rules — auto-populates the prior auth request form directly from the patient's EHR data, removing the 20-minute manual fax process. PAS — the Prior Authorization Support API — submits the structured FHIR request to the payer's UM engine and returns an approval, denial, or pend response. Together, this compresses what used to be a 2-5 day process of phone calls and faxes into a real-time or near-real-time workflow entirely within the EHR. CMS-0057 mandates this be live for MA and Medicaid plans by 2027 — so the work I've built is exactly what every major payer is either building or buying right now."

**6. "What's the difference between a grievance and an appeal?"**

> "A grievance is a complaint about experience — a member is unhappy with how they were treated, couldn't reach their doctor, had a billing issue, or felt staff was unhelpful. There's no coverage decision being reversed. An appeal is a formal challenge to a clinical or coverage decision — a prior auth was denied, a claim wasn't paid, a drug wasn't covered. The member or provider is asking the payer to reconsider and reverse that decision. The workflows are completely different: grievances go to member services and are resolved through investigation and communication. Appeals go through a clinical review process — the payer's medical director reviews first, then an independent review entity if the denial is upheld, and in Medicare Advantage cases can escalate all the way to an Administrative Law Judge or federal court. From a payer operations standpoint, appeal overturn rates are closely watched by CMS because a high rate signals the plan is denying care it shouldn't be — which impacts Star Ratings."

---

# CROSS-CUTTING: Regulatory Awareness (Quick Reference)

The interviewer may test if you understand *why* the FHIR mandates you've built to even exist.

- **HIPAA** (1996) — privacy, security, EDI transaction standards
- **HITECH Act** (2009) — drove EHR adoption
- **ACA** (2010) — created exchanges, individual mandate (now gone), MLR rules
- **MACRA** (2015) — moved physician payment toward value
- **21st Century Cures Act** (2016) — anti-information blocking, paved way for FHIR rules
- **CMS Interoperability and Patient Access Final Rule (CMS-9115-F)** (2020) — REQUIRED payers to expose Patient Access API, Provider Directory API, Payer-to-Payer (your entire workspace exists because of this rule)
- **CMS Prior Authorization Final Rule (CMS-0057-F)** (2024) — requires payers to implement FHIR-based prior auth APIs by 2027 (your DTR/PAS/CRD work is exactly this)
- **No Surprises Act** (2022) — protects from surprise out-of-network bills
- **TEFCA** — Trusted Exchange Framework, national interoperability network (you have a repo on this)

## Likely Interview Questions

1. *"Why did the CMS Interoperability Rule come about?"*
2. *"What is CMS-0057 and how does it affect payers?"*
3. *"How does TEFCA relate to FHIR?"*

---

# FOUNDATIONAL CONCEPTS — EDI, FHIR, TEFCA, EOB & Enrollment

---

## Adjudication — A Ground-Level Explanation

Adjudication is one of those words that sounds complex but describes something very logical. Here is the simplest possible version:

> **Adjudication = the process of deciding whether to pay a claim, how much to pay, and why.**

That's it. Everything else is detail about HOW that decision is made.

### The Analogy

Imagine you submit an expense report at work. Your finance team:
1. Checks that you're actually an employee (eligibility)
2. Checks that the expense is within policy (clinical/coding edits)
3. Checks the amount is reasonable (pricing/fee schedule)
4. Applies your reimbursement cap (benefit application)
5. Approves, partially approves, or rejects (adjudication decision)
6. Sends you the money with a breakdown (remittance/835)

A health insurance claim is exactly this — just with 1,000 policy rules running simultaneously, automated.

### The Five Stages of Adjudication (In Plain English)

```
Stage 1 — INTAKE
"Did we receive this claim correctly? Is it a valid format?"
Clearinghouse already ran format checks; now payer ingests it.
        ↓
Stage 2 — ELIGIBILITY
"Was this member actually covered on the date of service?"
Payer queries enrollment system. If not active → deny immediately.
        ↓
Stage 3 — EDITS (Clinical + Coding)
"Are the codes valid? Do they make medical sense together?"
Does ICD-10 diagnosis support the CPT procedure? Is this a covered service?
National Correct Coding Initiative (NCCI) bundling rules applied.
        ↓
Stage 4 — PRICING
"What is the correct payment amount?"
Apply contracted fee schedule for this provider.
For inpatient: use DRG (one flat payment for the whole stay).
        ↓
Stage 5 — BENEFIT APPLICATION
"What does the member owe vs what do we pay?"
Apply deductible (how much member has left), copay, coinsurance.
Paid amount = Allowed amount minus member cost-sharing.
        ↓
DECISION → Pay / Deny / Pend
835 remittance sent to provider with decision + CARC codes
```

### Why "Auto-Adjudication" Is the Goal

All 5 stages above can run **fully automatically for a clean claim** — no human involved. That is auto-adjudication. The target is 85%+ of claims processed this way.

The 15% that fall out go into a **pend queue** — a human reviewer looks at them. This is expensive. The entire IT agenda for payer claims operations is: reduce that 15% by making the rules smarter, the data cleaner, and the edge cases fewer.

### Where Your FHIR Work Connects
- **Before adjudication**: CRD/DTR/PAS ensures prior auth is in place so claims don't fail at Stage 3
- **After adjudication**: CARIN BB EOB exposes the adjudicated result to the member via FHIR
- **During data prep for adjudication**: FHIR Coverage resource ensures eligibility data is accurate for Stage 2

---

## EOB — Where Your Workspace Fits in the Claims World

Your Phase 2 EOB work is the **read-only, member-facing display** of a claim that was already adjudicated by a legacy system. It is NOT the claims processing system itself.

```
837 (Provider submits) → [Adjudication Engine: Facets/QNXT] → 835 (Remittance to Provider)
                                        ↓
                              Adjudicated claim data stored
                                        ↓
                              FHIR EOB Resource  ← This is what you built in Phase 2
                                        ↓
                         Member reads it via CARIN BB Member Access API
```

**Key distinction**: Your EOB shows the member what happened AFTER adjudication was done by the legacy system. The 837/835 EDI transactions happen completely upstream of your FHIR layer.

---

## EDI — What It Is and How It Actually Works

EDI (Electronic Data Interchange) is a **standardized message format** defined by X12 (ANSI-accredited standards body). It is a strict positional/delimited text format — not XML, not JSON, not REST. Completely human-unreadable without a parser.

HIPAA mandated specific X12 versions for each transaction type. Every payer and clearinghouse in the US is legally required to accept them.

---

### EDI Syntax — Reading a Raw File

**Three delimiters** control the entire format. They are defined in the ISA header itself (positions 104, 105, 106):

| Delimiter | Character (typical) | Purpose |
|---|---|---|
| **Element separator** | `*` (asterisk) | Separates fields within a segment |
| **Sub-element separator** | `:` (colon) | Separates components within a field |
| **Segment terminator** | `~` (tilde) | Ends a segment (like a newline) |

These are not fixed characters — the sender defines them in the ISA segment, and the receiver must read them from there. Most implementations use `*`, `:`, `~` but any character can be used.

**Segment structure**:
```
SEGMENT_ID * ELEMENT1 * ELEMENT2 * ELEMENT3 ~ 
```
Every segment starts with a 2–3 character identifier and ends with `~`.

---

### The Three-Layer Envelope Structure

Every X12 EDI file has three nested envelope layers:

```
ISA ... IEA          ← Interchange Envelope (outermost)
  GS ... GE          ← Functional Group (groups transactions by type)
    ST ... SE        ← Transaction Set (the actual document — one 837, one 835, etc.)
    ST ... SE        ← (another transaction set)
  GE
  GS ... GE          ← (another functional group if needed)
ISA ... IEA
```

One ISA/IEA file can contain multiple GS/GE groups, each containing multiple ST/SE transaction sets. A batch clearinghouse file may have hundreds of 837 claims in one ISA envelope.

---

### ISA Segment — The Interchange Envelope Header

The ISA segment is always exactly 106 characters (fixed-width, not delimited for positions 1–103). It identifies sender, receiver, and the file itself.

```
ISA*00*          *00*          *ZZ*SENDER123456789*ZZ*RECEIVER12345*260521*1430*^*00501*000000001*0*P*:~
```

| Position | Field | Example Value | Meaning |
|---|---|---|---|
| ISA01 | Auth info qualifier | `00` | No auth info (00 = not used) |
| ISA02 | Auth info | 10 spaces | Filler when 00 |
| ISA03 | Security info qualifier | `00` | No security info |
| ISA04 | Security info | 10 spaces | Filler |
| ISA05 | Sender ID qualifier | `ZZ` | Mutually defined (most common) |
| ISA06 | Sender ID | `SENDER123456789` | Sender's ID (15 chars, right-padded) |
| ISA07 | Receiver ID qualifier | `ZZ` | Mutually defined |
| ISA08 | Receiver ID | `RECEIVER12345` | Payer or clearinghouse ID |
| ISA09 | Date | `260521` | YYMMDD — date the file was created |
| ISA10 | Time | `1430` | HHMM |
| ISA11 | Repetition separator | `^` | Character used for repeating elements |
| ISA12 | Version | `00501` | X12 version 5010 |
| ISA13 | Interchange control number | `000000001` | Unique file ID — must match IEA |
| ISA14 | Ack requested | `0` | 0=no TA1 requested, 1=TA1 requested |
| ISA15 | Usage indicator | `P` | P=Production, T=Test |
| ISA16 | Sub-element separator | `:` | Defines the sub-element delimiter |

**IEA** closes the envelope:
```
IEA*1*000000001~
```
- IEA01 = number of functional groups in this interchange (must match actual count)
- IEA02 = must match ISA13

---

### GS/GE — Functional Group

Groups transactions of the same type. One GS/GE per transaction type per file (or multiple if different versions).

```
GS*HC*SENDERID*RECEIVERID*20260521*1430*1*X*005010X222A2~
```

| Field | Example | Meaning |
|---|---|---|
| GS01 | `HC` | Functional ID — HC = Health Care Claim (837) |
| GS02 | `SENDERID` | Application sender ID |
| GS03 | `RECEIVERID` | Application receiver ID |
| GS04 | `20260521` | Date — CCYYMMDD |
| GS05 | `1430` | Time — HHMM |
| GS06 | `1` | Group control number |
| GS07 | `X` | Responsible agency — X = ANSI X12 |
| GS08 | `005010X222A2` | Version/release + implementation guide ID |

**Common GS01 functional IDs**:

| GS01 | Transaction Type |
|---|---|
| `HC` | Health Care Claim (837) |
| `HB` | Health Care Eligibility (270/271) |
| `HR` | Health Care Claim Payment (835) |
| `BE` | Benefit Enrollment (834) |
| `FA` | Functional Acknowledgement (999) |

**GE** closes the group:
```
GE*1*1~
```
GE01 = number of transaction sets, GE02 = must match GS06.

---

### ST/SE — Transaction Set

The actual document. Starts with ST, ends with SE.

```
ST*837*0001*005010X222A2~
...claim data...
SE*47*0001~
```

| Field | Example | Meaning |
|---|---|---|
| ST01 | `837` | Transaction set identifier code |
| ST02 | `0001` | Transaction set control number (unique within group) |
| ST03 | `005010X222A2` | Implementation guide version |

SE01 = number of segments in the transaction set (including ST and SE)  
SE02 = must match ST02

**Transaction set IDs**:

| ST01 | Document |
|---|---|
| `837` | Health Care Claim |
| `835` | Health Care Claim Payment/Advice |
| `270` | Eligibility/Benefit Inquiry |
| `271` | Eligibility/Benefit Response |
| `834` | Benefit Enrollment |
| `276` | Claim Status Request |
| `277` | Claim Status Response |
| `999` | Implementation Acknowledgement |

---

### Loops — How EDI Organizes Repeating Data

EDI uses **loops** to group related segments that repeat. A loop is just a named group of segments — it has no opening/closing tag (unlike XML). You know a loop starts when you see its first segment appear.

**837 loop structure (simplified)**:

```
ST*837...                          ← Transaction start
BHT*...                            ← Beginning of Hierarchical Transaction
  Loop 1000A — Submitter
    NM1*41*...                     ← Submitter name (provider billing system)
    PER*IC*...                     ← Contact info
  Loop 1000B — Receiver
    NM1*40*...                     ← Receiver name (payer)
  Loop 2000A — Billing Provider Hierarchical Level
    HL*1**20*1~                    ← Hierarchical Level
    PRV*BI*PXC*207Q00000X~         ← Provider info
    Loop 2010AA — Billing Provider Name
      NM1*85*2*GENERAL HOSPITAL*...*XX*1234567890~
      N3*123 MAIN ST~
      N4*BOSTON*MA*02101~
      REF*EI*123456789~            ← Tax ID
  Loop 2000B — Subscriber Hierarchical Level
    HL*2*1*22*1~
    SBR*P*18*...                   ← Subscriber info (P=primary)
    Loop 2010BA — Subscriber Name
      NM1*IL*1*SMITH*JOHN***MI*ABC123456~   ← Member ID
      N3, N4...                    ← Address
    Loop 2010BB — Payer Name
      NM1*PR*2*AETNA...            ← Payer
  Loop 2000C — Patient Hierarchical Level (if different from subscriber)
    HL*3*2*23*0~
    PAT*19~                        ← Patient relationship code
  Loop 2300 — Claim Information
    CLM*CLAIM001*1500.00**11:B:1*Y*A*Y*I~   ← Claim details
    DTP*434*RD8*20260515-20260515~  ← Service date
    REF*EA*AUTH12345~               ← Prior auth number
    HI*ABK:Z8711~                   ← Diagnosis codes (ICD-10)
    Loop 2310B — Rendering Provider
      NM1*82*1*JONES*MARY***XX*9876543210~
    Loop 2400 — Service Line
      LX*1~                         ← Line counter
      SV1*HC:99213*150.00*UN*1***1~ ← Procedure (CPT 99213), $150, 1 unit
      DTP*472*D8*20260515~          ← Service date
      Loop 2430 — Line Adjudication (if secondary)
SE*47*0001~
```

---

### Key Segments — 837 Professional (837P)

| Segment | Purpose | Key Fields |
|---|---|---|
| **BHT** | Beginning of Hierarchical Transaction | BHT06: 00 (original claim) or 18 (resubmission) |
| **NM1** | Name | NM101: entity code; NM108: ID qualifier; NM109: ID |
| **HL** | Hierarchical Level | HL03: level code (20=billing, 22=subscriber, 23=patient) |
| **SBR** | Subscriber Info | SBR01: P=primary, S=secondary; SBR09: claim filing indicator |
| **CLM** | Claim Information | CLM01: claim ID; CLM02: total billed amount; CLM05: place of service; CLM11: release of info |
| **DTP** | Date/Time | DTP01: qualifier (434=service, 431=onset); DTP02: format; DTP03: date |
| **HI** | Health Care Information Codes | HI01-1: code qualifier (ABK=principal ICD-10); HI01-2: diagnosis code |
| **SV1** | Professional Service | SV101: CPT/HCPCS code; SV102: charge amount; SV104: units |
| **REF** | Reference ID | REF01: qualifier (EA=auth number, 9F=referral, D9=claim number) |
| **PRV** | Provider Info | PRV01: role (BI=billing, RF=referring, PE=performing); PRV03: taxonomy code |
| **NM1 *85*** | Billing Provider | Entity code 85 |
| **NM1 *82*** | Rendering Provider | Entity code 82 |
| **NM1 *IL*** | Insured/Subscriber | Entity code IL |
| **NM1 *PR*** | Payer | Entity code PR |

---

### Key Segments — 835 Remittance

```
ST*835*0001*005010X221A1~
BPR*I*1250.00*C*ACH*CCP*01*021000021*DA*123456789*...*20260522~
TRN*1*835000001*1234567890~
  Loop 1000A — Payer
    N1*PR*AETNA HEALTH PLANS~
  Loop 1000B — Payee (Provider)
    N1*PE*GENERAL HOSPITAL*XX*1234567890~
  Loop 2000 — Header Number (one per claim)
    LX*1~
    Loop 2100 — Claim Payment Info
      CLP*CLAIM001*1*1500.00*1250.00*0*MC*PAY12345**11~
      NM1*QC*1*SMITH*JOHN~        ← Patient name
      NM1*74*1*JONES*MARY~        ← Corrected patient name (if applicable)
      Loop 2110 — Service Payment Info
        SVC*HC:99213*150.00*130.00*1~    ← CPT, billed, paid, units
        DTM*472*20260515~                ← Service date
        CAS*CO*42*20.00~                 ← Adjustment: contractual (CO), reason 42, $20
        CAS*PR*1*5.00~                   ← Patient responsibility (PR), deductible
SE*28*0001~
```

| Segment | Purpose | Key Fields |
|---|---|---|
| **BPR** | Financial Information | BPR02: payment amount; BPR04: payment method (ACH/CHK); BPR16: payment date |
| **TRN** | Trace Number | TRN02: check/EFT number; TRN03: payer ID |
| **CLP** | Claim Payment | CLP01: claim ID from 837; CLP02: status (1=paid, 2=adjusted, 3=denied, 4=denied); CLP03: billed; CLP04: paid; CLP08: payer claim number |
| **SVC** | Service Payment | SVC01: CPT code; SVC02: billed; SVC03: paid; SVC04: units |
| **CAS** | Claim Adjustment | CAS01: group code; CAS02: CARC (reason code); CAS03: adjustment amount |
| **DTM** | Date | DTM01: qualifier (472=service date, 050=received date) |

**CAS adjustment group codes** (critical for denial management):

| Group Code | Meaning | Who "Owns" This Adjustment |
|---|---|---|
| **CO** | Contractual Obligation | Payer adjusts per contract — provider cannot bill member |
| **PR** | Patient Responsibility | Member owes this (deductible, copay, coinsurance) |
| **OA** | Other Adjustment | Catch-all (COB adjustments, capitation adjustments) |
| **PI** | Payer Initiated | Payer error correction |
| **CR** | Correction/Reversal | Used in adjustment transactions |

---

### Reading a Real Denial on an 835

```
CLP*CLAIM001*2*1500.00*0.00*0.00*MC*PAYERCLM456**11~
CAS*CO*4*1500.00~
```

Translation:
- `CLP02 = 2` → claim status = **denied**
- `CLP03 = 1500.00` → billed amount
- `CLP04 = 0.00` → paid amount (nothing)
- `CAS*CO*4*1500.00` → Contractual adjustment, **CARC 4** ("Service not covered by plan"), $1,500 adjusted — provider cannot bill member for this

```
CLP*CLAIM002*1*500.00*400.00*100.00*MC*PAYERCLM789**11~
CAS*CO*42*100.00~
CAS*PR*1*75.00~
CAS*PR*2*25.00~
```

Translation:
- `CLP02 = 1` → paid
- Billed $500, paid $400, patient responsibility $100
- `CO*42` → contractual write-off (not medically necessary per contract — $100)
- `PR*1` → patient deductible ($75)
- `PR*2` → patient coinsurance ($25)

---

### 999 — Acknowledgement Transaction

The 999 (formerly 997) is how the payer/clearinghouse acknowledges receipt and format validity:

```
ST*999*0001~
AK1*HC*1~            ← Functional group ack: HC (claim), GS06 control number
AK2*837*0001~        ← Transaction set ack: 837, ST02 control number
AK5*A~               ← Transaction set accepted (A=Accepted, R=Rejected, E=Accepted with errors)
AK9*A*1*1*1~         ← Group accepted: 1 received, 1 accepted, 1 included
SE*6*0001~
```

AK5 values: **A** = Accepted, **E** = Accepted with errors, **R** = Rejected — **R** means the entire transaction set was rejected and must be resubmitted. This is distinct from a claim denial — a 999 rejection means the EDI format was wrong, not that the claim was denied on clinical/coverage grounds.

---

### Common EDI Gotchas (Real-World PM Knowledge)

| Issue | What Happens | How to Fix |
|---|---|---|
| **Wrong ISA15** | File sent with T (test) flag to production | Payer ignores the file — nothing is processed |
| **ISA13 not unique** | Duplicate interchange control number | Payer may reject as duplicate file |
| **Loop out of order** | Segments in wrong sequence | 999 R (rejected) — must fix and resubmit |
| **Companion guide violation** | Optional field payer requires is missing | 999 E (accepted with errors) or claim pends |
| **NPI not in payer system** | Rendering provider NPI unknown to payer | Claim denied — "provider not credentialed" |
| **Stale auth number in REF*EA** | Auth expired, claim submitted after auth end date | Medical necessity denial |
| **Wrong place of service code** | CLM05 says 11 (office) but should be 22 (outpatient hospital) | Claim denied — wrong benefit category applied |
| **Diagnosis pointer mismatch** | SV1 diagnosis pointer references HI position that doesn't exist | Claim rejected or pended |

---

### Is EDI Truly Interoperable?

**Technically yes. Practically messy.** The standard exists, but:

- Every payer publishes a **companion guide** — their own rules layered ON TOP of the standard. Aetna's requirements differ from UnitedHealth's.
- Payers use optional loops/segments differently
- This is why **clearinghouses exist** — they sit in the middle and handle translation

```
Provider Billing System
        ↓ sends ONE format
   CLEARINGHOUSE (Change Healthcare, Availity, Waystar)
        ↓ translates, validates, routes per payer companion guide
   Payer A       Payer B       Payer C
```

Without clearinghouses, every provider would need a custom connection to every payer.

### Where Clearinghouses Are (and Aren't) Involved

| Transaction | Clearinghouse? |
|---|---|
| 837 Claim submission | Almost always — high volume, complex routing |
| 835 Remittance | Sometimes direct, sometimes via clearinghouse |
| 270/271 Eligibility | Often DIRECT and real-time — no clearinghouse |
| 834 Enrollment | Often DIRECT from employer/sponsor to payer |

---

## EDI vs FHIR — They Are NOT Competing, They Are Complementary

Companies are absolutely still running RCM on traditional EDI workflows and are NOT using FHIR for RCM transactions. This is correct. They solve different problems.

| Function | EDI | FHIR |
|---|---|---|
| Claim submission | 837 ✅ | Not used |
| Remittance / payment | 835 ✅ | Not used |
| Enrollment | 834 ✅ | Not used |
| Eligibility inquiry | 270/271 ✅ | Coverage resource (emerging) |
| Prior auth | Was fax/portal | ✅ PAS — actively replacing (CMS-0057) |
| Member viewing claims | ❌ Can't do this | ✅ EOB via CARIN BB |
| Payer-to-payer exchange | ❌ No standard | ✅ PDex |
| Provider directory | ❌ | ✅ Plan-Net |
| Clinical notes, labs, history | ❌ | ✅ Condition, Observation, DiagnosticReport |

### The Mental Model

> **EDI = the plumbing inside the walls. FHIR = the smart display panel on the wall. You don't rip out the plumbing to add a smart display. They coexist.**

EDI answers: *"What did the provider bill, what did the payer pay, who is enrolled?"*
FHIR answers: *"What is the member's clinical history, what needs prior auth, what does the formulary cover?"*

**Payers today run BOTH**: a legacy adjudication system (Facets/QNXT) processing EDI all day, with a FHIR server on top feeding the CMS-mandated APIs.

> **Which CMS-mandated APIs specifically?** There are four — all required under CMS-9115-F (2020) and CMS-0057-F (2024):
> 1. **Patient Access API** — members can pull their own claims, clinical data, and formulary via FHIR. Implemented in your Phase 2 (MemberAccessAPI). Mandatory since July 2021.
> 2. **Provider Directory API** — providers can query network data (who's in-network, locations, specialties) via FHIR. Your Phase 3 (ProviderDirectoryAPI). Mandatory since July 2021.
> 3. **Payer-to-Payer API** — when a member switches payers, the old payer must send 5 years of clinical + claims data to the new payer in FHIR. Your Phase 7 (PDexAPI). Mandatory since January 2022.
> 4. **Prior Authorization API** (CMS-0057-F) — FHIR-based prior auth: CRD + DTR + PAS. Your Phase 5 + Phase 6. Mandatory for MA, Medicaid MCOs, QHP by January 2027.
>
> The legacy Facets/QNXT system adjudicates claims and stores member data. A FHIR server (HAPI FHIR, Smile CDR, Azure FHIR) sits alongside it, pulls data from the legacy DB, and exposes it through these four APIs. That is literally the architecture of your Phase 1-8 workspace.

The **one area where FHIR is actively replacing EDI** is prior authorization — fax/portal replaced by PAS. That's exactly your Phase 6 work and the CMS-0057 mandate.

---

## Where TEFCA Sits

TEFCA is a completely different layer from both EDI and FHIR APIs. It is the **national governance and trust framework** — the rules of the road for who can exchange data with whom.

```
                    ┌─────────────────────────────────────┐
                    │              TEFCA                   │
                    │  "Who is allowed to ask for data     │
                    │   from whom, and under what rules"   │
                    └──────────────┬──────────────────────┘
                                   │ Governs exchange across networks
                    ┌──────────────▼──────────────────────┐
                    │  QHINs (Qualified Health Information │
                    │  Networks)                           │
                    │  CommonWell, eHealth Exchange,       │
                    │  Carequality, Kno2, KONZA etc.       │
                    └──────────────┬──────────────────────┘
                                   │ Networks use multiple protocols
               ┌───────────────────┼───────────────────┐
               ▼                   ▼                   ▼
          HL7 FHIR             HL7 v2 / CDA         Direct
       (CMS APIs, PDex,      (traditional           Messaging
        CARIN BB)             EHR exchange)
```

- TEFCA doesn't replace EDI or FHIR — it governs **which organizations can exchange data with which others**
- **Permitted use cases**: Treatment, Payment, Healthcare Operations, Individual Access, Public Health, Benefits Determination
- **"Benefits Determination"** use case — this is where payers fit. A payer can query a QHIN for a member's clinical data to support care management or prior authorization
- Before TEFCA, payers had to fax or manually request medical records. With TEFCA + FHIR, payer queries QHIN → routes to provider EHR → clinical data flows back
- Your `TEFCA-Knowledge` repo is directly relevant here

---

## The Complete Four-Layer Picture

```
LAYER 1 — TRANSACTION (EDI)
  Claims, eligibility, enrollment, remittance
  Governs payer ↔ provider financial transactions
  HIPAA mandated. Will not be replaced.

LAYER 2 — ACCESS (FHIR APIs)
  Member views data, payer-to-payer exchange,
  prior auth, provider directory, formulary
  CMS mandated. Growing rapidly.
  Your entire workspace lives here.

LAYER 3 — NETWORK (TEFCA/QHINs)
  National trust fabric for clinical data exchange
  ONC mandated. Connects everyone to everyone.
  Uses FHIR as the query language underneath.
  Your TEFCA-Knowledge repo lives here.

LAYER 4 — INTELLIGENCE (AI)
  MCP servers querying FHIR, mapping agents,
  risk stratification, care gap identification
  Your differentiator — fhir-mcp-suite, fhir-mapping-agent
```

---

## Enrollment — The Full Landscape (Not Just Employers)

**834 EDI is specifically a group/employer transaction.** Individual market, Medicare, and Medicaid each have their own enrollment mechanisms.

### Three Distinct Roles in Group Insurance

```
EMPLOYER (Sponsor)      PAYER (Insurer)       MEMBER (Beneficiary)
──────────────────      ───────────────       ──────────────────
TCS, a hospital,        UnitedHealth,         Employee + dependents
a union,                Aetna, BCBS,
a government agency     Cigna, Humana
        │                     │
        └──── 834 EDI ────────►│
```

### When There Is No Employer

| Who You Are | Who Sponsors | Enrollment Mechanism |
|---|---|---|
| Employee at a company | Employer | 834 EDI from employer to payer |
| Self-employed | Yourself | ACA Marketplace or direct to payer |
| Retired, under 65 | Yourself / COBRA | ACA Marketplace or direct to payer |
| Retired, 65+ Medicare FFS | CMS | CMS direct enrollment |
| Retired, 65+ Medicare Advantage | CMS pays, private plan delivers | CMS **MMR/TRR files** to MA plan — NOT an 834 |
| Low income — Medicaid | State government | State enrollment system to MCO |
| Child — CHIP | State government | State enrollment system |

**Key point**: When interviewers say "834 enrollment" they are talking about the **large group employer market** — the biggest volume for commercial insurers. Medicare Advantage uses CMS-specific **Monthly Membership Reports (MMR)** and **Transaction Reply Reports (TRR)** — a completely separate operational domain.

**Also note**: CMS is both a **regulator** AND a **payer** (Medicare FFS, Medicaid co-funder). A company registered with CMS as an MA plan is a **payer/insurer**, not an employer. A large hospital system can be BOTH — employer (buys coverage for its own staff) AND provider (delivers care).

### Interview-Ready Answer on Enrollment

*"The 834 enrollment transaction flows from the plan sponsor — typically an employer, a marketplace, or a government agency — to the payer. But 834 is specific to the group/commercial market. Medicare Advantage uses CMS-specific MMR/TRR files, and Medicaid enrollment flows through state systems. Each channel has its own mechanism, though they all ultimately result in the same thing: a member record in the payer's system that providers verify via 270/271 eligibility checks."*

---

## Interview-Ready Answer on EDI vs FHIR vs TEFCA

*"EDI handles the financial transactions — 837 for claim submission, 835 for remittance, 834 for enrollment. These are HIPAA-mandated, batch-oriented B2B transactions that run through clearinghouses. They're not going away. FHIR operates at a completely different layer — real-time APIs for member data access, clinical data exchange, and now prior authorization via CMS-0057. The gap EDI never solved was payer-to-payer clinical exchange and member-facing access — that's what CMS mandates and Da Vinci IGs fill. TEFCA sits above both as the national trust network governing who can exchange data with whom. A payer today runs all of these: a core admin system processing EDI all day, a FHIR server on top for CMS-mandated APIs, and TEFCA connectivity for nationwide clinical data queries."*

---

# TECHNICAL ARCHITECTURE & SYSTEMS — PM Deep Dive

> **Why this section exists**: A PM at an IT consulting firm (Infosys, Cognizant, TCS, Accenture Health) needs to go beyond business vocabulary and demonstrate architectural fluency. You don't need to code it — you need to understand the constraints that drive delivery decisions.

---

## Real-Time Architecture Patterns in Healthcare Payer Systems

### The Core Problem

Payer core systems (Facets, QNXT) are **batch-oriented** — built for overnight claim runs, not millisecond API responses. Every real-time capability is a **facade + cache strategy** layered on top of them.

### Pattern 1 — Real-Time Eligibility (270/271 or FHIR Coverage)

```
EHR / Provider System
        ↓  REST API call (must respond < 3 seconds)
API Gateway  (MuleSoft Anypoint / Apigee)
        ↓
Eligibility Service
        ├── Cache HIT  →  Redis / Hazelcast  (eligibility cached 24hr)
        │                 Returns immediately — no DB call
        └── Cache MISS →  Facets/QNXT Enrollment DB
                          Fetch → cache it → return response
```

**Why caching is mandatory**: A large payer receives 5–15 million eligibility checks per day. Hitting Facets for every one would collapse the system. Cache hit ratio target is > 95%. Eligibility data doesn't change minute-to-minute so 24-hour TTL is acceptable.

**FHIR Coverage**: Same architecture — a FHIR facade translates `GET /Coverage?patient=X` into the same cache query. The member is always reading cached data, not live Facets.

### Pattern 2 — CDS Hooks / CRD (Real-Time PA Determination at Point of Order)

```
Doctor writes order in Epic
        ↓
Epic fires CDS Hook (synchronous HTTP POST)
Hard limit: must respond in < 5 seconds (CDS Hooks spec)
        ↓
CRD Service (your Phase 5 CRDService)
        ├── Coverage Rules Cache  (pre-loaded from payer rules DB, Redis)
        │   e.g. "Plan XYZ: MRI codes 70553/70554 require PA"
        └── Rules Engine  (Drools / IBM ODM / custom evaluator)
                ↓
        Returns CDS Card: "PA Required / Not Required / Warning"
        back to Epic — within 5 seconds
```

**Critical constraint**: CRD **cannot** make a live call to the payer's adjudication engine — too slow. Coverage rules must be **pre-loaded into a local cache** and refreshed on a schedule (nightly or event-triggered on rule change). This is why your CRDService has an in-memory rules store.

---

## Rules Engines Deep Dive — What Actually Runs the Decision Logic

> **Why this matters for PMs**: Every time a payer says "our system automatically approves/denies/flags that," there is a rules engine behind it. Understanding which engine, how rules are authored, and how they are versioned determines how fast the payer can respond to regulatory changes, new clinical guidelines, or provider complaints. This is a constant source of project risk.

---

### The Four Engines You Will Encounter

| Engine | Type | Who Owns It | Open Source? | FHIR-Native? | Typical Payer Use |
|---|---|---|---|---|---|
| **Drools** | General-purpose BRMS | Red Hat / JBoss | ✅ Yes | ❌ No | Benefit rules, CRD cache, coding edits |
| **IBM ODM** | Enterprise BRMS | IBM | ❌ No (licensed) | ❌ No | PA criteria, compliance rules, complex benefit config |
| **Optum ClaimCheck / CES** | Clinical editing product | Optum (UHG) | ❌ No (licensed) | ❌ No | Claims editing — NCCI, MUE, LCD/NCD |
| **CQL** | Clinical query language | HL7 (standard) | ✅ Yes | ✅ Yes | CRD/CDS Hooks rules, quality measures, clinical criteria |

---

### Engine 1 — Drools (Most Common in Payer Systems)

**What it is**: Drools is a Java-based open-source Business Rules Management System (BRMS) originally from JBoss, now maintained by Red Hat. It is the most widely deployed rules engine in payer IT because most payer backend systems are Java-based and Drools integrates without licensing cost.

**Architecture — three core components**:

```
Facts (Java Objects — claim, member, provider, benefit)
        ↓
  Working Memory  ←── Rules (DRL files) loaded at startup
        ↓
  Inference Engine  (Pattern Matching — RETE algorithm)
        ↓
  Agenda  (fires matched rules in priority order)
        ↓
  Rule Actions  (approve, deny, flag, modify fact, trigger next rule)
```

- **Working Memory**: holds all "facts" — the current state of the objects being evaluated (a ClaimLine, a Member, a BenefitConfig)
- **RETE Algorithm**: efficient pattern-matching algorithm that avoids re-evaluating all rules on every fact change — only rechecks rules whose conditions could be affected
- **Agenda**: when multiple rules match, Drools uses salience (priority number) to decide firing order
- **Inference Engine**: can chain rules — a rule action that modifies a fact can trigger additional rules (forward chaining)

**DRL (Drools Rule Language) — what a rule looks like**:

```drools
rule "PA Required — MRI Lumbar Spine"
    salience 100
    when
        $req : ServiceRequest(
            code.coding[0].code == "70553",   // MRI Brain w/wo contrast
            subject.coverage.planType == "HMO"
        )
        $member : Member(age >= 18, priorAuth == false)
    then
        $req.setPaRequired(true);
        $req.setDenialReason("MRI requires prior authorization under HMO benefit");
        update($req);
end
```

**How payers use Drools**:
- **CRD rules cache**: pre-loading coverage rules into a Drools working memory session — when a CDS Hooks request arrives, the rules fire in < 100ms without a database call (this is exactly what your CRDService does)
- **Benefit adjudication rules**: copay, deductible, coinsurance logic — "if member is in deductible period and service is specialist visit, apply $40 copay"
- **Coding edit rules**: payer-specific edits on top of standard NCCI edits
- **Formulary tiering**: "if drug is on Tier 3 and member is on Silver plan, apply 40% coinsurance"

**Pros**:
- Free, open source, large community
- Native Java integration — no serialization overhead
- Powerful rule chaining for complex benefit logic
- Rules can be stored in a database (Drools Workbench / KIE Server) and reloaded without restart

**Cons**:
- Business analysts cannot author rules — requires Java developers
- DRL syntax is technical, not readable by non-engineers
- Debugging complex rule chains is difficult
- No built-in audit trail or rule governance workflow

**PM implication**: When a payer says they can "update coverage rules quickly," ask whether business analysts can change Drools rules themselves or whether a developer sprint is required. The answer tells you the true change velocity.

---

### Engine 2 — IBM ODM (Operational Decision Manager)

**What it is**: IBM's enterprise-grade BRMS. Far more expensive than Drools (six-figure annual licensing), but designed specifically to let **business analysts — not developers — author and govern rules**. Used heavily by large Blues plans, large regional payers, and payers with complex multi-state benefit configurations.

**Architecture — two runtime environments**:

```
┌─────────────────────────────────────────────────────────────┐
│  DECISION CENTER (Authoring + Governance — web UI)          │
│  ├── Business user authors rules in Excel-like tables       │
│  ├── Rule review / approval workflow (4-eyes principle)     │
│  ├── Version control, audit log, diff between rule versions │
│  └── Deploy button → pushes rule artifact to Decision Server│
└────────────────────────────┬────────────────────────────────┘
                             │  Rule Execution Artifact (.jar)
┌────────────────────────────▼────────────────────────────────┐
│  DECISION SERVER (Runtime — REST API)                        │
│  ├── HTDS: Hosted Transparent Decision Service              │
│  ├── Accepts JSON/XML request, returns decision + trace     │
│  ├── Stateless — scales horizontally                        │
│  └── Full execution trace: which rules fired, in what order │
└─────────────────────────────────────────────────────────────┘
```

**Rule authoring formats** (business users choose):

| Format | Best For | Looks Like |
|---|---|---|
| **Decision Table** | PA criteria matrices ("if diagnosis + procedure + plan type → PA required") | Excel spreadsheet |
| **Decision Tree** | Step-by-step branching logic | Flowchart |
| **Rule Flow** | Orchestrating multiple rule sets in sequence | Process diagram |
| **BAL (Business Action Language)** | Complex conditions, readable English-like syntax | "If the member's age is greater than 65 and the service code is on the PA list then set PA required to true" |

**BOM (Business Object Model)**: ODM requires a formal object model — a structured definition of every fact the rules can reference (Member, Claim, BenefitPlan, ServiceRequest). The BOM bridges the Java/technical object model and the business-readable vocabulary used in Decision Center.

**How payers use IBM ODM**:
- **PA criteria configuration**: clinical PA criteria authored by Medical Directors in Decision Center, reviewed by compliance, deployed to production without a software release
- **Multi-state benefit compliance**: different state mandates (e.g., NY requires covering 30 days of inpatient mental health, CA has different autism benefit mandates) — encoded as separate rule sets, deployed by state
- **ACA / CMS compliance rules**: preventive care coverage mandates, EPSDT rules, surprise billing rules — each regulatory change = a Decision Center update, not a code change
- **Coding edit overlays**: payer-specific edits on top of CCI/NCCI

**Key differentiator from Drools — governance**:
- In Drools, rule changes require a developer and a deployment
- In ODM, a Medical Director can author a rule change, a compliance analyst reviews it, it gets approved and deployed — **no developer involved**
- Full audit trail: who changed what rule, when, what the previous version was — critical for CMS audits and state DOI examinations

**PM implication**: If a payer is on ODM, the question is whether Decision Center is actually being used by business users or whether IT has locked it down and "owns" all rule changes anyway. Many payers pay for ODM's governance features but don't use them — a significant PM optimization opportunity.

---

### Engine 3 — Optum ClaimCheck / CES (Clinical Editing System)

**Critical distinction**: ClaimCheck is **not a general-purpose rules engine**. It is a **licensed clinical editing product** — a pre-built rule set that payers license and plug into their adjudication pipeline. You do not write rules in ClaimCheck; you configure which of Optum's pre-built edits to enable and what your payer-specific overrides are.

**What it contains** (the actual content):

| Edit Type | What It Does | Source |
|---|---|---|
| **NCCI (National Correct Coding Initiative)** | Prevents billing of procedure pairs that should not be billed together (e.g., unbundling) | CMS — updates quarterly |
| **MUE (Medically Unlikely Edits)** | Caps units per claim line (e.g., max 2 units for a bilateral procedure) | CMS |
| **LCD (Local Coverage Determinations)** | Medicare contractor coverage policies by geographic region | MACs (Medicare Administrative Contractors) |
| **NCD (National Coverage Determinations)** | CMS-level national coverage policies | CMS |
| **Optum proprietary edits** | Additional clinical edits developed by Optum's clinical team | Optum |
| **Payer-specific custom edits** | Each payer can add their own custom edits on top | Payer IT |

**How it plugs into the adjudication pipeline**:

```
Claim received by adjudication engine (Facets / QNXT / AMISYS)
        ↓
  Pre-adjudication editing step
        ↓
  ClaimCheck API call:  POST /evaluate  { claimLines, providerNPI, diagnosisCodes }
        ↓
  ClaimCheck returns:   { editResults: [ { editCode, editAction, ruleDescription } ] }
        ↓
  Edit Actions:
    ├── DENY — line denied, EOB generated with edit code
    ├── REDUCE — units or allowed amount reduced
    ├── FLAG — routed to clinical review queue
    └── PASS — no edit, continue adjudication
        ↓
  Adjudication engine applies edit results, finalizes claim
```

**Licensing model**: Annual license based on claim volume. Updates (NCCI, MUE) are delivered quarterly by Optum. Payers configure which edits to activate, whether to allow provider overrides (modifier 59, XU, XS, XE, XP), and custom edit logic.

**PM implication**: When a provider calls to dispute a claim denial with edit code "97" (component of another procedure), that came from ClaimCheck. The payer's ability to update the edit or grant a provider-specific override is a configuration change in ClaimCheck — typically a 2–4 week cycle, not a software release. This is often the source of provider relations escalations.

---

### Engine 4 — CQL (Clinical Quality Language) — The FHIR-Native Approach

**What it is**: CQL is an HL7 standard language for expressing clinical logic — designed specifically to work with FHIR resources. It is the rules language used in CDS Hooks, quality measures (HEDIS-equivalent in FHIR), and clinical decision support.

**Relationship to your CRDService**: CQL is the language in which CRD coverage rules are ideally expressed. Instead of a Drools DRL file, a CRD rule is a CQL library:

```cql
library "PARequired_MRI_LumbarSpine" version '1.0.0'

using FHIR version '4.0.1'

include FHIRHelpers version '4.0.1'

parameter "ServiceRequest" ServiceRequest

context Patient

define "IsMRILumbarSpine":
    exists(
        "ServiceRequest".code.coding C
        where C.code = '72148'   // MRI Lumbar Spine without contrast
    )

define "MemberIsHMO":
    exists(
        [Coverage] C
        where C.type.coding.code = 'HMO'
        and C.status = 'active'
    )

define "PARequired":
    "IsMRILumbarSpine" and "MemberIsHMO"
```

**How CQL differs from Drools / ODM**:

| Dimension | Drools | IBM ODM | CQL |
|---|---|---|---|
| **Data model** | Java objects | BOM (custom) | FHIR resources (native) |
| **Execution trigger** | Java API call | REST API call | CDS Hooks / FHIR $evaluate |
| **Author** | Java developer | Business analyst (Decision Center) | Clinical informaticist |
| **Use case** | General business rules | Enterprise governance | Clinical criteria, quality measures |
| **Interoperability** | Payer-specific | Payer-specific | Shareable across payers (standard) |
| **CMS/ONC relevance** | Low | Low | **High** — CMS requires CQL for quality reporting |

**CQL execution engines**: CQL is a language spec — you need an execution engine. Common engines:
- **CQL Engine (open source)** — reference implementation from HL7
- **Alphora CQL** — used in some EHRs
- **HAPI FHIR CQL** — integrated into HAPI FHIR server (what your Phase 5 CRDService can use)

**PM implication**: CMS interoperability rules are increasingly pushing payers toward CQL for coverage criteria (the Coverage Requirements Discovery use case in Da Vinci). A payer on Drools today may need to expose CQL-equivalent rules for CMS compliance — this is a migration project, not just a configuration change.

---

### Comparison Table — All Four Side by Side

| Dimension | Drools | IBM ODM | Optum ClaimCheck | CQL |
|---|---|---|---|---|
| **Category** | General BRMS | Enterprise BRMS | Clinical editing product | Clinical rules language |
| **Cost** | Free / open source | $$$$ licensed | $$ per claim volume | Free (standard) |
| **Who authors rules** | Java developer | Business analyst | Optum + payer config | Clinical informaticist |
| **FHIR-native** | No | No | No | Yes |
| **Business user UI** | No (KIE Workbench — technical) | Yes (Decision Center) | Configuration UI | No |
| **Audit trail** | Manual / custom | Built-in (Decision Center) | Optum-managed | Depends on engine |
| **Update cycle** | Developer sprint | Business analyst + approval | Quarterly (NCCI/MUE) | Library versioning |
| **Governance** | Weak (code-based) | Strong (built-in workflow) | Vendor-managed | Version-controlled |
| **Horizontal scaling** | Yes (stateless KIE Server) | Yes (Decision Server) | Yes (API-based) | Yes |
| **CMS compliance use** | Internal only | Internal only | CCI/NCCI edits | Quality measures, CRD |
| **Payer adoption** | Very High | High (large Blues, nationals) | Very High | Growing (Da Vinci mandate) |
| **Typical location in stack** | CRD cache, benefit rules | PA criteria, compliance | Pre-adjudication editing | CDS Hooks, quality |

---

### Greenfield Alternative — No Separate Rules Engine

Some newer payers (Oscar, Devoted, Clover) do not deploy Drools or ODM. Instead:

```
Coverage rule = a microservice with versioned configuration
        ↓
  Rule logic encoded in Python / Go functions
  Rule "data" (what procedure codes require PA) in a database table
  Rule versioning = Git + feature flags (LaunchDarkly / Flagsmith)
        ↓
  ML model handles ambiguous cases (not deterministic rules)
        ↓
  Audit trail = event log (Kafka → S3 → Snowflake)
```

**Trade-offs**:

| Dimension | Drools / ODM | Microservice + Config |
|---|---|---|
| Business user authorship | ODM yes, Drools no | No |
| Change velocity | ODM fast, Drools slow | Fast (config change or feature flag) |
| Regulatory audit trail | ODM excellent, Drools manual | Depends on logging discipline |
| Complexity at scale | High (rule chain debugging) | High (distributed logic, no single view) |
| CMS interoperability | Low | Low unless CQL adapter is added |

**PM implication**: Greenfield payers move faster on rule changes but often struggle to document which rules apply to which claims for audit purposes — a gap that becomes critical when CMS conducts a program integrity audit.

---

### PM Interview Answer — Rules Engines

> *"When a payer evaluates a PA request in CRD, the coverage determination fires against a pre-loaded rules cache — most commonly Drools, which holds the benefit rules as Java-based DRL files and can evaluate a request in under 100 milliseconds without hitting the adjudication engine. For complex PA criteria that need to be authored by Medical Directors without developer involvement, larger payers use IBM ODM — its Decision Center lets clinical staff manage rules with a full governance workflow, which matters for CMS audit readiness. Claims editing is a separate layer — most payers license Optum ClaimCheck to apply NCCI and MUE edits pre-adjudication, which is where the majority of line-level claim denials originate. The emerging standard for sharing coverage criteria across payer-EHR integrations is CQL, which is FHIR-native and is what Da Vinci CRD is designed to use — so there is an active migration pressure on payers to translate their Drools rules into CQL libraries for CMS compliance."*

---

**Rules engines used**: Drools (Java, open source — most common in payer systems), IBM Operational Decision Manager (ODM), Optum Clinical Decision Support engine for medical necessity. *(see detailed analysis above)*

### Pattern 3 — PAS Submission (Two Modes)

```
Provider submits FHIR PAS Bundle (ClaimRequest + Coverage + ServiceRequest)
        ↓
        ├── SYNCHRONOUS PATH  (low-risk, high-confidence requests)
        │   ML Scoring Model evaluates the request
        │   Score > threshold  →  auto-approve, HTTP 200 + decision
        │   Entire round-trip < 10 seconds
        │
        └── ASYNCHRONOUS PATH  (complex / pended)
            Payer returns HTTP 202 + Task resource ID immediately
            Request enters clinical review queue (Kafka topic)
            Provider polls:  GET /Task/{id}
            OR subscribes:   FHIR Subscription (push when status changes)
            Human reviewer resolves  →  status updated
            Provider notified (hours to days depending on type)
```

**The ML auto-approval layer**: Payers train ML models on historical PA decisions. Incoming PAS request → model scores approval probability → above threshold = auto-approve synchronously. Below threshold = pend for human. This is the evolution your CRDService rules engine points toward.

### Pattern 4 — Claims Adjudication (Internal Payer Event Pipeline)

```
837 arrives from clearinghouse
        ↓
Message Queue  (Apache Kafka / IBM MQ / Azure Service Bus)
"ClaimReceived" event published — claim cannot be lost
        ↓
Adjudication Pipeline  (microservices or Facets pipeline steps)
  ├── Eligibility Validator          ← hits eligibility cache
  ├── Coding Edit Engine             ← Optum ClaimCheck (rule-based)
  ├── Clinical Review Scorer         ← ML: pend or auto-adjudicate
  ├── Pricing Engine                 ← fee schedule lookup
  └── Benefit Accumulator            ← Redis atomic counter (see below)
        ↓
"ClaimAdjudicated" event published → Kafka
        ↓
835 Generator + Payment Processor
```

**Why Kafka**: Each step scales independently. If the pricing engine is slow, other steps are not blocked. A step failure replays from the Kafka offset — no claim is lost. This is critical for a system processing millions of claims where data loss = regulatory violation.

### Pattern 5 — Benefit Accumulator (Deductible / OOP Tracking)

A subtle but architecturally important problem: when 10 claims for the same member are processed simultaneously, how do you track their deductible without a race condition?

```
Member Jane: $1,500 deductible for 2026
        ↓
10 claims processed in parallel across the day
Each claim asks: "how much deductible has Jane consumed?"
        ↓
Problem:  read-modify-write on a DB causes race conditions
Solution: Redis atomic INCRBY per member per plan year
        ↓
Claim 1:  INCRBY jane:deduct:2026  $200  →  returns $200
Claim 2:  INCRBY jane:deduct:2026  $350  →  returns $550
All 10 claims get consistent, non-overlapping values
```

### The Integration Layer — MuleSoft Dominates

Almost every large payer uses **MuleSoft Anypoint Platform** (or Apigee, IBM API Connect) as the API gateway and integration hub:

```
EHR (Epic / Cerner)
Clearinghouse (Availity / Change Healthcare)
Provider Portal
Member Mobile App
        ↓  all enter through ↓
MuleSoft API Gateway
  - Authentication  (OAuth 2.0 / SMART on FHIR)
  - Rate limiting and throttling
  - FHIR ↔ EDI translation layer
  - Routing to backend services
  - Audit logging (HIPAA requirement)
        ↓
Facets / QNXT / Rules Engines / ML Models / Redis Cache
```

MuleSoft also handles **EDI ↔ FHIR translation** — takes 270/271 EDI and translates to FHIR Coverage, takes 837 and translates to FHIR Claim. This is how payers add FHIR APIs without rewriting Facets — it is the most common implementation pattern **for existing large payers with legacy CAPS**.

### Two Paths — Legacy Modernization vs. Greenfield

This is the correct nuance to have: the MuleSoft/Facets facade is NOT the only pattern, and it won't be the pattern for new builds.

| Dimension | Legacy Payer (Modernization) | New / Greenfield Payer |
|---|---|---|
| **Starting point** | Facets/QNXT already running, can't stop | Blank slate |
| **FHIR strategy** | Facade layer — MuleSoft translates, HAPI FHIR or Azure FHIR sits in front | FHIR-native from day one — HealthEdge, Innovaccer, or custom microservices |
| **EDI processing** | Legacy CAPS still adjudicates; EDI goes to Facets as always | Modern cloud-native adjudication engine with EDI adapter (not the core) |
| **Typical timeline** | 5–10 years of incremental modernization | 18–36 months for full launch |
| **Example companies** | UnitedHealth, Aetna, BCBS plans | Bright Health (before collapse), Oscar Health, Devoted Health, Clover Health |
| **Main risk** | Change management, data migration, keeping the legacy running | Scalability at volume, regulatory approvals, provider network build |
| **API Gateway** | MuleSoft / Apigee as mandatory middleware | May be built natively — AWS API Gateway, Azure APIM, or service mesh (Istio) |
| **Adjudication engine** | Facets/QNXT — monolith, on-premise or hosted | HealthEdge HealthRules, cloud-native microservices, or custom-built |

### Greenfield / Cloud-Native Stack (What New Payers Actually Build)

```
Member App / Provider EHR / Clearinghouse
        ↓  REST / FHIR APIs natively
AWS API Gateway  OR  Azure API Management  (no MuleSoft needed)
        ↓
Microservices (each independently deployable, auto-scaling)
  ├── Eligibility Service          → DynamoDB / Aurora
  ├── Benefits Engine              → Rules engine (Drools, or custom)
  ├── Claims Adjudication          → Event-driven on Kafka
  ├── Prior Auth Service           → FHIR PAS natively
  └── Member 360 / CRM             → Salesforce Health Cloud or custom
        ↓
FHIR Server is CENTRAL, not bolted on
  (Azure Health Data Services / AWS HealthLake / HAPI FHIR on EKS)
        ↓
Data Lake (Snowflake / Databricks) for analytics, HEDIS, risk adjustment
```

**No Facets. No MuleSoft. FHIR is the primary data model, not an afterthought.**

### Real-World Examples

- **Oscar Health** — built their own cloud-native stack from scratch. FHIR-first, member app-first, vertically integrated. Avoided legacy CAPS entirely.
- **Devoted Health** — similar greenfield approach, strong engineering org building proprietary care management + adjudication tech.
- **Bright Health** — went too fast, underestimated actuarial risk, collapsed in 2022. The tech was fine; the financial model wasn't. Cautionary tale that greenfield tech doesn't solve insurance fundamentals.
- **Clover Health** — cloud-native with heavy AI/ML for care management. Lost money on medical costs, not technology.

**The lesson from the failures**: New payers that failed didn't fail because of technology choices — they failed because of **underpriced risk and inadequate actuarial reserves**. The technology worked. The insurance fundamentals didn't.

### Why Large Incumbents Stay on Facets Longer Than You'd Expect

1. **Benefit configuration depth**: A large commercial payer may have 50,000+ unique benefit plan configurations loaded into Facets over 20 years. Migrating that is not a technology problem — it's a data and validation problem.
2. **Regulatory continuity**: Regulators (state DOI, CMS) require that payers process claims accurately without interruption. A failed migration = regulatory violation = fines.
3. **Provider contract complexity**: Fee schedules, carve-outs, value-based arrangements all live in Facets. Migrating without breaking provider payments is extremely high stakes.
4. **Risk asymmetry**: The downside of a failed core migration (regulatory action, provider payment failure, member disruption) far outweighs the upside of a faster API.

### The PM Interview Answer

*"The MuleSoft/Facets facade is the dominant pattern for large payers who can't afford to stop and rewrite. But it's not the only path — greenfield payers like Oscar and Devoted built FHIR-native cloud stacks from scratch where FHIR is the primary data model, not an API wrapper over legacy. The real architectural question a PM should ask is: are we modernizing an existing system where disruption risk is high, or building new where we control the stack? The answer determines everything — the integration approach, the timeline, the vendor selection, and the risk profile."*

---

## HL7 v2 — The OTHER Messaging Standard (Most People Forget This)

FHIR gets all the attention. But inside hospitals, **HL7 v2 is still the dominant standard** for real-time clinical messaging — and it has been since the 1990s.

**What HL7 v2 is used for** (things FHIR does NOT do today):
- **ADT messages** (Admit, Discharge, Transfer) — every time a patient is admitted, moved, or discharged, the hospital fires an ADT event to all downstream systems (lab, pharmacy, billing, care management)
- **Lab result delivery** — ORU messages carry lab results from LIS (Lab Information System) to EHR
- **Order entry** — ORM messages carry orders from EHR to ancillary systems
- **Scheduling** — SIU messages for appointment scheduling events

**Common ADT message types** to know:
- A01 — Patient admitted
- A03 — Patient discharged
- A04 — Patient registered (outpatient visit)
- A08 — Patient information updated
- A11 — Cancel admit (undo A01)

**Why it matters for your interview**: When payers say they receive "ADT feeds" from hospitals for care management (e.g., to know when a high-risk member was admitted), that is HL7 v2 ADT, not FHIR. The care management use case in Gap 4 depends on these feeds.

**Interface engine**: HL7 v2 messages are routed and transformed by an **interface engine** — the most common are Rhapsody (now Infor), Mirth Connect (open source), and InterSystems HealthShare. These sit at every hospital doing real-time message routing between 50+ clinical systems.

---

## NCPDP — The Pharmacy Standard (Not EDI X12)

Pharmacy claims and e-prescribing use a completely different standard from the 837/835 world. Most PM candidates don't know this.

> **Key framing**: Medical claims go payer → CAPS (Facets). Pharmacy claims go pharmacy → PBM. These are entirely separate adjudication systems, separate standards, separate payment flows. The only place they connect is in the health plan's data warehouse (for HEDIS, risk adjustment, and member 360 views).

---

### PBM Claim Submission — How It Actually Works at the Pharmacy Counter

**The routing identifiers** (what's on the back of every insurance card):

| Field | What It Is | Example |
|---|---|---|
| **BIN** (Bank Identification Number) | 6-digit number that routes the claim to the right PBM's network | 610591 = OptumRx |
| **PCN** (Processor Control Number) | Sub-routes within the PBM to the right plan/program | ADJADJ, 9999, varies |
| **Group** | Identifies the employer group or health plan within the PBM | ACMECORP01 |
| **Member ID** | Member's ID within the PBM — may differ from the medical plan ID | Same or different |

When a pharmacist types these into their dispensing system (e.g., QS/1, PioneerRx, Rx30), the claim routes through the **pharmacy switch** (Relay Health / Change Healthcare, Emdeon, SureScripts) to the correct PBM.

**Full point-of-sale transaction flow**:

```
Patient drops off prescription at pharmacy counter
        ↓
Pharmacist enters prescription into dispensing system
(NDC code, quantity, days supply, prescriber NPI, DAW code)
        ↓
Dispensing system formats NCPDP D.0 claim transaction
(BIN + PCN routes it to the correct PBM)
        ↓
Claim transmitted via pharmacy switch (Change Healthcare / RelayHealth)
to PBM adjudication engine — OptumRx / CVS Caremark / Express Scripts
        ↓
PBM runs real-time adjudication (< 3 seconds):
├── Eligibility check: Is this member active? Correct group/plan?
├── Formulary check: Is this NDC on the formulary? What tier?
├── Benefit check: What is the member's copay / coinsurance?
│   Applies deductible, out-of-pocket accumulator
├── Clinical edits:
│   ├── Step therapy: Did member try required generic first?
│   ├── Quantity limit: Is requested quantity within plan limit?
│   ├── DAW edit: Brand requested — is generic required?
│   ├── Refill too soon: Days supply not exhausted yet?
│   ├── Drug-drug interaction check: Flag or reject?
│   └── Drug-age edit: Age-inappropriate drug for member?
├── Prior auth check: Is a PA on file for this drug/member/prescriber?
└── Coordination of benefits: Is there another payer?
        ↓
PBM returns response to pharmacy dispensing system (< 3 seconds):
├── PAID response:
│   ├── Patient pay amount (copay) = $15.00
│   ├── Ingredient cost paid = $42.50 (AWP-based)
│   ├── Dispensing fee = $1.50
│   └── Authorization number (pharmacy must retain)
└── REJECTED response:
    ├── Reject code (e.g., 75 = Prior auth required)
    ├── Reject message (e.g., "Step therapy — try metformin first")
    └── Pharmacy shows member the reject reason
        ↓
If PAID: Pharmacist dispenses drug, member pays copay
If REJECTED: Pharmacist counsels member on next steps
(call doctor for PA, try generic, contact plan)
```

---

### Key NCPDP D.0 Fields (What Goes in the Claim)

| Field | Description | Example |
|---|---|---|
| **NDC** | National Drug Code — 11 digits (5-4-2): manufacturer + product + package | 00071-0155-23 (Lipitor 10mg 90-count) |
| **Quantity Dispensed** | Units dispensed (tablets, mL, grams) | 90 (tablets) |
| **Days Supply** | How many days the dispensed quantity covers | 30 |
| **DAW Code** | Dispense As Written — 0=no brand required, 1=prescriber specifies brand, 7=brand dispensed per member request | 0 |
| **Compound Code** | 1=not compound, 2=compound | 1 |
| **Fill Number** | 0=new prescription, 1–99=refill number | 0 |
| **Prescriber NPI** | NPI of the prescribing physician | 1234567890 |
| **Pharmacy NPI** | NPI of the dispensing pharmacy | 0987654321 |
| **Date of Service** | Date drug was dispensed | 20260521 |
| **Ingredient Cost Submitted** | What pharmacy is charging for the drug | $45.00 |
| **Dispensing Fee Submitted** | Pharmacy's dispensing fee | $2.00 |
| **Usual and Customary (U&C)** | Pharmacy's cash price — PBM pays lesser of calculated amount or U&C | $38.00 |

---

### Drug Pricing — How the PBM Calculates What to Pay the Pharmacy

**Pricing formulas** (pharmacies are reimbursed based on one of these):

| Pricing Method | Formula | Who Uses It |
|---|---|---|
| **AWP-based** | AWP (Average Wholesale Price) minus a discount % | Legacy — most common in commercial |
| **WAC-based** | Wholesale Acquisition Cost (manufacturer list price) ± % | Specialty drugs |
| **MAC pricing** | Maximum Allowable Cost — PBM sets its own price for generics, below AWP | Generics — very common |
| **Usual and Customary** | Pharmacy's own cash price — PBM pays the lesser of MAC or U&C | Ties to price transparency |

**AWP** is a published benchmark price — NOT the actual acquisition cost. It's an industry fiction that serves as a starting point for negotiations. A pharmacy that buys generic metformin for $0.02/tablet may be reimbursed at $0.15/tablet (AWP minus 80%) — or be subject to a MAC of $0.05/tablet.

**MAC (Maximum Allowable Cost)**: For generic drugs, the PBM sets its own maximum reimbursement price — the MAC list. Pharmacies that can source the generic below MAC profit; those that cannot may lose money. MAC pricing is a major source of pharmacy-PBM disputes and state regulatory attention.

---

### Reject Codes — What Happens When a Claim Fails

| Reject Code | Meaning | Resolution |
|---|---|---|
| **07** | M/I (Missing/Invalid) member ID | Pharmacist verifies ID, re-submits |
| **14** | M/I date of birth | Eligibility mismatch |
| **25** | Missing or invalid prescriber ID | NPI not on file with PBM |
| **40** | Inactive/invalid member | Not enrolled, or benefit not yet active |
| **41** | No coverage for this drug | Drug not on formulary |
| **70** | NDC not covered | Non-formulary drug |
| **75** | Prior authorization required | Pharmacist calls doctor to initiate PA |
| **76** | Plan limitations exceeded | Quantity limit or days supply limit reached |
| **79** | Refill too soon | Days supply not used up (adherence enforcement) |
| **88** | DUR (Drug Utilization Review) reject | Drug interaction, age edit, dose limit |

---

### Pharmacy Prior Authorization — Different from Medical PA

Drug PA through the PBM is a separate process from the medical prior auth you built in PAS:

```
PBM rejects claim: Reject Code 75 "Prior Authorization Required"
        ↓
Pharmacist calls doctor's office — "Your patient needs a PA for Humira"
        ↓
Doctor's staff submits PA request to PBM
(via CoverMyMeds, manual fax, or PA portal)
        ↓
PBM clinical team reviews against formulary criteria
(step therapy completion? diagnosis code? specialty drug criteria?)
        ↓
Approve → PA number issued, pharmacy re-submits claim with PA number
Deny → Doctor can appeal, or prescribe alternative
```

**CoverMyMeds** (acquired by McKesson): The dominant electronic PA platform for pharmacy. Connects prescribers, pharmacies, and PBMs to automate the PA workflow. ~75% of electronic pharmacy PAs in the US flow through CoverMyMeds.

**Key difference from medical PA**: Medical PA goes through the health plan's UM platform (your CRD/PAS). Drug PA goes through the PBM's system. If a patient has both a medical condition and a drug that needs PA, they are two separate workflows, often with different criteria and timelines.

---

### PBM Remittance — How the Money Flows

**Critical distinction**: PBM remittance is NOT an 835 transaction. There are two separate payment flows in the pharmacy world:

```
┌─────────────────────────────────────────────────────────────────┐
│  FLOW 1: PBM → Pharmacy  (pharmacy reimbursement)               │
│  PBM pays pharmacy for dispensed drugs + dispensing fees        │
│  Frequency: typically weekly (vs. monthly for medical)          │
│  Format: PBM-proprietary remittance report (PDF or 835-like)   │
│  Amount: Ingredient cost reimbursement + dispensing fee         │
│          minus DIR fees (see below)                             │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  FLOW 2: Health Plan → PBM  (plan funding)                      │
│  Health plan pays PBM for adjudicated claims                    │
│  Frequency: daily or weekly funding wire                        │
│  Format: PBM financial reconciliation report                    │
│  Reconciliation: monthly capitation vs. actual claims paid      │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  FLOW 3: Manufacturer → PBM → Health Plan  (rebates)           │
│  Drug manufacturers pay rebates to PBM for formulary placement  │
│  PBM passes portion to health plan (% varies by contract)      │
│  Frequency: quarterly                                           │
│  This is the most opaque and controversial money flow in PBM    │
└─────────────────────────────────────────────────────────────────┘
```

---

### DIR Fees — The Most Controversial Pharmacy Payment Issue

**DIR = Direct and Indirect Remuneration**. Created under Medicare Part D rules, DIR fees are retroactive adjustments the PBM applies to pharmacy reimbursements — sometimes months after the claim was paid.

**How it works**:

```
January: Pharmacy dispenses drug, PBM pays $45.00 ingredient cost
        ↓
        ... time passes ...
        ↓
April: PBM calculates pharmacy's performance score
(medication adherence, star ratings contribution, generic dispensing rate)
        ↓
PBM claws back a DIR fee based on performance score:
Low performer → DIR clawback of $8.00 per claim
High performer → DIR clawback of $2.00 per claim
        ↓
Pharmacy's net reimbursement for that January claim: $37.00–$43.00
(they didn't know this in January)
```

**Why it's controversial**:
- Pharmacies cannot price DIR fees into their business model because they're retroactive and variable
- Small independent pharmacies especially harmed — they don't have the scale to negotiate lower DIR fees
- CMS attempted to eliminate retroactive DIR fees for Medicare Part D effective 2024 (point-of-sale DIR reform), requiring DIR to be reflected at time of dispensing
- This is a politically and financially significant policy area — it has forced several major pharmacy chains to close locations

**PM implication**: If a payer-side PM is evaluating their PBM contract, DIR pass-through to the plan vs. retention by the PBM is a major financial negotiation point. Some PBMs retain most DIR; some pass through 80%+.

---

### Rebate Flow — The Other Hidden Money

**Drug manufacturer rebates**: Manufacturers pay the PBM to place their drug on a preferred tier (lower copay = more utilization = more revenue for manufacturer).

```
Manufacturer (e.g., AstraZeneca) negotiates with PBM
"Put Crestor on Tier 2 preferred, and we'll pay you $X per claim"
        ↓
PBM places Crestor on Tier 2 (member pays $15 copay)
Generic rosuvastatin on Tier 1 (member pays $5 copay)
Brand atorvastatin (Lipitor) on Tier 3 (member pays $45 copay)
        ↓
Member fills Crestor (brand preferred)
Manufacturer pays PBM $18 rebate per claim
PBM passes $12 to the health plan (contract-dependent)
PBM retains $6 as "rebate spread"
        ↓
Health plan uses rebate revenue to offset drug spend
```

**Spread pricing** (a related controversy): PBM charges the health plan $45 for a generic drug that it reimburses the pharmacy $35 — keeping $10 as "spread." This practice is now banned in Medicaid in many states and heavily scrutinized in commercial markets.

**Formulary placement implications for PM**: When a payer's formulary team moves a drug from Tier 2 to Tier 3, it's not just a clinical decision — it's a negotiation with the manufacturer about rebate terms. PMs managing formulary change projects need to understand this financial dynamic.

---

### NCPDP SCRIPT — E-Prescribing Standard

Used for electronic prescriptions — a completely different transaction from point-of-sale D.0 claims:

| Message Type | Direction | Purpose |
|---|---|---|
| **NewRx** | Prescriber → Pharmacy | New prescription sent electronically |
| **RxChangeRequest** | Pharmacy → Prescriber | Request to change drug (PA needed, substitution, clarification) |
| **RxChangeResponse** | Prescriber → Pharmacy | Approve or deny the change request |
| **CancelRx** | Prescriber → Pharmacy | Cancel a prescription before dispensing |
| **CancelRxResponse** | Pharmacy → Prescriber | Confirm cancellation (or reject if already dispensed) |
| **RxFill** | Pharmacy → Prescriber | Notify prescriber that Rx was filled (for controlled substances) |
| **RefillRequest** | Pharmacy → Prescriber | Request a new prescription for a refill |

**Mandated by CMS** for all Medicare Part D controlled substance prescriptions. Most states also mandate electronic prescribing for controlled substances (EPCS) under state law.

**SureScripts**: The dominant network for NCPDP SCRIPT routing — connects ~90% of US prescribers to ~90% of US pharmacies. When a doctor clicks "Send to Pharmacy" in Epic, the NewRx message routes through SureScripts.

---

### NDC — National Drug Code

**11-digit structure**: `NNNNN-NNNN-NN` (5-4-2 format)

| Segment | Digits | Identifies |
|---|---|---|
| **Labeler code** | 5 | Manufacturer or distributor (FDA-assigned) |
| **Product code** | 4 | Drug, strength, and dosage form |
| **Package code** | 2 | Package size and type |

**Example**: `00071-0155-23` = Pfizer (00071) / Lipitor 10mg tablet (0155) / 90-count bottle (23)

**NDC vs. RxNorm**: NDC is manufacturer-specific — generic atorvastatin from Mylan has a different NDC than the same drug from Teva. RxNorm is the FHIR-standard drug code that normalizes across manufacturers and maps to clinical concepts. Mapping NDC → RxNorm is a common ETL task in payer data pipelines (the FHIR MedicationRequest uses RxNorm; the pharmacy claim uses NDC).

**NDC-11 billing format**: Some systems use 10-digit NDC on the bottle but 11-digit in claims (zero-padding). A common data quality issue.

---

### Complete PBM Ecosystem — How Everything Connects

```
Doctor (EHR)  ──NCPDP SCRIPT NewRx──►  Pharmacy (dispensing system)
                                                │
                              NCPDP D.0 claim via pharmacy switch
                                                │
                                                ▼
                            Pharmacy Switch (Change Healthcare / RelayHealth)
                                                │
                                  Routes by BIN/PCN to correct PBM
                                                │
                                                ▼
                             PBM Adjudication Engine
                    (OptumRx / CVS Caremark / Express Scripts)
                             │                  │
                Eligibility  │      Formulary    │    Clinical edits
                (member file)│      (formulary DB)│   (step therapy, QL, PA)
                             │                  │
                             └──────────────────┘
                                       │
                          Paid / Rejected response → Pharmacy
                                       │
                            ┌──────────┴──────────┐
                     Money Flow                Data Flow
                            │                    │
            ┌───────────────┘                    └──────────────┐
            │                                                   │
   PBM pays pharmacy weekly                    PBM sends claim data to
   (ingredient cost + dispense fee             health plan (daily file)
    minus DIR fees)                                    │
            │                                          ▼
   Manufacturer pays PBM rebates          Health plan data warehouse
   quarterly (based on utilization)       (HEDIS, risk adjustment,
            │                              medication adherence tracking,
   PBM passes portion of rebates          care management triggers)
   to health plan per contract
```

---

### PM Interview Answer — PBM Claim Submission and Remittance

> *"Pharmacy claims don't flow through the medical adjudication system at all — they use a completely separate standard called NCPDP D.0, routed through a pharmacy switch to the PBM based on the BIN and PCN on the member's ID card. The pharmacist gets a real-time response in under 3 seconds with the exact patient copay and any clinical edits — step therapy flags, quantity limits, prior auth requirements. That real-time loop is fundamentally different from medical claims, which are batch-processed. On the remittance side, there are actually three separate money flows: the PBM paying the pharmacy weekly for dispensed drugs, the health plan funding the PBM, and drug manufacturers paying rebates to the PBM quarterly for formulary placement — a portion of which flows back to the plan. The rebate economics and DIR fee structures are where most of the financial complexity and controversy in pharmacy benefits lives, and it's increasingly under CMS and congressional scrutiny."*

---

## SMART on FHIR — How Authorization Works

**SMART on FHIR** is the OAuth 2.0 authorization framework for FHIR APIs. It defines how an app gets permission to access a patient's FHIR data.

**Two launch flows**:

*EHR Launch* (your CDS Hooks / DTR scenario):
```
Doctor is in Epic
        ↓
Epic launches your app in an iframe (EHR launch)
        ↓
App receives launch token + FHIR server URL from Epic
        ↓
App exchanges token for OAuth access token with specific scopes
e.g. patient/MedicationRequest.read, patient/Coverage.read
        ↓
App calls FHIR API with Bearer token — gets patient-context data
```

*Standalone Launch* (your Member Access API / patient app scenario):
```
Member opens payer's member portal (not inside an EHR)
        ↓
Portal redirects to payer's auth server
Member logs in, grants consent
        ↓
Auth server returns access token with patient-scoped permissions
        ↓
App calls FHIR API — gets that member's EOB, Coverage, etc.
```

**SMART scopes**: `patient/Patient.read`, `patient/ExplanationOfBenefit.read`, `user/Coverage.read`, `launch/patient`. The scope tells the FHIR server exactly what data the app is allowed to access.

**Why PMs need to know this**: Every CMS-mandated FHIR API requires SMART on FHIR. When your payer client asks "how do we ensure only the right member sees their own EOB?" — SMART on FHIR patient-scoped tokens is the answer. The Token Introspection endpoint validates every API call.

---

## Code Systems Landscape — What Lives Where

Healthcare data is represented in multiple overlapping code systems. A PM needs to know which system is used where:

**Diagnosis and procedure codes (in claims / EHR):**
- **ICD-10-CM** — diagnosis codes (A00.0, E11.9, etc.) — on 837, in EHR problem list
- **ICD-10-PCS** — inpatient procedure codes (used on 837I for hospital inpatient)
- **CPT** (Current Procedural Terminology) — outpatient procedure codes (99213 = office visit) — on 837P
- **HCPCS Level II** — non-physician services, DME, drugs by J-code (J0180 = adalimumab = Humira)
- **DRG** — not a code you submit, it's calculated from ICD codes for inpatient pricing

**Drug codes:**
- **NDC** (National Drug Code) — used in pharmacy claims (NCPDP), manufacturer-specific
- **RxNorm** — standardized drug concept codes used in FHIR (clinical, system-independent)
- **J-codes (HCPCS)** — drugs administered in a clinical setting (infusion, injection) billed on 837

**Clinical / lab codes:**
- **LOINC** — lab test codes (2345-7 = glucose, 718-7 = hemoglobin) — used in FHIR Observation
- **SNOMED CT** — clinical concept codes for diagnoses, findings, procedures in FHIR — more granular than ICD-10
- **CVX** — vaccine codes (used in Immunization resource)

**Provider / facility codes:**
- **NPI** — National Provider Identifier — 10-digit number for every provider and facility
- **TIN / EIN** — Tax ID used for billing/payment
- **Place of Service (POS)** codes — 11 = office, 21 = inpatient hospital, 23 = ER

**Quick rule for interviews**: Claims use ICD-10 + CPT/HCPCS. FHIR clinical resources prefer SNOMED + LOINC + RxNorm. The mapping between them (ICD-10 ↔ SNOMED, NDC ↔ RxNorm) is one of the hardest ongoing data engineering problems in health IT.

---

## FHIR Server Options — Where Your Data Lives

When building FHIR APIs, you need a FHIR server to store and query resources. The main options:

**HAPI FHIR** (open source, Java)
- The most widely used open-source FHIR server
- Your workspace almost certainly uses this (or a containerized version of it)
- Runs as a Spring Boot application, PostgreSQL or MySQL backend
- Supports FHIR R4, R4B, R5
- Used by many mid-size payers and in development environments

**Azure Health Data Services** (Microsoft)
- Managed FHIR service (previously Azure API for FHIR)
- Also includes DICOM service and MedTech (IoT device data → FHIR)
- Most common in Microsoft-aligned payer/provider shops
- Native integration with Azure AD (authentication), Azure Synapse (analytics)

**AWS HealthLake**
- Amazon's managed FHIR datastore
- Unique feature: built-in NLP to extract FHIR resources from unstructured clinical notes
- Integrates with Amazon Comprehend Medical for NLP
- Good for payers building population health analytics on AWS

**Google Cloud Healthcare API**
- Supports FHIR, HL7 v2, DICOM in one managed service
- Strong in genomics and research use cases
- BigQuery integration for analytics

**InterSystems HealthShare / IRIS**
- Enterprise-grade, used by large IDNs (integrated delivery networks) and some payers
- Handles HL7 v2, FHIR, and CDA in one platform
- Very common in health information exchanges (HIEs)

---

## ONC Certification, Inferno & USCDI — The Compliance Layer

**ONC** (Office of the National Coordinator for Health IT) sets the rules for what EHR vendors must implement. Understanding this is key for PM work.

**21st Century Cures Act (2016, implemented 2020–2022)**: Made FHIR APIs mandatory for certified EHRs. Any EHR sold in the US must expose a FHIR R4 API for patient data access or it cannot be certified (and therefore cannot be sold to Medicare/Medicaid providers).

**USCDI** (US Core Data for Interoperability): The minimum dataset ONC requires to be interoperable. Different from US Core IG — USCDI is the *data elements* (patient demographics, problems, medications, allergies, lab results, etc.), US Core IG is the *FHIR implementation* of those elements.

**Inferno**: ONC's official FHIR testing tool. EHR vendors run their FHIR API through Inferno test suites to verify compliance before ONC certification. If your team is helping a payer or provider prepare for an ONC audit, Inferno is what they need to pass.

**g(10) certification**: The specific ONC certification criterion requiring standardized API access (SMART on FHIR + FHIR R4). All major EHRs (Epic, Cerner, Athena, etc.) must hold this certification.

---

## Provider Data Systems — NPI, NPPES, PECOS, CAQH

Provider data is one of the messiest problems in healthcare IT. Understanding these systems is important for Gap 1 (provider network) and Gap 3 (Medicare enrollment).

**NPI — National Provider Identifier**
- 10-digit number assigned by CMS to every healthcare provider and organization
- Type 1 NPI = individual provider (a doctor)
- Type 2 NPI = organizational provider (a hospital, practice)
- Required on every 837 claim — missing/wrong NPI = instant rejection

**NPPES** (National Plan and Provider Enumeration System)
- CMS's public registry of all NPIs
- Free public API: `https://npiregistry.cms.hhs.gov/api`
- Your Plan-Net provider directory (Phase 3) should be cross-referencing NPPES for NPI validation

**PECOS** (Provider Enrollment, Chain and Ownership System)
- CMS's system for Medicare/Medicaid provider enrollment
- A provider must be enrolled in PECOS to bill Medicare
- Common cause of claim denials: provider is credentialed but not yet enrolled in PECOS

**CAQH ProView**
- Industry-wide provider credentialing database
- Providers enter their credentials once in CAQH; payers query it instead of collecting separately
- Reduces the administrative burden of re-credentialing with every payer
- ~99% of health plans use CAQH for credentialing

**Provider data quality problem**: NPI is in NPPES, credentialing is in CAQH, network status is in Facets, clinical privileges are in the hospital's credentialing system — and none of them sync automatically. Keeping provider directories accurate is a perpetual operational pain that FHIR Plan-Net (your Phase 3) is trying to solve.

---

## HIPAA Technical Safeguards — What PM Must Know

HIPAA Security Rule requires covered entities (payers, providers) and their Business Associates (IT vendors) to implement technical safeguards for PHI.

**The key safeguards relevant to your FHIR work:**

*Access control*
- Unique user identification — no shared credentials
- Automatic logoff after inactivity
- SMART on FHIR scopes enforce minimum necessary access

*Audit controls*
- Log every access to PHI — who accessed what record and when
- FHIR AuditEvent resource is the standard way to record this
- Logs must be retained and tamper-proof

*Transmission security*
- All PHI in transit must be encrypted — TLS 1.2 minimum (TLS 1.3 preferred)
- This applies to every FHIR API call, every EDI file transfer, every HL7 v2 message

*Integrity*
- PHI must not be altered in transit without detection
- Digital signatures, checksums on EDI files

*BAA (Business Associate Agreement)*
- Any IT vendor (Infosys, Cognizant, AWS, Azure) that touches PHI must sign a BAA with the covered entity
- No BAA = the vendor cannot legally access PHI
- This is a procurement/legal prerequisite before any project starts

**De-identification**: Two methods — Safe Harbor (remove 18 specific identifiers) and Expert Determination. De-identified data is no longer PHI and can be used freely for analytics/AI training. Important for your `fhir-mapping-agent` and bulk data analytics work.

---

## Data Exchange Methods — How Files Actually Move

Beyond APIs, a lot of healthcare data still moves via older methods:

**SFTP / VPN (for EDI)**
- 834 enrollment files, 837 claim files — often still delivered via SFTP
- VPN tunnels between payer and clearinghouse
- Files have structured naming conventions and pickup/delivery schedules

**Direct (Direct Trust)**
- Secure email-like protocol for clinical documents
- Predecessor to FHIR for provider-to-provider exchange
- Still used for referral letters, discharge summaries between providers who don't share an EHR

**CCD / C-CDA (Clinical Document Architecture)**
- XML-based clinical document format — used before FHIR became dominant
- Still generated by many EHRs for transitions of care, referrals
- FHIR $document operation creates a FHIR-native equivalent

**REST API (FHIR)**
- The modern standard — JSON over HTTPS
- Synchronous for single-resource queries, Bulk Data for population exports

**FHIR Bulk Data / NDJSON**
- For large-scale exports: `GET /Patient/$export`
- Returns NDJSON files (Newline Delimited JSON — one FHIR resource per line)
- Designed for population health: analytics, care gap identification, risk stratification
- Your Phase 8 BulkDataAPI implements this

---

## Summary — Technology Stack Cheat Sheet

**Integration / API Gateway**: MuleSoft Anypoint, Apigee, IBM API Connect

**FHIR Servers**: HAPI FHIR (open source), Azure Health Data Services, AWS HealthLake, Google Cloud Healthcare API, InterSystems IRIS

**Rules Engines**: Drools, IBM ODM, Optum Clinical Decision Support

**Caching / Real-time**: Redis, Hazelcast (eligibility cache, benefit accumulator, CRD rules cache)

**Event Streaming**: Apache Kafka, IBM MQ, Azure Service Bus (claims pipeline, PA notifications)

**HL7 v2 Interface Engines**: Rhapsody/Infor, Mirth Connect, InterSystems HealthShare

**Core Admin Systems (CAPS)**: TriZetto Facets, QNXT, HealthEdge HealthRules Payor

**Clinical Editing**: Optum ClaimCheck / CES, Cotiviti

**Provider Credentialing**: CAQH ProView, PECOS (Medicare), NPPES (NPI registry)

**Pharmacy**: NCPDP D.0 (claims), NCPDP SCRIPT (e-prescribing), NDC codes

**Authorization**: SMART on FHIR (OAuth 2.0 for healthcare), UDAP (emerging)

**ONC Compliance Testing**: Inferno, USCDI, g(10) certification criterion

**De-identification / Analytics**: Safe Harbor method, NDJSON Bulk Data export, AWS HealthLake NLP, Azure Synapse

---

# PM-FRAMING TALKING POINTS (Practice Saying These Out Loud)

1. **Bridge story**: *"17 years in healthcare IT, focused the last year on the payer-provider FHIR ecosystem. I bridge clinicians, engineering, and product — and I've built working prototypes on top of the same CMS mandates that drive payer roadmaps."*

2. **Prior auth connection**: *"Most payers do utilization management with phone and fax. I've built the FHIR-based future — CRD identifies need, DTR auto-populates documentation, PAS submits. That's the CMS-0057 mandate, end-to-end."*

3. **AI on mandates**: *"CMS mandated FHIR APIs — most organizations are still trying to be compliant. I've gone further: built `fhir-mcp-suite` letting LLMs safely query FHIR data, and `fhir-mapping-agent` automating the legacy-to-FHIR mapping work that makes compliance feasible."*

4. **Business outcome framing**: Always tie technical work back to: CMS deadline compliance, member experience, provider abrasion reduction, admin cost reduction, Star Rating impact.

---

# QUIZ MODE — How to Use Me

Ask any of these in the chat:

- *"Quiz me on Gap 1 (Claims)"* — I'll fire 10 questions, you answer, I score
- *"Mock interview — 15 minutes, payer PM role"*
- *"Drill me on Medicare Advantage vocabulary"*
- *"Give me hard scenario questions on UM"*
- *"Pretend you're the hiring manager and ask me 5 questions about my workspace"*

---

# 48-HOUR PREP SCHEDULE

**Monday Evening (2 hrs)**
- Read this whole doc once
- Quiz: Gap 1 (Claims) + Gap 2 (Enrollment)

**Tuesday Morning (1 hr)**
- Re-read Medicare Advantage section
- Quiz: Gap 3 (Medicare)

**Tuesday Evening (2 hrs)**
- Re-read HEDIS / UM section
- Quiz: Gap 4 (HEDIS/CM/UM)
- Mock interview round 1

**Wednesday Morning (1 hr)**
- Re-read Key Terms in each gap only
- Practice the 4 PM-framing talking points out loud
- Mock interview round 2

---

**Reminder**: You don't need to code any of this. You need to *speak* the language, *draw* the flows, and *connect* it to what you've already built. Your workspace + GitHub profile + this doc = enough to compete strongly.
