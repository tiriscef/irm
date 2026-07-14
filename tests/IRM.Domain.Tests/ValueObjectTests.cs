using IRM.Domain;
using Shouldly;
using Xunit;

namespace IRM.Domain.Tests;

public class StepTests
{
    [Fact]
    public void Valid_step_trims_instruction()
        => new Step(1, "  rühren  ").Instruction.ShouldBe("rühren");

    [Fact]
    public void Order_below_one_throws()
        => Should.Throw<DomainValidationException>(() => new Step(0, "rühren"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_instruction_throws(string instruction)
        => Should.Throw<DomainValidationException>(() => new Step(1, instruction));
}

public class RecipeIngredientTests
{
    [Fact]
    public void Valid_ingredient_trims_unit()
        => new RecipeIngredient(new IngredientId(1), 5, "  g  ").Unit.ShouldBe("g");

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_amount_throws(decimal amount)
        => Should.Throw<DomainValidationException>(() => new RecipeIngredient(new IngredientId(1), amount, "g"));

    [Fact]
    public void Blank_unit_throws()
        => Should.Throw<DomainValidationException>(() => new RecipeIngredient(new IngredientId(1), 5, " "));
}
