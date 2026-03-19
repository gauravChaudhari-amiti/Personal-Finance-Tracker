using PersonalFinanceTracker.Api.Data;
using PersonalFinanceTracker.Api.Entities;

namespace PersonalFinanceTracker.Api.Services;

public interface IUserService
{
    AppUser? ValidateUser(string email, string password);
    AppUser CreateUser(string displayName, string email, string password);
}

public class UserService : IUserService
{
    private readonly AppDbContext _dbContext;

    public UserService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public AppUser? ValidateUser(string email, string password)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = _dbContext.Users.FirstOrDefault(x => x.Email.ToLower() == normalizedEmail);

        if (user is null)
        {
            return null;
        }

        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
    }

    public AppUser CreateUser(string displayName, string email, string password)
    {
        var normalizedDisplayName = displayName.Trim();
        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalizedDisplayName))
        {
            throw new ArgumentException("Display name is required.");
        }

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            throw new ArgumentException("Email is required.");
        }

        if (!normalizedEmail.Contains('@'))
        {
            throw new ArgumentException("Enter a valid email address.");
        }

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

        if (_dbContext.Users.Any(x => x.Email.ToLower() == normalizedEmail))
        {
            throw new InvalidOperationException("An account with this email already exists.");
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            DisplayName = normalizedDisplayName,
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        _dbContext.Categories.AddRange(CategoryDefaults.BuildForUser(user.Id));
        _dbContext.SaveChanges();

        return user;
    }
}
