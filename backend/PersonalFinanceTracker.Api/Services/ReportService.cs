using System.Text;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Api.Data;
using PersonalFinanceTracker.Api.DTOs;
using PersonalFinanceTracker.Api.Entities;

namespace PersonalFinanceTracker.Api.Services;

public interface IReportService
{
    ReportResponseDto GetReport(string userId, DateTime? from, DateTime? to, string? accountId, string? categoryId, string? type);
    string ExportCsv(string userId, DateTime? from, DateTime? to, string? accountId, string? categoryId, string? type);
}

public class ReportService : IReportService
{
    private readonly AppDbContext _dbContext;

    public ReportService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public ReportResponseDto GetReport(string userId, DateTime? from, DateTime? to, string? accountId, string? categoryId, string? type)
    {
        var (startDate, endDate) = NormalizeRange(from, to);
        var normalizedType = NormalizeOptional(type);

        var filteredTransactions = BuildFilteredTransactionQuery(userId, startDate, endDate, accountId, categoryId, normalizedType)
            .Include(x => x.Account)
            .Include(x => x.Goal)
            .Include(x => x.CategoryItem)
            .ToList();

        var summary = new ReportSummaryDto
        {
            TotalIncome = filteredTransactions.Where(x => x.Type == "income").Sum(x => x.Amount),
            TotalExpense = filteredTransactions.Where(x => x.Type == "expense").Sum(x => x.Amount),
            TransactionCount = filteredTransactions.Count
        };
        summary.NetCashFlow = summary.TotalIncome - summary.TotalExpense;

        var categorySpend = filteredTransactions
            .Where(x => x.Type == "expense")
            .GroupBy(x => x.CategoryItem?.Name ?? x.Category ?? "Uncategorized")
            .Select(group => new CategorySpendReportItemDto
            {
                CategoryName = group.Key,
                Amount = group.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.Amount)
            .Take(8)
            .ToList();

        var incomeExpenseTrend = BuildIncomeExpenseTrend(filteredTransactions, startDate, endDate);
        var accountBalanceTrend = BuildAccountBalanceTrend(userId, startDate, endDate, accountId, categoryId, normalizedType);
        var accountPositions = _dbContext.Accounts
            .Where(x => x.UserId == userId && (string.IsNullOrWhiteSpace(accountId) || x.Id == accountId))
            .OrderBy(x => x.Name)
            .Select(x => new AccountPositionDto
            {
                AccountId = x.Id,
                AccountName = x.Name,
                AccountType = x.Type,
                CurrentBalance = x.CurrentBalance
            })
            .ToList();

        return new ReportResponseDto
        {
            Summary = summary,
            CategorySpend = categorySpend,
            IncomeExpenseTrend = incomeExpenseTrend,
            AccountBalanceTrend = accountBalanceTrend,
            AccountPositions = accountPositions
        };
    }

    public string ExportCsv(string userId, DateTime? from, DateTime? to, string? accountId, string? categoryId, string? type)
    {
        var (startDate, endDate) = NormalizeRange(from, to);
        var normalizedType = NormalizeOptional(type);

        var transactions = BuildFilteredTransactionQuery(userId, startDate, endDate, accountId, categoryId, normalizedType)
            .Include(x => x.Account)
            .Include(x => x.Goal)
            .Include(x => x.CategoryItem)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAt)
            .ToList();

        var builder = new StringBuilder();
        builder.AppendLine("Date,Account,Type,Category,Merchant,Amount,PaymentMethod,Note,Tags");

        foreach (var transaction in transactions)
        {
            builder.AppendLine(string.Join(",",
                Escape(transaction.Date.ToString("yyyy-MM-dd")),
                Escape(transaction.Account?.Name ?? transaction.Goal?.Name ?? string.Empty),
                Escape(transaction.Type),
                Escape(transaction.CategoryItem?.Name ?? transaction.Category ?? string.Empty),
                Escape(transaction.Merchant ?? string.Empty),
                Escape(transaction.Amount.ToString("0.00")),
                Escape(transaction.PaymentMethod ?? string.Empty),
                Escape(transaction.Note ?? string.Empty),
                Escape(string.Join(" | ", transaction.Tags))));
        }

        return builder.ToString();
    }

    private IQueryable<TransactionRecord> BuildFilteredTransactionQuery(
        string userId,
        DateTime startDate,
        DateTime endDate,
        string? accountId,
        string? categoryId,
        string? type)
    {
        var query = _dbContext.Transactions.Where(x =>
            x.UserId == userId &&
            x.Date >= startDate &&
            x.Date <= endDate);

        if (!string.IsNullOrWhiteSpace(accountId))
        {
            query = query.Where(x => x.AccountId == accountId);
        }

        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            query = query.Where(x => x.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(x => x.Type == type);
        }

        return query;
    }

    private List<IncomeExpenseTrendPointDto> BuildIncomeExpenseTrend(
        List<TransactionRecord> transactions,
        DateTime startDate,
        DateTime endDate)
    {
        var monthStarts = GetMonthStarts(startDate, endDate);
        return monthStarts.Select(monthStart =>
        {
            var monthTransactions = transactions.Where(x => x.Date.Year == monthStart.Year && x.Date.Month == monthStart.Month).ToList();
            var income = monthTransactions.Where(x => x.Type == "income").Sum(x => x.Amount);
            var expense = monthTransactions.Where(x => x.Type == "expense").Sum(x => x.Amount);

            return new IncomeExpenseTrendPointDto
            {
                Label = monthStart.ToString("MMM yyyy"),
                Income = income,
                Expense = expense,
                Net = income - expense
            };
        }).ToList();
    }

    private List<AccountBalanceTrendPointDto> BuildAccountBalanceTrend(
        string userId,
        DateTime startDate,
        DateTime endDate,
        string? accountId,
        string? categoryId,
        string? type)
    {
        var accounts = _dbContext.Accounts
            .Where(x => x.UserId == userId && (string.IsNullOrWhiteSpace(accountId) || x.Id == accountId))
            .ToList();

        var openingTotal = accounts.Sum(account =>
        {
            if (account.Type.Equals("Credit Card", StringComparison.OrdinalIgnoreCase))
                return 0;

            return account.OpeningBalance;
        });

        var transactions = BuildFilteredTransactionQuery(userId, DateTime.MinValue, endDate, accountId, categoryId, type)
            .Include(x => x.Account)
            .ToList();

        var monthStarts = GetMonthStarts(startDate, endDate);
        return monthStarts.Select(monthStart =>
        {
            var monthEnd = new DateTime(monthStart.Year, monthStart.Month, DateTime.DaysInMonth(monthStart.Year, monthStart.Month));
            var cumulativeImpact = transactions
                .Where(x => x.Date <= monthEnd)
                .Sum(GetBalanceImpact);

            return new AccountBalanceTrendPointDto
            {
                Label = monthStart.ToString("MMM yyyy"),
                Balance = openingTotal + cumulativeImpact
            };
        }).ToList();
    }

    private static decimal GetBalanceImpact(TransactionRecord transaction)
    {
        if (transaction.Account is null && transaction.GoalId is not null)
            return 0;

        var accountType = transaction.Account?.Type ?? string.Empty;
        var isCreditCard = accountType.Equals("Credit Card", StringComparison.OrdinalIgnoreCase);

        if (isCreditCard)
        {
            return transaction.Type switch
            {
                "expense" => -transaction.Amount,
                "income" => transaction.Amount,
                "transfer-in" => transaction.Amount,
                "card-settlement-in" => transaction.Amount,
                _ => 0
            };
        }

        return transaction.Type switch
        {
            "income" => transaction.Amount,
            "expense" => -transaction.Amount,
            "transfer-in" => transaction.Amount,
            "self-transfer-in" => transaction.Amount,
            "transfer-out" => -transaction.Amount,
            "self-transfer-out" => -transaction.Amount,
            "card-settlement-out" => -transaction.Amount,
            _ => 0
        };
    }

    private static (DateTime StartDate, DateTime EndDate) NormalizeRange(DateTime? from, DateTime? to)
    {
        var today = DateTime.UtcNow.Date;
        var startDate = (from?.Date ?? new DateTime(today.Year, today.Month, 1)).Date;
        var endDate = (to?.Date ?? today).Date;

        if (endDate < startDate)
            throw new ArgumentException("End date cannot be before start date.");

        return (startDate, endDate);
    }

    private static List<DateTime> GetMonthStarts(DateTime startDate, DateTime endDate)
    {
        var current = new DateTime(startDate.Year, startDate.Month, 1);
        var end = new DateTime(endDate.Year, endDate.Month, 1);
        var result = new List<DateTime>();

        while (current <= end)
        {
            result.Add(current);
            current = current.AddMonths(1);
        }

        return result;
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed.ToLowerInvariant();
    }

    private static string Escape(string value)
    {
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
