namespace IRM.Domain;

/// <summary>
/// Aggregate Root der Rezeptverwaltung. Besitzt seine Schritte und Zutaten-Verknüpfungen
/// (composition); Kategorien und Zutaten werden nur per Id referenziert, nicht besessen.
/// Ungültige Rezepte lassen sich nicht konstruieren – alle Invarianten werden hier erzwungen.
/// </summary>
public sealed class Recipe
{
    private readonly List<Step> _steps = new();
    private readonly List<RecipeIngredient> _ingredients = new();
    private readonly List<RecipeCategory> _categories = new();

    public RecipeId Id { get; private set; }
    public string Name { get; private set; } = null!;
    public UserId OwnerId { get; private set; }
    public int Servings { get; private set; }

    public IReadOnlyList<Step> Steps => _steps;
    public IReadOnlyList<RecipeIngredient> Ingredients => _ingredients;
    public IReadOnlyList<CategoryId> CategoryIds => _categories.Select(c => c.CategoryId).ToList();

    // Interne Navigation für die Persistenz (relationale Join-Tabelle mit FK); nach außen
    // bleibt nur CategoryIds sichtbar. Siehe RecipeCategory.
    internal IReadOnlyList<RecipeCategory> Categories => _categories;

    private Recipe() { } // für EF Core

    /// <summary>Legt ein neues Rezept an. Der Owner steht danach fest und ändert sich nicht mehr.</summary>
    public static Recipe Create(
        UserId owner, string name, int servings,
        IReadOnlyList<string> stepInstructions,
        IReadOnlyList<RecipeIngredient> ingredients,
        IReadOnlyList<CategoryId> categoryIds)
    {
        var recipe = new Recipe { OwnerId = owner };
        recipe.SetContent(name, servings, stepInstructions, ingredients, categoryIds);
        return recipe;
    }

    /// <summary>Ersetzt die veränderlichen Inhalte des Rezepts vollständig (Owner bleibt unberührt).</summary>
    public void Update(
        string name, int servings,
        IReadOnlyList<string> stepInstructions,
        IReadOnlyList<RecipeIngredient> ingredients,
        IReadOnlyList<CategoryId> categoryIds)
        => SetContent(name, servings, stepInstructions, ingredients, categoryIds);

    private void SetContent(
        string name, int servings,
        IReadOnlyList<string> stepInstructions,
        IReadOnlyList<RecipeIngredient> ingredients,
        IReadOnlyList<CategoryId> categoryIds)
    {
        name = Guard.NotNullOrWhiteSpace(name, nameof(name));
        if (servings < 1)
            throw new DomainValidationException("Ein Rezept muss für mindestens eine Person ausgelegt sein.");
        if (stepInstructions is not { Count: > 0 })
            throw new DomainValidationException("Ein Rezept braucht mindestens einen Zubereitungsschritt.");
        if (ingredients is not { Count: > 0 })
            throw new DomainValidationException("Ein Rezept braucht mindestens eine Zutat.");
        if (categoryIds is not { Count: > 0 })
            throw new DomainValidationException("Ein Rezept muss mindestens einer Kategorie zugeordnet sein.");
        if (ingredients.Select(i => i.IngredientId).Distinct().Count() != ingredients.Count)
            throw new DomainValidationException("Eine Zutat darf in einem Rezept nur einmal vorkommen.");
        if (categoryIds.Distinct().Count() != categoryIds.Count)
            throw new DomainValidationException("Eine Kategorie darf einem Rezept nur einmal zugeordnet sein.");

        Name = name;
        Servings = servings;

        _steps.Clear();
        for (var i = 0; i < stepInstructions.Count; i++)
            _steps.Add(new Step(i + 1, stepInstructions[i]));

        _ingredients.Clear();
        _ingredients.AddRange(ingredients);

        _categories.Clear();
        _categories.AddRange(categoryIds.Select(id => new RecipeCategory(id)));
    }
}
