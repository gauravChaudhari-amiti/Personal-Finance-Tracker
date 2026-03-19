namespace PersonalFinanceTracker.Api.Entities;

public class AppUser
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int UserNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AuthProvider { get; set; } = "password";
    public bool IsEmailVerified { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    public string Role { get; set; } = "User";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Account> Accounts { get; set; } = new();
    public List<UserActionToken> AuthTokens { get; set; } = new();
    public List<Budget> Budgets { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Goal> Goals { get; set; } = new();
    public List<RecurringTransaction> RecurringTransactions { get; set; } = new();
    public List<TransactionRecord> Transactions { get; set; } = new();
}
