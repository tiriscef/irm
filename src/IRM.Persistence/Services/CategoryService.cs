using Microsoft.EntityFrameworkCore;

namespace IRM.Persistence.Services;

internal sealed class CategoryService : ICategoryService
{
    private readonly RecipeDbContext _db;

    public CategoryService(RecipeDbContext db) => _db = db;

    public async Task<CategoryDto> CreateAsync(string name, CancellationToken ct = default)
    {
        var category = Category.Create(name);
        var duplicate = $"Eine Kategorie mit dem Namen '{category.Name}' existiert bereits.";
        if (await _db.Categories.AnyAsync(c => c.Name == category.Name, ct))
            throw new DuplicateNameException(duplicate);

        _db.Categories.Add(category);
        await _db.SaveTranslatingDuplicateAsync(duplicate, ct);
        return new CategoryDto(category.Id, category.Name);
    }

    public async Task<CategoryDto> RenameAsync(CategoryId id, string newName, CancellationToken ct = default)
    {
        var category = await _db.Categories.FindAsync([id], ct)
            ?? throw new CategoryNotFoundException($"Kategorie {id.Value} wurde nicht gefunden.");

        category.Rename(newName);
        var duplicate = $"Eine Kategorie mit dem Namen '{category.Name}' existiert bereits.";
        if (await _db.Categories.AnyAsync(c => c.Id != id && c.Name == category.Name, ct))
            throw new DuplicateNameException(duplicate);

        await _db.SaveTranslatingDuplicateAsync(duplicate, ct);
        return new CategoryDto(category.Id, category.Name);
    }

    public async Task DeleteAsync(CategoryId id, CancellationToken ct = default)
    {
        var category = await _db.Categories.FindAsync([id], ct)
            ?? throw new CategoryNotFoundException($"Kategorie {id.Value} wurde nicht gefunden.");

        if (await _db.Recipes.AnyAsync(r => r.Categories.Any(c => c.CategoryId == id), ct))
            throw new InUseException($"Die Kategorie '{category.Name}' wird noch von mindestens einem Rezept verwendet.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken ct = default)
        => await _db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name))
            .ToListAsync(ct);
}
