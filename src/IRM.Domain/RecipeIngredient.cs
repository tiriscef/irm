namespace IRM.Domain;

/// <summary>
/// Verknüpft ein Rezept mit einer Zutat der globalen Liste inkl. Menge und Einheit (Value Object).
/// Die Einheit ist bewusst frei ("g", "ml", "Prise", "nach Geschmack").
/// </summary>
public sealed record RecipeIngredient
{
    public IngredientId IngredientId { get; }
    public decimal Amount { get; }
    public string Unit { get; }

    public RecipeIngredient(IngredientId ingredientId, decimal amount, string unit)
    {
        if (amount <= 0)
            throw new DomainValidationException("Die Menge einer Zutat muss größer als 0 sein.");
        IngredientId = ingredientId;
        Amount = amount;
        Unit = Guard.NotNullOrWhiteSpace(unit, nameof(unit));
    }
}
