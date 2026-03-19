using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Api.Data;
using PersonalFinanceTracker.Api.DTOs;
using PersonalFinanceTracker.Api.Entities;

namespace PersonalFinanceTracker.Api.Services;

public interface IAccountService
{
    List<AccountDto> GetAll(string userId);
    AccountDto? GetById(string userId, string id);
    AccountDto Create(string userId, CreateAccountRequestDto request);
    AccountDto? Update(string userId, string id, UpdateAccountRequestDto request);
    bool Delete(string userId, string id);
    bool Transfer(string userId, TransferRequestDto request, out string message);
}

public class AccountService : IAccountService
{
    private readonly AppDbContext _dbContext;
    private const string CreditCardType = "Credit Card";
    private const string FundType = "Fund";
    private const string SelfTransferOutType = "self-transfer-out";
    private const string SelfTransferInType = "self-transfer-in";
    private const string CardSettlementOutType = "card-settlement-out";
    private const string CardSettlementInType = "card-settlement-in";
    private const string CardPayOffCategory = "Card Pay Off";
    private const string CardPayDownCategory = "Card Pay Down";

    public AccountService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<AccountDto> GetAll(string userId)
    {
        return _dbContext.Accounts
            .Include(x => x.Category)
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Name)
            .Select(x => Map(x))
            .ToList();
    }

    public AccountDto? GetById(string userId, string id)
    {
        var account = _dbContext.Accounts
            .Include(x => x.Category)
            .FirstOrDefault(x => x.UserId == userId && x.Id == id);
        return account is null ? null : Map(account);
    }

    public AccountDto Create(string userId, CreateAccountRequestDto request)
    {
        var normalizedType = NormalizeType(request.Type);
        var category = ResolveFundCategory(userId, normalizedType, request.CategoryId);

        var normalizedName = string.IsNullOrWhiteSpace(request.Name)
            ? category is not null ? $"{category.Name} Fund" : string.Empty
            : request.Name.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName))
            throw new ArgumentException("Account name is required.");

        ValidateCreateRequest(normalizedType, request.OpeningBalance, request.CreditLimit, category);

        var account = new Account
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Name = normalizedName,
            Type = normalizedType,
            CategoryId = category?.Id,
            OpeningBalance = IsCreditCard(normalizedType) ? 0 : request.OpeningBalance,
            CurrentBalance = 0,
            CreditLimit = IsCreditCard(normalizedType) ? request.CreditLimit : null,
            InstitutionName = request.InstitutionName?.Trim(),
            LastUpdatedAt = DateTime.UtcNow
        };

        if (!IsCreditCard(normalizedType))
        {
            account.CurrentBalance = request.OpeningBalance;
        }

        _dbContext.Accounts.Add(account);
        _dbContext.SaveChanges();

        account.Category = category;
        return Map(account);
    }

    public AccountDto? Update(string userId, string id, UpdateAccountRequestDto request)
    {
        var account = _dbContext.Accounts
            .Include(x => x.Category)
            .FirstOrDefault(x => x.UserId == userId && x.Id == id);
        if (account is null)
            return null;

        var normalizedType = NormalizeType(request.Type);
        var isChangingType = !account.Type.Equals(normalizedType, StringComparison.OrdinalIgnoreCase);
        var category = ResolveFundCategory(userId, normalizedType, request.CategoryId);
        var isChangingFundCategory = account.CategoryId != category?.Id;
        var normalizedName = string.IsNullOrWhiteSpace(request.Name)
            ? category is not null ? $"{category.Name} Fund" : string.Empty
            : request.Name.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName))
            throw new ArgumentException("Account name is required.");

        if (isChangingType)
        {
            var hasTransactions = _dbContext.Transactions.Any(x => x.UserId == userId && x.AccountId == id);
            if (hasTransactions)
            {
                throw new InvalidOperationException("Account type cannot be changed after transactions exist.");
            }
        }

        if (isChangingFundCategory)
        {
            var hasTransactions = _dbContext.Transactions.Any(x => x.UserId == userId && x.AccountId == id);
            if (hasTransactions)
                throw new InvalidOperationException("Fund category cannot be changed after transactions exist.");
        }

        if (IsCreditCard(normalizedType))
        {
            if (request.CreditLimit is null || request.CreditLimit <= 0)
                throw new ArgumentException("Credit limit must be greater than 0 for credit cards.");

            if (account.CurrentBalance > request.CreditLimit)
                throw new InvalidOperationException("Credit limit cannot be lower than the current outstanding balance.");
        }

        account.Name = normalizedName;
        account.Type = normalizedType;
        account.CategoryId = category?.Id;
        account.Category = category;
        account.CreditLimit = IsCreditCard(normalizedType) ? request.CreditLimit : null;
        if (!IsCreditCard(normalizedType))
        {
            account.OpeningBalance = account.CurrentBalance;
        }
        account.InstitutionName = request.InstitutionName?.Trim();
        account.LastUpdatedAt = DateTime.UtcNow;

        _dbContext.SaveChanges();

        return Map(account);
    }

    public bool Delete(string userId, string id)
    {
        var account = _dbContext.Accounts.FirstOrDefault(x => x.UserId == userId && x.Id == id);
        if (account is null)
            return false;

        var hasTransactions = _dbContext.Transactions.Any(x => x.UserId == userId && x.AccountId == id);
        if (hasTransactions)
            throw new InvalidOperationException("Cannot delete an account that has transactions.");

        _dbContext.Accounts.Remove(account);
        _dbContext.SaveChanges();
        return true;
    }

    public bool Transfer(string userId, TransferRequestDto request, out string message)
    {
        message = string.Empty;

        if (request.Amount <= 0)
        {
            message = "Transfer amount must be greater than 0.";
            return false;
        }

        if (request.SourceAccountId == request.DestinationAccountId)
        {
            message = "Source and destination accounts must be different.";
            return false;
        }

        using var transaction = _dbContext.Database.BeginTransaction();
        try
        {
            var source = _dbContext.Accounts.FirstOrDefault(x => x.UserId == userId && x.Id == request.SourceAccountId);
            var destination = _dbContext.Accounts.FirstOrDefault(x => x.UserId == userId && x.Id == request.DestinationAccountId);

            if (source is null || destination is null)
            {
                message = "Invalid account selection.";
                return false;
            }

            var transferKind = ResolveTransferKind(source, destination, request.Amount, out message);
            if (transferKind is null)
            {
                return false;
            }

            source.LastUpdatedAt = DateTime.UtcNow;
            destination.LastUpdatedAt = DateTime.UtcNow;

            var transferGroupId = Guid.NewGuid().ToString();
            var date = request.Date ?? DateTime.UtcNow;
            var labels = BuildTransferLabels(transferKind.Value, source, destination, request.Amount, request.Note);

            _dbContext.Transactions.Add(new TransactionRecord
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                AccountId = source.Id,
                Type = labels.SourceType,
                Amount = request.Amount,
                Date = date,
                Category = labels.Category,
                Merchant = labels.SourceMerchant,
                Note = labels.SourceNote,
                PaymentMethod = "Transfer",
                TransferGroupId = transferGroupId,
                Tags = Array.Empty<string>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            _dbContext.Transactions.Add(new TransactionRecord
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                AccountId = destination.Id,
                Type = labels.DestinationType,
                Amount = request.Amount,
                Date = date,
                Category = labels.Category,
                Merchant = labels.DestinationMerchant,
                Note = labels.DestinationNote,
                PaymentMethod = "Transfer",
                TransferGroupId = transferGroupId,
                Tags = Array.Empty<string>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            _dbContext.SaveChanges();
            transaction.Commit();

            message = transferKind.Value == TransferKind.CardPayOff
                ? "Card paid off successfully."
                : transferKind.Value == TransferKind.CardPayDown
                    ? "Card payment recorded successfully."
                : "Self transfer completed successfully.";
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static AccountDto Map(Account account)
    {
        return new AccountDto
        {
            Id = account.Id,
            UserId = account.UserId,
            Name = account.Name,
            Type = account.Type,
            CategoryId = account.CategoryId,
            CategoryName = account.Category?.Name,
            OpeningBalance = account.OpeningBalance,
            CurrentBalance = account.CurrentBalance,
            CreditLimit = account.CreditLimit,
            AvailableCredit = IsCreditCard(account.Type) && account.CreditLimit.HasValue
                ? account.CreditLimit.Value - account.CurrentBalance
                : null,
            InstitutionName = account.InstitutionName,
            LastUpdatedAt = account.LastUpdatedAt
        };
    }

    private static void ValidateCreateRequest(string type, decimal openingBalance, decimal? creditLimit, Category? category)
    {
        if (IsCreditCard(type))
        {
            if (creditLimit is null || creditLimit <= 0)
                throw new ArgumentException("Credit limit must be greater than 0 for credit cards.");

            return;
        }

        if (IsFund(type) && category is null)
            throw new ArgumentException("Funds must be linked to an expense category.");

        if (openingBalance < 0)
            throw new ArgumentException("Opening balance cannot be negative.");
    }

    private static string NormalizeType(string type)
    {
        var normalizedType = type.Trim();
        if (string.IsNullOrWhiteSpace(normalizedType))
            throw new ArgumentException("Account type is required.");

        return normalizedType;
    }

    private static bool IsCreditCard(Account account) => IsCreditCard(account.Type);

    private static bool IsCreditCard(string accountType) =>
        accountType.Equals(CreditCardType, StringComparison.OrdinalIgnoreCase);

    private static bool IsFund(Account account) => IsFund(account.Type);

    private static bool IsFund(string accountType) =>
        accountType.Equals(FundType, StringComparison.OrdinalIgnoreCase);

    private Category? ResolveFundCategory(string userId, string accountType, string? categoryId)
    {
        if (!IsFund(accountType))
            return null;

        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ArgumentException("Fund category is required.");

        var category = _dbContext.Categories.FirstOrDefault(x =>
            x.UserId == userId &&
            x.Id == categoryId &&
            !x.IsArchived);

        if (category is null)
            throw new InvalidOperationException("Selected fund category not found.");

        if (!category.Type.Equals("expense", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Funds can only be linked to expense categories.");

        return category;
    }

    private static TransferKind? ResolveTransferKind(Account source, Account destination, decimal amount, out string message)
    {
        message = string.Empty;

        var sourceIsCard = IsCreditCard(source);
        var destinationIsCard = IsCreditCard(destination);

        if (sourceIsCard && destinationIsCard)
        {
            message = "Transfers between credit cards are not supported.";
            return null;
        }

        if (!sourceIsCard && !destinationIsCard)
        {
            if (source.CurrentBalance < amount)
            {
                message = "Insufficient balance in source account.";
                return null;
            }

            source.CurrentBalance -= amount;
            destination.CurrentBalance += amount;
            return TransferKind.SelfTransfer;
        }

        if (sourceIsCard)
        {
            message = "Self transfer from a credit card to another account is not supported.";
            return null;
        }

        if (source.CurrentBalance < amount)
        {
            message = "Insufficient balance in source account.";
            return null;
        }

        if (destination.CurrentBalance < amount)
        {
            message = "Payment exceeds current credit card outstanding balance.";
            return null;
        }

        var wasFullPayment = destination.CurrentBalance == amount;
        source.CurrentBalance -= amount;
        destination.CurrentBalance -= amount;
        return wasFullPayment ? TransferKind.CardPayOff : TransferKind.CardPayDown;
    }

    private static TransferLabels BuildTransferLabels(
        TransferKind transferKind,
        Account source,
        Account destination,
        decimal amount,
        string? note)
    {
        if (transferKind is TransferKind.CardPayOff or TransferKind.CardPayDown)
        {
            var category = transferKind == TransferKind.CardPayOff
                ? CardPayOffCategory
                : CardPayDownCategory;
            var actionLabel = transferKind == TransferKind.CardPayOff ? "Paid off" : "Paid down";

            return new TransferLabels(
                SourceType: CardSettlementOutType,
                DestinationType: CardSettlementInType,
                Category: category,
                SourceMerchant: destination.Name,
                DestinationMerchant: source.Name,
                SourceNote: string.IsNullOrWhiteSpace(note) ? $"{actionLabel} {destination.Name}" : note.Trim(),
                DestinationNote: string.IsNullOrWhiteSpace(note)
                    ? $"{actionLabel} using {source.Name}"
                    : note.Trim()
            );
        }

        return new TransferLabels(
            SourceType: SelfTransferOutType,
            DestinationType: SelfTransferInType,
            Category: "Self Transfer",
            SourceMerchant: destination.Name,
            DestinationMerchant: source.Name,
            SourceNote: string.IsNullOrWhiteSpace(note) ? $"Transferred to {destination.Name}" : note.Trim(),
            DestinationNote: string.IsNullOrWhiteSpace(note) ? $"Transferred from {source.Name}" : note.Trim()
        );
    }

    private enum TransferKind
    {
        SelfTransfer,
        CardPayOff,
        CardPayDown
    }

    private sealed record TransferLabels(
        string SourceType,
        string DestinationType,
        string Category,
        string SourceMerchant,
        string DestinationMerchant,
        string SourceNote,
        string DestinationNote
    );
}
