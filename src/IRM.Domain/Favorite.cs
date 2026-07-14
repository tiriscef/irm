namespace IRM.Domain;

/// <summary>
/// Markiert ein Rezept als Favorit eines Benutzers (Zuordnung User ─*──*─ Recipe).
/// Eigenes Aggregat, referenziert User und Recipe nur per Id.
/// </summary>
public sealed class Favorite
{
    public UserId UserId { get; private set; }
    public RecipeId RecipeId { get; private set; }

    private Favorite() { } // für EF Core

    public static Favorite Create(UserId userId, RecipeId recipeId)
        => new() { UserId = userId, RecipeId = recipeId };
}
