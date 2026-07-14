using Microsoft.EntityFrameworkCore;

namespace IRM.Persistence;

/// <summary>
/// Baut <see cref="RecipeDto"/>s aus Rezept-Aggregaten. Da ein Rezept Zutaten und Kategorien
/// nur per Id referenziert (Aggregat-Grenze), werden deren Namen separat nachgeladen und
/// über Lookups eingesetzt – statt EF-Navigationen über fremde Aggregate hinweg.
/// </summary>
internal static class RecipeMapper
{
    public static async Task<RecipeDto> ToDtoAsync(RecipeDbContext db, Recipe recipe, CancellationToken ct)
        => (await ToDtosAsync(db, new[] { recipe }, ct))[0];

    public static async Task<IReadOnlyList<RecipeDto>> ToDtosAsync(
        RecipeDbContext db, IReadOnlyList<Recipe> recipes, CancellationToken ct)
    {
        var ingredientIds = recipes.SelectMany(r => r.Ingredients.Select(i => i.IngredientId)).Distinct().ToList();
        var categoryIds = recipes.SelectMany(r => r.CategoryIds).Distinct().ToList();

        var ingredientNames = await db.Ingredients
            .AsNoTracking()
            .Where(i => ingredientIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.Name, ct);
        var categoryNames = await db.Categories
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        return recipes.Select(r => Map(r, ingredientNames, categoryNames)).ToList();
    }

    private static RecipeDto Map(
        Recipe recipe,
        IReadOnlyDictionary<IngredientId, string> ingredientNames,
        IReadOnlyDictionary<CategoryId, string> categoryNames)
        => new(
            recipe.Id,
            recipe.Name,
            recipe.OwnerId,
            recipe.Servings,
            recipe.Steps
                .OrderBy(s => s.Order)
                .Select(s => new StepDto(s.Order, s.Instruction))
                .ToList(),
            recipe.Ingredients
                .Select(i => new RecipeIngredientDto(
                    new IngredientDto(i.IngredientId, ingredientNames[i.IngredientId]), i.Amount, i.Unit))
                .ToList(),
            recipe.CategoryIds
                .Select(c => new CategoryDto(c, categoryNames[c]))
                .ToList());
}
