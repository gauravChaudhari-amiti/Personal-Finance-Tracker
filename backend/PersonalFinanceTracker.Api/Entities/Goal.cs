namespace PersonalFinanceTracker.Api.Entities;

public class Goal
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateTime? TargetDate { get; set; }
    public string? CategoryId { get; set; }
    public string? LinkedAccountId { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public AppUser? User { get; set; }
    public Category? Category { get; set; }
    public Account? LinkedAccount { get; set; }
    public List<TransactionRecord> Transactions { get; set; } = new();
}
