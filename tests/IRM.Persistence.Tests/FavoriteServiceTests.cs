using Shouldly;

namespace IRM.Persistence.Tests;

public class FavoriteServiceTests : DatabaseTest
{
    private async Task<(UserId user, RecipeId recipe)> SeedRecipeAsync(string user = "Isa", string recipeName = "Pfannkuchen")
    {
        var author = await SeedUserAsync(user);
        var ingredient = await SeedIngredientAsync($"Mehl-{recipeName}");
        var category = await SeedCategoryAsync($"Kat-{recipeName}");
        var recipe = await Recipes().CreateAsync(author, ValidRequest(ingredient, category, recipeName));
        return (author, recipe.Id);
    }

    [Fact]
    public async Task Add_makes_recipe_a_favorite()
    {
        var (user, recipe) = await SeedRecipeAsync();
        await Favorites().AddAsync(user, recipe);

        (await Favorites().GetFavoritesAsync(user)).ShouldHaveSingleItem().Id.ShouldBe(recipe);
    }

    [Fact]
    public async Task Add_is_idempotent()
    {
        var (user, recipe) = await SeedRecipeAsync();
        await Favorites().AddAsync(user, recipe);
        await Favorites().AddAsync(user, recipe);

        (await Favorites().GetFavoritesAsync(user)).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Add_for_unknown_user_throws()
    {
        var (_, recipe) = await SeedRecipeAsync();
        await Should.ThrowAsync<UserNotFoundException>(() => Favorites().AddAsync(new UserId(999), recipe));
    }

    [Fact]
    public async Task Add_for_unknown_recipe_throws()
    {
        var user = await SeedUserAsync();
        await Should.ThrowAsync<RecipeNotFoundException>(() => Favorites().AddAsync(user, new RecipeId(999)));
    }

    [Fact]
    public async Task Remove_deletes_favorite()
    {
        var (user, recipe) = await SeedRecipeAsync();
        await Favorites().AddAsync(user, recipe);

        await Favorites().RemoveAsync(user, recipe);

        (await Favorites().GetFavoritesAsync(user)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Remove_non_existing_favorite_is_noop()
    {
        var (user, recipe) = await SeedRecipeAsync();
        await Should.NotThrowAsync(() => Favorites().RemoveAsync(user, recipe));
    }

    [Fact]
    public async Task Get_favorites_returns_only_that_users_favorites()
    {
        var (isa, pancake) = await SeedRecipeAsync("Isa", "Pfannkuchen");
        var (bob, omelette) = await SeedRecipeAsync("Bob", "Omelette");
        await Favorites().AddAsync(isa, pancake);
        await Favorites().AddAsync(bob, omelette);

        (await Favorites().GetFavoritesAsync(isa)).ShouldHaveSingleItem().Id.ShouldBe(pancake);
    }
}
