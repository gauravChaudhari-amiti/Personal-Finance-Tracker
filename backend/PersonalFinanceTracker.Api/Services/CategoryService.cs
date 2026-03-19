using PersonalFinanceTracker.Api.Data;
using PersonalFinanceTracker.Api.DTOs;
using PersonalFinanceTracker.Api.Entities;

namespace PersonalFinanceTracker.Api.Services;

public interface ICategoryService
{
    List<CategoryDto> GetAll(string userId, string? type, bool includeArchived);
    CategoryDto Create(string userId, CreateCategoryRequestDto request);
    CategoryDto? Update(string userId, string id, UpdateCategoryRequestDto request);
    CategoryDto? Archive(string userId, string id, bool isArchived);
}

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _dbContext;

    public CategoryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<CategoryDto> GetAll(string userId, string? type, bool includeArchived)
    {
        var query = _dbContext.Categories.Where(x => x.UserId == userId);

        if (!string.IsNullOrWhiteSpace(type))
        {
            var normalizedType = NormalizeType(type);
            query = query.Where(x => x.Type == normalizedType);
        }

        if (!includeArchived)
        {
            query = query.Where(x => !x.IsArchived);
        }

        return query
            .OrderBy(x => x.Type)
            .ThenBy(x => x.Name)
            .Select(x => Map(x))
            .AsEnumerable()
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .ToList();
    }

    public CategoryDto Create(string userId, CreateCategoryRequestDto request)
    {
        var normalizedType = NormalizeType(request.Type);
        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required.");

        if (CategoryDefaults.IsWholeMonthCategory(normalizedType, name))
            throw new InvalidOperationException("Whole Month is a system budget category and cannot be created manually.");

        var exists = _dbContext.Categories.Any(x =>
            x.UserId == userId &&
            x.Type == normalizedType &&
            x.Name.ToLower() == name.ToLower());

        if (exists)
            throw new InvalidOperationException("A category with this name already exists for this type.");

        var category = new Category
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Name = name,
            Type = normalizedType,
            Color = NormalizeOptional(request.Color),
            Icon = NormalizeOptional(request.Icon),
            IsArchived = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Categories.Add(category);
        _dbContext.SaveChanges();

        return Map(category);
    }

    public CategoryDto? Update(string userId, string id, UpdateCategoryRequestDto request)
    {
        var category = _dbContext.Categories.FirstOrDefault(x => x.UserId == userId && x.Id == id);
        if (category is null)
            return null;

        var normalizedType = NormalizeType(request.Type);
        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required.");

        if (CategoryDefaults.IsWholeMonthCategory(category.Type, category.Name))
            throw new InvalidOperationException("Whole Month is a system budget category and cannot be edited.");

        if (CategoryDefaults.IsWholeMonthCategory(normalizedType, name))
            throw new InvalidOperationException("Whole Month is a system budget category and cannot be renamed.");

        var exists = _dbContext.Categories.Any(x =>
            x.UserId == userId &&
            x.Id != id &&
            x.Type == normalizedType &&
            x.Name.ToLower() == name.ToLower());

        if (exists)
            throw new InvalidOperationException("A category with this name already exists for this type.");

        category.Name = name;
        category.Type = normalizedType;
        category.Color = NormalizeOptional(request.Color);
        category.Icon = NormalizeOptional(request.Icon);

        _dbContext.SaveChanges();
        return Map(category);
    }

    public CategoryDto? Archive(string userId, string id, bool isArchived)
    {
        var category = _dbContext.Categories.FirstOrDefault(x => x.UserId == userId && x.Id == id);
        if (category is null)
            return null;

        if (CategoryDefaults.IsWholeMonthCategory(category.Type, category.Name))
            throw new InvalidOperationException("Whole Month is a system budget category and cannot be archived.");

        category.IsArchived = isArchived;
        _dbContext.SaveChanges();
        return Map(category);
    }

    private static string NormalizeType(string type)
    {
        var normalizedType = type.Trim().ToLowerInvariant();
        if (normalizedType is not ("income" or "expense"))
            throw new ArgumentException("Category type must be income or expense.");

        return normalizedType;
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static CategoryDto Map(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            UserId = category.UserId,
            Name = category.Name.Trim(),
            Type = category.Type,
            Color = category.Color,
            Icon = category.Icon,
            IsArchived = category.IsArchived
        };
    }
}
