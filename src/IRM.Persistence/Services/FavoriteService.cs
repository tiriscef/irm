using Microsoft.EntityFrameworkCore;

namespace IRM.Persistence.Services;

internal sealed class FavoriteService : IFavoriteService
{
    private readonly RecipeDbContext _db;

    public FavoriteService(RecipeDbContext db) => _db = db;

    public async Task AddAsync(UserId userId, RecipeId recipeId, CancellationToken ct = default)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == userId, ct))
            throw new UserNotFoundException($"Benutzer {userId.Value} wurde nicht gefunden.");
        if (!await _db.Recipes.AnyAsync(r => r.Id == recipeId, ct))
            throw new RecipeNotFoundException($"Rezept {recipeId.Value} wurde nicht gefunden.");

        var exists = await _db.Favorites.AnyAsync(f => f.UserId == userId && f.RecipeId == recipeId, ct);
        if (exists)
            return; // bereits Favorit – idempotent

        _db.Favorites.Add(Favorite.Create(userId, recipeId));
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(UserId userId, RecipeId recipeId, CancellationToken ct = default)
    {
        var favorite = await _db.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.RecipeId == recipeId, ct);
        if (favorite is null)
            return; // kein Favorit – idempotent

        _db.Favorites.Remove(favorite);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<RecipeDto>> GetFavoritesAsync(UserId userId, CancellationToken ct = default)
    {
        var recipeIds = _db.Favorites.Where(f => f.UserId == userId).Select(f => f.RecipeId);
        var recipes = await _db.Recipes.AsNoTracking().Where(r => recipeIds.Contains(r.Id)).ToListAsync(ct);
        return await RecipeMapper.ToDtosAsync(_db, recipes, ct);
    }
}
