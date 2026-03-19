namespace PersonalFinanceTracker.Api.Entities;

public class Account
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? CategoryId { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal? CreditLimit { get; set; }
    public string? InstitutionName { get; set; }
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    public AppUser? User { get; set; }
    public Category? Category { get; set; }
    public List<Goal> LinkedGoals { get; set; } = new();
    public List<RecurringTransaction> RecurringTransactions { get; set; } = new();
    public List<TransactionRecord> Transactions { get; set; } = new();
}
