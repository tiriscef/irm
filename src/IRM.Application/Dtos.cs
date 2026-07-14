namespace IRM.Application;

// Read-Models der öffentlichen API. Entkoppeln den Contract von EF- und Domain-Details;
// bewusst reine Datensätze (records) ohne Verhalten.

public record UserDto(UserId Id, string Name);

public record IngredientDto(IngredientId Id, string Name);

public record CategoryDto(CategoryId Id, string Name);

public record StepDto(int Order, string Instruction);

public record RecipeIngredientDto(IngredientDto Ingredient, decimal Amount, string Unit);

public record RecipeDto(
    RecipeId Id,
    string Name,
    UserId OwnerId,
    int Servings,
    IReadOnlyList<StepDto> Steps,
    IReadOnlyList<RecipeIngredientDto> Ingredients,
    IReadOnlyList<CategoryDto> Categories);
