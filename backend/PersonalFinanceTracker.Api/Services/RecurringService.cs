using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Api.Data;
using PersonalFinanceTracker.Api.DTOs;
using PersonalFinanceTracker.Api.Entities;

namespace PersonalFinanceTracker.Api.Services;

public interface IRecurringService
{
    List<RecurringTransactionDto> GetAll(string userId);
    RecurringTransactionDto Create(string userId, CreateRecurringTransactionRequestDto request);
    RecurringTransactionDto? Update(string userId, string id, UpdateRecurringTransactionRequestDto request);
    bool Delete(string userId, string id);
    Task ProcessDueItemsAsync(CancellationToken cancellationToken = default);
}

public class RecurringService : IRecurringService
{
    private readonly AppDbContext _dbContext;
    private readonly ITransactionService _transactionService;

    public RecurringService(AppDbContext dbContext, ITransactionService transactionService)
    {
        _dbContext = dbContext;
        _transactionService = transactionService;
    }

    public List<RecurringTransactionDto> GetAll(string userId)
    {
        return _dbContext.RecurringTransactions
            .Include(x => x.Account)
            .Include(x => x.Category)
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.IsPaused)
            .ThenBy(x => x.NextRunDate)
            .ThenBy(x => x.Title)
            .AsEnumerable()
            .Select(Map)
            .ToList();
    }

    public RecurringTransactionDto Create(string userId, CreateRecurringTransactionRequestDto request)
    {
        var account = ResolveAccount(userId, request.AccountId);
        var category = ResolveCategory(userId, request.CategoryId, request.Type);
        ValidateRequest(request.Title, request.Type, request.Amount, request.Frequency, request.StartDate, request.EndDate, request.NextRunDate);

        var normalizedStartDate = NormalizeUtcDate(request.StartDate);
        var normalizedEndDate = NormalizeUtcDate(request.EndDate);
        var normalizedNextRunDate = NormalizeUtcDate(request.NextRunDate ?? request.StartDate);

        var item = new RecurringTransaction
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Title = request.Title.Trim(),
            Type = NormalizeType(request.Type),
            Amount = request.Amount,
            CategoryId = category.Id,
            AccountId = account.Id,
            Frequency = NormalizeFrequency(request.Frequency),
            StartDate = normalizedStartDate,
            EndDate = normalizedEndDate,
            NextRunDate = normalizedNextRunDate,
            AutoCreateTransaction = request.AutoCreateTransaction,
            IsPaused = request.IsPaused,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.RecurringTransactions.Add(item);
        _dbContext.SaveChanges();

        item.Account = account;
        item.Category = category;
        return Map(item);
    }

    public RecurringTransactionDto? Update(string userId, string id, UpdateRecurringTransactionRequestDto request)
    {
        var item = _dbContext.RecurringTransactions
            .Include(x => x.Account)
            .Include(x => x.Category)
            .FirstOrDefault(x => x.UserId == userId && x.Id == id);

        if (item is null)
            return null;

        var account = ResolveAccount(userId, request.AccountId);
        var category = ResolveCategory(userId, request.CategoryId, request.Type);
        ValidateRequest(request.Title, request.Type, request.Amount, request.Frequency, request.StartDate, request.EndDate, request.NextRunDate);

        var normalizedStartDate = NormalizeUtcDate(request.StartDate);
        var normalizedEndDate = NormalizeUtcDate(request.EndDate);
        var normalizedNextRunDate = NormalizeUtcDate(request.NextRunDate ?? item.NextRunDate);

        item.Title = request.Title.Trim();
        item.Type = NormalizeType(request.Type);
        item.Amount = request.Amount;
        item.CategoryId = category.Id;
        item.AccountId = account.Id;
        item.Frequency = NormalizeFrequency(request.Frequency);
        item.StartDate = normalizedStartDate;
        item.EndDate = normalizedEndDate;
        item.NextRunDate = normalizedNextRunDate;
        item.AutoCreateTransaction = request.AutoCreateTransaction;
        item.IsPaused = request.IsPaused;
        item.UpdatedAt = DateTime.UtcNow;

        _dbContext.SaveChanges();

        item.Account = account;
        item.Category = category;
        return Map(item);
    }

    public bool Delete(string userId, string id)
    {
        var item = _dbContext.RecurringTransactions.FirstOrDefault(x => x.UserId == userId && x.Id == id);
        if (item is null)
            return false;

        _dbContext.RecurringTransactions.Remove(item);
        _dbContext.SaveChanges();
        return true;
    }

    public async Task ProcessDueItemsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var dueItems = await _dbContext.RecurringTransactions
            .Include(x => x.Category)
            .Where(x =>
                !x.IsPaused &&
                x.AutoCreateTransaction &&
                x.NextRunDate <= today &&
                (x.EndDate == null || x.NextRunDate <= x.EndDate))
            .OrderBy(x => x.NextRunDate)
            .ToListAsync(cancellationToken);

        foreach (var item in dueItems)
        {
            while (item.NextRunDate.Date <= today && (item.EndDate == null || item.NextRunDate.Date <= item.EndDate.Value.Date))
            {
                var request = new CreateTransactionRequestDto
                {
                    AccountId = item.AccountId,
                    CategoryId = item.CategoryId,
                    Type = item.Type,
                    Amount = item.Amount,
                    Date = item.NextRunDate.Date,
                    Merchant = item.Title,
                    Note = $"Auto-created from recurring item: {item.Title}",
                    PaymentMethod = "Recurring",
                    Tags = ["recurring", item.Frequency.ToLowerInvariant()]
                };

                _transactionService.Create(item.UserId, request);

                item.LastRunAt = DateTime.UtcNow;
                item.NextRunDate = GetNextRunDate(item.NextRunDate.Date, item.Frequency);
                item.UpdatedAt = DateTime.UtcNow;
            }
        }

        if (dueItems.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private Category ResolveCategory(string userId, string categoryId, string type)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ArgumentException("Category is required.");

        var normalizedType = NormalizeType(type);
        var category = _dbContext.Categories.FirstOrDefault(x =>
            x.UserId == userId &&
            x.Id == categoryId &&
            !x.IsArchived);

        if (category is null)
            throw new InvalidOperationException("Selected category not found.");

        if (!category.Type.Equals(normalizedType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Selected category does not match the recurring type.");

        return category;
    }

    private Account ResolveAccount(string userId, string accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            throw new ArgumentException("Account is required.");

        var account = _dbContext.Accounts.FirstOrDefault(x => x.UserId == userId && x.Id == accountId);
        if (account is null)
            throw new InvalidOperationException("Selected account not found.");

        return account;
    }

    private static void ValidateRequest(
        string title,
        string type,
        decimal amount,
        string frequency,
        DateTime startDate,
        DateTime? endDate,
        DateTime? nextRunDate)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.");

        _ = NormalizeType(type);
        _ = NormalizeFrequency(frequency);

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than 0.");

        if (endDate.HasValue && endDate.Value.Date < startDate.Date)
            throw new ArgumentException("End date cannot be before start date.");

        if (nextRunDate.HasValue && nextRunDate.Value.Date < startDate.Date)
            throw new ArgumentException("Next run date cannot be before the start date.");
    }

    private static string NormalizeType(string type)
    {
        var normalized = type.Trim().ToLowerInvariant();
        if (normalized is not ("income" or "expense"))
            throw new ArgumentException("Recurring type must be income or expense.");

        return normalized;
    }

    private static string NormalizeFrequency(string frequency)
    {
        var normalized = frequency.Trim().ToLowerInvariant();
        if (normalized is not ("daily" or "weekly" or "monthly" or "yearly"))
            throw new ArgumentException("Frequency must be daily, weekly, monthly, or yearly.");

        return normalized;
    }

    private static DateTime GetNextRunDate(DateTime current, string frequency)
    {
        var utcCurrent = NormalizeUtcDate(current);

        var nextDate = frequency.ToLowerInvariant() switch
        {
            "daily" => utcCurrent.AddDays(1),
            "weekly" => utcCurrent.AddDays(7),
            "monthly" => utcCurrent.AddMonths(1),
            "yearly" => utcCurrent.AddYears(1),
            _ => utcCurrent.AddMonths(1)
        };

        return NormalizeUtcDate(nextDate);
    }

    private static DateTime NormalizeUtcDate(DateTime value)
    {
        return new DateTime(value.Year, value.Month, value.Day, 0, 0, 0, DateTimeKind.Utc);
    }

    private static DateTime? NormalizeUtcDate(DateTime? value)
    {
        return value.HasValue ? NormalizeUtcDate(value.Value) : null;
    }

    private static RecurringTransactionDto Map(RecurringTransaction item)
    {
        return new RecurringTransactionDto
        {
            Id = item.Id,
            UserId = item.UserId,
            Title = item.Title,
            Type = item.Type,
            Amount = item.Amount,
            CategoryId = item.CategoryId,
            CategoryName = item.Category?.Name ?? "Unknown Category",
            AccountId = item.AccountId,
            AccountName = item.Account?.Name ?? "Unknown Account",
            Frequency = item.Frequency,
            StartDate = item.StartDate,
            EndDate = item.EndDate,
            NextRunDate = item.NextRunDate,
            AutoCreateTransaction = item.AutoCreateTransaction,
            IsPaused = item.IsPaused,
            LastRunAt = item.LastRunAt
        };
    }
}
