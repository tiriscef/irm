using IRM.Domain;
using Shouldly;
using Xunit;

namespace IRM.Domain.Tests;

public class UserTests
{
    [Fact]
    public void Register_trims_name()
        => User.Register("  Isa  ").Name.ShouldBe("Isa");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_with_blank_name_throws(string name)
        => Should.Throw<DomainValidationException>(() => User.Register(name));

    [Fact]
    public void Rename_to_blank_throws()
    {
        var user = User.Register("Isa");
        Should.Throw<DomainValidationException>(() => user.Rename(" "));
    }
}

public class IngredientTests
{
    [Fact]
    public void Create_trims_name()
        => Ingredient.Create("  Mehl ").Name.ShouldBe("Mehl");

    [Fact]
    public void Rename_changes_name_keeping_the_entity()
    {
        var ingredient = Ingredient.Create("Zuker");
        ingredient.Rename("Zucker");
        ingredient.Name.ShouldBe("Zucker");
    }

    [Fact]
    public void Create_with_blank_name_throws()
        => Should.Throw<DomainValidationException>(() => Ingredient.Create(""));

    [Fact]
    public void Rename_to_blank_throws()
    {
        var ingredient = Ingredient.Create("Mehl");
        Should.Throw<DomainValidationException>(() => ingredient.Rename(" "));
    }
}

public class CategoryTests
{
    [Fact]
    public void Create_trims_name()
        => Category.Create("  Dessert ").Name.ShouldBe("Dessert");

    [Fact]
    public void Rename_changes_name()
    {
        var category = Category.Create("Desert");
        category.Rename("Dessert");
        category.Name.ShouldBe("Dessert");
    }

    [Fact]
    public void Create_with_blank_name_throws()
        => Should.Throw<DomainValidationException>(() => Category.Create("  "));
}
