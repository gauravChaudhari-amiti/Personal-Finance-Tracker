using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinanceTracker.Api.DTOs;
using PersonalFinanceTracker.Api.Services;

namespace PersonalFinanceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string AuthCookieName = "pft_auth";
    private readonly IUserService _userService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;

    public AuthController(IUserService userService, IJwtTokenService jwtTokenService, IConfiguration configuration)
    {
        _userService = userService;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
    }

    [HttpGet("google-config")]
    public IActionResult GetGoogleConfig()
    {
        var clientId = _configuration["GoogleAuth:ClientId"]?.Trim();

        return Ok(new GoogleAuthConfigDto
        {
            ClientId = string.IsNullOrWhiteSpace(clientId) ? null : clientId,
            Enabled = !string.IsNullOrWhiteSpace(clientId)
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        try
        {
            var user = await _userService.ValidateUserAsync(request.Email, request.Password);

            if (user is null)
            {
                return Unauthorized(new { message = "Invalid credentials." });
            }

            SignInUser(user);
            return Ok(BuildLoginResponse(user));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        try
        {
            var result = await _userService.CreateUserAsync(request.DisplayName, request.Email, request.Password);
            return Ok(new AuthActionResponseDto
            {
                Message = result.Message,
                PreviewUrl = result.PreviewUrl
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequestDto request)
    {
        try
        {
            var result = await _userService.ResendVerificationEmailAsync(request.Email);
            return Ok(new AuthActionResponseDto
            {
                Message = result.Message,
                PreviewUrl = result.PreviewUrl
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] EmailTokenRequestDto request)
    {
        try
        {
            await _userService.VerifyEmailAsync(request.Token);
            return Ok(new AuthActionResponseDto
            {
                Message = "Your email has been verified. You can log in now."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        try
        {
            var result = await _userService.RequestPasswordResetAsync(request.Email);
            return Ok(new AuthActionResponseDto
            {
                Message = result.Message,
                PreviewUrl = result.PreviewUrl
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        try
        {
            await _userService.ResetPasswordAsync(request.Token, request.Password);
            return Ok(new AuthActionResponseDto
            {
                Message = "Your password has been reset. You can log in now."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequestDto request)
    {
        try
        {
            var user = await _userService.SignInWithGoogleAsync(request.Credential);
            SignInUser(user);
            return Ok(BuildLoginResponse(user));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirst("userId")?.Value;
        var user = await _userService.GetUserByIdAsync(userId ?? string.Empty);

        if (user is null)
        {
            return Unauthorized(new { message = "No active session." });
        }

        return Ok(BuildLoginResponse(user));
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AuthCookieName, BuildCookieOptions(DateTimeOffset.UtcNow.AddDays(-1)));
        return Ok(new AuthActionResponseDto
        {
            Message = "Logged out successfully."
        });
    }

    private LoginResponseDto BuildLoginResponse(Entities.AppUser user)
    {
        return new LoginResponseDto
        {
            UserNumber = user.UserNumber,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role
        };
    }

    private void SignInUser(Entities.AppUser user)
    {
        var token = _jwtTokenService.GenerateToken(user);
        var expiration = DateTimeOffset.UtcNow.AddHours(8);
        Response.Cookies.Append(AuthCookieName, token, BuildCookieOptions(expiration));
    }

    private CookieOptions BuildCookieOptions(DateTimeOffset expiresAt)
    {
        var isHttps = Request.IsHttps;

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Expires = expiresAt,
            Path = "/"
        };
    }
}
