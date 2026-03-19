namespace PersonalFinanceTracker.Api.DTOs;

public class BudgetDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryColor { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal Amount { get; set; }
    public int AlertThresholdPercent { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal ProgressPercent { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateBudgetRequestDto
{
    public string CategoryId { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal Amount { get; set; }
    public int AlertThresholdPercent { get; set; } = 80;
}

public class UpdateBudgetRequestDto
{
    public string CategoryId { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal Amount { get; set; }
    public int AlertThresholdPercent { get; set; } = 80;
}

public class DuplicateBudgetRequestDto
{
    public int SourceMonth { get; set; }
    public int SourceYear { get; set; }
    public int TargetMonth { get; set; }
    public int TargetYear { get; set; }
}
