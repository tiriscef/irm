using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace IRM.Persistence;

// Value-Converter für die strongly-typed IDs: mappen den record-struct auf sein long-Value
// und zurück. Zentral registriert in RecipeDbContext.ConfigureConventions.

internal sealed class UserIdConverter() : ValueConverter<UserId, long>(id => id.Value, v => new UserId(v));

internal sealed class RecipeIdConverter() : ValueConverter<RecipeId, long>(id => id.Value, v => new RecipeId(v));

internal sealed class IngredientIdConverter() : ValueConverter<IngredientId, long>(id => id.Value, v => new IngredientId(v));

internal sealed class CategoryIdConverter() : ValueConverter<CategoryId, long>(id => id.Value, v => new CategoryId(v));
