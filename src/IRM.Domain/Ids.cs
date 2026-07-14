namespace IRM.Domain;

// Strongly-typed IDs: verhindern das versehentliche Vertauschen verschiedener Id-Arten
// (ein UserId lässt sich nicht dort einsetzen, wo ein RecipeId erwartet wird → Compile-Fehler).
// Vermeidet "primitive obsession" gegenüber nackten long-Werten.

public readonly record struct UserId(long Value);

public readonly record struct RecipeId(long Value);

public readonly record struct IngredientId(long Value);

public readonly record struct CategoryId(long Value);
