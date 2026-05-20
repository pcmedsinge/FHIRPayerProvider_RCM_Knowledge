# Phase 2: Member Access API — Hands-On Exercises

Complete these 25 exercises in order. Part A is Postman/browser only. Part B builds the .NET API. Part C tests real-world scenarios.

**HAPI FHIR Base**: http://localhost:8082/fhir  
**Sample Patient**: Ramon749 Schulist381 (ID: 51707)

---

## Part A: Explore CARIN Blue Button Data (Exercises 1–8)

*No coding — use browser or Postman to understand the data*

---

### Exercise 1: Explore Patient as a Member

View a patient's full resource and identify the **payer-relevant fields**: identifiers (MemberID), name, birthDate, gender, address, telecom.

**URL**: http://localhost:8082/fhir/Patient/51707

**What to look for**:
- `identifier[]` — Member IDs (system + value). In production, CARIN BB requires a Member Identifier
- `name` — Used for member matching
- `birthDate` — Used for eligibility verification
- `address` — Used for coordination of benefits

**Try also**: View another patient to compare:
- http://localhost:8082/fhir/Patient/52458
- http://localhost:8082/fhir/Patient/65520

**Key learning**: In CARIN BB, Patient must have a member identifier with system `http://terminology.hl7.org/CodeSystem/v2-0203` and code `MB`. Our Synthea data doesn't have this — in production, payers add their member ID.

---

### Exercise 2: Explore Coverage Resource

Coverage resources represent a member's insurance enrollment. In our data, Coverage is **contained inside** EOBs (not standalone). Let's see it:

**URL**: http://localhost:8082/fhir/ExplanationOfBenefit/51713

**What to look for** — find the `contained` array at the top:
```json
"contained": [
  {
    "resourceType": "Coverage",
    "id": "coverage",
    "status": "active",
    "type": { "text": "Humana" },
    "beneficiary": { "reference": "Patient/51707" },
    "payor": [{ "display": "Humana" }]
  }
]
```

**Key fields in a full Coverage resource**:
| Field | Purpose | Example |
|-------|---------|---------|
| `status` | Active or cancelled | active |
| `type` | Plan type | HMO, PPO |
| `subscriber` | Primary policyholder | Patient/51707 |
| `beneficiary` | Who is covered | Patient/51707 |
| `period` | Coverage dates | 2023-01-01 to 2024-12-31 |
| `payor` | Insurance company | Humana |
| `class[]` | Plan, group, subgroup | Plan ID, Group number |

**Key learning**: Synthea generates Coverage as contained resources. In Exercise 7, we'll create standalone Coverage resources to properly represent member enrollment.

---

### Exercise 3: EOB — Professional Claims (Doctor Visits)

Professional claims represent doctor office visits, outpatient procedures — billed on CMS 1500 form.

**URL**: http://localhost:8082/fhir/ExplanationOfBenefit/51713

**Walk through the resource structure**:

1. **Header**: Find `type.coding[0].code` — should be `"professional"`
2. **Patient**: `patient.reference` → `"Patient/51707"`
3. **Provider**: `provider.reference` → the doctor
4. **Facility**: `facility.display` → where the visit happened
5. **Diagnosis**: `diagnosis[].diagnosisReference` → linked Condition
6. **Care Team**: `careTeam[].provider` → who treated the patient
7. **Line Items**: `item[]` — each service performed:
   - `productOrService` → procedure code (SNOMED/CPT)
   - `servicedPeriod` → when
   - `locationCodeableConcept` → place of service
8. **Totals**: `total[].amount` → submitted amount
9. **Payment**: `payment.amount` → what was paid

**Try**: Find all professional claims for patient 51707:
- http://localhost:8082/fhir/ExplanationOfBenefit?patient=Patient/51707&_count=10

---

### Exercise 4: EOB — Institutional Claims (Hospital)

Institutional claims represent hospital admissions — billed on UB-04 form.

**URL**: http://localhost:8082/fhir/ExplanationOfBenefit/52477

**Compare with professional**:
- `type.coding[0].code` → should be `"institutional"`
- May have `revenue` codes on line items
- Typically higher dollar amounts
- Often has DRG (Diagnosis Related Group) codes

**Key learning**: The same ExplanationOfBenefit resource handles all claim types — the `type` field distinguishes them. CARIN BB defines separate profiles for each.

---

### Exercise 5: EOB — Pharmacy Claims (Prescriptions)

Pharmacy claims represent prescription drug fills.

**URL**: http://localhost:8082/fhir/ExplanationOfBenefit/51776

**What's different from professional/institutional**:
- `type.coding[0].code` → `"pharmacy"`
- `item[].productOrService` → drug codes (RxNorm/NDC instead of CPT)
- In production: days supply, DAW (Dispense As Written) code, refill number
- Typically linked to a MedicationRequest

**Try**: See all pharmacy claims:
- http://localhost:8082/fhir/ExplanationOfBenefit?patient=Patient/51707&_count=100

Then look for entries where `type.coding[0].code` is `"pharmacy"`.

---

### Exercise 6: EOB Adjudication Deep Dive

Let's examine the financial story of a claim.

**URL**: http://localhost:8082/fhir/ExplanationOfBenefit/51713

**Find the `total[]` array** near the bottom:
```json
"total": [{
  "category": {
    "coding": [{"code": "submitted", "display": "Submitted Amount"}]
  },
  "amount": {"value": 545.27, "currency": "USD"}
}]
```

**Also check `payment`**:
```json
"payment": { "amount": {"value": 0.0, "currency": "USD"} }
```

**In a fully adjudicated claim (CARIN BB requires all of these)**:

| Total Category | What It Means |
|---------------|---------------|
| `submitted` | What the provider charged |
| `eligible` | What the plan allows (contracted rate) |
| `benefit` | What the plan pays |
| `deductible` | Applied to member's deductible |
| `copay` | Member's copay |
| `coinsurance` | Member's coinsurance |

**Key learning**: Our Synthea data has `submitted` totals. Production systems have all adjudication categories — this is what makes a claim an "Explanation of Benefit" vs just a "Claim".

---

### Exercise 7: Create Standalone Coverage Resources

Our data has Coverage contained inside EOBs. For a proper Member Access API, we need **standalone Coverage resources** that members can query independently.

**In Postman**, send a POST request:

**Method**: POST  
**URL**: http://localhost:8082/fhir/Coverage  
**Headers**: `Content-Type: application/fhir+json`  
**Body**:
```json
{
  "resourceType": "Coverage",
  "status": "active",
  "type": {
    "coding": [{
      "system": "http://terminology.hl7.org/CodeSystem/v3-ActCode",
      "code": "HIP",
      "display": "health insurance plan policy"
    }]
  },
  "subscriber": {
    "reference": "Patient/51707"
  },
  "beneficiary": {
    "reference": "Patient/51707"
  },
  "relationship": {
    "coding": [{
      "system": "http://terminology.hl7.org/CodeSystem/subscriber-relationship",
      "code": "self"
    }]
  },
  "period": {
    "start": "2024-01-01",
    "end": "2025-12-31"
  },
  "payor": [{
    "display": "Humana"
  }],
  "class": [
    {
      "type": {
        "coding": [{
          "system": "http://terminology.hl7.org/CodeSystem/coverage-class",
          "code": "plan"
        }]
      },
      "value": "HUM-GOLD-2024",
      "name": "Humana Gold Plus"
    },
    {
      "type": {
        "coding": [{
          "system": "http://terminology.hl7.org/CodeSystem/coverage-class",
          "code": "group"
        }]
      },
      "value": "GRP-12345",
      "name": "Employer Group 12345"
    }
  ]
}
```

**After POST**: Note the ID returned in the response. Then verify:
- http://localhost:8082/fhir/Coverage?beneficiary=Patient/51707

**Create one more** for another patient (52458) — change the `subscriber`, `beneficiary`, and `payor` to "Blue Cross Blue Shield":
```json
{
  "resourceType": "Coverage",
  "status": "active",
  "type": {
    "coding": [{
      "system": "http://terminology.hl7.org/CodeSystem/v3-ActCode",
      "code": "HIP",
      "display": "health insurance plan policy"
    }]
  },
  "subscriber": {
    "reference": "Patient/52458"
  },
  "beneficiary": {
    "reference": "Patient/52458"
  },
  "relationship": {
    "coding": [{
      "system": "http://terminology.hl7.org/CodeSystem/subscriber-relationship",
      "code": "self"
    }]
  },
  "period": {
    "start": "2024-01-01",
    "end": "2025-12-31"
  },
  "payor": [{
    "display": "Blue Cross Blue Shield"
  }],
  "class": [
    {
      "type": {
        "coding": [{
          "system": "http://terminology.hl7.org/CodeSystem/coverage-class",
          "code": "plan"
        }]
      },
      "value": "BCBS-SILVER-2024",
      "name": "BCBS Silver Standard"
    }
  ]
}
```

**Verify both**: http://localhost:8082/fhir/Coverage?_summary=count

---

### Exercise 8: Resource Relationships — The Member Data Graph

Understand how resources connect for a single member:

```
Patient/51707 (Ramon Schulist)
    │
    ├──► Coverage (Humana Gold Plus)
    │       └── payor: Humana
    │
    ├──► ExplanationOfBenefit (many)
    │       ├── type: professional (doctor visits)
    │       ├── type: pharmacy (prescriptions)
    │       ├── type: institutional (hospital)
    │       ├── provider → Practitioner
    │       ├── facility → Location
    │       ├── diagnosis → Condition
    │       └── item[].encounter → Encounter
    │
    ├──► Condition (diagnoses)
    ├──► Encounter (visits)
    ├──► MedicationRequest (prescriptions)
    └──► Procedure (procedures)
```

**Test these relationships** — all for patient 51707:

| What | URL |
|------|-----|
| The member | http://localhost:8082/fhir/Patient/51707 |
| Their coverage | http://localhost:8082/fhir/Coverage?beneficiary=Patient/51707 |
| Their claims | http://localhost:8082/fhir/ExplanationOfBenefit?patient=Patient/51707&_count=5 |
| Their conditions | http://localhost:8082/fhir/Condition?patient=Patient/51707 |
| Their encounters | http://localhost:8082/fhir/Encounter?patient=Patient/51707&_count=5 |
| Their medications | http://localhost:8082/fhir/MedicationRequest?patient=Patient/51707&_count=5 |
| Everything at once | http://localhost:8082/fhir/Patient?_id=51707&_revinclude=ExplanationOfBenefit:patient&_revinclude=Condition:patient |

**Key learning**: The Member Access API must expose all of these. A member should be able to query each resource type filtered to their own data.

---

## Part B: Build .NET 8 Member Access API (Exercises 9–19)

*Hands-on coding — build the API step by step*

---

### Exercise 9: Create the .NET 8 Project

Create the API project with required NuGet packages.

**Step 1**: In terminal, navigate to Phase2 and create the project:
```
cd D:\PracticeApps\FHIRRelated\FHIRPayerProvider\Phase2
dotnet new webapi -n MemberAccessAPI --framework net8.0
cd MemberAccessAPI
```

**Step 2**: Add NuGet packages:
```
dotnet add package Hl7.Fhir.R4 --version 5.11.2
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

- `Hl7.Fhir.R4` — Firely SDK for FHIR R4 (creates FHIR client, parses resources)
- `JwtBearer` — JWT token authentication

**Step 3**: Verify it builds:
```
dotnet build
```

**Step 4**: Test default template works:
```
dotnet run
```
Access the Swagger UI at the URL shown in terminal (typically https://localhost:5220/swagger).

Stop the server (Ctrl+C) before proceeding to next exercise.

---

### Exercise 10: Configure FHIR Client Service

Create a service that talks to HAPI FHIR using Firely SDK.

**Create** `Services/IFhirService.cs`:
```csharp
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace MemberAccessAPI.Services;

public interface IFhirService
{
    Task<Patient?> GetPatientAsync(string patientId);
    Task<Bundle> SearchEOBByPatientAsync(string patientId, string? type = null, 
        string? startDate = null, string? endDate = null, int count = 10);
    Task<Bundle> SearchCoverageByPatientAsync(string patientId);
    Task<Bundle> GetPatientEverythingAsync(string patientId);
}
```

**Create** `Services/FhirService.cs`:
```csharp
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;

namespace MemberAccessAPI.Services;

public class FhirService : IFhirService
{
    private readonly FhirClient _fhirClient;
    private readonly ILogger<FhirService> _logger;

    public FhirService(IConfiguration config, ILogger<FhirService> logger)
    {
        _logger = logger;
        var fhirServerUrl = config["FhirServer:BaseUrl"] ?? "http://localhost:8082/fhir";
        
        var settings = new FhirClientSettings
        {
            PreferredFormat = ResourceFormat.Json,
            PreferredReturn = Prefer.ReturnRepresentation
        };
        _fhirClient = new FhirClient(fhirServerUrl, settings);
    }

    public async Task<Patient?> GetPatientAsync(string patientId)
    {
        try
        {
            return await _fhirClient.ReadAsync<Patient>($"Patient/{patientId}");
        }
        catch (FhirOperationException ex) when (ex.Status == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Patient {PatientId} not found", patientId);
            return null;
        }
    }

    public async Task<Bundle> SearchEOBByPatientAsync(string patientId, 
        string? type = null, string? startDate = null, string? endDate = null, int count = 10)
    {
        var searchParams = new SearchParams()
            .Where($"patient=Patient/{patientId}")
            .LimitTo(count);

        if (!string.IsNullOrEmpty(type))
            searchParams.Where($"type=http://terminology.hl7.org/CodeSystem/claim-type|{type}");

        if (!string.IsNullOrEmpty(startDate))
            searchParams.Where($"created=ge{startDate}");

        if (!string.IsNullOrEmpty(endDate))
            searchParams.Where($"created=le{endDate}");

        return await _fhirClient.SearchAsync<ExplanationOfBenefit>(searchParams);
    }

    public async Task<Bundle> SearchCoverageByPatientAsync(string patientId)
    {
        var searchParams = new SearchParams()
            .Where($"beneficiary=Patient/{patientId}");

        return await _fhirClient.SearchAsync<Coverage>(searchParams);
    }

    public async Task<Bundle> GetPatientEverythingAsync(string patientId)
    {
        // $everything is a FHIR operation that returns all data for a patient
        var result = await _fhirClient.WholeSystemOperationAsync("Patient", patientId, "everything");
        return result as Bundle ?? new Bundle();
    }
}
```

**Update** `appsettings.json` — add FHIR server config:
```json
{
  "FhirServer": {
    "BaseUrl": "http://localhost:8082/fhir"
  },
  "Jwt": {
    "Key": "Phase2MemberAccessAPI-SuperSecret-Key-Min32Chars!",
    "Issuer": "MemberAccessAPI",
    "Audience": "MemberPortal"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**Register in** `Program.cs`:
```csharp
builder.Services.AddSingleton<IFhirService, FhirService>();
```

**Test**: `dotnet build` — should compile with no errors.

---

### Exercise 11: Build Patient Endpoint

Create the endpoint that returns a member's demographics.

**Create** `Controllers/PatientController.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Hl7.Fhir.Serialization;
using MemberAccessAPI.Services;

namespace MemberAccessAPI.Controllers;

[ApiController]
[Route("api/fhir/[controller]")]
public class PatientController : ControllerBase
{
    private readonly IFhirService _fhirService;
    private readonly FhirJsonSerializer _serializer = new(new SerializerSettings { Pretty = true });

    public PatientController(IFhirService fhirService)
    {
        _fhirService = fhirService;
    }

    /// <summary>
    /// GET /api/fhir/Patient/{id} — Get member demographics
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPatient(string id)
    {
        var patient = await _fhirService.GetPatientAsync(id);
        
        if (patient == null)
            return NotFound(new { error = "Patient not found", patientId = id });

        var json = _serializer.SerializeToString(patient);
        return Content(json, "application/fhir+json");
    }
}
```

**Test with Postman**:
1. Run the API: `dotnet run`
2. `GET http://localhost:5220/api/fhir/Patient/51707`
3. You should get the same Patient resource, but served through YOUR API
4. Try a non-existent patient: `GET http://localhost:5220/api/fhir/Patient/99999` → 404

**Key learning**: Your API is now a **facade** over HAPI FHIR. The member never talks to HAPI directly.

---

### Exercise 12: Build Coverage Endpoint

**Create** `Controllers/CoverageController.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Hl7.Fhir.Serialization;
using MemberAccessAPI.Services;

namespace MemberAccessAPI.Controllers;

[ApiController]
[Route("api/fhir/[controller]")]
public class CoverageController : ControllerBase
{
    private readonly IFhirService _fhirService;
    private readonly FhirJsonSerializer _serializer = new(new SerializerSettings { Pretty = true });

    public CoverageController(IFhirService fhirService)
    {
        _fhirService = fhirService;
    }

    /// <summary>
    /// GET /api/fhir/Coverage?patient={id} — Get member's insurance coverage
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SearchCoverage([FromQuery] string patient)
    {
        if (string.IsNullOrEmpty(patient))
            return BadRequest(new { error = "patient parameter is required" });

        var bundle = await _fhirService.SearchCoverageByPatientAsync(patient);
        
        var json = _serializer.SerializeToString(bundle);
        return Content(json, "application/fhir+json");
    }
}
```

**Test with Postman** (after Exercise 7 creates Coverage resources):
1. `GET http://localhost:5220/api/fhir/Coverage?patient=51707`
2. Should return a Bundle with the Coverage resource created in Exercise 7

---

### Exercise 13: Build EOB Endpoint with Search

The core endpoint — claims with filtering by type and date.

**Create** `Controllers/EOBController.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Hl7.Fhir.Serialization;
using MemberAccessAPI.Services;

namespace MemberAccessAPI.Controllers;

[ApiController]
[Route("api/fhir/ExplanationOfBenefit")]
public class EOBController : ControllerBase
{
    private readonly IFhirService _fhirService;
    private readonly FhirJsonSerializer _serializer = new(new SerializerSettings { Pretty = true });

    public EOBController(IFhirService fhirService)
    {
        _fhirService = fhirService;
    }

    /// <summary>
    /// GET /api/fhir/ExplanationOfBenefit?patient={id} — Get member's claims
    /// Optional: type (professional|institutional|pharmacy), startDate, endDate, _count
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SearchEOB(
        [FromQuery] string patient,
        [FromQuery] string? type = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        [FromQuery(Name = "_count")] int count = 10)
    {
        if (string.IsNullOrEmpty(patient))
            return BadRequest(new { error = "patient parameter is required" });

        var bundle = await _fhirService.SearchEOBByPatientAsync(patient, type, startDate, endDate, count);
        
        var json = _serializer.SerializeToString(bundle);
        return Content(json, "application/fhir+json");
    }
}
```

**Test with Postman**:
1. All claims: `GET http://localhost:5220/api/fhir/ExplanationOfBenefit?patient=51707`
2. Only pharmacy: `GET http://localhost:5220/api/fhir/ExplanationOfBenefit?patient=51707&type=pharmacy`
3. Date range: `GET http://localhost:5220/api/fhir/ExplanationOfBenefit?patient=51707&startDate=2020-01-01&endDate=2024-12-31`
4. Combined: `GET http://localhost:5220/api/fhir/ExplanationOfBenefit?patient=51707&type=professional&_count=5`

---

### Exercise 14: Add JWT Authentication

Implement token-based auth so members must "log in" first.

**Create** `Models/AuthModels.cs`:
```csharp
namespace MemberAccessAPI.Models;

public class LoginRequest
{
    public string MemberId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
```

**Create** `Controllers/AuthController.cs`:
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MemberAccessAPI.Models;
using MemberAccessAPI.Services;

namespace MemberAccessAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IFhirService _fhirService;

    public AuthController(IConfiguration config, IFhirService fhirService)
    {
        _config = config;
        _fhirService = fhirService;
    }

    /// <summary>
    /// POST /api/auth/token — Simulate member login, get JWT token
    /// </summary>
    [HttpPost("token")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Verify patient exists in FHIR server
        var patient = await _fhirService.GetPatientAsync(request.MemberId);
        if (patient == null)
            return Unauthorized(new { error = "Member not found" });

        // Generate JWT with patient ID embedded
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("patient_id", request.MemberId),
            new Claim(ClaimTypes.Name, request.Name),
            new Claim("scope", "patient/Patient.read patient/ExplanationOfBenefit.read patient/Coverage.read")
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return Ok(new LoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            PatientId = request.MemberId,
            ExpiresAt = token.ValidTo
        });
    }
}
```

**Update** `Program.cs` to configure JWT authentication:
```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MemberAccessAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register FHIR service
builder.Services.AddSingleton<IFhirService, FhirService>();

// Configure JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

**Test with Postman**:
1. `POST http://localhost:5220/api/auth/token`  
   Body: `{"memberId": "51707", "name": "Ramon Schulist"}`
2. Copy the `token` from response
3. Use it in next exercises as `Authorization: Bearer <token>`

---

### Exercise 15: Add Patient Access Control

Enforce that a member can ONLY see their own data. This is the most critical security requirement.

**Create** `Auth/PatientAccessHandler.cs`:
```csharp
using System.Security.Claims;

namespace MemberAccessAPI.Auth;

public static class PatientAccessHandler
{
    /// <summary>
    /// Extracts patient_id from JWT claims
    /// </summary>
    public static string? GetPatientId(ClaimsPrincipal user)
    {
        return user.FindFirst("patient_id")?.Value;
    }

    /// <summary>
    /// Checks if the authenticated member is allowed to access this patient's data
    /// </summary>
    public static bool CanAccessPatient(ClaimsPrincipal user, string requestedPatientId)
    {
        var tokenPatientId = GetPatientId(user);
        return tokenPatientId != null && tokenPatientId == requestedPatientId;
    }
}
```

**Update all controllers** — add `[Authorize]` and access check.

Update `PatientController.cs` — add to the `GetPatient` method:
```csharp
using Microsoft.AspNetCore.Authorization;
using MemberAccessAPI.Auth;

// Add [Authorize] to the class
[ApiController]
[Route("api/fhir/[controller]")]
[Authorize]
public class PatientController : ControllerBase
{
    // ... existing code ...

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPatient(string id)
    {
        // Access control: member can only see their own data
        if (!PatientAccessHandler.CanAccessPatient(User, id))
            return Forbid();

        var patient = await _fhirService.GetPatientAsync(id);
        if (patient == null)
            return NotFound(new { error = "Patient not found" });

        var json = _serializer.SerializeToString(patient);
        return Content(json, "application/fhir+json");
    }

    /// <summary>
    /// GET /api/fhir/Patient/me — Get the authenticated member's own data
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var patientId = PatientAccessHandler.GetPatientId(User);
        if (patientId == null) return Unauthorized();

        var patient = await _fhirService.GetPatientAsync(patientId);
        if (patient == null) return NotFound();

        var json = _serializer.SerializeToString(patient);
        return Content(json, "application/fhir+json");
    }
}
```

Add `[Authorize]` and access control to `CoverageController` and `EOBController` similarly — check that `patient` query parameter matches the token's `patient_id`.

**Test with Postman**:
1. Login as patient 51707 → get token
2. `GET /api/fhir/Patient/me` with token → 200 OK (your data)
3. `GET /api/fhir/Patient/51707` with token → 200 OK (your data)
4. `GET /api/fhir/Patient/52458` with token → 403 Forbidden (not your data!)
5. `GET /api/fhir/ExplanationOfBenefit?patient=52458` with token → 403 Forbidden
6. Without token → 401 Unauthorized

---

### Exercise 16: Add FHIR OperationOutcome Error Responses

FHIR APIs should return `OperationOutcome` for errors, not plain JSON.

**Create** `Middleware/FhirErrorMiddleware.cs`:
```csharp
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace MemberAccessAPI.Middleware;

public class FhirErrorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly FhirJsonSerializer _serializer = new(new SerializerSettings { Pretty = true });

    public FhirErrorMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        // Convert standard HTTP errors on FHIR endpoints to OperationOutcome
        if (context.Request.Path.StartsWithSegments("/api/fhir") && 
            context.Response.StatusCode >= 400 &&
            !context.Response.HasStarted)
        {
            var outcome = new OperationOutcome();
            var issue = new OperationOutcome.IssueComponent
            {
                Severity = OperationOutcome.IssueSeverity.Error,
                Code = context.Response.StatusCode switch
                {
                    401 => OperationOutcome.IssueType.Login,
                    403 => OperationOutcome.IssueType.Forbidden,
                    404 => OperationOutcome.IssueType.NotFound,
                    _ => OperationOutcome.IssueType.Processing
                },
                Diagnostics = context.Response.StatusCode switch
                {
                    401 => "Authentication required. Use POST /api/auth/token to get a token.",
                    403 => "Access denied. You can only access your own data.",
                    404 => "Resource not found.",
                    _ => "An error occurred processing the request."
                }
            };
            outcome.Issue.Add(issue);

            context.Response.ContentType = "application/fhir+json";
            await context.Response.WriteAsync(_serializer.SerializeToString(outcome));
        }
    }
}
```

**Register in** `Program.cs` (before `app.MapControllers()`):
```csharp
app.UseMiddleware<MemberAccessAPI.Middleware.FhirErrorMiddleware>();
```

**Test with Postman**:
1. Any endpoint without token → should return FHIR OperationOutcome:
```json
{
  "resourceType": "OperationOutcome",
  "issue": [{
    "severity": "error",
    "code": "login",
    "diagnostics": "Authentication required. Use POST /api/auth/token to get a token."
  }]
}
```

---

### Exercise 17: Add Pagination Support

Handle large claim sets with proper FHIR Bundle navigation.

The Firely SDK and HAPI FHIR already return paginated Bundles with `link` entries. Your API passes these through, but the links point to HAPI FHIR directly. Let's fix that.

**Add to** `FhirService.cs`:
```csharp
public async Task<Bundle> GetNextPageAsync(string nextUrl)
{
    // Follow the next page link from a Bundle
    var result = await _fhirClient.ContinueAsync(null);
    return result ?? new Bundle();
}
```

**Add to** `EOBController.cs`:
```csharp
/// <summary>
/// GET /api/fhir/ExplanationOfBenefit?patient={id}&_count=5 
/// Response includes Bundle.link with next/prev page URLs
/// </summary>
```

**Test with Postman**:
1. `GET /api/fhir/ExplanationOfBenefit?patient=51707&_count=5`
2. In the response Bundle, check `link[]` for `relation: "next"`
3. Note: The next URL points to HAPI — in production you'd rewrite these

---

### Exercise 18: SMART on FHIR Scopes

Add scope checking — the token contains FHIR scopes that limit what resources the member can read.

**Update** `Auth/PatientAccessHandler.cs`:
```csharp
/// <summary>
/// Checks if token has the required SMART on FHIR scope
/// Scopes follow format: patient/{ResourceType}.read
/// </summary>
public static bool HasScope(ClaimsPrincipal user, string resourceType)
{
    var scopeClaim = user.FindFirst("scope")?.Value ?? "";
    var requiredScope = $"patient/{resourceType}.read";
    return scopeClaim.Contains(requiredScope);
}
```

**Use in controllers**:
```csharp
// In EOBController
if (!PatientAccessHandler.HasScope(User, "ExplanationOfBenefit"))
    return StatusCode(403, "Insufficient scope. Required: patient/ExplanationOfBenefit.read");
```

**Test with Postman**:
- Our default token has scopes: `patient/Patient.read patient/ExplanationOfBenefit.read patient/Coverage.read`
- All three endpoints should work
- If you manually edit the token to remove a scope, that endpoint should return 403

---

### Exercise 19: Member Health Summary Endpoint

Build the `$everything` equivalent — get all of a member's data in one call.

**Add to** `PatientController.cs`:
```csharp
/// <summary>
/// GET /api/fhir/Patient/{id}/$everything — Complete member health summary
/// </summary>
[HttpGet("{id}/$everything")]
public async Task<IActionResult> GetEverything(string id)
{
    if (!PatientAccessHandler.CanAccessPatient(User, id))
        return Forbid();

    var bundle = await _fhirService.GetPatientEverythingAsync(id);

    var json = _serializer.SerializeToString(bundle);
    return Content(json, "application/fhir+json");
}
```

**Test with Postman**:
1. Login as 51707
2. `GET http://localhost:5220/api/fhir/Patient/51707/$everything`
3. Should return a massive Bundle with Patient, all EOBs, Conditions, Encounters, MedicationRequests, Procedures

---

## Part C: Real-World Scenarios (Exercises 20–25)

*Test end-to-end flows using Postman*

---

### Exercise 20: Member Portal Simulation

Simulate a complete member portal session:

**Flow**:
1. **Login**: `POST /api/auth/token` → `{"memberId": "51707", "name": "Ramon Schulist"}`
2. **View Profile**: `GET /api/fhir/Patient/me` → See name, DOB, address
3. **Check Coverage**: `GET /api/fhir/Coverage?patient=51707` → See plan details
4. **View Recent Claims**: `GET /api/fhir/ExplanationOfBenefit?patient=51707&_count=5` → Latest claims
5. **Filter Pharmacy**: `GET /api/fhir/ExplanationOfBenefit?patient=51707&type=pharmacy` → Rx claims only
6. **View Claim Detail**: Pick an EOB ID from step 4, `GET` it from HAPI directly to see full detail

Save this as a **Postman Collection** named "Member Access API - Phase 2".

---

### Exercise 21: Date Range Queries

**Scenarios**:
1. "Show me claims from the last year":
   `GET /api/fhir/ExplanationOfBenefit?patient=51707&startDate=2025-01-01`

2. "Show me claims for 2024 only":
   `GET /api/fhir/ExplanationOfBenefit?patient=51707&startDate=2024-01-01&endDate=2024-12-31`

3. "Show me all historical claims":
   `GET /api/fhir/ExplanationOfBenefit?patient=51707&_count=100`

---

### Exercise 22: Cross-Resource Queries  

Test queries that pull related data.

**Claims with provider details** (direct HAPI call):
- http://localhost:8082/fhir/ExplanationOfBenefit?patient=Patient/51707&_count=3&_include=ExplanationOfBenefit:provider

**Claims with care-team info** (direct HAPI call):
- http://localhost:8082/fhir/ExplanationOfBenefit?patient=Patient/51707&_count=3&_include=ExplanationOfBenefit:care-team

**Key learning**: `_include` pulls related resources in the same Bundle. The Member Access API should support this for a richer member experience.

---

### Exercise 23: Unauthorized Access Tests

Verify the security layer works — these should ALL fail:

| Test | Request | Expected |
|------|---------|----------|
| No token | `GET /api/fhir/Patient/51707` (no Auth header) | 401 |
| Expired token | Use a token from hours ago | 401 |
| Wrong patient | Login as 51707, request `/Patient/52458` | 403 |
| Wrong patient's claims | Login as 51707, request `/ExplanationOfBenefit?patient=52458` | 403 |
| Invalid token | `Authorization: Bearer garbage` | 401 |

---

### Exercise 24: Second Member Test

Repeat the full flow with a DIFFERENT member to ensure multi-member support:

1. Login as patient **52458** (Rasheeda Heaney): `POST /api/auth/token` → `{"memberId": "52458", "name": "Rasheeda Heaney"}`
2. `GET /api/fhir/Patient/me` → Should see Rasheeda's data
3. `GET /api/fhir/ExplanationOfBenefit?patient=52458` → Rasheeda's claims
4. `GET /api/fhir/Coverage?patient=52458` → Rasheeda's coverage
5. `GET /api/fhir/Patient/51707` with Rasheeda's token → **403 Forbidden**

---

### Exercise 25: CARIN BB Compliance Review

Review your API against CARIN Blue Button requirements:

| Requirement | Status | Notes |
|-------------|--------|-------|
| Patient resource endpoint | ✅ / ❌ | GET by ID |
| Coverage resource endpoint | ✅ / ❌ | Search by patient |
| EOB resource endpoint | ✅ / ❌ | Search by patient |
| EOB search by type | ✅ / ❌ | professional, institutional, pharmacy |
| EOB search by date | ✅ / ❌ | created date range |
| OAuth 2.0 / SMART on FHIR | ✅ / ❌ | JWT with patient scopes |
| Patient-scoped access | ✅ / ❌ | Member sees only own data |
| Proper content type | ✅ / ❌ | application/fhir+json |
| OperationOutcome errors | ✅ / ❌ | For 401, 403, 404 |
| Pagination | ✅ / ❌ | _count and next links |

**What's missing for full CARIN BB compliance** (discussed, not implemented):
- Profile declarations in `meta.profile`
- Full adjudication categories (submitted, eligible, benefit, copay, deductible)
- Member identifier with `MB` code
- Organization resource for the payer
- Proper OAuth 2.0 authorization server (we simulated with JWT)

---

## 🎓 Key Takeaways

| Concept | What You Learned |
|---------|-----------------|
| CARIN Blue Button | Profiles for Patient, Coverage, EOB (4 types) |
| Facade Pattern | Your API wraps HAPI FHIR, adds auth + access control |
| SMART on FHIR | Token-based auth with patient/{Resource}.read scopes |
| Patient Access Control | JWT contains patientId, API enforces scoping |
| Firely SDK | .NET FHIR client for reading/searching resources |
| Adjudication | Financial breakdown: submitted → allowed → paid → patient owes |
| Coverage | Member enrollment, plan details, period |
| OperationOutcome | FHIR-standard error responses |

---

## ✅ Completion Checklist

**Part A — Understanding CARIN BB:**
- [x] Explored Patient as member
- [x] Understood Coverage resource
- [x] Examined Professional EOB
- [x] Examined Institutional EOB
- [x] Examined Pharmacy EOB
- [x] Understood adjudication amounts
- [x] Created standalone Coverage resources
- [x] Mapped resource relationships

**Part B — Built .NET API:**
- [x] Created .NET 8 project with Firely SDK
- [x] Built FHIR client service
- [x] Built Patient endpoint
- [x] Built Coverage endpoint
- [x] Built EOB endpoint with search
- [x] Added JWT authentication
- [x] Added patient access control
- [x] Added OperationOutcome error responses
- [x] Added pagination support
- [x] Added SMART on FHIR scopes
- [x] Built $everything endpoint

**Part C — Tested Scenarios:**
- [x] Completed member portal simulation
- [x] Tested date range queries
- [ ] Tested cross-resource queries
- [x] Verified unauthorized access is blocked
- [ ] Tested with second member
- [ ] Reviewed CARIN BB compliance

**Phase 2 Complete — Ready for Phase 3: Provider Directory!** 🎉
