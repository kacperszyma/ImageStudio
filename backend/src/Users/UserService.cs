using Microsoft.EntityFrameworkCore;
using Users.Contracts;

namespace Users;

internal sealed class UserService(UsersDbContext db) : IUserService
{
    public async Task EnsureProvisioned(string sub)
    {
        if (!await db.Users.AnyAsync(u => u.Sub == sub))
        {
            db.Users.Add(new User { Sub = sub, CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
    }

    public async Task<bool> TryCreate(string sub)
    {
        if (await db.Users.AnyAsync(u => u.Sub == sub))
            return false;

        db.Users.Add(new User { Sub = sub, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        return true;
    }

    public Task<bool> Exists(string sub) =>
        db.Users.AnyAsync(u => u.Sub == sub);

    public async Task<UserDto?> Get(string sub) =>
        await db.Users
            .Where(u => u.Sub == sub)
            .Select(u => new UserDto(u.Sub, u.CreatedAt))
            .FirstOrDefaultAsync();
}
