namespace IRM.Domain;

/// <summary>Zutat aus der globalen, user-unabhängigen Zutatenliste.</summary>
public sealed class Ingredient
{
    public IngredientId Id { get; private set; }
    public string Name { get; private set; } = null!;

    private Ingredient() { } // für EF Core

    public static Ingredient Create(string name)
        => new() { Name = Guard.NotNullOrWhiteSpace(name, nameof(name)) };

    /// <summary>Benennt die Zutat um (z.B. Tippfehler-Korrektur). Die Id bleibt erhalten.</summary>
    public void Rename(string newName)
        => Name = Guard.NotNullOrWhiteSpace(newName, nameof(newName));
}
