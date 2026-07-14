using Shouldly;

namespace IRM.Persistence.Tests;

public class UserServiceTests : DatabaseTest
{
    [Fact]
    public async Task Register_persists_and_returns_dto()
    {
        var user = await Users().RegisterAsync("Isa");

        user.Id.Value.ShouldBeGreaterThan(0);
        user.Name.ShouldBe("Isa");
        (await Users().ListAsync()).ShouldHaveSingleItem().ShouldBe(user);
    }

    [Fact]
    public async Task Register_with_duplicate_name_throws()
    {
        await Users().RegisterAsync("Isa");
        await Should.ThrowAsync<DuplicateNameException>(() => Users().RegisterAsync("Isa"));
    }

    [Fact]
    public async Task Register_is_case_insensitive_for_uniqueness()
    {
        await Users().RegisterAsync("Isa");
        await Should.ThrowAsync<DuplicateNameException>(() => Users().RegisterAsync("isa"));
    }

    [Fact]
    public async Task List_returns_all_users_ordered_by_name()
    {
        await Users().RegisterAsync("Bob");
        await Users().RegisterAsync("Ann");

        (await Users().ListAsync()).Select(u => u.Name).ShouldBe(new[] { "Ann", "Bob" });
    }

    [Fact]
    public async Task Get_returns_registered_user()
    {
        var id = await SeedUserAsync("Isa");
        (await Users().GetAsync(id))!.Name.ShouldBe("Isa");
    }

    [Fact]
    public async Task Get_unknown_user_returns_null()
        => (await Users().GetAsync(new UserId(999))).ShouldBeNull();

    [Fact]
    public async Task Rename_changes_name()
    {
        var id = await SeedUserAsync("Isa");

        (await Users().RenameAsync(id, "Bella")).Name.ShouldBe("Bella");
        (await Users().GetAsync(id))!.Name.ShouldBe("Bella");
    }

    [Fact]
    public async Task Rename_unknown_user_throws()
        => await Should.ThrowAsync<UserNotFoundException>(
            () => Users().RenameAsync(new UserId(999), "Bella"));

    [Fact]
    public async Task Rename_to_existing_name_throws_case_insensitively()
    {
        var id = await SeedUserAsync("Isa");
        await Users().RegisterAsync("Bob");

        await Should.ThrowAsync<DuplicateNameException>(() => Users().RenameAsync(id, "bob"));
    }

    [Fact]
    public async Task Delete_removes_user()
    {
        var id = await SeedUserAsync();
        await Users().DeleteAsync(id);

        (await Users().ListAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Delete_unknown_user_throws()
        => await Should.ThrowAsync<UserNotFoundException>(() => Users().DeleteAsync(new UserId(999)));

    [Fact]
    public async Task Delete_user_owning_recipes_throws()
    {
        var owner = await SeedUserAsync();
        var ingredient = await SeedIngredientAsync();
        var category = await SeedCategoryAsync();
        await Recipes().CreateAsync(owner, ValidRequest(ingredient, category));

        await Should.ThrowAsync<InUseException>(() => Users().DeleteAsync(owner));
    }

    // Ein Fan besitzt keine Rezepte, hat aber einen Favoriten (FK Restrict auf User).
    // Das Löschen muss trotzdem gelingen – der Favorit wird mit aufgeräumt.
    [Fact]
    public async Task Delete_also_removes_users_favorites()
    {
        var owner = await SeedUserAsync("Owner");
        var fan = await SeedUserAsync("Fan");
        var ingredient = await SeedIngredientAsync();
        var category = await SeedCategoryAsync();
        var recipe = await Recipes().CreateAsync(owner, ValidRequest(ingredient, category));
        await Favorites().AddAsync(fan, recipe.Id);

        await Users().DeleteAsync(fan);

        (await Users().ListAsync()).Select(u => u.Name).ShouldBe(new[] { "Owner" });
    }
}
