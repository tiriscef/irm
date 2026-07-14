namespace IRM.Application;

/// <summary>Registrierung, Abfrage und Verwaltung von Benutzern.</summary>
public interface IUserService
{
    Task<UserDto> RegisterAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct = default);

    /// <summary>Liefert den Benutzer oder <c>null</c>, wenn keiner mit dieser Id existiert.</summary>
    Task<UserDto?> GetAsync(UserId id, CancellationToken ct = default);

    Task<UserDto> RenameAsync(UserId id, string newName, CancellationToken ct = default);

    /// <summary>
    /// Löscht einen Benutzer samt seiner Favoriten-Zuordnungen; wirft <see cref="InUseException"/>,
    /// wenn er noch Rezepte besitzt. Weitergehende Policies (Platzhalter-Owner, DSGVO-Cascade)
    /// bleiben dem aufrufenden Service überlassen.
    /// </summary>
    Task DeleteAsync(UserId id, CancellationToken ct = default);
}
