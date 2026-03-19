using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Api.Data;
using PersonalFinanceTracker.Api.DTOs;

namespace PersonalFinanceTracker.Api.Services;

public interface IDashboardService
{
    DashboardSummaryDto GetSummary(string userId, string role);
}

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _dbContext;

    public DashboardService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public DashboardSummaryDto GetSummary(string userId, string role)
    {
        var now = DateTime.UtcNow;

        var currentMonthTransactions = _dbContext.Transactions
            .Where(x => x.UserId == userId && x.Date.Month == now.Month && x.Date.Year == now.Year)
            .ToList();

        var income = currentMonthTransactions
            .Where(x => x.Type == "income")
            .Sum(x => x.Amount);

        var expense = currentMonthTransactions
            .Where(x => x.Type == "expense")
            .Sum(x => x.Amount);

        var netBalance = _dbContext.Accounts
            .Where(x => x.UserId == userId)
            .Sum(x => (decimal?)x.CurrentBalance) ?? 0;

        var recentTransactions = _dbContext.Transactions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAt)
            .Take(5)
            .Select(x => new RecentTransactionDto
            {
                Id = x.Id,
                Title = x.Merchant ?? x.Category ?? x.Type,
                Category = x.Category ?? "General",
                Type = x.Type,
                Amount = x.Amount,
                Date = x.Date.ToString("yyyy-MM-dd")
            })
            .ToList();

        var upcomingBills = _dbContext.RecurringTransactions
            .Where(x =>
                x.UserId == userId &&
                x.Type == "expense" &&
                !x.IsPaused &&
                x.NextRunDate >= now.Date &&
                (x.EndDate == null || x.NextRunDate <= x.EndDate))
            .OrderBy(x => x.NextRunDate)
            .Take(5)
            .Select(x => new UpcomingBillDto
            {
                Id = x.Id,
                Title = x.Title,
                Amount = x.Amount,
                DueDate = x.NextRunDate.ToString("yyyy-MM-dd")
            })
            .ToList();

        return new DashboardSummaryDto
        {
            CurrentMonthIncome = income,
            CurrentMonthExpense = expense,
            NetBalance = netBalance,
            RecentTransactions = recentTransactions,
            UpcomingBills = upcomingBills
        };
    }
}
