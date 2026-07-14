using Shouldly;

namespace IRM.Persistence.Tests;

public class IngredientServiceTests : DatabaseTest
{
    [Fact]
    public async Task Add_persists_and_returns_dto()
    {
        var ingredient = await Ingredients().AddAsync("Mehl");

        ingredient.Id.Value.ShouldBeGreaterThan(0);
        ingredient.Name.ShouldBe("Mehl");
    }

    [Fact]
    public async Task Add_with_duplicate_name_throws_case_insensitively()
    {
        await Ingredients().AddAsync("Mehl");
        await Should.ThrowAsync<DuplicateNameException>(() => Ingredients().AddAsync("mehl"));
    }

    [Fact]
    public async Task Rename_changes_name()
    {
        var ingredient = await Ingredients().AddAsync("Zuker");
        var renamed = await Ingredients().RenameAsync(ingredient.Id, "Zucker");

        renamed.Id.ShouldBe(ingredient.Id);
        renamed.Name.ShouldBe("Zucker");
    }

    [Fact]
    public async Task Rename_to_existing_name_throws()
    {
        await Ingredients().AddAsync("Mehl");
        var sugar = await Ingredients().AddAsync("Zucker");

        await Should.ThrowAsync<DuplicateNameException>(() => Ingredients().RenameAsync(sugar.Id, "Mehl"));
    }

    [Fact]
    public async Task Rename_unknown_ingredient_throws()
        => await Should.ThrowAsync<IngredientNotFoundException>(
            () => Ingredients().RenameAsync(new IngredientId(999), "Mehl"));

    [Fact]
    public async Task Delete_removes_unused_ingredient()
    {
        var ingredient = await Ingredients().AddAsync("Mehl");
        await Ingredients().DeleteAsync(ingredient.Id);

        (await Ingredients().ListAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Delete_ingredient_in_use_throws()
    {
        var author = await SeedUserAsync();
        var category = await SeedCategoryAsync();
        var ingredient = await Ingredients().AddAsync("Mehl");
        await Recipes().CreateAsync(author, ValidRequest(ingredient.Id, category));

        await Should.ThrowAsync<InUseException>(() => Ingredients().DeleteAsync(ingredient.Id));
        (await Ingredients().ListAsync()).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Delete_unknown_ingredient_throws()
        => await Should.ThrowAsync<IngredientNotFoundException>(
            () => Ingredients().DeleteAsync(new IngredientId(999)));
}
