using IRM.Domain;
using Shouldly;
using Xunit;

namespace IRM.Domain.Tests;

public class RecipeTests
{
    // Baut ein gültiges Rezept; einzelne Bestandteile lassen sich für den jeweiligen Testfall überschreiben.
    private static Recipe CreateValid(
        string name = "Pfannkuchen",
        int servings = 4,
        IReadOnlyList<string>? steps = null,
        IReadOnlyList<RecipeIngredient>? ingredients = null,
        IReadOnlyList<CategoryId>? categories = null)
        => Recipe.Create(
            new UserId(1),
            name,
            servings,
            steps ?? new[] { "Teig anrühren", "In der Pfanne backen" },
            ingredients ?? new[] { new RecipeIngredient(new IngredientId(1), 200, "g") },
            categories ?? new[] { new CategoryId(1) });

    [Fact]
    public void Create_with_valid_data_sets_properties()
    {
        var recipe = CreateValid();

        recipe.Name.ShouldBe("Pfannkuchen");
        recipe.Servings.ShouldBe(4);
        recipe.OwnerId.ShouldBe(new UserId(1));
        recipe.Steps.Count.ShouldBe(2);
        recipe.Ingredients.Count.ShouldBe(1);
        recipe.CategoryIds.Count.ShouldBe(1);
    }

    [Fact]
    public void Create_assigns_sequential_step_order_from_position()
    {
        var recipe = CreateValid(steps: new[] { "A", "B", "C" });

        recipe.Steps.Select(s => s.Order).ShouldBe(new[] { 1, 2, 3 });
        recipe.Steps.Select(s => s.Instruction).ShouldBe(new[] { "A", "B", "C" });
    }

    [Fact]
    public void Create_trims_name()
        => CreateValid(name: "  Suppe  ").Name.ShouldBe("Suppe");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_name_throws(string name)
        => Should.Throw<DomainValidationException>(() => CreateValid(name: name));

    [Fact]
    public void Create_with_servings_below_one_throws()
        => Should.Throw<DomainValidationException>(() => CreateValid(servings: 0));

    [Fact]
    public void Create_without_steps_throws()
        => Should.Throw<DomainValidationException>(() => CreateValid(steps: Array.Empty<string>()));

    [Fact]
    public void Create_without_ingredients_throws()
        => Should.Throw<DomainValidationException>(() => CreateValid(ingredients: Array.Empty<RecipeIngredient>()));

    [Fact]
    public void Create_without_categories_throws()
        => Should.Throw<DomainValidationException>(() => CreateValid(categories: Array.Empty<CategoryId>()));

    [Fact]
    public void Create_with_duplicate_ingredient_throws()
        => Should.Throw<DomainValidationException>(() => CreateValid(ingredients: new[]
        {
            new RecipeIngredient(new IngredientId(1), 200, "g"),
            new RecipeIngredient(new IngredientId(1), 1, "Prise"),
        }));

    [Fact]
    public void Create_with_duplicate_category_throws()
        => Should.Throw<DomainValidationException>(
            () => CreateValid(categories: new[] { new CategoryId(1), new CategoryId(1) }));

    [Fact]
    public void Update_replaces_content_completely()
    {
        var recipe = CreateValid();

        recipe.Update(
            "Neuer Name", 2,
            new[] { "Nur ein Schritt" },
            new[] { new RecipeIngredient(new IngredientId(2), 1, "Stück") },
            new[] { new CategoryId(3) });

        recipe.Name.ShouldBe("Neuer Name");
        recipe.Servings.ShouldBe(2);
        recipe.Steps.Count.ShouldBe(1);
        recipe.Ingredients.Single().IngredientId.ShouldBe(new IngredientId(2));
        recipe.CategoryIds.Single().ShouldBe(new CategoryId(3));
    }

    [Fact]
    public void Update_also_enforces_invariants()
    {
        var recipe = CreateValid();

        Should.Throw<DomainValidationException>(() => recipe.Update(
            "X", 2,
            Array.Empty<string>(),
            new[] { new RecipeIngredient(new IngredientId(1), 1, "g") },
            new[] { new CategoryId(1) }));
    }
}
