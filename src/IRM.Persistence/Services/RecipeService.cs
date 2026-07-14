using Microsoft.EntityFrameworkCore;

namespace IRM.Persistence.Services;

internal sealed class RecipeService : IRecipeService
{
    private readonly RecipeDbContext _db;

    public RecipeService(RecipeDbContext db) => _db = db;

    public async Task<RecipeDto> CreateAsync(UserId actor, CreateRecipeRequest request, CancellationToken ct = default)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == actor, ct))
            throw new UserNotFoundException($"Benutzer {actor.Value} wurde nicht gefunden.");

        var recipe = Recipe.Create(actor, request.Name, request.Servings, request.Steps, ToRecipeIngredients(request.Ingredients), request.CategoryIds); // validiert Struktur-Invarianten

        await EnsureNameAvailableAsync(request.Name, exceptId: null, ct);
        await EnsureReferencesExistAsync(request.CategoryIds, request.Ingredients, ct);

        _db.Recipes.Add(recipe);
        await _db.SaveTranslatingDuplicateAsync($"Ein Rezept mit dem Namen '{recipe.Name}' existiert bereits.", ct);
        return await RecipeMapper.ToDtoAsync(_db, recipe, ct);
    }

    public async Task<RecipeDto> UpdateAsync(UserId actor, RecipeId id, UpdateRecipeRequest request, CancellationToken ct = default)
    {
        var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new RecipeNotFoundException($"Rezept {id.Value} wurde nicht gefunden.");

        if (recipe.OwnerId != actor)
            throw new UnauthorizedRecipeAccessException("Nur der Ersteller darf dieses Rezept ändern.");

        // Service-Prüfungen vor der Mutation: schlägt eine fehl, bleibt das geladene Aggregat unberührt.
        await EnsureReferencesExistAsync(request.CategoryIds, request.Ingredients, ct);
        await EnsureNameAvailableAsync(request.Name, exceptId: id, ct);

        recipe.Update(request.Name, request.Servings, request.Steps, ToRecipeIngredients(request.Ingredients), request.CategoryIds);
        await _db.SaveTranslatingDuplicateAsync($"Ein Rezept mit dem Namen '{recipe.Name}' existiert bereits.", ct);
        return await RecipeMapper.ToDtoAsync(_db, recipe, ct);
    }

    public async Task DeleteAsync(UserId actor, RecipeId id, CancellationToken ct = default)
    {
        var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new RecipeNotFoundException($"Rezept {id.Value} wurde nicht gefunden.");

        if (recipe.OwnerId != actor)
            throw new UnauthorizedRecipeAccessException("Nur der Ersteller darf dieses Rezept löschen.");

        _db.Recipes.Remove(recipe);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<RecipeDto?> GetAsync(RecipeId id, CancellationToken ct = default)
    {
        var recipe = await _db.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
        return recipe is null ? null : await RecipeMapper.ToDtoAsync(_db, recipe, ct);
    }

    public async Task<IReadOnlyList<RecipeDto>> GetByUserAsync(UserId userId, CancellationToken ct = default)
    {
        var recipes = await _db.Recipes.AsNoTracking().Where(r => r.OwnerId == userId).ToListAsync(ct);
        return await RecipeMapper.ToDtosAsync(_db, recipes, ct);
    }

    public async Task<IReadOnlyList<RecipeDto>> GetByCategoryAsync(CategoryId categoryId, CancellationToken ct = default)
    {
        var recipes = await _db.Recipes.AsNoTracking().Where(r => r.Categories.Any(c => c.CategoryId == categoryId)).ToListAsync(ct);
        return await RecipeMapper.ToDtosAsync(_db, recipes, ct);
    }

    public async Task<IReadOnlyList<RecipeDto>> GetByIngredientAsync(IngredientId ingredientId, CancellationToken ct = default)
    {
        var recipes = await _db.Recipes.AsNoTracking().Where(r => r.Ingredients.Any(i => i.IngredientId == ingredientId)).ToListAsync(ct);
        return await RecipeMapper.ToDtosAsync(_db, recipes, ct);
    }

    private static List<RecipeIngredient> ToRecipeIngredients(IReadOnlyList<RecipeIngredientInput> inputs)
        => inputs.Select(i => new RecipeIngredient(i.IngredientId, i.Amount, i.Unit)).ToList();

    private async Task EnsureNameAvailableAsync(string name, RecipeId? exceptId, CancellationToken ct)
    {
        var normalized = name?.Trim();
        if (string.IsNullOrEmpty(normalized))
            return; // leerer Name wird von der Domain-Factory gemeldet

        var query = _db.Recipes.Where(r => r.Name == normalized);
        if (exceptId is RecipeId except)
            query = query.Where(r => r.Id != except);

        if (await query.AnyAsync(ct))
            throw new DuplicateNameException($"Ein Rezept mit dem Namen '{normalized}' existiert bereits.");
    }

    private async Task EnsureReferencesExistAsync(
        IReadOnlyList<CategoryId> categoryIds, IReadOnlyList<RecipeIngredientInput> ingredients, CancellationToken ct)
    {
        var wantedCategories = categoryIds.Distinct().ToList();
        var foundCategories = await _db.Categories.CountAsync(c => wantedCategories.Contains(c.Id), ct);
        if (foundCategories != wantedCategories.Count)
            throw new CategoryNotFoundException("Mindestens eine angegebene Kategorie existiert nicht.");

        var wantedIngredients = ingredients.Select(i => i.IngredientId).Distinct().ToList();
        var foundIngredients = await _db.Ingredients.CountAsync(i => wantedIngredients.Contains(i.Id), ct);
        if (foundIngredients != wantedIngredients.Count)
            throw new IngredientNotFoundException("Mindestens eine angegebene Zutat existiert nicht.");
    }
}
