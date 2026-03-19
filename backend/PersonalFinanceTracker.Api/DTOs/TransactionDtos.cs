namespace PersonalFinanceTracker.Api.DTOs;

public class TransactionDto
{
    public string Id { get; set; } = string.Empty;
    public long TransactionNumber { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string? GoalId { get; set; }
    public string? GoalName { get; set; }
    public string? CategoryId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Category { get; set; }
    public string? Merchant { get; set; }
    public string? Note { get; set; }
    public string? PaymentMethod { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TransactionListResponseDto
{
    public List<TransactionDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
}

public class CreateTransactionRequestDto
{
    public string? AccountId { get; set; }
    public string? GoalId { get; set; }
    public string? CategoryId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Merchant { get; set; }
    public string? Note { get; set; }
    public string? PaymentMethod { get; set; }
    public List<string>? Tags { get; set; }
}

public class UpdateTransactionRequestDto
{
    public string? AccountId { get; set; }
    public string? GoalId { get; set; }
    public string? CategoryId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Merchant { get; set; }
    public string? Note { get; set; }
    public string? PaymentMethod { get; set; }
    public List<string>? Tags { get; set; }
}
