using Shouldly;

namespace IRM.Persistence.Tests;

public class RecipeServiceTests : DatabaseTest
{
    [Fact]
    public async Task Create_persists_and_resolves_ingredient_and_category_names()
    {
        var author = await SeedUserAsync();
        var flour = await SeedIngredientAsync("Mehl");
        var main = await SeedCategoryAsync("Hauptgericht");

        var recipe = await Recipes().CreateAsync(author, ValidRequest(flour, main));

        recipe.Id.Value.ShouldBeGreaterThan(0);
        recipe.Name.ShouldBe("Pfannkuchen");
        recipe.OwnerId.ShouldBe(author);
        recipe.Steps.Select(s => s.Order).ShouldBe(new[] { 1, 2 });
        recipe.Ingredients.ShouldHaveSingleItem().Ingredient.Name.ShouldBe("Mehl");
        recipe.Categories.ShouldHaveSingleItem().Name.ShouldBe("Hauptgericht");
    }

    [Fact]
    public async Task Get_returns_null_for_unknown_recipe()
        => (await Recipes().GetAsync(new RecipeId(999))).ShouldBeNull();

    [Fact]
    public async Task Create_with_duplicate_name_throws()
    {
        var author = await SeedUserAsync();
        var flour = await SeedIngredientAsync();
        var main = await SeedCategoryAsync();
        await Recipes().CreateAsync(author, ValidRequest(flour, main));

        await Should.ThrowAsync<DuplicateNameException>(
            () => Recipes().CreateAsync(author, ValidRequest(flour, main)));
    }

    [Fact]
    public async Task Create_with_unknown_author_throws()
    {
        var flour = await SeedIngredientAsync();
        var main = await SeedCategoryAsync();

        await Should.ThrowAsync<UserNotFoundException>(
            () => Recipes().CreateAsync(new UserId(999), ValidRequest(flour, main)));
    }

    [Fact]
    public async Task Create_with_unknown_category_throws()
    {
        var author = await SeedUserAsync();
        var flour = await SeedIngredientAsync();

        await Should.ThrowAsync<CategoryNotFoundException>(
            () => Recipes().CreateAsync(author, ValidRequest(flour, new CategoryId(999))));
    }

    [Fact]
    public async Task Create_with_unknown_ingredient_throws()
    {
        var author = await SeedUserAsync();
        var main = await SeedCategoryAsync();

        await Should.ThrowAsync<IngredientNotFoundException>(
            () => Recipes().CreateAsync(author, ValidRequest(new IngredientId(999), main)));
    }

    [Fact]
    public async Task Update_by_owner_replaces_content()
    {
        var author = await SeedUserAsync();
        var flour = await SeedIngredientAsync("Mehl");
        var egg = await SeedIngredientAsync("Ei");
        var main = await SeedCategoryAsync("Hauptgericht");
        var dessert = await SeedCategoryAsync("Dessert");
        var recipe = await Recipes().CreateAsync(author, ValidRequest(flour, main));

        var updated = await Recipes().UpdateAsync(author, recipe.Id, new UpdateRecipeRequest(
            "Omelette", 2,
            new[] { "Eier verquirlen" },
            new[] { new RecipeIngredientInput(egg, 3, "Stück") },
            new[] { dessert }));

        updated.Name.ShouldBe("Omelette");
        updated.Servings.ShouldBe(2);
        updated.Ingredients.ShouldHaveSingleItem().Ingredient.Name.ShouldBe("Ei");
        updated.Categories.ShouldHaveSingleItem().Name.ShouldBe("Dessert");

        // Persistiert, nicht nur im Rückgabewert.
        (await Recipes().GetAsync(recipe.Id))!.Name.ShouldBe("Omelette");
    }

    [Fact]
    public async Task Update_by_non_owner_throws()
    {
        var author = await SeedUserAsync("Isa");
        var intruder = await SeedUserAsync("Bob");
        var flour = await SeedIngredientAsync();
        var main = await SeedCategoryAsync();
        var recipe = await Recipes().CreateAsync(author, ValidRequest(flour, main));

        await Should.ThrowAsync<UnauthorizedRecipeAccessException>(() => Recipes().UpdateAsync(
            intruder, recipe.Id, new UpdateRecipeRequest(
                "Gestohlen", 1, new[] { "x" },
                new[] { new RecipeIngredientInput(flour, 1, "g") }, new[] { main })));
    }

    [Fact]
    public async Task Update_unknown_recipe_throws()
    {
        var author = await SeedUserAsync();
        var flour = await SeedIngredientAsync();
        var main = await SeedCategoryAsync();

        await Should.ThrowAsync<RecipeNotFoundException>(() => Recipes().UpdateAsync(
            author, new RecipeId(999), new UpdateRecipeRequest(
                "X", 1, new[] { "x" },
                new[] { new RecipeIngredientInput(flour, 1, "g") }, new[] { main })));
    }

    [Fact]
    public async Task Delete_by_owner_removes_recipe_and_its_favorites()
    {
        var author = await SeedUserAsync();
        var flour = await SeedIngredientAsync();
        var main = await SeedCategoryAsync();
        var recipe = await Recipes().CreateAsync(author, ValidRequest(flour, main));
        await Favorites().AddAsync(author, recipe.Id);

        await Recipes().DeleteAsync(author, recipe.Id);

        (await Recipes().GetAsync(recipe.Id)).ShouldBeNull();
        (await Favorites().GetFavoritesAsync(author)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Delete_by_non_owner_throws()
    {
        var author = await SeedUserAsync("Isa");
        var intruder = await SeedUserAsync("Bob");
        var flour = await SeedIngredientAsync();
        var main = await SeedCategoryAsync();
        var recipe = await Recipes().CreateAsync(author, ValidRequest(flour, main));

        await Should.ThrowAsync<UnauthorizedRecipeAccessException>(
            () => Recipes().DeleteAsync(intruder, recipe.Id));
    }

    [Fact]
    public async Task Query_paths_return_matching_recipes()
    {
        var isa = await SeedUserAsync("Isa");
        var bob = await SeedUserAsync("Bob");
        var flour = await SeedIngredientAsync("Mehl");
        var egg = await SeedIngredientAsync("Ei");
        var main = await SeedCategoryAsync("Hauptgericht");
        var dessert = await SeedCategoryAsync("Dessert");

        var pancake = await Recipes().CreateAsync(isa, new CreateRecipeRequest(
            "Pfannkuchen", 4, new[] { "backen" },
            new[] { new RecipeIngredientInput(flour, 200, "g") }, new[] { main }));
        var omelette = await Recipes().CreateAsync(bob, new CreateRecipeRequest(
            "Omelette", 2, new[] { "braten" },
            new[] { new RecipeIngredientInput(egg, 3, "Stück") }, new[] { dessert }));

        (await Recipes().GetByUserAsync(isa)).ShouldHaveSingleItem().Id.ShouldBe(pancake.Id);
        (await Recipes().GetByCategoryAsync(dessert)).ShouldHaveSingleItem().Id.ShouldBe(omelette.Id);
        (await Recipes().GetByIngredientAsync(flour)).ShouldHaveSingleItem().Id.ShouldBe(pancake.Id);
    }
}
