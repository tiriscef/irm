namespace IRM.Domain;

/// <summary>Basis aller fachlichen (domänenspezifischen) Fehler der Bibliothek.</summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

/// <summary>
/// Eine Domänen-Invariante wurde verletzt, z.B. der Versuch, ein Rezept ohne Zutat anzulegen.
/// Wird direkt im Domain-Model geworfen, damit ungültige Zustände gar nicht erst entstehen.
/// </summary>
public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string message) : base(message) { }
}

/// <summary>Ein Name, der eindeutig sein muss (Rezept, Kategorie, Zutat), existiert bereits.</summary>
public sealed class DuplicateNameException : DomainException
{
    public DuplicateNameException(string message) : base(message) { }
}

/// <summary>Ein Benutzer versucht, ein fremdes Rezept zu ändern oder zu löschen.</summary>
public sealed class UnauthorizedRecipeAccessException : DomainException
{
    public UnauthorizedRecipeAccessException(string message) : base(message) { }
}

/// <summary>
/// Eine Entity soll gelöscht werden, wird aber noch verwendet: eine Kategorie/Zutat
/// von einem Rezept referenziert, oder ein Benutzer besitzt noch Rezepte
/// (symmetrische "block if in-use"-Regel).
/// </summary>
public sealed class InUseException : DomainException
{
    public InUseException(string message) : base(message) { }
}

/// <summary>Basis für "Entity nicht gefunden"; erlaubt das gemeinsame Fangen aller NotFound-Fälle.</summary>
public abstract class NotFoundException : DomainException
{
    protected NotFoundException(string message) : base(message) { }
}

/// <summary>Der referenzierte Benutzer existiert nicht.</summary>
public sealed class UserNotFoundException : NotFoundException
{
    public UserNotFoundException(string message) : base(message) { }
}

/// <summary>Das referenzierte Rezept existiert nicht.</summary>
public sealed class RecipeNotFoundException : NotFoundException
{
    public RecipeNotFoundException(string message) : base(message) { }
}

/// <summary>Die referenzierte Kategorie existiert nicht.</summary>
public sealed class CategoryNotFoundException : NotFoundException
{
    public CategoryNotFoundException(string message) : base(message) { }
}

/// <summary>Die referenzierte Zutat existiert nicht.</summary>
public sealed class IngredientNotFoundException : NotFoundException
{
    public IngredientNotFoundException(string message) : base(message) { }
}
