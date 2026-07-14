using Microsoft.EntityFrameworkCore;

namespace IRM.Persistence.Services;

internal sealed class IngredientService : IIngredientService
{
    private readonly RecipeDbContext _db;

    public IngredientService(RecipeDbContext db) => _db = db;

    public async Task<IngredientDto> AddAsync(string name, CancellationToken ct = default)
    {
        var ingredient = Ingredient.Create(name);
        var duplicate = $"Eine Zutat mit dem Namen '{ingredient.Name}' existiert bereits.";
        if (await _db.Ingredients.AnyAsync(i => i.Name == ingredient.Name, ct))
            throw new DuplicateNameException(duplicate);

        _db.Ingredients.Add(ingredient);
        await _db.SaveTranslatingDuplicateAsync(duplicate, ct);
        return new IngredientDto(ingredient.Id, ingredient.Name);
    }

    public async Task<IngredientDto> RenameAsync(IngredientId id, string newName, CancellationToken ct = default)
    {
        var ingredient = await _db.Ingredients.FindAsync([id], ct)
            ?? throw new IngredientNotFoundException($"Zutat {id.Value} wurde nicht gefunden.");

        ingredient.Rename(newName);
        var duplicate = $"Eine Zutat mit dem Namen '{ingredient.Name}' existiert bereits.";
        if (await _db.Ingredients.AnyAsync(i => i.Id != id && i.Name == ingredient.Name, ct))
            throw new DuplicateNameException(duplicate);

        await _db.SaveTranslatingDuplicateAsync(duplicate, ct);
        return new IngredientDto(ingredient.Id, ingredient.Name);
    }

    public async Task DeleteAsync(IngredientId id, CancellationToken ct = default)
    {
        var ingredient = await _db.Ingredients.FindAsync([id], ct)
            ?? throw new IngredientNotFoundException($"Zutat {id.Value} wurde nicht gefunden.");

        if (await _db.Recipes.AnyAsync(r => r.Ingredients.Any(i => i.IngredientId == id), ct))
            throw new InUseException($"Die Zutat '{ingredient.Name}' wird noch von mindestens einem Rezept verwendet.");

        _db.Ingredients.Remove(ingredient);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<IngredientDto>> ListAsync(CancellationToken ct = default)
        => await _db.Ingredients
            .OrderBy(i => i.Name)
            .Select(i => new IngredientDto(i.Id, i.Name))
            .ToListAsync(ct);
}
