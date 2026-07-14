namespace IRM.Application;

// Input-Modelle. Tragen keine Ids für zu erzeugende Bestandteile:
// die Schritt-Reihenfolge ergibt sich aus der Position in der Liste.

public record RecipeIngredientInput(IngredientId IngredientId, decimal Amount, string Unit);

public record CreateRecipeRequest(
    string Name,
    int Servings,
    IReadOnlyList<string> Steps,
    IReadOnlyList<RecipeIngredientInput> Ingredients,
    IReadOnlyList<CategoryId> CategoryIds);

/// <summary>Ersetzt Name/Servings/Steps/Ingredients/Categories eines Rezepts vollständig.</summary>
public record UpdateRecipeRequest(
    string Name,
    int Servings,
    IReadOnlyList<string> Steps,
    IReadOnlyList<RecipeIngredientInput> Ingredients,
    IReadOnlyList<CategoryId> CategoryIds);
