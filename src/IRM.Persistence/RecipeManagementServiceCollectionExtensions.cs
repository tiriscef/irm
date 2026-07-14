using IRM.Persistence;
using IRM.Persistence.Services;
using Microsoft.EntityFrameworkCore;

// Bewusst im Microsoft.Extensions.DependencyInjection-Namespace: so findet der Consumer die
// Erweiterung, ohne ein IRM-spezifisches using zu setzen (Konvention wie bei EF Core/ASP.NET).
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registrierung der Rezeptverwaltung im DI-Container – die einzige öffentliche Einstiegs-API der Persistenz.</summary>
public static class RecipeManagementServiceCollectionExtensions
{
    /// <summary>
    /// Registriert den (internen) Datenbankkontext und alle Service-Implementierungen hinter ihren
    /// Interfaces. Die Datenbank selbst wird über <paramref name="configureDatabase"/> konfiguriert,
    /// z.B. <c>o =&gt; o.UseSqlite("Data Source=recipes.db")</c>.
    /// </summary>
    public static IServiceCollection AddRecipeManagement(
        this IServiceCollection services, Action<DbContextOptionsBuilder> configureDatabase)
    {
        services.AddDbContext<RecipeDbContext>(configureDatabase);

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IIngredientService, IngredientService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IFavoriteService, FavoriteService>();

        return services;
    }

    /// <summary>
    /// Wendet ausstehende EF-Migrationen an und legt die Datenbank bei Bedarf an. Bewusst explizit
    /// aufzurufen (produktionsnäher als automatisches EnsureCreated beim ersten Zugriff).
    /// </summary>
    public static async Task InitializeRecipesDatabaseAsync(
        this IServiceProvider provider, CancellationToken ct = default)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RecipeDbContext>();
        await db.Database.MigrateAsync(ct);
    }
}
