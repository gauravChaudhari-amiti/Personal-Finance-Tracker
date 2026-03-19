namespace PersonalFinanceTracker.Api.DTOs;

public class GoalDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal ProgressPercent { get; set; }
    public DateTime? TargetDate { get; set; }
    public string? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? LinkedAccountId { get; set; }
    public string? LinkedAccountName { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateGoalRequestDto
{
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public DateTime? TargetDate { get; set; }
    public string? CategoryId { get; set; }
    public string? LinkedAccountId { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
}

public class UpdateGoalRequestDto
{
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public DateTime? TargetDate { get; set; }
    public string? CategoryId { get; set; }
    public string? LinkedAccountId { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
}

public class GoalContributionRequestDto
{
    public decimal Amount { get; set; }
    public string? AccountId { get; set; }
    public string? Note { get; set; }
}

public class GoalWithdrawalRequestDto
{
    public decimal Amount { get; set; }
    public string? AccountId { get; set; }
    public string? Note { get; set; }
}
