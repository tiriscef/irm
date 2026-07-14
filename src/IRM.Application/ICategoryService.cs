namespace IRM.Application;

/// <summary>Verwaltung der globalen Kategorienliste (volles CRUD).</summary>
public interface ICategoryService
{
    Task<CategoryDto> CreateAsync(string name, CancellationToken ct = default);
    Task<CategoryDto> RenameAsync(CategoryId id, string newName, CancellationToken ct = default);

    /// <summary>Löscht eine Kategorie; wirft <see cref="InUseException"/>, wenn sie noch verwendet wird.</summary>
    Task DeleteAsync(CategoryId id, CancellationToken ct = default);

    Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken ct = default);
}
