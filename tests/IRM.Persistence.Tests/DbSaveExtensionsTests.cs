using IRM.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace IRM.Persistence.Tests;

public class DbSaveExtensionsTests : DatabaseTest
{
    [Fact]
    public async Task Save_translates_unique_violation_into_DuplicateNameException()
    {
        await Categories().CreateAsync("Suppe");

        // Zweiter Kontext ohne Vorab-Prüfung: simuliert den Wettlauf, in dem zwei gleichzeitige
        // Aufrufe die Service-Prüfung bestehen und erst der Unique-Index am Insert kollidiert.
        await using var db = NewContext();
        db.Categories.Add(Category.Create("Suppe"));

        await Should.ThrowAsync<DuplicateNameException>(
            () => db.SaveTranslatingDuplicateAsync("Kategorie existiert bereits.", default));
    }

    [Fact]
    public async Task Save_lets_unrelated_db_errors_propagate()
    {
        // Kein Unique-Konflikt (FK auf nicht existierende Zutat) → keine Übersetzung,
        // der Fehler muss als DbUpdateException durchschlagen.
        await using var db = NewContext();
        var recipe = Recipe.Create(
            new UserId(1), "Geist", 1,
            new[] { "x" },
            new[] { new RecipeIngredient(new IngredientId(999), 1, "g") },
            new[] { new CategoryId(999) });
        db.Recipes.Add(recipe);

        await Should.ThrowAsync<DbUpdateException>(
            () => db.SaveTranslatingDuplicateAsync("sollte nicht erscheinen", default));
    }
}
