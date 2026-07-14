# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet build                                     # whole solution (net10.0)
dotnet test                                      # all tests
dotnet test tests/IRM.Domain.Tests               # one project
dotnet test --filter "FullyQualifiedName~RecipeServiceTests.Update_by_non_owner_throws"   # one test
dotnet run --project samples/IRM.Demo            # scripted end-to-end demo against a real recipes.db
dotnet run --project samples/IRM.Demo -- interactive   # same functionality via an interactive menu
```

Regenerating the EF migration (only when the model changes) needs the global tool, which is **not on PATH by default**:

```bash
dotnet tool install --global dotnet-ef --version "10.*"
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet ef migrations add <Name> --project src/IRM.Persistence
```

After any model change, verify the migration matches: `dotnet ef migrations has-pending-model-changes --project src/IRM.Persistence` (see the autoincrement caveat below — it reports a false positive).

`docs/PLAN.md` is currently the design source of truth (scope, domain model, ADRs, test strategy). Consult it before changing architecture.

## Architecture

Four projects layered by the **Dependency Rule** (dependencies point inward):

- **IRM.Domain** — entities, strongly-typed IDs, invariants, exceptions. No dependencies.
- **IRM.Application** — the public contract: service interfaces + DTOs + request records only. EF-free. Depends on Domain.
- **IRM.Persistence** — `DbContext`, EF config, service implementations, migration, DI extension. Depends on Application + Domain + EF Core.
- **samples/IRM.Demo** — references *only* Persistence; reaches the domain solely through `IRM.Application` interfaces + the DI extension.

**The `internal` boundary is the core design constraint.** `RecipeDbContext` and all service implementations are `internal`. The only `public` surface is: the service interfaces, DTOs, exceptions, and `AddRecipeManagement()` / `InitializeRecipesDatabaseAsync()`. Do not make persistence types public — the boundary is compiler-enforced and the Demo proves it. `IRM.Domain` grants `InternalsVisibleTo` to `IRM.Persistence` (for mapping the internal `RecipeCategory`); `IRM.Persistence` grants it to `IRM.Persistence.Tests`.

**Invariants live in the Domain, not the services.** `Recipe` is the aggregate root and cannot be constructed in an invalid state (≥1 step/ingredient/category, no duplicates, positional step order). Services enforce only the cross-aggregate rules: name uniqueness, owner authorization, and the "block if in-use" delete policy. When adding a rule, decide which layer owns it — structural → Domain factory/`SetContent`; requires a DB query → service.

**Strongly-typed IDs** (`readonly record struct RecipeId(long Value)`, etc.) map to `long` via value converters registered centrally in `RecipeDbContext.ConfigureConventions`. New ID types need a converter in `IdConverters.cs` and a registration there.

**The `Recipe → Category` link is a deliberate workaround.** `CategoryId` is a struct and can't be an EF owned type, so the aggregate holds an internal `List<RecipeCategory>` (a reference-type link entity) and projects the public `IReadOnlyList<CategoryId> CategoryIds` from it. `Steps` and `RecipeIngredient`s are owned collections mapped via their public navigations; the category link is mapped via the internal `Categories` navigation. All three are queried through the owner (`r.Ingredients.Any(...)`, `r.Categories.Any(...)`) — there are **no EF navigations from `Recipe` to the `Ingredient`/`Category` aggregates** (aggregate boundary). Consequently, building a `RecipeDto` resolves ingredient/category names via separate lookups in `RecipeMapper`, never a cross-aggregate `Include`.

**Exceptions** all derive from `DomainException`. Fachliche (user-facing) messages are **German**; code identifiers and type names are **English**. Keep this split when adding messages.

## Two EF workarounds (don't "fix" these blindly)

- **`PendingModelChangesWarning` is suppressed** in `RecipeDbContext.OnConfiguring`. EF Core 10 applies the `Sqlite:Autoincrement` annotation to value-converted keys non-deterministically across processes, so `Migrate()` and `has-pending-model-changes` report false model drift. The schema (`INTEGER PRIMARY KEY`) is unaffected. Removing the suppression will break `Migrate()` at runtime again.
- **`SQLitePCLRaw.lib.e_sqlite3` is pinned to `3.53.3`** in `IRM.Persistence.csproj` to clear a transitive `NU1903` advisory from EF's default native SQLite binary.

## Testing

xUnit + Shouldly (FluentAssertions is avoided — v8 is commercially licensed). Domain tests are pure unit tests. Persistence tests are integration tests: `DatabaseTest` gives each test its own open `:memory:` SQLite connection (the DB lives only while the connection is open) and applies the **real migration** (not `EnsureCreated`). Services are resolved with a **fresh `DbContext` per call** to mirror scoped-per-operation usage and avoid identity-map false positives — follow that pattern (`Users()`, `Recipes()`, seed helpers) when adding tests.
