namespace PersonalFinanceTracker.Api.Entities;

public class Category
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser? User { get; set; }
    public List<Budget> Budgets { get; set; } = new();
    public List<RecurringTransaction> RecurringTransactions { get; set; } = new();
    public List<TransactionRecord> Transactions { get; set; } = new();
}
