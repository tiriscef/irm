namespace IRM.Domain;

/// <summary>Registrierter Benutzer. Owner von Rezepten; Träger von Favoriten.</summary>
public sealed class User
{
    public UserId Id { get; private set; }
    public string Name { get; private set; } = null!;

    private User() { } // für EF Core

    /// <summary>
    /// Registriert einen neuen Benutzer. Die globale Eindeutigkeit des Namens
    /// ist nicht Sache des Domain-Models, sondern wird im Service geprüft.
    /// </summary>
    public static User Register(string name)
        => new() { Name = Guard.NotNullOrWhiteSpace(name, nameof(name)) };

    public void Rename(string newName)
        => Name = Guard.NotNullOrWhiteSpace(newName, nameof(newName));
}
