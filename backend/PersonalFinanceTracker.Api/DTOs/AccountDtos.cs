namespace PersonalFinanceTracker.Api.DTOs;

public class AccountDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal? CreditLimit { get; set; }
    public decimal? AvailableCredit { get; set; }
    public string? InstitutionName { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}

public class CreateAccountRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? CategoryId { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal? CreditLimit { get; set; }
    public string? InstitutionName { get; set; }
}

public class UpdateAccountRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? CategoryId { get; set; }
    public decimal? CreditLimit { get; set; }
    public string? InstitutionName { get; set; }
}

public class TransferRequestDto
{
    public string SourceAccountId { get; set; } = string.Empty;
    public string DestinationAccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime? Date { get; set; }
    public string? Note { get; set; }
}
