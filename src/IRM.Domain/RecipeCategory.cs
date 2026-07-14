namespace IRM.Domain;

/// <summary>
/// Verknüpfung eines Rezepts mit einer Kategorie – Teil des Recipe-Aggregats (composition).
/// Existiert nur, damit die als Value-Struct modellierte <see cref="CategoryId"/> relational
/// als eigene Tabelle mit Fremdschlüssel persistiert werden kann. Nach außen bleibt allein
/// <see cref="Recipe.CategoryIds"/> sichtbar; dieser Typ ist bewusst nicht Teil der API.
/// </summary>
internal sealed class RecipeCategory
{
    public CategoryId CategoryId { get; private set; }

    private RecipeCategory() { } // für EF Core

    public RecipeCategory(CategoryId categoryId) => CategoryId = categoryId;
}
