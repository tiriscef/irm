using System.Globalization;
using IRM.Application;
using IRM.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// Demo der Rezeptverwaltung. Zwei Modi, beide nutzen ausschließlich die DI-Extension und die
// Service-Interfaces (kein Zugriff auf Persistenz oder Interna):
//   dotnet run                 -> scripted: fester Ablauf typischer Use-Cases
//   dotnet run -- interactive  -> interaktives Menü zum manuellen Verifizieren
// Der interaktive Modus ist eine reine I/O-Schale: jede Auswahl mappt 1:1 auf einen Service-Aufruf,
// alle fachlichen Regeln kommen als Exceptions aus der Bibliothek zurück (keine Logik in der Demo).

// Ausgabe explizit auf UTF-8, sonst zeigen ältere Windows-Konsolen (nicht-UTF-8-Codepage)
// die Umlaute und die '→'/'✗'-Symbole als '?' an.
Console.OutputEncoding = System.Text.Encoding.UTF8;

var interactive = args.Any(a => string.Equals(a, "interactive", StringComparison.OrdinalIgnoreCase));
var dbPath = Path.Combine(AppContext.BaseDirectory, "recipes.db");

// Scripted startet frisch (wiederholbar); interaktiv behält seine Daten über Läufe hinweg.
if (!interactive && File.Exists(dbPath))
    File.Delete(dbPath);

var services = new ServiceCollection();
services.AddRecipeManagement(o => o.UseSqlite($"Data Source={dbPath}"));
await using var provider = services.BuildServiceProvider();

await provider.InitializeRecipesDatabaseAsync();

using var scope = provider.CreateScope();
var users = scope.ServiceProvider.GetRequiredService<IUserService>();
var ingredients = scope.ServiceProvider.GetRequiredService<IIngredientService>();
var categories = scope.ServiceProvider.GetRequiredService<ICategoryService>();
var recipes = scope.ServiceProvider.GetRequiredService<IRecipeService>();
var favorites = scope.ServiceProvider.GetRequiredService<IFavoriteService>();

if (interactive)
    await RunInteractiveAsync();
else
    await RunScriptedAsync();

// ======================================================================================
// Scripted: fester Ablauf, zeigt die typischen Use-Cases deterministisch (inkl. Fehlerfälle).
// ======================================================================================
async Task RunScriptedAsync()
{
    Console.WriteLine($"Datenbank initialisiert: {dbPath}\n");

    // 1) Benutzer registrieren
    var isa = await users.RegisterAsync("Isa");
    var bob = await users.RegisterAsync("Bob");
    Console.WriteLine($"Registriert: {isa.Name} (#{isa.Id.Value}), {bob.Name} (#{bob.Id.Value})\n");

    // 2) Globale Zutaten- und Kategorienliste füllen
    var flour = await ingredients.AddAsync("Mehl");
    var milk = await ingredients.AddAsync("Milch");
    var egg = await ingredients.AddAsync("Ei");
    var sugar = await ingredients.AddAsync("Zucker");
    var breakfast = await categories.CreateAsync("Frühstück");
    var dessert = await categories.CreateAsync("Dessert");

    // 3) Zutat umbenennen (Tippfehler-Korrektur auf der globalen Liste)
    sugar = await ingredients.RenameAsync(sugar.Id, "Rohrzucker");
    Console.WriteLine("Zutaten: " + string.Join(", ", (await ingredients.ListAsync()).Select(i => i.Name)));
    Console.WriteLine("Kategorien: " + string.Join(", ", (await categories.ListAsync()).Select(c => c.Name)) + "\n");

    // 4) Rezept anlegen (als Isa)
    var pancakes = await recipes.CreateAsync(isa.Id, new CreateRecipeRequest(
        Name: "Pfannkuchen",
        Servings: 4,
        Steps: new[] { "Mehl, Milch und Eier verrühren", "Teig ruhen lassen", "In der Pfanne goldbraun backen" },
        Ingredients: new[]
        {
            new RecipeIngredientInput(flour.Id, 250, "g"),
            new RecipeIngredientInput(milk.Id, 500, "ml"),
            new RecipeIngredientInput(egg.Id, 3, "Stück"),
        },
        CategoryIds: new[] { breakfast.Id, dessert.Id }));
    Console.WriteLine("Rezept angelegt:");
    PrintRecipe(pancakes);

    // 5) Rezept aktualisieren (Zucker ergänzen, Portionen anpassen)
    pancakes = await recipes.UpdateAsync(isa.Id, pancakes.Id, new UpdateRecipeRequest(
        Name: "Pfannkuchen",
        Servings: 2,
        Steps: new[] { "Alle Zutaten verrühren", "In der Pfanne backen" },
        Ingredients: new[]
        {
            new RecipeIngredientInput(flour.Id, 125, "g"),
            new RecipeIngredientInput(milk.Id, 250, "ml"),
            new RecipeIngredientInput(egg.Id, 2, "Stück"),
            new RecipeIngredientInput(sugar.Id, 1, "EL"),
        },
        CategoryIds: new[] { breakfast.Id }));
    Console.WriteLine("Nach Update:");
    PrintRecipe(pancakes);

    // 6) Abfragen
    Console.WriteLine("Rezepte von Isa:        " + Names(await recipes.GetByUserAsync(isa.Id)));
    Console.WriteLine("Rezepte in 'Frühstück': " + Names(await recipes.GetByCategoryAsync(breakfast.Id)));
    Console.WriteLine("Rezepte mit 'Mehl':     " + Names(await recipes.GetByIngredientAsync(flour.Id)) + "\n");

    // 7) Favoriten
    await favorites.AddAsync(bob.Id, pancakes.Id);
    Console.WriteLine("Bobs Favoriten: " + Names(await favorites.GetFavoritesAsync(bob.Id)) + "\n");

    // 8) Domänen-Regeln greifen: fremdes Rezept ändern schlägt fehl
    try
    {
        await recipes.UpdateAsync(bob.Id, pancakes.Id, new UpdateRecipeRequest(
            "Bobs Pfannkuchen", 1, new[] { "x" },
            new[] { new RecipeIngredientInput(flour.Id, 1, "g") }, new[] { breakfast.Id }));
    }
    catch (UnauthorizedRecipeAccessException ex)
    {
        Console.WriteLine($"Erwartet abgelehnt (nicht der Owner): {ex.Message}");
    }

    // 9) In-use-Löschschutz: verwendete Kategorie lässt sich nicht löschen
    try
    {
        await categories.DeleteAsync(breakfast.Id);
    }
    catch (InUseException ex)
    {
        Console.WriteLine($"Erwartet abgelehnt (noch in Verwendung): {ex.Message}");
    }

    Console.WriteLine("\nFertig.");
}

// ======================================================================================
// Interaktiv: dünne I/O-Schale. Ein Menü, ein switch, ein try/catch – jede fachliche
// Regelverletzung landet einheitlich im DomainException-Handler.
// ======================================================================================
async Task RunInteractiveAsync()
{
    Console.WriteLine($"Interaktiver Modus – Datenbank: {dbPath}");

    while (true)
    {
        Console.WriteLine();
        Console.WriteLine("Benutzer:  [1] +   [2] Liste   [3] umbenennen   [4] löschen");
        Console.WriteLine("Zutat:     [5] +   [6] Liste   [7] umbenennen   [8] löschen");
        Console.WriteLine("Kategorie: [9] +   [10] Liste  [11] umbenennen  [12] löschen");
        Console.WriteLine("Rezept:    [13] +  [14] anzeigen  [15] ändern    [16] löschen");
        Console.WriteLine("Abfrage:   [17] nach Benutzer  [18] nach Kategorie  [19] nach Zutat");
        Console.WriteLine("Favorit:   [20] markieren  [21] entfernen  [22] Liste");
        Console.WriteLine("[0] Ende");
        Console.Write("> ");

        var choice = Console.ReadLine()?.Trim();
        if (choice == "0")
            return;

        try
        {
            switch (choice)
            {
                case "1":
                    var u = await users.RegisterAsync(Prompt("Name"));
                    Console.WriteLine($"→ #{u.Id.Value} {u.Name}");
                    break;
                case "2":
                    foreach (var x in await users.ListAsync())
                        Console.WriteLine($"  #{x.Id.Value} {x.Name}");
                    break;
                case "3":
                    var ur = await users.RenameAsync(new UserId(PromptLong("Benutzer-Id")), Prompt("Neuer Name"));
                    Console.WriteLine($"→ #{ur.Id.Value} {ur.Name}");
                    break;
                case "4":
                    await users.DeleteAsync(new UserId(PromptLong("Benutzer-Id")));
                    Console.WriteLine("→ gelöscht");
                    break;
                case "5":
                    var i = await ingredients.AddAsync(Prompt("Zutat"));
                    Console.WriteLine($"→ #{i.Id.Value} {i.Name}");
                    break;
                case "6":
                    foreach (var x in await ingredients.ListAsync())
                        Console.WriteLine($"  #{x.Id.Value} {x.Name}");
                    break;
                case "7":
                    var ir = await ingredients.RenameAsync(new IngredientId(PromptLong("Zutat-Id")), Prompt("Neuer Name"));
                    Console.WriteLine($"→ #{ir.Id.Value} {ir.Name}");
                    break;
                case "8":
                    await ingredients.DeleteAsync(new IngredientId(PromptLong("Zutat-Id")));
                    Console.WriteLine("→ gelöscht");
                    break;
                case "9":
                    var c = await categories.CreateAsync(Prompt("Kategorie"));
                    Console.WriteLine($"→ #{c.Id.Value} {c.Name}");
                    break;
                case "10":
                    foreach (var x in await categories.ListAsync())
                        Console.WriteLine($"  #{x.Id.Value} {x.Name}");
                    break;
                case "11":
                    var cr = await categories.RenameAsync(new CategoryId(PromptLong("Kategorie-Id")), Prompt("Neuer Name"));
                    Console.WriteLine($"→ #{cr.Id.Value} {cr.Name}");
                    break;
                case "12":
                    await categories.DeleteAsync(new CategoryId(PromptLong("Kategorie-Id")));
                    Console.WriteLine("→ gelöscht");
                    break;
                case "13":
                    var author = new UserId(PromptLong("Autor-Id"));
                    var create = await PromptRecipeBodyAsync();
                    var recipe = await recipes.CreateAsync(author, new CreateRecipeRequest(
                        create.Name, create.Servings, create.Steps, create.Ingredients, create.Categories));
                    Console.WriteLine($"→ #{recipe.Id.Value} '{recipe.Name}' angelegt");
                    break;
                case "14":
                    var found = await recipes.GetAsync(new RecipeId(PromptLong("Rezept-Id")));
                    if (found is null)
                        Console.WriteLine("  (nicht gefunden)");
                    else
                        PrintRecipe(found);
                    break;
                case "15":
                    var editor = new UserId(PromptLong("Editor-Id"));
                    var recipeId = new RecipeId(PromptLong("Rezept-Id"));
                    var edit = await PromptRecipeBodyAsync();
                    var updated = await recipes.UpdateAsync(editor, recipeId, new UpdateRecipeRequest(
                        edit.Name, edit.Servings, edit.Steps, edit.Ingredients, edit.Categories));
                    Console.WriteLine($"→ #{updated.Id.Value} '{updated.Name}' aktualisiert");
                    break;
                case "16":
                    await recipes.DeleteAsync(new UserId(PromptLong("Editor-Id")), new RecipeId(PromptLong("Rezept-Id")));
                    Console.WriteLine("→ gelöscht");
                    break;
                case "17":
                    PrintList(await recipes.GetByUserAsync(new UserId(PromptLong("Benutzer-Id"))));
                    break;
                case "18":
                    PrintList(await recipes.GetByCategoryAsync(new CategoryId(PromptLong("Kategorie-Id"))));
                    break;
                case "19":
                    PrintList(await recipes.GetByIngredientAsync(new IngredientId(PromptLong("Zutat-Id"))));
                    break;
                case "20":
                    await favorites.AddAsync(new UserId(PromptLong("Benutzer-Id")), new RecipeId(PromptLong("Rezept-Id")));
                    Console.WriteLine("→ als Favorit markiert");
                    break;
                case "21":
                    await favorites.RemoveAsync(new UserId(PromptLong("Benutzer-Id")), new RecipeId(PromptLong("Rezept-Id")));
                    Console.WriteLine("→ Favorit entfernt");
                    break;
                case "22":
                    PrintList(await favorites.GetFavoritesAsync(new UserId(PromptLong("Benutzer-Id"))));
                    break;
                default:
                    Console.WriteLine("Unbekannte Auswahl.");
                    break;
            }
        }
        catch (DomainException ex)
        {
            // Alle fachlichen Regeln (Duplikat, unbekannt, nicht Owner, in Verwendung, …) landen hier.
            Console.WriteLine($"✗ {ex.Message}");
        }
        catch (Exception ex)
        {
            // Reine Eingabefehler (z.B. keine Zahl) sollen die Sitzung nicht beenden.
            Console.WriteLine($"✗ Ungültige Eingabe: {ex.Message}");
        }
    }

    // Gemeinsamer Rumpf für Anlegen/Ändern – Create- und UpdateRequest haben dieselbe Form.
    async Task<(string Name, int Servings, IReadOnlyList<string> Steps,
                IReadOnlyList<RecipeIngredientInput> Ingredients, IReadOnlyList<CategoryId> Categories)>
        PromptRecipeBodyAsync()
        => (Prompt("Rezeptname"),
            PromptInt("Portionen"),
            PromptLines("Schritte (Leerzeile beendet)"),
            await PromptIngredientsAsync(),
            await PromptCategoriesAsync());

    async Task<IReadOnlyList<RecipeIngredientInput>> PromptIngredientsAsync()
    {
        Console.WriteLine("  Verfügbar: " +
            string.Join(", ", (await ingredients.ListAsync()).Select(x => $"#{x.Id.Value} {x.Name}")));
        Console.WriteLine("  Zutaten als 'id menge einheit' (Leerzeile beendet):");
        var list = new List<RecipeIngredientInput>();
        for (string? line; !string.IsNullOrWhiteSpace(line = Console.ReadLine());)
        {
            var p = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            list.Add(new RecipeIngredientInput(
                new IngredientId(long.Parse(p[0])), decimal.Parse(p[1], CultureInfo.InvariantCulture), p[2]));
        }
        return list;
    }

    async Task<IReadOnlyList<CategoryId>> PromptCategoriesAsync()
    {
        Console.WriteLine("  Verfügbar: " +
            string.Join(", ", (await categories.ListAsync()).Select(x => $"#{x.Id.Value} {x.Name}")));
        return Prompt("  Kategorie-Ids (komma-getrennt)")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => new CategoryId(long.Parse(s)))
            .ToList();
    }
}

// ===== gemeinsame Ausgabe-/Eingabe-Helfer (reine I/O, ohne fachliche Bedeutung) =====

static string Names(IReadOnlyList<RecipeDto> list)
    => list.Count == 0 ? "(keine)" : string.Join(", ", list.Select(r => r.Name));

static void PrintList(IReadOnlyList<RecipeDto> list)
{
    if (list.Count == 0)
    {
        Console.WriteLine("  (keine)");
        return;
    }
    foreach (var r in list)
        Console.WriteLine($"  #{r.Id.Value} {r.Name}");
}

static void PrintRecipe(RecipeDto r)
{
    Console.WriteLine($"  {r.Name} — {r.Servings} Portionen — Kategorien: {string.Join(", ", r.Categories.Select(c => c.Name))}");
    foreach (var step in r.Steps)
        Console.WriteLine($"    {step.Order}. {step.Instruction}");
    foreach (var ing in r.Ingredients)
        Console.WriteLine($"    - {ing.Amount:0.##} {ing.Unit} {ing.Ingredient.Name}");
    Console.WriteLine();
}

static string Prompt(string label)
{
    Console.Write($"{label}: ");
    return Console.ReadLine() ?? "";
}

static int PromptInt(string label) => int.Parse(Prompt(label));

static long PromptLong(string label) => long.Parse(Prompt(label));

static IReadOnlyList<string> PromptLines(string label)
{
    Console.WriteLine(label + ":");
    var lines = new List<string>();
    for (string? l; !string.IsNullOrWhiteSpace(l = Console.ReadLine());)
        lines.Add(l);
    return lines;
}
