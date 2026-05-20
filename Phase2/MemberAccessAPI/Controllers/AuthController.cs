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
