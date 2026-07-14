using Shouldly;

namespace IRM.Persistence.Tests;

public class CategoryServiceTests : DatabaseTest
{
    [Fact]
    public async Task Create_persists_and_returns_dto()
    {
        var category = await Categories().CreateAsync("Dessert");

        category.Id.Value.ShouldBeGreaterThan(0);
        category.Name.ShouldBe("Dessert");
    }

    [Fact]
    public async Task Create_with_duplicate_name_throws_case_insensitively()
    {
        await Categories().CreateAsync("Dessert");
        await Should.ThrowAsync<DuplicateNameException>(() => Categories().CreateAsync("dessert"));
    }

    [Fact]
    public async Task Rename_unknown_category_throws()
        => await Should.ThrowAsync<CategoryNotFoundException>(
            () => Categories().RenameAsync(new CategoryId(999), "Dessert"));

    [Fact]
    public async Task Delete_category_in_use_throws()
    {
        var author = await SeedUserAsync();
        var ingredient = await SeedIngredientAsync();
        var category = await Categories().CreateAsync("Hauptgericht");
        await Recipes().CreateAsync(author, ValidRequest(ingredient, category.Id));

        await Should.ThrowAsync<InUseException>(() => Categories().DeleteAsync(category.Id));
    }

    [Fact]
    public async Task Delete_removes_unused_category()
    {
        var category = await Categories().CreateAsync("Dessert");
        await Categories().DeleteAsync(category.Id);

        (await Categories().ListAsync()).ShouldBeEmpty();
    }
}
