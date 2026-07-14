using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace IRM.Persistence;

/// <summary>
/// EF-Core-Kontext der Rezeptverwaltung. Bewusst <c>internal</c> – Consumer sehen nur die
/// Service-Interfaces aus IRM.Application, nicht das Persistenz-Detail.
/// </summary>
internal sealed class RecipeDbContext : DbContext
{
    public RecipeDbContext(DbContextOptions<RecipeDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        // EF Core wendet die "Sqlite:Autoincrement"-Annotation auf value-converted Keys über
        // Prozessgrenzen hinweg nicht deterministisch an, wodurch Migrate() fälschlich einen
        // PendingModelChangesWarning wirft. Das Schema (INTEGER PRIMARY KEY) ist davon unberührt.
        => optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

    public DbSet<User> Users => Set<User>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<Favorite> Favorites => Set<Favorite>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configuration)
    {
        configuration.Properties<UserId>().HaveConversion<UserIdConverter>();
        configuration.Properties<RecipeId>().HaveConversion<RecipeIdConverter>();
        configuration.Properties<IngredientId>().HaveConversion<IngredientIdConverter>();
        configuration.Properties<CategoryId>().HaveConversion<CategoryIdConverter>();
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).ValueGeneratedOnAdd().HasAnnotation("Sqlite:Autoincrement", true);
            e.Property(u => u.Name).UseCollation("NOCASE");
            e.HasIndex(u => u.Name).IsUnique();
        });

        model.Entity<Ingredient>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).ValueGeneratedOnAdd().HasAnnotation("Sqlite:Autoincrement", true);
            e.Property(i => i.Name).UseCollation("NOCASE");
            e.HasIndex(i => i.Name).IsUnique();
        });

        model.Entity<Category>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).ValueGeneratedOnAdd().HasAnnotation("Sqlite:Autoincrement", true);
            e.Property(c => c.Name).UseCollation("NOCASE");
            e.HasIndex(c => c.Name).IsUnique();
        });

        model.Entity<Recipe>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).ValueGeneratedOnAdd().HasAnnotation("Sqlite:Autoincrement", true);
            e.Property(r => r.Name).UseCollation("NOCASE");
            e.HasIndex(r => r.Name).IsUnique();

            // Schritte: reines Value Object, im Rezept-Aggregat besessen (eigene Tabelle).
            e.OwnsMany(r => r.Steps, s =>
            {
                s.WithOwner().HasForeignKey("RecipeId");
                s.Property(x => x.Order);
                s.Property(x => x.Instruction);
                s.HasKey("RecipeId", nameof(Step.Order));
            });

            // Zutaten-Verknüpfung: besessen, aber mit echtem FK auf die globale Zutat.
            // Restrict spiegelt die "block if in-use"-Löschregel auf DB-Ebene.
            e.OwnsMany(r => r.Ingredients, ri =>
            {
                ri.WithOwner().HasForeignKey("RecipeId");
                ri.Property(x => x.IngredientId);
                ri.Property(x => x.Amount);
                ri.Property(x => x.Unit);
                ri.HasKey("RecipeId", nameof(RecipeIngredient.IngredientId));
                ri.HasOne<Ingredient>()
                    .WithMany()
                    .HasForeignKey(x => x.IngredientId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Kategorie-Verknüpfung: interne Join-Entity, ebenfalls mit FK + Restrict.
            e.OwnsMany(r => r.Categories, rc =>
            {
                rc.WithOwner().HasForeignKey("RecipeId");
                rc.HasKey("RecipeId", nameof(RecipeCategory.CategoryId));
                rc.HasOne<Category>()
                    .WithMany()
                    .HasForeignKey(x => x.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        });

        model.Entity<Favorite>(e =>
        {
            e.HasKey(f => new { f.UserId, f.RecipeId });
            e.HasOne<User>().WithMany().HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Restrict);
            // Rezept gelöscht → zugehörige Favoriten verschwinden mit.
            e.HasOne<Recipe>().WithMany().HasForeignKey(f => f.RecipeId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
