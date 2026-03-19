using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Api.Data;
using PersonalFinanceTracker.Api.DTOs;
using PersonalFinanceTracker.Api.Entities;

namespace PersonalFinanceTracker.Api.Services;

public interface ITransactionService
{
    TransactionListResponseDto GetAll(string userId, string? type, string? accountId, string? search, int page, int pageSize);
    TransactionDto? GetById(string userId, string id);
    TransactionDto Create(string userId, CreateTransactionRequestDto request);
    TransactionDto? Update(string userId, string id, UpdateTransactionRequestDto request);
    bool Delete(string userId, string id);
}

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _dbContext;

    public TransactionService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public TransactionListResponseDto GetAll(string userId, string? type, string? accountId, string? search, int page, int pageSize)
    {
        var resolvedPage = page <= 0 ? 1 : page;
        var resolvedPageSize = pageSize <= 0 ? 15 : Math.Min(pageSize, 100);
        var allUserTransactionIds = _dbContext.Transactions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => x.Id)
            .ToList();

        var userScopedTransactionNumbers = allUserTransactionIds
            .Select((id, index) => new
            {
                id,
                number = allUserTransactionIds.Count - index
            })
            .ToDictionary(x => x.id, x => (long)x.number);

        IQueryable<TransactionRecord> query = _dbContext.Transactions
            .Include(x => x.Account)
            .Include(x => x.Goal)
            .Include(x => x.CategoryItem)
            .Where(x => x.UserId == userId);

        if (!string.IsNullOrWhiteSpace(type))
        {
            var normalizedType = type.Trim().ToLowerInvariant();
            query = query.Where(x => x.Type == normalizedType);
        }

        if (!string.IsNullOrWhiteSpace(accountId))
        {
            query = query.Where(x => x.AccountId == accountId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Merchant ?? string.Empty, term) ||
                EF.Functions.ILike(x.Note ?? string.Empty, term));
        }

        var totalCount = query.Count();
        var totalIncome = query
            .Where(x => x.Type == "income")
            .Sum(x => (decimal?)x.Amount) ?? 0;
        var totalExpense = query
            .Where(x => x.Type == "expense")
            .Sum(x => (decimal?)x.Amount) ?? 0;
        var totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)resolvedPageSize);

        var items = query
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((resolvedPage - 1) * resolvedPageSize)
            .Take(resolvedPageSize)
            .AsEnumerable()
            .Select(x => Map(
                x,
                x.Account?.Name ?? "Unknown Account",
                userScopedTransactionNumbers.TryGetValue(x.Id, out var displayNumber)
                    ? displayNumber
                    : x.TransactionNumber))
            .ToList();

        return new TransactionListResponseDto
        {
            Items = items,
            Page = resolvedPage,
            PageSize = resolvedPageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            TotalIncome = totalIncome,
            TotalExpense = totalExpense
        };
    }

    public TransactionDto? GetById(string userId, string id)
    {
        var transaction = _dbContext.Transactions
            .Include(x => x.Account)
            .Include(x => x.Goal)
            .Include(x => x.CategoryItem)
            .FirstOrDefault(x => x.UserId == userId && x.Id == id);

        if (transaction is null)
            return null;

        var displayNumber = _dbContext.Transactions.Count(x =>
            x.UserId == userId &&
            (x.Date < transaction.Date ||
             (x.Date == transaction.Date && x.CreatedAt <= transaction.CreatedAt)));

        return Map(transaction, ResolveSourceName(transaction), displayNumber);
    }

    public TransactionDto Create(string userId, CreateTransactionRequestDto request)
    {
        ValidateRequest(request.AccountId, request.GoalId, request.CategoryId, request.Type, request.Amount);

        using var transaction = _dbContext.Database.BeginTransaction();
        try
        {
            var category = ResolveCategory(userId, request.Type, request.CategoryId);
            var source = ResolveSource(userId, request.AccountId, request.GoalId, category?.Id, request.Type);
            ApplyBalanceForCreate(source, request.Type, request.Amount);

            var transactionRecord = new TransactionRecord
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                AccountId = source.Account?.Id,
                GoalId = source.Goal?.Id,
                CategoryId = category?.Id,
                Type = request.Type.Trim().ToLowerInvariant(),
                Amount = request.Amount,
                Date = request.Date,
                Category = category?.Name,
                Merchant = request.Merchant?.Trim(),
                Note = request.Note?.Trim(),
                PaymentMethod = request.PaymentMethod?.Trim(),
                Tags = request.Tags?.ToArray() ?? Array.Empty<string>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (source.Account is not null)
            {
                source.Account.LastUpdatedAt = DateTime.UtcNow;
            }

            _dbContext.Transactions.Add(transactionRecord);
            _dbContext.SaveChanges();
            transaction.Commit();

            return Map(transactionRecord, source.Name);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public TransactionDto? Update(string userId, string id, UpdateTransactionRequestDto request)
    {
        ValidateRequest(request.AccountId, request.GoalId, request.CategoryId, request.Type, request.Amount);

        using var transaction = _dbContext.Database.BeginTransaction();
        try
        {
            var existing = _dbContext.Transactions
                .Include(x => x.Account)
                .Include(x => x.Goal)
                .FirstOrDefault(x => x.UserId == userId && x.Id == id);
            if (existing is null)
                return null;

            if (IsSystemGeneratedTransferType(existing.Type))
                throw new InvalidOperationException("Transfer transactions cannot be edited individually.");

            var category = ResolveCategory(userId, request.Type, request.CategoryId);
            var oldSource = ResolveExistingSource(existing);
            var newSource = ResolveSource(userId, request.AccountId, request.GoalId, category?.Id, request.Type);

            RevertBalanceForExisting(oldSource, existing.Type, existing.Amount);
            ApplyBalanceForCreate(newSource, request.Type, request.Amount);

            existing.AccountId = newSource.Account?.Id;
            existing.GoalId = newSource.Goal?.Id;
            existing.CategoryId = category?.Id;
            existing.Type = request.Type.Trim().ToLowerInvariant();
            existing.Amount = request.Amount;
            existing.Date = request.Date;
            existing.Category = category?.Name;
            existing.Merchant = request.Merchant?.Trim();
            existing.Note = request.Note?.Trim();
            existing.PaymentMethod = request.PaymentMethod?.Trim();
            existing.Tags = request.Tags?.ToArray() ?? Array.Empty<string>();
            existing.UpdatedAt = DateTime.UtcNow;

            if (oldSource.Account is not null)
            {
                oldSource.Account.LastUpdatedAt = DateTime.UtcNow;
            }

            if (newSource.Account is not null)
            {
                newSource.Account.LastUpdatedAt = DateTime.UtcNow;
            }

            _dbContext.SaveChanges();
            transaction.Commit();

            return Map(existing, newSource.Name);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public bool Delete(string userId, string id)
    {
        using var transaction = _dbContext.Database.BeginTransaction();
        try
        {
            var existing = _dbContext.Transactions
                .Include(x => x.Account)
                .Include(x => x.Goal)
                .FirstOrDefault(x => x.UserId == userId && x.Id == id);
            if (existing is null)
                return false;

            if (IsSystemGeneratedTransferType(existing.Type))
                throw new InvalidOperationException("Transfer transactions cannot be deleted individually.");

            var source = ResolveExistingSource(existing);

            RevertBalanceForExisting(source, existing.Type, existing.Amount);
            if (source.Account is not null)
            {
                source.Account.LastUpdatedAt = DateTime.UtcNow;
            }

            _dbContext.Transactions.Remove(existing);
            _dbContext.SaveChanges();
            transaction.Commit();

            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void ValidateRequest(string? accountId, string? goalId, string? categoryId, string type, decimal amount)
    {
        var hasAccount = !string.IsNullOrWhiteSpace(accountId);
        var hasGoal = !string.IsNullOrWhiteSpace(goalId);

        if (hasAccount == hasGoal)
            throw new ArgumentException("Select either an account or a goal fund.");

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Transaction type is required.");

        var normalizedType = type.Trim().ToLowerInvariant();
        if (normalizedType is not ("income" or "expense"))
            throw new ArgumentException("Transaction type must be income or expense.");

        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ArgumentException("Category is required.");

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than 0.");
    }

    private Category? ResolveCategory(string userId, string type, string? categoryId)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ArgumentException("Category is required.");

        var normalizedType = type.Trim().ToLowerInvariant();
        var category = _dbContext.Categories.FirstOrDefault(x =>
            x.UserId == userId &&
            x.Id == categoryId &&
            !x.IsArchived);

        if (category is null)
            throw new InvalidOperationException("Selected category not found.");

        if (!category.Type.Equals(normalizedType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Selected category does not match the transaction type.");

        return category;
    }

    private static void ApplyBalanceForCreate(TransactionSource source, string type, decimal amount)
    {
        var normalizedType = type.Trim().ToLowerInvariant();
        if (source.Goal is not null)
        {
            if (normalizedType != "expense")
                throw new InvalidOperationException("Goal funds can only be used for expense transactions.");

            if (source.Goal.CurrentAmount < amount)
                throw new InvalidOperationException("Insufficient balance in the selected goal fund.");

            source.Goal.CurrentAmount -= amount;
            source.Goal.UpdatedAt = DateTime.UtcNow;
            source.Goal.Status = source.Goal.CurrentAmount >= source.Goal.TargetAmount ? "completed" : "active";
            return;
        }

        var account = source.Account ?? throw new InvalidOperationException("Transaction source not found.");
        var isCreditCard = account.Type.Equals("Credit Card", StringComparison.OrdinalIgnoreCase);
        var isFund = account.Type.Equals("Fund", StringComparison.OrdinalIgnoreCase);

        if (normalizedType == "income")
        {
            if (isCreditCard)
            {
                if (amount > account.CurrentBalance)
                    throw new InvalidOperationException("Payment exceeds the current credit card outstanding balance.");

                account.CurrentBalance -= amount;
                return;
            }

            account.CurrentBalance += amount;
            return;
        }

        if (isFund)
        {
            if (account.CurrentBalance < amount)
                throw new InvalidOperationException("Insufficient balance in the selected fund.");

            account.CurrentBalance -= amount;
            return;
        }

        if (isCreditCard)
        {
            var creditLimit = account.CreditLimit ?? 0;
            if (account.CurrentBalance + amount > creditLimit)
                throw new InvalidOperationException("Credit limit exceeded.");

            account.CurrentBalance += amount;
            return;
        }

        if (account.CurrentBalance < amount)
            throw new InvalidOperationException("Insufficient account balance.");

        account.CurrentBalance -= amount;
    }

    private static void RevertBalanceForExisting(TransactionSource source, string type, decimal amount)
    {
        var normalizedType = type.Trim().ToLowerInvariant();
        if (source.Goal is not null)
        {
            if (normalizedType == "expense")
            {
                source.Goal.CurrentAmount += amount;
                source.Goal.UpdatedAt = DateTime.UtcNow;
                source.Goal.Status = source.Goal.CurrentAmount >= source.Goal.TargetAmount ? "completed" : "active";
                return;
            }

            throw new InvalidOperationException("Goal fund transactions must be expenses.");
        }

        var account = source.Account ?? throw new InvalidOperationException("Transaction source not found.");
        var isCreditCard = account.Type.Equals("Credit Card", StringComparison.OrdinalIgnoreCase);
        var isFund = account.Type.Equals("Fund", StringComparison.OrdinalIgnoreCase);

        if (normalizedType == "income")
        {
            if (isCreditCard)
            {
                account.CurrentBalance += amount;
                return;
            }

            account.CurrentBalance -= amount;
            return;
        }

        if (isFund)
        {
            account.CurrentBalance += amount;
            return;
        }

        if (isCreditCard)
        {
            account.CurrentBalance -= amount;
            return;
        }

        account.CurrentBalance += amount;
    }

    private static bool IsSystemGeneratedTransferType(string type)
    {
        return type is
            "transfer-in" or
            "transfer-out" or
            "self-transfer-in" or
            "self-transfer-out" or
            "card-settlement-in" or
            "card-settlement-out";
    }

    private static void ValidateAccountForTransaction(Account account, string type, string? categoryId)
    {
        var isFund = account.Type.Equals("Fund", StringComparison.OrdinalIgnoreCase);
        if (!isFund)
            return;

        var normalizedType = type.Trim().ToLowerInvariant();
        if (normalizedType != "expense")
            throw new InvalidOperationException("Funds can only be used for expense transactions.");

        if (string.IsNullOrWhiteSpace(categoryId) || account.CategoryId != categoryId)
            throw new InvalidOperationException("Selected fund does not match the transaction category.");
    }

    private TransactionSource ResolveSource(string userId, string? accountId, string? goalId, string? categoryId, string type)
    {
        if (!string.IsNullOrWhiteSpace(goalId))
        {
            var goal = _dbContext.Goals.FirstOrDefault(x => x.UserId == userId && x.Id == goalId);
            if (goal is null)
                throw new InvalidOperationException("Selected goal fund not found.");

            if (!string.Equals(type.Trim(), "expense", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Goal funds can only be used for expense transactions.");

            if (string.IsNullOrWhiteSpace(categoryId) || goal.CategoryId != categoryId)
                throw new InvalidOperationException("Selected goal fund does not match the transaction category.");

            return new TransactionSource(null, goal, goal.Name);
        }

        var account = _dbContext.Accounts
            .Include(x => x.Category)
            .FirstOrDefault(x => x.UserId == userId && x.Id == accountId);
        if (account is null)
            throw new InvalidOperationException("Selected account not found.");

        ValidateAccountForTransaction(account, type, categoryId);
        return new TransactionSource(account, null, account.Name);
    }

    private static TransactionSource ResolveExistingSource(TransactionRecord existing)
    {
        if (existing.Goal is not null)
            return new TransactionSource(null, existing.Goal, existing.Goal.Name);

        if (existing.Account is not null)
            return new TransactionSource(existing.Account, null, existing.Account.Name);

        throw new InvalidOperationException("Transaction source not found.");
    }

    private static string ResolveSourceName(TransactionRecord transaction)
    {
        return transaction.Account?.Name
            ?? transaction.Goal?.Name
            ?? "Unknown Source";
    }

    private static TransactionDto Map(TransactionRecord transaction, string accountName, long? displayTransactionNumber = null)
    {
        return new TransactionDto
        {
            Id = transaction.Id,
            TransactionNumber = displayTransactionNumber ?? transaction.TransactionNumber,
            UserId = transaction.UserId,
            AccountId = transaction.AccountId,
            AccountName = accountName,
            GoalId = transaction.GoalId,
            GoalName = transaction.Goal?.Name,
            CategoryId = transaction.CategoryId,
            Type = transaction.Type,
            Amount = transaction.Amount,
            Date = transaction.Date,
            Category = transaction.CategoryItem?.Name ?? transaction.Category,
            Merchant = transaction.Merchant,
            Note = transaction.Note,
            PaymentMethod = transaction.PaymentMethod,
            Tags = transaction.Tags.ToList(),
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt
        };
    }

    private sealed record TransactionSource(Account? Account, Goal? Goal, string Name);
}
