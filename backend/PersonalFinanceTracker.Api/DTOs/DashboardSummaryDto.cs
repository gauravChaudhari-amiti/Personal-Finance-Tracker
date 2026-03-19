namespace PersonalFinanceTracker.Api.DTOs;

public class DashboardSummaryDto
{
    public decimal CurrentMonthIncome { get; set; }
    public decimal CurrentMonthExpense { get; set; }
    public decimal NetBalance { get; set; }

    public List<RecentTransactionDto> RecentTransactions { get; set; } = new();
    public List<UpcomingBillDto> UpcomingBills { get; set; } = new();
}

public class RecentTransactionDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Date { get; set; } = string.Empty;
}

public class UpcomingBillDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string DueDate { get; set; } = string.Empty;
}
