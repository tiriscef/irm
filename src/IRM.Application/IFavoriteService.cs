namespace IRM.Application;

/// <summary>Verwaltet die Favoriten eines Benutzers (eigener Service = Single Responsibility).</summary>
public interface IFavoriteService
{
    Task AddAsync(UserId userId, RecipeId recipeId, CancellationToken ct = default);
    Task RemoveAsync(UserId userId, RecipeId recipeId, CancellationToken ct = default);
    Task<IReadOnlyList<RecipeDto>> GetFavoritesAsync(UserId userId, CancellationToken ct = default);
}
