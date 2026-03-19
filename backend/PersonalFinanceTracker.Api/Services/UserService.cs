using System.Security.Cryptography;
using System.Text;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Api.Data;
using PersonalFinanceTracker.Api.Entities;

namespace PersonalFinanceTracker.Api.Services;

public record AuthActionResult(string Message, string? PreviewUrl = null);

public interface IUserService
{
    Task<AppUser?> ValidateUserAsync(string email, string password);
    Task<AppUser?> GetUserByIdAsync(string userId);
    Task<AuthActionResult> CreateUserAsync(string displayName, string email, string password);
    Task<AuthActionResult> ResendVerificationEmailAsync(string email);
    Task VerifyEmailAsync(string token);
    Task<AuthActionResult> RequestPasswordResetAsync(string email);
    Task ResetPasswordAsync(string token, string password);
    Task<AppUser> SignInWithGoogleAsync(string credential);
}

public class UserService : IUserService
{
    private const string EmailVerificationTokenType = "email-verification";
    private const string PasswordResetTokenType = "password-reset";

    private readonly AppDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<UserService> _logger;

    public UserService(
        AppDbContext dbContext,
        IEmailService emailService,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger<UserService> logger)
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public async Task<AppUser?> ValidateUserAsync(string email, string password)
    {
        var normalizedEmail = NormalizeEmail(email);
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail);

        if (user is null)
        {
            return null;
        }

        if (!user.IsEmailVerified)
        {
            throw new InvalidOperationException("Verify your email before logging in.");
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            throw new InvalidOperationException("Use Google sign-in for this account or reset your password first.");
        }

        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
    }

    public async Task<AppUser?> GetUserByIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId);
    }

    public async Task<AuthActionResult> CreateUserAsync(string displayName, string email, string password)
    {
        var normalizedDisplayName = displayName.Trim();
        var normalizedEmail = NormalizeEmail(email);

        ValidateDisplayName(normalizedDisplayName);
        ValidateEmail(normalizedEmail);
        ValidatePassword(password);

        if (await _dbContext.Users.AnyAsync(x => x.Email.ToLower() == normalizedEmail))
        {
            throw new InvalidOperationException("An account with this email already exists.");
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            DisplayName = normalizedDisplayName,
            AuthProvider = "password",
            IsEmailVerified = false,
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        _dbContext.Categories.AddRange(CategoryDefaults.BuildForUser(user.Id));
        await _dbContext.SaveChangesAsync();

        return await SendEmailVerificationAsync(user, "Account created. Check your email to verify your account.");
    }

    public async Task<AuthActionResult> ResendVerificationEmailAsync(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        ValidateEmail(normalizedEmail);

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail);
        if (user is null)
        {
            return new AuthActionResult("If an account exists for that email, a verification email has been sent.");
        }

        if (user.IsEmailVerified)
        {
            return new AuthActionResult("Your email is already verified.");
        }

        return await SendEmailVerificationAsync(user, "A fresh verification link has been sent to your email.");
    }

    public async Task VerifyEmailAsync(string token)
    {
        var actionToken = await FindValidTokenAsync(token, EmailVerificationTokenType);
        if (actionToken is null)
        {
            throw new InvalidOperationException("This verification link is invalid or has expired.");
        }

        actionToken.User.IsEmailVerified = true;
        actionToken.User.EmailVerifiedAt = DateTime.UtcNow;
        actionToken.ConsumedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
    }

    public async Task<AuthActionResult> RequestPasswordResetAsync(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        ValidateEmail(normalizedEmail);

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail);
        if (user is null)
        {
            return new AuthActionResult("If an account exists for that email, a reset link has been sent.");
        }

        var resetLink = await CreateActionLinkAsync(user, PasswordResetTokenType, TimeSpan.FromHours(1), "/reset-password");
        await _emailService.SendAsync(
            user.Email,
            "Reset your Personal Finance Tracker password",
            $"""
             <p>Hello {System.Net.WebUtility.HtmlEncode(user.DisplayName)},</p>
             <p>Use the link below to reset your password:</p>
             <p><a href="{resetLink}">{resetLink}</a></p>
             <p>This link expires in 1 hour.</p>
             """,
            $"""
             Hello {user.DisplayName},

             Use the link below to reset your password:
             {resetLink}

             This link expires in 1 hour.
             """
        );

        return new AuthActionResult(
            "If an account exists for that email, a reset link has been sent.",
            GetPreviewUrl(resetLink)
        );
    }

    public async Task ResetPasswordAsync(string token, string password)
    {
        ValidatePassword(password);

        var actionToken = await FindValidTokenAsync(token, PasswordResetTokenType);
        if (actionToken is null)
        {
            throw new InvalidOperationException("This reset link is invalid or has expired.");
        }

        actionToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        actionToken.ConsumedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
    }

    public async Task<AppUser> SignInWithGoogleAsync(string credential)
    {
        if (string.IsNullOrWhiteSpace(credential))
        {
            throw new ArgumentException("Google credential is required.");
        }

        var googleClientId = _configuration["GoogleAuth:ClientId"]?.Trim();
        if (string.IsNullOrWhiteSpace(googleClientId))
        {
            throw new InvalidOperationException("Google sign-in is not configured.");
        }

        var payload = await GoogleJsonWebSignature.ValidateAsync(
            credential,
            new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [googleClientId]
            });

        if (!payload.EmailVerified)
        {
            throw new InvalidOperationException("Google account email is not verified.");
        }

        var normalizedEmail = NormalizeEmail(payload.Email);
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail);

        if (user is null)
        {
            user = new AppUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = normalizedEmail,
                PasswordHash = string.Empty,
                DisplayName = string.IsNullOrWhiteSpace(payload.Name) ? payload.Email.Split('@')[0] : payload.Name,
                AuthProvider = "google",
                IsEmailVerified = true,
                EmailVerifiedAt = DateTime.UtcNow,
                Role = "User",
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            _dbContext.Categories.AddRange(CategoryDefaults.BuildForUser(user.Id));
            await _dbContext.SaveChangesAsync();
            return user;
        }

        var changed = false;

        if (!user.IsEmailVerified)
        {
            user.IsEmailVerified = true;
            user.EmailVerifiedAt = DateTime.UtcNow;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(user.DisplayName) && !string.IsNullOrWhiteSpace(payload.Name))
        {
            user.DisplayName = payload.Name;
            changed = true;
        }

        if (changed)
        {
            await _dbContext.SaveChangesAsync();
        }

        return user;
    }

    private async Task<AuthActionResult> SendEmailVerificationAsync(AppUser user, string message)
    {
        var verificationLink = await CreateActionLinkAsync(
            user,
            EmailVerificationTokenType,
            TimeSpan.FromHours(24),
            "/verify-email");

        await _emailService.SendAsync(
            user.Email,
            "Verify your Personal Finance Tracker email",
            $"""
             <p>Hello {System.Net.WebUtility.HtmlEncode(user.DisplayName)},</p>
             <p>Use the link below to verify your email and activate your account:</p>
             <p><a href="{verificationLink}">{verificationLink}</a></p>
             <p>This link expires in 24 hours.</p>
             """,
            $"""
             Hello {user.DisplayName},

             Use the link below to verify your email and activate your account:
             {verificationLink}

             This link expires in 24 hours.
             """
        );

        var responseMessage = message;
        if (!_emailService.IsConfigured)
        {
            responseMessage = _hostEnvironment.IsProduction()
                ? "Email delivery is not configured on this environment yet, so no verification email could be sent."
                : "Email delivery is not configured locally. Use the preview link below to verify your account.";
        }

        return new AuthActionResult(responseMessage, GetPreviewUrl(verificationLink));
    }

    private async Task<string> CreateActionLinkAsync(AppUser user, string tokenType, TimeSpan lifetime, string frontendPath)
    {
        await InvalidateOutstandingTokensAsync(user.Id, tokenType);

        var rawToken = GenerateSecureToken();
        var tokenHash = HashToken(rawToken);

        _dbContext.UserActionTokens.Add(new UserActionToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = user.Id,
            Type = tokenType,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(lifetime)
        });

        await _dbContext.SaveChangesAsync();

        return $"{GetFrontendBaseUrl()}{frontendPath}?token={Uri.EscapeDataString(rawToken)}";
    }

    private async Task InvalidateOutstandingTokensAsync(string userId, string tokenType)
    {
        var activeTokens = await _dbContext.UserActionTokens
            .Where(x => x.UserId == userId && x.Type == tokenType && x.ConsumedAt == null)
            .ToListAsync();

        if (activeTokens.Count == 0)
        {
            return;
        }

        foreach (var token in activeTokens)
        {
            token.ConsumedAt = DateTime.UtcNow;
        }
    }

    private async Task<UserActionToken?> FindValidTokenAsync(string rawToken, string tokenType)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var tokenHash = HashToken(rawToken.Trim());
        var now = DateTime.UtcNow;

        return await _dbContext.UserActionTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x =>
                x.Type == tokenType &&
                x.TokenHash == tokenHash &&
                x.ConsumedAt == null &&
                x.ExpiresAt >= now);
    }

    private string? GetPreviewUrl(string url)
    {
        if (_emailService.IsConfigured || _hostEnvironment.IsProduction())
        {
            return null;
        }

        _logger.LogInformation("Email delivery preview URL: {Url}", url);
        return url;
    }

    private string GetFrontendBaseUrl()
    {
        var configuredUrl = _configuration["Frontend:BaseUrl"]?.Trim();
        if (!string.IsNullOrWhiteSpace(configuredUrl))
        {
            return configuredUrl.TrimEnd('/');
        }

        var corsOrigin = _configuration["Cors:AllowedOrigins:0"]?.Trim();
        if (!string.IsNullOrWhiteSpace(corsOrigin))
        {
            return corsOrigin.TrimEnd('/');
        }

        return "http://localhost:5173";
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static void ValidateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.");
        }
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.");
        }

        if (!email.Contains('@'))
        {
            throw new ArgumentException("Enter a valid email address.");
        }
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required.");
        }

        if (password.Length < 8 ||
            !password.Any(char.IsUpper) ||
            !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit))
        {
            throw new ArgumentException("Password must be at least 8 characters and include upper, lower, and numeric characters.");
        }
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string HashToken(string token)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes);
    }
}
