using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Api.Data;
using PersonalFinanceTracker.Api.Entities;

namespace PersonalFinanceTracker.Api.Services;

public interface IDatabaseSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

public class DatabaseSeeder : IDatabaseSeeder
{
    private readonly AppDbContext _dbContext;

    public DatabaseSeeder(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var userId = "11111111-1111-1111-1111-111111111111";
        var adminId = "22222222-2222-2222-2222-222222222222";

        if (!await _dbContext.Users.AnyAsync(cancellationToken))
        {
            var users = new[]
            {
                new AppUser
                {
                    Id = userId,
                    Email = "user@demo.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123"),
                    DisplayName = "Demo User",
                    AuthProvider = "password",
                    IsEmailVerified = true,
                    EmailVerifiedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                    Role = "User",
                    CreatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new AppUser
                {
                    Id = adminId,
                    Email = "admin@demo.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    DisplayName = "Demo Admin",
                    AuthProvider = "password",
                    IsEmailVerified = true,
                    EmailVerifiedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                    Role = "Admin",
                    CreatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            };

            var userBank = new Account
            {
                Id = "acc-user-bank-001",
                UserId = userId,
                Name = "HDFC Bank",
                Type = "Bank Account",
                OpeningBalance = 25000,
                CurrentBalance = 56500,
                InstitutionName = "HDFC Bank",
                LastUpdatedAt = DateTime.UtcNow
            };

            var userWallet = new Account
            {
                Id = "acc-user-wallet-001",
                UserId = userId,
                Name = "Cash Wallet",
                Type = "Cash Wallet",
                OpeningBalance = 2000,
                CurrentBalance = 1530,
                InstitutionName = "Self",
                LastUpdatedAt = DateTime.UtcNow
            };

            var adminBank = new Account
            {
                Id = "acc-admin-bank-001",
                UserId = adminId,
                Name = "ICICI Primary",
                Type = "Bank Account",
                OpeningBalance = 100000,
                CurrentBalance = 118000,
                InstitutionName = "ICICI",
                LastUpdatedAt = DateTime.UtcNow
            };

            var adminCard = new Account
            {
                Id = "acc-admin-card-001",
                UserId = adminId,
                Name = "ICICI Rewards Card",
                Type = "Credit Card",
                OpeningBalance = 0,
                CurrentBalance = 12500,
                CreditLimit = 50000,
                InstitutionName = "ICICI",
                LastUpdatedAt = DateTime.UtcNow
            };

            var transactions = new[]
            {
                new TransactionRecord
                {
                    Id = "txn-user-001",
                    UserId = userId,
                    AccountId = userBank.Id,
                    Type = "income",
                    Amount = 50000,
                    Date = new DateTime(2026, 3, 1),
                    Category = "Salary",
                    Merchant = "Employer Inc",
                    Note = "Monthly salary",
                    PaymentMethod = "Bank Transfer",
                    Tags = ["salary"],
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new TransactionRecord
                {
                    Id = "txn-user-002",
                    UserId = userId,
                    AccountId = userBank.Id,
                    Type = "expense",
                    Amount = 2450,
                    Date = new DateTime(2026, 3, 5),
                    Category = "Food",
                    Merchant = "Grocery Mart",
                    Note = "Weekly groceries",
                    PaymentMethod = "UPI",
                    Tags = ["family", "weekly"],
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new TransactionRecord
                {
                    Id = "txn-user-003",
                    UserId = userId,
                    AccountId = userWallet.Id,
                    Type = "expense",
                    Amount = 470,
                    Date = new DateTime(2026, 3, 8),
                    Category = "Transport",
                    Merchant = "Auto Fare",
                    Note = "Local commute",
                    PaymentMethod = "Cash",
                    Tags = ["travel"],
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new TransactionRecord
                {
                    Id = "txn-admin-001",
                    UserId = adminId,
                    AccountId = adminBank.Id,
                    Type = "income",
                    Amount = 180000,
                    Date = new DateTime(2026, 3, 1),
                    Category = "Salary",
                    Merchant = "Admin Corp",
                    Note = "Admin salary",
                    PaymentMethod = "Bank Transfer",
                    Tags = ["salary"],
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new TransactionRecord
                {
                    Id = "txn-admin-002",
                    UserId = adminId,
                    AccountId = adminBank.Id,
                    Type = "expense",
                    Amount = 62000,
                    Date = new DateTime(2026, 3, 3),
                    Category = "Operations",
                    Merchant = "Office Vendor",
                    Note = "Monthly spend",
                    PaymentMethod = "Card",
                    Tags = ["ops"],
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new TransactionRecord
                {
                    Id = "txn-admin-003",
                    UserId = adminId,
                    AccountId = adminCard.Id,
                    Type = "expense",
                    Amount = 12500,
                    Date = new DateTime(2026, 3, 10),
                    Category = "Shopping",
                    Merchant = "Electronics Store",
                    Note = "Credit card purchase",
                    PaymentMethod = "Credit Card",
                    Tags = ["card"],
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            var goals = new[]
            {
                new Goal
                {
                    Id = "goal-user-001",
                    UserId = userId,
                    Name = "Emergency Fund",
                    TargetAmount = 100000,
                    CurrentAmount = 45000,
                    TargetDate = UtcDate(2026, 12, 31),
                    LinkedAccountId = userBank.Id,
                    Icon = "shield",
                    Color = "#2563EB",
                    Status = "active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Goal
                {
                    Id = "goal-admin-001",
                    UserId = adminId,
                    Name = "Team Offsite",
                    TargetAmount = 200000,
                    CurrentAmount = 120000,
                    TargetDate = UtcDate(2026, 10, 1),
                    LinkedAccountId = adminBank.Id,
                    Icon = "plane",
                    Color = "#10B981",
                    Status = "active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await _dbContext.Users.AddRangeAsync(users, cancellationToken);
            await _dbContext.Accounts.AddRangeAsync([userBank, userWallet, adminBank, adminCard], cancellationToken);
            await _dbContext.Goals.AddRangeAsync(goals, cancellationToken);
            await _dbContext.Transactions.AddRangeAsync(transactions, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var usersInDb = await _dbContext.Users.ToListAsync(cancellationToken);
        foreach (var user in usersInDb)
        {
            await SyncDefaultCategoriesAsync(user.Id, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await SyncGoalCategoriesAsync(cancellationToken);

        var allCategories = await _dbContext.Categories.ToListAsync(cancellationToken);
        var transactionsWithoutCategoryId = await _dbContext.Transactions
            .Where(x => x.CategoryId == null && x.Category != null)
            .ToListAsync(cancellationToken);

        foreach (var transaction in transactionsWithoutCategoryId)
        {
            var matchingCategory = allCategories.FirstOrDefault(x =>
                x.UserId == transaction.UserId &&
                x.Type == transaction.Type &&
                x.Name.ToLower() == transaction.Category!.ToLower());

            if (matchingCategory is null)
            {
                matchingCategory = new Category
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = transaction.UserId,
                    Name = transaction.Category!,
                    Type = transaction.Type,
                    Color = transaction.Type == "income" ? "#22C55E" : "#64748B",
                    Icon = transaction.Type == "income" ? "wallet" : "folder",
                    IsArchived = false,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Categories.Add(matchingCategory);
                allCategories.Add(matchingCategory);
            }

            transaction.CategoryId = matchingCategory.Id;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await SeedRecurringItemsAsync(userId, adminId, cancellationToken);
    }

    private async Task SyncGoalCategoriesAsync(CancellationToken cancellationToken)
    {
        var goalsWithoutCategory = await _dbContext.Goals
            .Where(x => x.CategoryId == null)
            .ToListAsync(cancellationToken);

        if (goalsWithoutCategory.Count == 0)
            return;

        var categories = await _dbContext.Categories
            .Where(x => x.Type == "expense" && !x.IsArchived)
            .ToListAsync(cancellationToken);

        foreach (var goal in goalsWithoutCategory)
        {
            var suggestedCategoryName = InferGoalCategoryName(goal.Name);
            if (suggestedCategoryName is null)
                continue;

            var category = categories.FirstOrDefault(x =>
                x.UserId == goal.UserId &&
                x.Name.Equals(suggestedCategoryName, StringComparison.OrdinalIgnoreCase));

            if (category is not null)
            {
                goal.CategoryId = category.Id;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SyncDefaultCategoriesAsync(string userId, CancellationToken cancellationToken)
    {
        var categories = await _dbContext.Categories
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        if (categories.Count == 0)
        {
            await _dbContext.Categories.AddRangeAsync(CategoryDefaults.BuildForUser(userId), cancellationToken);
            return;
        }

        var missingDefaults = CategoryDefaults.BuildForUser(userId)
            .Where(defaultCategory => !categories.Any(existing =>
                existing.Type.Equals(defaultCategory.Type, StringComparison.OrdinalIgnoreCase) &&
                existing.Name.Equals(defaultCategory.Name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (missingDefaults.Count > 0)
        {
            await _dbContext.Categories.AddRangeAsync(missingDefaults, cancellationToken);
        }
    }

    private async Task SeedRecurringItemsAsync(string userId, string adminId, CancellationToken cancellationToken)
    {
        var hasUserRecurring = await _dbContext.RecurringTransactions.AnyAsync(x => x.UserId == userId, cancellationToken);
        if (!hasUserRecurring)
        {
            var userBank = await _dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == "acc-user-bank-001", cancellationToken);
            if (userBank is not null)
            {
                var rentCategory = await FindCategoryAsync(userId, "Rent", "expense", cancellationToken);
                var subscriptionsCategory = await FindCategoryAsync(userId, "Subscriptions", "expense", cancellationToken);

                if (rentCategory is not null && subscriptionsCategory is not null)
                {
                    await _dbContext.RecurringTransactions.AddRangeAsync(
                    [
                        new RecurringTransaction
                        {
                            Id = "rec-user-001",
                            UserId = userId,
                            Title = "House Rent",
                            Type = "expense",
                            Amount = 18000,
                            CategoryId = rentCategory.Id,
                            AccountId = userBank.Id,
                            Frequency = "monthly",
                            StartDate = UtcDate(DateTime.UtcNow.AddDays(-15)),
                            NextRunDate = UtcDate(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 25),
                            AutoCreateTransaction = false,
                            IsPaused = false,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        },
                        new RecurringTransaction
                        {
                            Id = "rec-user-002",
                            UserId = userId,
                            Title = "Netflix",
                            Type = "expense",
                            Amount = 649,
                            CategoryId = subscriptionsCategory.Id,
                            AccountId = userBank.Id,
                            Frequency = "monthly",
                            StartDate = UtcDate(DateTime.UtcNow.AddDays(-10)),
                            NextRunDate = UtcDate(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 20),
                            AutoCreateTransaction = false,
                            IsPaused = false,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        },
                        new RecurringTransaction
                        {
                            Id = "rec-user-003",
                            UserId = userId,
                            Title = "Spotify",
                            Type = "expense",
                            Amount = 119,
                            CategoryId = subscriptionsCategory.Id,
                            AccountId = userBank.Id,
                            Frequency = "monthly",
                            StartDate = UtcDate(DateTime.UtcNow.AddDays(-8)),
                            NextRunDate = UtcDate(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 27),
                            AutoCreateTransaction = false,
                            IsPaused = false,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        }
                    ], cancellationToken);
                }
            }
        }

        var hasAdminRecurring = await _dbContext.RecurringTransactions.AnyAsync(x => x.UserId == adminId, cancellationToken);
        if (!hasAdminRecurring)
        {
            var adminBank = await _dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == "acc-admin-bank-001", cancellationToken);
            if (adminBank is not null)
            {
                var salaryCategory = await FindCategoryAsync(adminId, "Salary", "income", cancellationToken);
                if (salaryCategory is not null)
                {
                    await _dbContext.RecurringTransactions.AddAsync(new RecurringTransaction
                    {
                        Id = "rec-admin-001",
                        UserId = adminId,
                        Title = "Admin Salary",
                        Type = "income",
                        Amount = 180000,
                        CategoryId = salaryCategory.Id,
                        AccountId = adminBank.Id,
                        Frequency = "monthly",
                        StartDate = UtcDate(DateTime.UtcNow.AddDays(-20)),
                        NextRunDate = UtcDate(new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1)),
                        AutoCreateTransaction = true,
                        IsPaused = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }, cancellationToken);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task<Category?> FindCategoryAsync(string userId, string name, string type, CancellationToken cancellationToken)
    {
        return _dbContext.Categories.FirstOrDefaultAsync(x =>
            x.UserId == userId &&
            x.Type == type &&
            x.Name.ToLower() == name.ToLower(),
            cancellationToken);
    }

    private static string? InferGoalCategoryName(string goalName)
    {
        var normalizedName = goalName.Trim().ToLowerInvariant();

        if (normalizedName.Contains("medical") || normalizedName.Contains("health"))
            return "Health";

        if (normalizedName.Contains("travel") || normalizedName.Contains("trip") || normalizedName.Contains("vacation") || normalizedName.Contains("offsite"))
            return "Travel";

        if (normalizedName.Contains("education") || normalizedName.Contains("study") || normalizedName.Contains("college"))
            return "Education";

        if (normalizedName.Contains("shopping"))
            return "Shopping";

        if (normalizedName.Contains("emergency"))
            return "Miscellaneous";

        return null;
    }

    private static DateTime UtcDate(int year, int month, int day)
    {
        return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
    }

    private static DateTime UtcDate(DateTime value)
    {
        return new DateTime(value.Year, value.Month, value.Day, 0, 0, 0, DateTimeKind.Utc);
    }
}
