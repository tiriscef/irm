using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IRM.Persistence;

/// <summary>
/// Nur für Entwurfszeit (<c>dotnet ef migrations</c>): baut den internen Kontext, damit die
/// Migration ohne laufende Anwendung erzeugt werden kann. Zur Laufzeit ungenutzt.
/// </summary>
internal sealed class RecipeDbContextFactory : IDesignTimeDbContextFactory<RecipeDbContext>
{
    public RecipeDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RecipeDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;
        return new RecipeDbContext(options);
    }
}
