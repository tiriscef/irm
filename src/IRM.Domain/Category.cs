namespace IRM.Domain;

/// <summary>Kategorie, der Rezepte zugeordnet werden. Namen sind global eindeutig (Prüfung im Service).</summary>
public sealed class Category
{
    public CategoryId Id { get; private set; }
    public string Name { get; private set; } = null!;

    private Category() { } // für EF Core

    public static Category Create(string name)
        => new() { Name = Guard.NotNullOrWhiteSpace(name, nameof(name)) };

    public void Rename(string newName)
        => Name = Guard.NotNullOrWhiteSpace(newName, nameof(newName));
}
