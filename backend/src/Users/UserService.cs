using Microsoft.EntityFrameworkCore;
using Npgsql;
using Users.Contracts;

namespace Users;

internal sealed class UserService(UsersDbContext db) : IUserService
{
    public async Task<(bool wasCreated, Guid userId)> EnsureProvisionedAsync(string sub, string email)
    {
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Sub == sub);
        if (existing is not null)
            return (false, existing.Id);

        var user = new User { Id = Guid.NewGuid(), Sub = sub, Email = email, CreatedAt = DateTime.UtcNow };
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync();
            return (true, user.Id);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            db.ChangeTracker.Clear();
            var raced = await db.Users.FirstAsync(u => u.Sub == sub);
            return (false, raced.Id);
        }
    }

    public Task<bool> ExistsAsync(string sub) =>
        db.Users.AnyAsync(u => u.Sub == sub);

    public async Task<UserDto?> GetAsync(string sub) =>
        await db.Users
            .Where(u => u.Sub == sub)
            .Select(u => new UserDto(u.Id, u.Sub, u.Email, u.CreatedAt))
            .FirstOrDefaultAsync();
}
