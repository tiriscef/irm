# Isa's Rezept-Manager

Eine Bibliothek zur Verwaltung von Rezepten, Zutaten, Kategorien, Benutzern und Favoriten.
Fachliche Invarianten werden im Domain-Model erzwungen, die Persistenz läuft über EF Core + SQLite.
Der Consumer spricht ausschließlich mit Service-Interfaces – Datenbankdetails bleiben gekapselt.

## Schnellstart

Voraussetzung: **.NET 10 SDK**. 
Noch nicht installiert?
Windows: `winget install Microsoft.DotNet.SDK.10` — sonst [dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0).
Prüfen mit `dotnet --list-sdks` (eine `10.x`-Zeile).

```bash
dotnet run --project samples/IRM.Demo                # scripted: fester Ablauf typischer Use-Cases
dotnet run --project samples/IRM.Demo -- interactive # interaktiv: Menü zum manuellen Verifizieren
dotnet test                                          # alle Tests ausführen (Domain-Unit + Persistence-Integration)
```

Die **scripted** Demo legt bei jedem Start eine frische `recipes.db` an und zeigt die typischen
Use-Cases deterministisch (Benutzer, Zutaten/Kategorien, Rezept anlegen/ändern/abfragen, Favoriten,
Autorisierung, Löschschutz). Der **interaktive** Modus bietet dieselben Funktionen über ein Menü und
behält seine Daten über Läufe hinweg. Beide sprechen ausschließlich die öffentliche API an.

## Verwendung

```csharp
var services = new ServiceCollection();
services.AddRecipeManagement(o => o.UseSqlite("Data Source=recipes.db"));
await using var provider = services.BuildServiceProvider();

await provider.InitializeRecipesDatabaseAsync();   // wendet die EF-Migration an – bewusst explizit

using var scope = provider.CreateScope();
var recipes = scope.ServiceProvider.GetRequiredService<IRecipeService>();

var recipe = await recipes.CreateAsync(authorId, new CreateRecipeRequest(
    Name: "Pfannkuchen", Servings: 4,
    Steps: new[] { "Zutaten verrühren", "In der Pfanne backen" },
    Ingredients: new[] { new RecipeIngredientInput(mehlId, 250, "g") },
    CategoryIds: new[] { fruehstueckId }));
```

Der handelnde Benutzer wird **explizit** übergeben (`authorId`); die Bibliothek ist zustandslos
und kümmert sich nicht um Authentifizierung – nur um Ownership und Attribution.

## Architektur

Aufbau nach der **Dependency Rule** (Abhängigkeiten zeigen nach innen):

```
IRM.Domain         Entities, strongly-typed IDs, Invarianten, Exceptions    – keine Abhängigkeiten
IRM.Application    NUR Interfaces + DTOs + Request-Models (Contract)        – dep: Domain
IRM.Persistence    DbContext, EF-Config, Service-Impls, Migration, DI       – dep: Application, Domain, EF Core
samples/IRM.Demo   Konsolen-App                                             – dep: Persistence (nur Contract + DI)
```

**Grenzen per Compiler/Access Modifier erzwungen:** `DbContext` und Service-*Implementierungen* sind `internal`.
`public` sind nur die Interfaces, DTOs, Exceptions und die DI-Extension `AddRecipeManagement`.

**Öffentliche Services** (alle async, mit `CancellationToken`):
`IUserService`, `IIngredientService`, `ICategoryService`, `IRecipeService`, `IFavoriteService`.

**Fehler** leiten alle von `DomainException` ab: `DomainValidationException`, `DuplicateNameException`,
`UnauthorizedRecipeAccessException`, `InUseException` sowie `…NotFoundException` (je Entity, unter
einer gemeinsamen `NotFoundException`-Basis).

## Scope

**In scope:** Benutzer als Domänen-Entity (Ownership/Attribution), Rezept-/Kategorie-/Zutaten-Verwaltung,
Abfragen (nach Benutzer/Kategorie/Zutat), Favoriten, Autorisierung auf Domänen-Ebene.

**Bewusst out of scope:** Authentifizierung (der Consumer liefert die Identität), Sperren/Aktivierung von Konten (ein Auth-Zustand, gehört zum selben Belang), Rollen/Admin. Das *Wie* der Identitätsprüfung ist ein Infrastruktur-/Querschnittsbelang, der vom Host abhängt — die Bibliothek braucht nur das Ergebnis (wer handelt) und bleibt so zustandslos und einbettbar.

## Design-Entscheidungen (Kurzform)

| Entscheidung | Begründung |
|---|---|
| Authentifizierung out of scope | Bibliothek verwaltet User als Domänen-Konzept, erwartet die Identität vom Consumer |
| Expliziter `userId`-Parameter statt ambient Context | ehrlicher & testbarer für eine zustandslose Bibliothek (explizit > implizit) |
| `bigint identity` + strongly-typed IDs | verständlicher Default; verhindert *primitive obsession* (IDs nicht vertauschbar) |
| Read-DTOs statt Entities an der API | entkoppelt den Contract, kein Leaken von EF-/Domain-Details |
| Exceptions statt `Result<T>` | idiomatisch für eine Consumer-Bibliothek; klare Hierarchie |
| „block if in-use"-default für Delete-Operationen (Kategorien, Zutaten, Benutzer) | vorhersehbar, keine Seiteneffekte; Mechanismus für beliebige Consumer-Policy (z.B. Platzhalter, Delete-Cascade) |
| Struktur nach Dependency Rule, Grenze per `internal` access modifier | siehe Architektur |
| Invarianten im Domain-Model | *make illegal states unrepresentable* (≥1 Schritt/Zutat/Kategorie, keine Duplikate, Step-Order positional) |
| Uniqueness case-insensitiv (`NOCASE`) | Service-Vorabprüfung (freundliche Meldung) + Unique-Index als Absicherung |

## Technischer Stack

- **Persistenz:** EF Core 10 + SQLite (dateibasiert, kein Setup). DB-Initialisierung explizit über
  eine echte Migration (`InitializeRecipesDatabaseAsync`), nicht `EnsureCreated`.
- **Tests:** xUnit + Shouldly. Domain als reine Unit-Tests, Persistence als Integrationstests gegen
  SQLite-in-memory (offene Verbindung je Test, echte Migration angewendet).

EF-Core-Workarounds und das Neu-Erzeugen von Migrationen sind in
[`docs/entwicklungsnotizen.md`](docs/entwicklungsnotizen.md) dokumentiert.
