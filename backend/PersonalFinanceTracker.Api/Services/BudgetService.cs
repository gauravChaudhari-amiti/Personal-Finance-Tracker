using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Api.Data;
using PersonalFinanceTracker.Api.DTOs;
using PersonalFinanceTracker.Api.Entities;

namespace PersonalFinanceTracker.Api.Services;

public interface IBudgetService
{
    List<BudgetDto> GetAll(string userId, int month, int year);
    BudgetDto Create(string userId, CreateBudgetRequestDto request);
    BudgetDto? Update(string userId, string id, UpdateBudgetRequestDto request);
    bool Delete(string userId, string id);
    List<BudgetDto> Duplicate(string userId, DuplicateBudgetRequestDto request);
}

public class BudgetService : IBudgetService
{
    private readonly AppDbContext _dbContext;

    public BudgetService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<BudgetDto> GetAll(string userId, int month, int year)
    {
        ValidateMonthYear(month, year);

        var budgets = _dbContext.Budgets
            .Include(x => x.Category)
            .Where(x => x.UserId == userId && x.Month == month && x.Year == year)
            .AsEnumerable()
            .OrderByDescending(x => CategoryDefaults.IsWholeMonthCategory(x.Category?.Type ?? "expense", x.Category?.Name ?? string.Empty))
            .ThenBy(x => x.Category?.Name)
            .ToList();

        var spending = GetExpenseTotals(userId, month, year);
        var totalExpense = spending.Values.Sum();

        return budgets.Select(budget => Map(budget, spending, totalExpense)).ToList();
    }

    public BudgetDto Create(string userId, CreateBudgetRequestDto request)
    {
        ValidateRequest(userId, request.CategoryId, request.Month, request.Year, request.Amount, request.AlertThresholdPercent);

        var exists = _dbContext.Budgets.Any(x =>
            x.UserId == userId &&
            x.CategoryId == request.CategoryId &&
            x.Month == request.Month &&
            x.Year == request.Year);

        if (exists)
            throw new InvalidOperationException("A budget already exists for this category and month.");

        var budget = new Budget
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            CategoryId = request.CategoryId,
            Month = request.Month,
            Year = request.Year,
            Amount = request.Amount,
            AlertThresholdPercent = request.AlertThresholdPercent,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Budgets.Add(budget);
        _dbContext.SaveChanges();

        budget.Category = _dbContext.Categories.First(x => x.Id == budget.CategoryId);
        var spending = GetExpenseTotals(userId, request.Month, request.Year);
        return Map(budget, spending, spending.Values.Sum());
    }

    public BudgetDto? Update(string userId, string id, UpdateBudgetRequestDto request)
    {
        var budget = _dbContext.Budgets
            .Include(x => x.Category)
            .FirstOrDefault(x => x.UserId == userId && x.Id == id);

        if (budget is null)
            return null;

        ValidateRequest(userId, request.CategoryId, request.Month, request.Year, request.Amount, request.AlertThresholdPercent);

        var duplicate = _dbContext.Budgets.Any(x =>
            x.UserId == userId &&
            x.Id != id &&
            x.CategoryId == request.CategoryId &&
            x.Month == request.Month &&
            x.Year == request.Year);

        if (duplicate)
            throw new InvalidOperationException("A budget already exists for this category and month.");

        budget.CategoryId = request.CategoryId;
        budget.Month = request.Month;
        budget.Year = request.Year;
        budget.Amount = request.Amount;
        budget.AlertThresholdPercent = request.AlertThresholdPercent;
        budget.UpdatedAt = DateTime.UtcNow;

        _dbContext.SaveChanges();

        budget.Category = _dbContext.Categories.First(x => x.Id == budget.CategoryId);
        var spending = GetExpenseTotals(userId, budget.Month, budget.Year);
        return Map(budget, spending, spending.Values.Sum());
    }

    public bool Delete(string userId, string id)
    {
        var budget = _dbContext.Budgets.FirstOrDefault(x => x.UserId == userId && x.Id == id);
        if (budget is null)
            return false;

        _dbContext.Budgets.Remove(budget);
        _dbContext.SaveChanges();
        return true;
    }

    public List<BudgetDto> Duplicate(string userId, DuplicateBudgetRequestDto request)
    {
        ValidateMonthYear(request.SourceMonth, request.SourceYear);
        ValidateMonthYear(request.TargetMonth, request.TargetYear);

        var sourceBudgets = _dbContext.Budgets
            .Include(x => x.Category)
            .Where(x =>
                x.UserId == userId &&
                x.Month == request.SourceMonth &&
                x.Year == request.SourceYear)
            .ToList();

        if (sourceBudgets.Count == 0)
            throw new InvalidOperationException("No budgets found in the source month.");

        var targetExisting = _dbContext.Budgets
            .Where(x =>
                x.UserId == userId &&
                x.Month == request.TargetMonth &&
                x.Year == request.TargetYear)
            .ToList();

        foreach (var sourceBudget in sourceBudgets)
        {
            var existing = targetExisting.FirstOrDefault(x => x.CategoryId == sourceBudget.CategoryId);
            if (existing is not null)
            {
                existing.Amount = sourceBudget.Amount;
                existing.AlertThresholdPercent = sourceBudget.AlertThresholdPercent;
                existing.UpdatedAt = DateTime.UtcNow;
                continue;
            }

            _dbContext.Budgets.Add(new Budget
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                CategoryId = sourceBudget.CategoryId,
                Month = request.TargetMonth,
                Year = request.TargetYear,
                Amount = sourceBudget.Amount,
                AlertThresholdPercent = sourceBudget.AlertThresholdPercent,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        _dbContext.SaveChanges();

        return GetAll(userId, request.TargetMonth, request.TargetYear);
    }

    private void ValidateRequest(
        string userId,
        string categoryId,
        int month,
        int year,
        decimal amount,
        int alertThresholdPercent)
    {
        ValidateMonthYear(month, year);

        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ArgumentException("Category is required.");

        if (amount <= 0)
            throw new ArgumentException("Budget amount must be greater than 0.");

        if (alertThresholdPercent <= 0 || alertThresholdPercent > 500)
            throw new ArgumentException("Alert threshold must be between 1 and 500.");

        var category = _dbContext.Categories.FirstOrDefault(x => x.UserId == userId && x.Id == categoryId);
        if (category is null)
            throw new InvalidOperationException("Selected category not found.");

        if (!category.Type.Equals("expense", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Budgets can only be set for expense categories.");
    }

    private static void ValidateMonthYear(int month, int year)
    {
        if (month < 1 || month > 12)
            throw new ArgumentException("Month must be between 1 and 12.");

        if (year < 2000 || year > 3000)
            throw new ArgumentException("Year is out of supported range.");
    }

    private Dictionary<string, decimal> GetExpenseTotals(string userId, int month, int year)
    {
        return _dbContext.Transactions
            .Where(x =>
                x.UserId == userId &&
                x.Type == "expense" &&
                x.CategoryId != null &&
                x.Date.Month == month &&
                x.Date.Year == year)
            .GroupBy(x => x.CategoryId!)
            .Select(group => new
            {
                CategoryId = group.Key,
                Total = group.Sum(x => x.Amount)
            })
            .ToDictionary(x => x.CategoryId, x => x.Total);
    }

    private static BudgetDto Map(Budget budget, IReadOnlyDictionary<string, decimal> spending, decimal totalExpense)
    {
        var isWholeMonthBudget = CategoryDefaults.IsWholeMonthCategory(
            budget.Category?.Type ?? "expense",
            budget.Category?.Name ?? string.Empty);

        var spentAmount = isWholeMonthBudget
            ? totalExpense
            : spending.TryGetValue(budget.CategoryId, out var total) ? total : 0;
        var remainingAmount = budget.Amount - spentAmount;
        var progressPercent = budget.Amount <= 0 ? 0 : Math.Round((spentAmount / budget.Amount) * 100, 2);

        var status = progressPercent switch
        {
            >= 120 => "critical",
            >= 100 => "over",
            _ when progressPercent >= budget.AlertThresholdPercent => "warning",
            _ => "safe"
        };

        return new BudgetDto
        {
            Id = budget.Id,
            UserId = budget.UserId,
            CategoryId = budget.CategoryId,
            CategoryName = budget.Category?.Name ?? "Unknown Category",
            CategoryColor = budget.Category?.Color,
            Month = budget.Month,
            Year = budget.Year,
            Amount = budget.Amount,
            AlertThresholdPercent = budget.AlertThresholdPercent,
            SpentAmount = spentAmount,
            RemainingAmount = remainingAmount,
            ProgressPercent = progressPercent,
            Status = status
        };
    }
}
