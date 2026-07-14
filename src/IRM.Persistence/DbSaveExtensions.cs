using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IRM.Persistence;

/// <summary>
/// Übersetzt eine race-bedingte Unique-Constraint-Verletzung in die fachliche
/// <see cref="DuplicateNameException"/>. Die Services prüfen Namens-Eindeutigkeit zwar vorab,
/// doch zwischen Prüfung und Insert können zwei gleichzeitige Aufrufe beide bestehen – erst der
/// DB-Index kollidiert. Ohne diese Übersetzung dränge in genau diesem Wettlauf eine rohe
/// <see cref="DbUpdateException"/> nach außen statt der dokumentierten API-Exception.
/// </summary>
internal static class DbSaveExtensions
{
    // SQLITE_CONSTRAINT_UNIQUE (19 | 8<<8) – nur der Unique-Index, nicht FK/NOT NULL/PK.
    private const int SqliteUniqueViolation = 2067;

    public static async Task SaveTranslatingDuplicateAsync(
        this RecipeDbContext db, string duplicateMessage, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteUniqueViolation })
        {
            throw new DuplicateNameException(duplicateMessage);
        }
    }
}
