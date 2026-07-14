using Microsoft.EntityFrameworkCore;

namespace IRM.Persistence.Services;

internal sealed class UserService : IUserService
{
    private readonly RecipeDbContext _db;

    public UserService(RecipeDbContext db) => _db = db;

    public async Task<UserDto> RegisterAsync(string name, CancellationToken ct = default)
    {
        var user = User.Register(name); // validiert + trimmt
        var duplicate = $"Ein Benutzer mit dem Namen '{user.Name}' existiert bereits.";
        if (await _db.Users.AnyAsync(u => u.Name == user.Name, ct))
            throw new DuplicateNameException(duplicate);

        _db.Users.Add(user);
        await _db.SaveTranslatingDuplicateAsync(duplicate, ct);
        return new UserDto(user.Id, user.Name);
    }

    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct = default)
        => await _db.Users
            .OrderBy(u => u.Name)
            .Select(u => new UserDto(u.Id, u.Name))
            .ToListAsync(ct);

    public async Task<UserDto?> GetAsync(UserId id, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([id], ct);
        return user is null ? null : new UserDto(user.Id, user.Name);
    }

    public async Task<UserDto> RenameAsync(UserId id, string newName, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([id], ct)
            ?? throw new UserNotFoundException($"Benutzer {id.Value} wurde nicht gefunden.");

        user.Rename(newName);
        var duplicate = $"Ein Benutzer mit dem Namen '{user.Name}' existiert bereits.";
        if (await _db.Users.AnyAsync(u => u.Id != id && u.Name == user.Name, ct))
            throw new DuplicateNameException(duplicate);

        await _db.SaveTranslatingDuplicateAsync(duplicate, ct);
        return new UserDto(user.Id, user.Name);
    }

    public async Task DeleteAsync(UserId id, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([id], ct)
            ?? throw new UserNotFoundException($"Benutzer {id.Value} wurde nicht gefunden.");

        if (await _db.Recipes.AnyAsync(r => r.OwnerId == id, ct))
            throw new InUseException($"Der Benutzer '{user.Name}' besitzt noch mindestens ein Rezept.");

        // Eigene Favoriten sind Daten des Benutzers (FK Restrict) – vor dem Löschen mit aufräumen.
        var favorites = await _db.Favorites.Where(f => f.UserId == id).ToListAsync(ct);
        _db.Favorites.RemoveRange(favorites);
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
    }
}
