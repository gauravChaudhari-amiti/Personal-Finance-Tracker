namespace PersonalFinanceTracker.Api.DTOs;

public class ResetPasswordRequestDto
{
    public string Token { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
