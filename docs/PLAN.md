# Plan: Rezeptverwaltung als .NET-Klassenbibliothek

Planungsdokument für die Bewerbungsaufgabe. Hält Scope, Domain-Model, API-Contract,
Solution-Struktur, die Design-Entscheidungen (als ADRs) und die Test-Strategie fest.

---

## 1. Scope / Boundary

**In scope**
- User als Domänen-Entity (anlegen, auflisten, abfragen, umbenennen, löschen), Ownership, Attribution
- Autorisierung auf Domänen-Ebene (nur der Owner darf sein Rezept ändern/löschen)
- Rezept-, Kategorie- und (globale) Zutaten-Verwaltung
- Abfragen (Rezepte nach User / Kategorie / Zutat) + Favoriten

**Out of scope** (bewusst, in README dokumentieren)
- **Authentication** (Passwörter, Login, Tokens) – der Consumer liefert die Identität
- **Sperren / Aktivierung von Konten** – ist ein Auth-Zustand (steuert Login); gehört zum selben
  ausgelagerten Auth-Service. Ohne Autorisierungs-Gate hier wäre ein `Blocked`-Flag totes Feld.
- **Rollen / Admin-Konzept** – daher sind die globalen Listen nicht user-gated
- **Skalierung** von Rezepten (z.B. 4→2 Personen) – Daten (`Servings`) sind da, das
  Feature ist als Extension offengelassen (bräuchte ein typisiertes Unit-System)

---

## 2. Domain-Model

```
User(Id: UserId, Name)
Ingredient(Id: IngredientId, Name)         // globale, user-unabhängige Liste
Category(Id: CategoryId, Name)

Recipe  ◄── Aggregate Root
  Id: RecipeId
  Name            (global eindeutig)
  OwnerId: UserId
  Servings        (≥ 1)
  Steps           (≥ 1, owned, geordnet)         → Step(Order, Instruction); Order aus Position vergeben
  Ingredients     (≥ 1, owned)                   → RecipeIngredient(IngredientId, Amount, Unit)
  CategoryIds     (≥ 1, referenziert, nicht owned)

Favorite(UserId, RecipeId)                 // User ─*──*─ Recipe
```

**Aggregates**
- `Recipe` ist Aggregate Root und *besitzt* Steps + RecipeIngredient-Verknüpfungen (composition).
- `Ingredient`, `Category`, `User` sind *eigene* Aggregates. `Recipe` referenziert sie nur per Id.
- Neue Zutat/Kategorie anlegen = separater Call, kein implizites Auto-Create beim Recipe-Speichern.

**Typen**
- IDs: `bigint identity`, gekapselt als strongly-typed IDs (`readonly record struct RecipeId(long Value)` …)
- `Amount`: `decimal` (keine Binär-Float-Rundungsfehler)
- `Unit`: `string` (frei – „g", „ml", „Prise", „nach Geschmack")

**Invarianten & wo erzwungen**

| Invariante                                        | Ort                                   |
|---------------------------------------------------|---------------------------------------|
| Recipe hat ≥1 Step / ≥1 Ingredient / ≥1 Category  | **Domain** (Factory/Konstruktor)      |
| Keine doppelte Zutat / Kategorie im selben Rezept  | **Domain** (Factory/Konstruktor)      |
| Recipe.Name global eindeutig                      | **Service**-Prüfung + **DB** unique index |
| Category.Name / Ingredient.Name eindeutig         | **Service** + **DB**                  |
| Nur Owner darf Rezept ändern/löschen              | **Service** (authorization)           |
| Delete einer in-use Category/Ingredient verboten  | **Service** (wirft `…InUseException`) |

Leitprinzip: strukturelle Invarianten im Domain-Model → *make illegal states unrepresentable*.

---

## 3. API-Contract (öffentlich)

Der handelnde User ist **explizit** erster Parameter (stateless Library). `CancellationToken` überall.

```csharp
public interface IUserService {                    // Benutzer-Lifecycle (ohne Auth-Belange)
    Task<UserDto> RegisterAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct = default);
    Task<UserDto?> GetAsync(UserId id, CancellationToken ct = default);
    Task<UserDto> RenameAsync(UserId id, string newName, CancellationToken ct = default);
    Task DeleteAsync(UserId id, CancellationToken ct = default);         // block if owns recipes
}

public interface IIngredientService {              // globale Liste – volles CRUD (Typo-Korrektur etc.)
    Task<IngredientDto> AddAsync(string name, CancellationToken ct = default);
    Task<IngredientDto> RenameAsync(IngredientId id, string newName, CancellationToken ct = default);
    Task DeleteAsync(IngredientId id, CancellationToken ct = default);   // block if in-use
    Task<IReadOnlyList<IngredientDto>> ListAsync(CancellationToken ct = default);
}

public interface ICategoryService {                // volles CRUD
    Task<CategoryDto> CreateAsync(string name, CancellationToken ct = default);
    Task<CategoryDto> RenameAsync(CategoryId id, string newName, CancellationToken ct = default);
    Task DeleteAsync(CategoryId id, CancellationToken ct = default);     // block if in-use
    Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken ct = default);
}

public interface IRecipeService {
    Task<RecipeDto> CreateAsync(UserId author, CreateRecipeRequest request, CancellationToken ct = default);
    Task<RecipeDto> UpdateAsync(UserId editor, RecipeId id, UpdateRecipeRequest request, CancellationToken ct = default);
    Task DeleteAsync(UserId editor, RecipeId id, CancellationToken ct = default);
    Task<RecipeDto?> GetAsync(RecipeId id, CancellationToken ct = default);

    Task<IReadOnlyList<RecipeDto>> GetByUserAsync(UserId userId, CancellationToken ct = default);
    Task<IReadOnlyList<RecipeDto>> GetByCategoryAsync(CategoryId categoryId, CancellationToken ct = default);
    Task<IReadOnlyList<RecipeDto>> GetByIngredientAsync(IngredientId ingredientId, CancellationToken ct = default);
}

public interface IFavoriteService {                // eigener Service = SRP
    Task AddAsync(UserId userId, RecipeId recipeId, CancellationToken ct = default);
    Task RemoveAsync(UserId userId, RecipeId recipeId, CancellationToken ct = default);
    Task<IReadOnlyList<RecipeDto>> GetFavoritesAsync(UserId userId, CancellationToken ct = default);
}
```

**DTOs (Read-Models, records)**
```csharp
record UserDto(UserId Id, string Name);
record IngredientDto(IngredientId Id, string Name);
record CategoryDto(CategoryId Id, string Name);
record StepDto(int Order, string Instruction);
record RecipeIngredientDto(IngredientDto Ingredient, decimal Amount, string Unit);
record RecipeDto(RecipeId Id, string Name, UserId AuthorId, int Servings,
                 IReadOnlyList<StepDto> Steps,
                 IReadOnlyList<RecipeIngredientDto> Ingredients,
                 IReadOnlyList<CategoryDto> Categories);
```

**Request-Models (Input, ohne Id)**
```csharp
record RecipeIngredientInput(IngredientId IngredientId, decimal Amount, string Unit);
record CreateRecipeRequest(string Name, int Servings,
                           IReadOnlyList<string> Steps,               // Order ergibt sich aus der Position
                           IReadOnlyList<RecipeIngredientInput> Ingredients,
                           IReadOnlyList<CategoryId> CategoryIds);
record UpdateRecipeRequest(...);   // wie Create; ersetzt Name/Servings/Steps/Ingredients/Categories vollständig
```

**Exception-Hierarchie**
```
DomainException                         (Basis)
 ├─ DuplicateNameException              (Recipe / Category / Ingredient)
 ├─ RecipeNotFoundException  (o.ä. NotFound je Entity)
 ├─ UnauthorizedRecipeAccessException   (nicht der Owner)
 └─ …InUseException                     (Category / Ingredient noch referenziert)
```

---

## 4. Solution-Struktur

Layering nach der **Dependency Rule** (Deps zeigen nach innen):

```
IRM.sln
├─ src/
│  ├─ IRM.Domain/        Entities, IDs, Exceptions, Factories (Invarianten)   – keine Deps
│  ├─ IRM.Application/   NUR Interfaces + DTOs + Request-Models (Contract)     – dep: Domain
│  └─ IRM.Persistence/   DbContext, EF-Config, Service-Impls, Migrations,
│                            AddRecipeManagement()                                  – dep: Application, Domain, EF Core
├─ samples/
│  └─ IRM.Demo/          Console-App (nur Contract + DI-Extension)             – dep: Persistence
└─ tests/
   ├─ IRM.Domain.Tests/       Invarianten – reine Unit-Tests
   └─ IRM.Persistence.Tests/  Services als Integration-Tests gegen SQLite in-memory
```

**Boundary-Enforcement:** `DbContext`, EF-Konfigurationen und Service-*Implementierungen* sind
`internal`. `public` sind nur Interfaces, DTOs, Exceptions und die DI-Extension. → Die Demo *kann*
die interna nicht anfassen (Compiler-erzwungen).

**Wiring** (Consumer-Sicht):
```csharp
var services = new ServiceCollection();
services.AddRecipeManagement(o => o.UseSqlite("Data Source=recipes.db"));
var sp = services.BuildServiceProvider();

await sp.InitializeRecipesDatabaseAsync();          // wendet EF-Migration an – explizit
var recipes = sp.GetRequiredService<IRecipeService>();
```

- Persistence: **EF Core + SQLite** (dateibasiert, null Setup)
- DB-Init **explizit** + echte EF-**Migration** (produktionsnäher als `EnsureCreated`)

---

## 5. Design-Entscheidungen (ADRs – Kurzform für README/Gespräch)

1. **Authentication out of scope.** Library verwaltet User als Domänen-Konzept (Ownership,
   Attribution), erwartet die Identität vom Consumer. Hält Verantwortlichkeiten getrennt.
2. **Expliziter `userId`-Parameter** statt ambient `IUserContext`. Ehrlicher & testbarer für eine
   Library, die auch ohne Request-Kontext läuft. `IUserContext` wäre YAGNI. *(explizit > implizit)*
3. **`bigint identity` + strongly-typed IDs.** `bigint` = verständlicher Default, im Scope kein Grund
   gegen DB-Roundtrip. Strongly-typed verhindert *primitive obsession* (nackte `long`s vertauschbar).
4. **`Servings` modelliert, Skalierung nicht implementiert.** Realistische Metadaten, aber sauberes
   Scaling bräuchte ein typisiertes Unit-System → bewusst als Extension offengelassen.
5. **Read-DTOs statt Entities** an der öffentlichen API. Entkoppelt Contract, verhindert Leaken von
   EF-/Domain-Details. Kosten: etwas Mapping.
6. **Exceptions statt `Result<T>`.** Idiomatisch für eine Consumer-Library; kein built-in `Result` in
   .NET. Klare Custom-Hierarchie.
7. **Delete-Policy „block if in-use"** für Category, Ingredient *und* User (symmetrisch). Einfach,
   vorhersehbar, keine überraschenden Seiteneffekte auf fremde Rezepte. Beim User ist „in use" =
   „besitzt Rezepte"; eigene Favoriten (reine Link-Zeilen) werden mit aufgeräumt. Der Block ist der
   *Mechanismus*, auf dem ein aufrufender Service beliebige *Policy* baut (Platzhalter-Owner,
   DSGVO-Cascade) – Mechanism, not policy.
   *(Anmerkung: Update/Delete für User sind von der Aufgabe nicht explizit verlangt – anders als bei
   Rezept/Kategorie. Bewusst ergänzt, weil „Benutzerverwaltung" den Lifecycle üblicherweise umfasst
   und ein ausgelagerter Auth-Service diese Operationen braucht; in einem echten Projekt eine
   Rückfrage wert.)*
8. **Globale Listen nicht user-gated.** Literale Lesart der Aufgabe (nur *Rezepte* verlangen einen
   registrierten User). Keine Rollen → keine künstliche Gate. Als Annahme dokumentiert.
9. **Keine Repository-Abstraktion (B).** `DbContext` ist bereits Unit of Work, `DbSet`s sind
   Repositories. Ein Repo würde nur vor dem Ersetzen von *EF selbst* schützen (nicht vor DBMS-Wechsel,
   das können EF-Provider) → YAGNI. Application bleibt EF-frei; Service-Impls liegen in Persistence.
10. **Struktur nach Dependency Rule**, Boundary per `internal` erzwungen.
11. **Fachliche Exception-Messages auf Deutsch**, Code-Identifier/Typnamen englisch. Passt zur
    erwarteten deutschsprachigen Doku, ohne die API-Sprache zu vermischen.
12. **Step-Order positional** (Input = Liste von Instruction-Strings, `Recipe` vergibt `Order`) und
    **keine doppelten Zutaten/Kategorien** pro Rezept – beide Regeln im Domain-Model erzwungen.

---

## 6. Test-Strategie

- **Domain.Tests** (Unit): Invarianten – ungültige Recipes lassen sich nicht konstruieren; strongly-typed IDs.
- **Persistence.Tests** (Integration, SQLite in-memory): Services end-to-end – Uniqueness, Owner-Autorisierung,
  in-use-Delete-Policy, alle Query-Pfade (nach User/Kategorie/Zutat), Favoriten.
- Framework: **xUnit**. Für Assertions **Shouldly** oder plain `Assert` – FluentAssertions ist seit v8
  kommerziell lizenziert, das umgehen wir bewusst.

---

## 7. Empfohlene Implementierungs-Reihenfolge

1. `IRM.Domain` (Entities, IDs, Invarianten) + `Domain.Tests` → **verify:** Invarianten-Tests grün
2. `IRM.Application` (Interfaces, DTOs, Requests) → **verify:** kompiliert, Contract vollständig
3. `IRM.Persistence` (DbContext, EF-Config, Migration, Service-Impls) + `Persistence.Tests`
   → **verify:** Integration-Tests grün gegen SQLite in-memory
4. `AddRecipeManagement()` + `InitializeRecipesDatabaseAsync()` → **verify:** Wiring läuft
5. `IRM.Demo` – typische Use-Cases über die API → **verify:** läuft end-to-end gegen echte SQLite-Datei
6. `README` – Install/Start + Architekturüberblick + ADRs aus Abschnitt 5

**Target Framework:** .NET 10 (aktuelles LTS).
