# Entwicklungsnotizen

Details, die bei der Entwicklung angefallen sind und für den Projektüberblick nicht
nötig sind – aber für Wartung und Migrationen relevant bleiben.

## EF-Core-Workarounds

- **`PendingModelChangesWarning` unterdrückt** (`RecipeDbContext.OnConfiguring`): EF Core wendet die
  `Sqlite:Autoincrement`-Annotation auf value-converted Keys über Prozessgrenzen hinweg nicht
  deterministisch an, wodurch `Migrate()` fälschlich Modell-Drift meldet. Das Schema
  (`INTEGER PRIMARY KEY`) ist davon unberührt.
- **Native SQLite-Binary** auf `SQLitePCLRaw.lib.e_sqlite3 3.53.3` angehoben – behebt eine
  transitive Sicherheitswarnung (NU1903) aus dem EF-Core-SQLite-Provider.

## Migrationen neu erzeugen

Nur bei Modelländerungen nötig:

```bash
dotnet tool install --global dotnet-ef --version "10.*"
dotnet ef migrations add <Name> --project src/IRM.Persistence
```
