# Quick Reference - Phase 1

## 🚀 Server Commands

```powershell
# Start FHIR Server
.\Scripts\Start-FHIRServer.ps1

# Check Status
.\Scripts\Check-FHIRServer.ps1

# Stop Server
.\Scripts\Stop-FHIRServer.ps1

# Restart Server
.\Scripts\Restart-FHIRServer.ps1
```

---

## 📊 Current Data

| Resource | Count |
|----------|-------|
| Patients | 27 |
| ExplanationOfBenefit | 2,170 |
| Encounters | 1,120 |
| Conditions | 915 |
| Procedures | 754 |
| MedicationRequests | 1,050 |

---

## 🔗 Access Points

- **FHIR API**: http://localhost:8082/fhir
- **Web UI**: http://localhost:8082
- **Metadata**: http://localhost:8082/fhir/metadata

---

## 📝 Useful Queries

```powershell
# Get all patients
Invoke-RestMethod http://localhost:8082/fhir/Patient

# Get patient count
Invoke-RestMethod "http://localhost:8082/fhir/Patient?_summary=count"

# Get all claims (EOBs)
Invoke-RestMethod http://localhost:8082/fhir/ExplanationOfBenefit

# Get claims for specific patient
Invoke-RestMethod "http://localhost:8082/fhir/ExplanationOfBenefit?patient=Patient/51707"

# Get encounters
Invoke-RestMethod http://localhost:8082/fhir/Encounter

# Search by name
Invoke-RestMethod "http://localhost:8082/fhir/Patient?name=Ramon"
```

---

## 🔧 Data Management

```powershell
# Generate new synthetic data (20 patients, 2-year history)
cd Phase1\Data
.\Generate-SimpleData.ps1

# Load data into server
.\Load-DataToServer.ps1

# Explore loaded data
.\Explore-FHIRData.ps1
```

---

## 🐳 Docker Commands

```powershell
# View logs
docker logs fhir-server-payer --tail 50

# Check container status
cd Phase1\Setup
docker-compose ps

# Restart containers
docker-compose restart

# Stop and remove all data (fresh start)
docker-compose down -v
```

---

## 📚 Key Resources

| Resource | Purpose |
|----------|---------|
| **ExplanationOfBenefit** | Claims data (most important for payers) |
| **Patient** | Member demographics |
| **Coverage** | Insurance coverage details |
| **Encounter** | Medical visits (derived from claims) |
| **Condition** | Diagnoses (ICD-10) |
| **Procedure** | Medical procedures (CPT/HCPCS) |
| **MedicationRequest** | Prescriptions |

---

## 🎯 CARIN Blue Button EOB Types

1. **Professional** - Office visits, specialist visits
2. **Institutional (Inpatient)** - Hospital stays
3. **Pharmacy** - Prescription drugs (NCPDP)
4. **Oral** - Dental services

---

## ⚡ Quick Troubleshooting

### Server won't start
```powershell
# Check if Docker is running
docker ps

# Check if port 8082 is in use
netstat -ano | findstr :8082

# View detailed logs
docker logs fhir-server-payer
```

### Data loading fails
```powershell
# Verify server is running
Invoke-RestMethod http://localhost:8082/fhir/metadata

# Check file sizes (should be < 15MB)
cd Phase1\Data\output\fhir
Get-ChildItem *.json | Select Name, @{N="Size(MB)";E={[math]::Round($_.Length/1MB,2)}}
```

### Can't query data
```powershell
# Check if data is loaded
.\Scripts\Check-FHIRServer.ps1

# If counts are 0, reload data
cd Phase1\Data
.\Load-DataToServer.ps1
```

---

## 🎓 Phase 1 Checklist

- [x] HAPI FHIR R4 server running on port 8082
- [x] PostgreSQL database with persistent storage
- [x] 21 synthetic patients loaded
- [x] 2,170 ExplanationOfBenefit records
- [x] Server management scripts created
- [x] Data generation and loading scripts working
- [x] Can query FHIR data via API
- [x] Understand payer-specific FHIR resources

**Status**: ✅ Phase 1 Complete - Ready for Phase 2

---

## ➡️ Next: Phase 2 - Member Access API

Build a CARIN Blue Button compliant API:
- Implement EOB endpoints
- Add OAuth 2.0 authorization
- Support bulk data export
- Test with synthetic data

**Estimated Time**: 1-2 weeks
