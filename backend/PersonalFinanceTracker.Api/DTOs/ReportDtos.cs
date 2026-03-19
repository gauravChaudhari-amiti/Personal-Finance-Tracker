namespace PersonalFinanceTracker.Api.DTOs;

public class ReportSummaryDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetCashFlow { get; set; }
    public int TransactionCount { get; set; }
}

public class CategorySpendReportItemDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class IncomeExpenseTrendPointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Net { get; set; }
}

public class AccountBalanceTrendPointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}

public class AccountPositionDto
{
    public string AccountId { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
}

public class ReportResponseDto
{
    public ReportSummaryDto Summary { get; set; } = new();
    public List<CategorySpendReportItemDto> CategorySpend { get; set; } = new();
    public List<IncomeExpenseTrendPointDto> IncomeExpenseTrend { get; set; } = new();
    public List<AccountBalanceTrendPointDto> AccountBalanceTrend { get; set; } = new();
    public List<AccountPositionDto> AccountPositions { get; set; } = new();
}
