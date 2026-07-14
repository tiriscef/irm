using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace IRM.Persistence.Tests;

/// <summary>
/// Prüft die öffentliche Einstiegs-API end-to-end gegen eine echte SQLite-Datei: DI-Registrierung,
/// explizite Migration und ein Round-Trip über die aufgelösten Services.
/// </summary>
public class WiringTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"irm-wiring-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task AddRecipeManagement_wires_services_and_migrates()
    {
        var services = new ServiceCollection();
        services.AddRecipeManagement(o => o.UseSqlite($"Data Source={_dbPath}"));
        await using var provider = services.BuildServiceProvider();

        await provider.InitializeRecipesDatabaseAsync();

        using var scope = provider.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserService>();
        var recipes = scope.ServiceProvider.GetRequiredService<IRecipeService>();
        var ingredients = scope.ServiceProvider.GetRequiredService<IIngredientService>();
        var categories = scope.ServiceProvider.GetRequiredService<ICategoryService>();

        var author = await users.RegisterAsync("Isa");
        var flour = await ingredients.AddAsync("Mehl");
        var main = await categories.CreateAsync("Hauptgericht");

        var created = await recipes.CreateAsync(author.Id, new CreateRecipeRequest(
            "Pfannkuchen", 4,
            new[] { "Teig anrühren", "backen" },
            new[] { new RecipeIngredientInput(flour.Id, 200, "g") },
            new[] { main.Id }));

        var loaded = await recipes.GetAsync(created.Id);
        loaded.ShouldNotBeNull();
        loaded!.Name.ShouldBe("Pfannkuchen");
        loaded.Ingredients.ShouldHaveSingleItem().Ingredient.Name.ShouldBe("Mehl");

        File.Exists(_dbPath).ShouldBeTrue(); // echte Datei, nicht in-memory
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
        GC.SuppressFinalize(this);
    }
}
