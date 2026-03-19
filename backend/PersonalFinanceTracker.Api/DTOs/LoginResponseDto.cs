namespace PersonalFinanceTracker.Api.DTOs;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public int UserNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
