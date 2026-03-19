using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Api.Data;
using PersonalFinanceTracker.Api.DTOs;
using PersonalFinanceTracker.Api.Entities;

namespace PersonalFinanceTracker.Api.Services;

public interface IGoalService
{
    List<GoalDto> GetAll(string userId);
    GoalDto Create(string userId, CreateGoalRequestDto request);
    GoalDto? Update(string userId, string id, UpdateGoalRequestDto request);
    bool Delete(string userId, string id);
    GoalDto Contribute(string userId, string id, GoalContributionRequestDto request);
    GoalDto Withdraw(string userId, string id, GoalWithdrawalRequestDto request);
}

public class GoalService : IGoalService
{
    private const string GoalContributionCategory = "Goal Contribution";
    private const string GoalWithdrawalCategory = "Goal Withdrawal";

    private readonly AppDbContext _dbContext;

    public GoalService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<GoalDto> GetAll(string userId)
    {
        return _dbContext.Goals
            .Include(x => x.Category)
            .Include(x => x.LinkedAccount)
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Status == "completed")
            .ThenBy(x => x.TargetDate ?? DateTime.MaxValue)
            .ThenBy(x => x.Name)
            .AsEnumerable()
            .Select(Map)
            .ToList();
    }

    public GoalDto Create(string userId, CreateGoalRequestDto request)
    {
        var category = ResolveGoalCategory(userId, request.CategoryId);
        ValidateGoalRequest(userId, request.Name, request.TargetAmount, request.LinkedAccountId);

        var goal = new Goal
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Name = request.Name.Trim(),
            TargetAmount = request.TargetAmount,
            CurrentAmount = 0,
            TargetDate = NormalizeUtcDate(request.TargetDate),
            CategoryId = category?.Id,
            LinkedAccountId = NormalizeOptional(request.LinkedAccountId),
            Icon = NormalizeOptional(request.Icon),
            Color = NormalizeOptional(request.Color),
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Goals.Add(goal);
        _dbContext.SaveChanges();

        goal.Category = category;
        goal.LinkedAccount = ResolveLinkedAccount(userId, goal.LinkedAccountId);
        return Map(goal);
    }

    public GoalDto? Update(string userId, string id, UpdateGoalRequestDto request)
    {
        var goal = _dbContext.Goals
            .Include(x => x.Category)
            .Include(x => x.LinkedAccount)
            .FirstOrDefault(x => x.UserId == userId && x.Id == id);

        if (goal is null)
            return null;

        var category = ResolveGoalCategory(userId, request.CategoryId);
        ValidateGoalRequest(userId, request.Name, request.TargetAmount, request.LinkedAccountId);

        goal.Name = request.Name.Trim();
        goal.TargetAmount = request.TargetAmount;
        goal.TargetDate = NormalizeUtcDate(request.TargetDate);
        goal.CategoryId = category?.Id;
        goal.Category = category;
        goal.LinkedAccountId = NormalizeOptional(request.LinkedAccountId);
        goal.Icon = NormalizeOptional(request.Icon);
        goal.Color = NormalizeOptional(request.Color);
        UpdateGoalStatus(goal);
        goal.UpdatedAt = DateTime.UtcNow;

        _dbContext.SaveChanges();

        goal.Category = category;
        goal.LinkedAccount = ResolveLinkedAccount(userId, goal.LinkedAccountId);
        return Map(goal);
    }

    public bool Delete(string userId, string id)
    {
        var goal = _dbContext.Goals.FirstOrDefault(x => x.UserId == userId && x.Id == id);
        if (goal is null)
            return false;

        if (goal.CurrentAmount > 0)
            throw new InvalidOperationException("Withdraw the saved amount before deleting this goal.");

        _dbContext.Goals.Remove(goal);
        _dbContext.SaveChanges();
        return true;
    }

    public GoalDto Contribute(string userId, string id, GoalContributionRequestDto request)
    {
        ValidateAmount(request.Amount, "Contribution amount");

        using var transaction = _dbContext.Database.BeginTransaction();
        try
        {
            var goal = _dbContext.Goals
                .Include(x => x.LinkedAccount)
                .FirstOrDefault(x => x.UserId == userId && x.Id == id);

            if (goal is null)
                throw new InvalidOperationException("Goal not found.");

            var account = ResolveActionAccount(userId, request.AccountId, goal.LinkedAccountId, "contribute to");
            if (account is not null)
            {
                if (account.CurrentBalance < request.Amount)
                    throw new InvalidOperationException("Insufficient balance in the selected account.");

                account.CurrentBalance -= request.Amount;
                account.LastUpdatedAt = DateTime.UtcNow;

                var category = EnsureSystemCategory(userId, GoalContributionCategory, "expense", "#2563EB", "piggy-bank");
                _dbContext.Transactions.Add(new TransactionRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    AccountId = account.Id,
                    CategoryId = category.Id,
                    Type = "expense",
                    Amount = request.Amount,
                    Date = DateTime.UtcNow.Date,
                    Category = category.Name,
                    Merchant = goal.Name,
                    Note = NormalizeOptional(request.Note) ?? $"Contribution to {goal.Name}",
                    PaymentMethod = "Goal Transfer",
                    Tags = ["goal", "contribution"],
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            goal.CurrentAmount += request.Amount;
            goal.UpdatedAt = DateTime.UtcNow;
            UpdateGoalStatus(goal);

            _dbContext.SaveChanges();
            transaction.Commit();

            goal.LinkedAccount = ResolveLinkedAccount(userId, goal.LinkedAccountId);
            return Map(goal);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public GoalDto Withdraw(string userId, string id, GoalWithdrawalRequestDto request)
    {
        ValidateAmount(request.Amount, "Withdrawal amount");

        using var transaction = _dbContext.Database.BeginTransaction();
        try
        {
            var goal = _dbContext.Goals
                .Include(x => x.LinkedAccount)
                .FirstOrDefault(x => x.UserId == userId && x.Id == id);

            if (goal is null)
                throw new InvalidOperationException("Goal not found.");

            if (request.Amount > goal.CurrentAmount)
                throw new InvalidOperationException("Withdrawal exceeds the current goal balance.");

            var account = ResolveActionAccount(userId, request.AccountId, goal.LinkedAccountId, "withdraw from");
            if (account is not null)
            {
                account.CurrentBalance += request.Amount;
                account.LastUpdatedAt = DateTime.UtcNow;

                var category = EnsureSystemCategory(userId, GoalWithdrawalCategory, "income", "#059669", "wallet");
                _dbContext.Transactions.Add(new TransactionRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    AccountId = account.Id,
                    CategoryId = category.Id,
                    Type = "income",
                    Amount = request.Amount,
                    Date = DateTime.UtcNow.Date,
                    Category = category.Name,
                    Merchant = goal.Name,
                    Note = NormalizeOptional(request.Note) ?? $"Withdrawal from {goal.Name}",
                    PaymentMethod = "Goal Transfer",
                    Tags = ["goal", "withdrawal"],
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            goal.CurrentAmount -= request.Amount;
            goal.UpdatedAt = DateTime.UtcNow;
            UpdateGoalStatus(goal);

            _dbContext.SaveChanges();
            transaction.Commit();

            goal.LinkedAccount = ResolveLinkedAccount(userId, goal.LinkedAccountId);
            return Map(goal);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private void ValidateGoalRequest(string userId, string name, decimal targetAmount, string? linkedAccountId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Goal name is required.");

        if (targetAmount <= 0)
            throw new ArgumentException("Target amount must be greater than 0.");

        if (!string.IsNullOrWhiteSpace(linkedAccountId))
        {
            _ = ResolveLinkedAccount(userId, linkedAccountId, true);
        }
    }

    private Category? ResolveGoalCategory(string userId, string? categoryId)
    {
        var normalizedCategoryId = NormalizeOptional(categoryId);
        if (normalizedCategoryId is null)
            return null;

        var category = _dbContext.Categories.FirstOrDefault(x =>
            x.UserId == userId &&
            x.Id == normalizedCategoryId &&
            !x.IsArchived);

        if (category is null)
            throw new InvalidOperationException("Selected goal category not found.");

        if (!category.Type.Equals("expense", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Goals can only use expense categories.");

        return category;
    }

    private static void ValidateAmount(decimal amount, string label)
    {
        if (amount <= 0)
            throw new ArgumentException($"{label} must be greater than 0.");
    }

    private Account? ResolveActionAccount(string userId, string? requestAccountId, string? goalAccountId, string actionLabel)
    {
        var accountId = NormalizeOptional(requestAccountId) ?? NormalizeOptional(goalAccountId);
        if (accountId is null)
            return null;

        return ResolveLinkedAccount(userId, accountId, true, actionLabel);
    }

    private Account? ResolveLinkedAccount(string userId, string? accountId, bool requireValid = false, string actionLabel = "link")
    {
        var normalizedAccountId = NormalizeOptional(accountId);
        if (normalizedAccountId is null)
            return null;

        var account = _dbContext.Accounts.FirstOrDefault(x => x.UserId == userId && x.Id == normalizedAccountId);
        if (account is null)
        {
            if (requireValid)
                throw new InvalidOperationException("Selected account not found.");

            return null;
        }

        if (account.Type.Equals("Credit Card", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Credit cards cannot be used to {actionLabel} goals.");

        return account;
    }

    private Category EnsureSystemCategory(string userId, string name, string type, string color, string icon)
    {
        var category = _dbContext.Categories.FirstOrDefault(x =>
            x.UserId == userId &&
            x.Type == type &&
            x.Name.ToLower() == name.ToLower());

        if (category is not null)
            return category;

        category = new Category
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Name = name,
            Type = type,
            Color = color,
            Icon = icon,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Categories.Add(category);
        _dbContext.SaveChanges();
        return category;
    }

    private static void UpdateGoalStatus(Goal goal)
    {
        goal.Status = goal.CurrentAmount >= goal.TargetAmount ? "completed" : "active";
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static DateTime? NormalizeUtcDate(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        return new DateTime(value.Value.Year, value.Value.Month, value.Value.Day, 0, 0, 0, DateTimeKind.Utc);
    }

    private static GoalDto Map(Goal goal)
    {
        var progressPercent = goal.TargetAmount <= 0
            ? 0
            : Math.Round(Math.Min((goal.CurrentAmount / goal.TargetAmount) * 100, 100), 2);

        var remainingAmount = Math.Max(goal.TargetAmount - goal.CurrentAmount, 0);

        return new GoalDto
        {
            Id = goal.Id,
            UserId = goal.UserId,
            Name = goal.Name,
            TargetAmount = goal.TargetAmount,
            CurrentAmount = goal.CurrentAmount,
            RemainingAmount = remainingAmount,
            ProgressPercent = progressPercent,
            TargetDate = goal.TargetDate,
            CategoryId = goal.CategoryId,
            CategoryName = goal.Category?.Name,
            LinkedAccountId = goal.LinkedAccountId,
            LinkedAccountName = goal.LinkedAccount?.Name,
            Icon = goal.Icon,
            Color = goal.Color,
            Status = goal.Status
        };
    }
}
