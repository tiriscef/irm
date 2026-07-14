namespace IRM.Application;

/// <summary>Verwaltung der globalen Zutatenliste (volles CRUD, z.B. für Tippfehler-Korrektur).</summary>
public interface IIngredientService
{
    Task<IngredientDto> AddAsync(string name, CancellationToken ct = default);
    Task<IngredientDto> RenameAsync(IngredientId id, string newName, CancellationToken ct = default);

    /// <summary>Löscht eine Zutat; wirft <see cref="InUseException"/>, wenn sie noch verwendet wird.</summary>
    Task DeleteAsync(IngredientId id, CancellationToken ct = default);

    Task<IReadOnlyList<IngredientDto>> ListAsync(CancellationToken ct = default);
}
