using IRM.Persistence;
using IRM.Persistence.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IRM.Persistence.Tests;

/// <summary>
/// Basis für die Integrationstests. Jeder Test bekommt eine eigene, offen gehaltene
/// SQLite-in-memory-Datenbank (lebt nur solange die Verbindung offen ist) und wendet die
/// echte Migration an. Services werden je Aufruf mit einem frischen Kontext gebaut – das
/// entspricht dem scoped-per-Operation-Betrieb und vermeidet falsch-positive Treffer aus
/// dem Identity-Map eines geteilten Kontexts.
/// </summary>
public abstract class DatabaseTest : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<RecipeDbContext> _options;

    protected DatabaseTest()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<RecipeDbContext>().UseSqlite(_connection).Options;

        using var context = new RecipeDbContext(_options);
        context.Database.Migrate();
    }

    private protected RecipeDbContext NewContext() => new(_options);

    protected IUserService Users() => new UserService(NewContext());
    protected IIngredientService Ingredients() => new IngredientService(NewContext());
    protected ICategoryService Categories() => new CategoryService(NewContext());
    protected IRecipeService Recipes() => new RecipeService(NewContext());
    protected IFavoriteService Favorites() => new FavoriteService(NewContext());

    protected async Task<UserId> SeedUserAsync(string name = "Isa")
        => (await Users().RegisterAsync(name)).Id;

    protected async Task<CategoryId> SeedCategoryAsync(string name = "Hauptgericht")
        => (await Categories().CreateAsync(name)).Id;

    protected async Task<IngredientId> SeedIngredientAsync(string name = "Mehl")
        => (await Ingredients().AddAsync(name)).Id;

    // Baut ein gültiges CreateRecipeRequest; Bestandteile lassen sich je Test überschreiben.
    protected static CreateRecipeRequest ValidRequest(
        IngredientId ingredient,
        CategoryId category,
        string name = "Pfannkuchen",
        int servings = 4)
        => new(
            name,
            servings,
            new[] { "Teig anrühren", "In der Pfanne backen" },
            new[] { new RecipeIngredientInput(ingredient, 200, "g") },
            new[] { category });

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
