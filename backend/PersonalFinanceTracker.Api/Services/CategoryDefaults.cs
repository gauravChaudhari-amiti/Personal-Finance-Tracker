using PersonalFinanceTracker.Api.Entities;

namespace PersonalFinanceTracker.Api.Services;

public static class CategoryDefaults
{
    public const string WholeMonthCategoryName = "Whole Month";

    private static readonly (string Name, string Type, string Color, string Icon)[] Items =
    [
        (WholeMonthCategoryName, "expense", "#2563EB", "calendar"),
        ("Food", "expense", "#F97316", "utensils"),
        ("Rent", "expense", "#0EA5E9", "home"),
        ("Utilities", "expense", "#EAB308", "bolt"),
        ("Transport", "expense", "#14B8A6", "car"),
        ("Entertainment", "expense", "#8B5CF6", "film"),
        ("Shopping", "expense", "#EC4899", "shopping-bag"),
        ("Health", "expense", "#EF4444", "heart"),
        ("Education", "expense", "#6366F1", "book-open"),
        ("Travel", "expense", "#06B6D4", "plane"),
        ("Subscriptions", "expense", "#64748B", "repeat"),
        ("Miscellaneous", "expense", "#94A3B8", "folder"),
        ("Salary", "income", "#22C55E", "wallet"),
        ("Freelance", "income", "#10B981", "briefcase"),
        ("Bonus", "income", "#84CC16", "sparkles"),
        ("Investment", "income", "#059669", "trending-up"),
        ("Gift", "income", "#F59E0B", "gift"),
        ("Refund", "income", "#06B6D4", "rotate-ccw"),
        ("Other", "income", "#3B82F6", "circle")
    ];

    private static readonly HashSet<string> CurrentDefaultKeys = Items
        .Select(item => BuildKey(item.Type, item.Name))
        .ToHashSet();

    public static List<Category> BuildForUser(string userId)
    {
        return Items.Select(item => new Category
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Name = item.Name,
            Type = item.Type,
            Color = item.Color,
            Icon = item.Icon,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow
        }).ToList();
    }

    public static bool IsCurrentDefault(string type, string name)
    {
        return CurrentDefaultKeys.Contains(BuildKey(type, name));
    }

    public static bool IsWholeMonthCategory(string type, string name)
    {
        return BuildKey(type, name) == BuildKey("expense", WholeMonthCategoryName);
    }

    private static string BuildKey(string type, string name)
    {
        return $"{type.Trim().ToLowerInvariant()}::{name.Trim().ToLowerInvariant()}";
    }
}
