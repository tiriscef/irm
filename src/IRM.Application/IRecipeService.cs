namespace IRM.Application;

/// <summary>
/// Erstellen, Ändern, Löschen und Abfragen von Rezepten. Der handelnde Benutzer wird
/// explizit übergeben; nur der Owner darf sein Rezept ändern oder löschen.
/// </summary>
public interface IRecipeService
{
    Task<RecipeDto> CreateAsync(UserId actor, CreateRecipeRequest request, CancellationToken ct = default);
    Task<RecipeDto> UpdateAsync(UserId actor, RecipeId id, UpdateRecipeRequest request, CancellationToken ct = default);
    Task DeleteAsync(UserId actor, RecipeId id, CancellationToken ct = default);
    Task<RecipeDto?> GetAsync(RecipeId id, CancellationToken ct = default);

    Task<IReadOnlyList<RecipeDto>> GetByUserAsync(UserId userId, CancellationToken ct = default);
    Task<IReadOnlyList<RecipeDto>> GetByCategoryAsync(CategoryId categoryId, CancellationToken ct = default);
    Task<IReadOnlyList<RecipeDto>> GetByIngredientAsync(IngredientId ingredientId, CancellationToken ct = default);
}
