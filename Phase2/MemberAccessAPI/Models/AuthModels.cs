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
