namespace PersonalFinanceTracker.Api.Entities;

public class TransactionRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public long TransactionNumber { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? AccountId { get; set; }
    public string? GoalId { get; set; }
    public string? CategoryId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Category { get; set; }
    public string? Merchant { get; set; }
    public string? Note { get; set; }
    public string? PaymentMethod { get; set; }
    public string[] Tags { get; set; } = [];
    public string? TransferGroupId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public AppUser? User { get; set; }
    public Account? Account { get; set; }
    public Goal? Goal { get; set; }
    public Category? CategoryItem { get; set; }
}
