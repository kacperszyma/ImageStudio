using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using Users.Contracts;

namespace Users;

internal sealed class UserService(UsersDbContext db, IMemoryCache cache) : IUserService
{
    // The sub -> userId mapping is permanent once provisioned, so a cache hit
    // never goes stale; this TTL just bounds memory for inactive users rather
    // than guarding against the value changing underneath us.
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    public async Task<(bool wasCreated, Guid userId)> EnsureProvisionedAsync(string sub, string email)
    {
        if (cache.TryGetValue(CacheKey(sub), out Guid cachedUserId))
            return (false, cachedUserId);

        var existing = await db.Users.FirstOrDefaultAsync(u => u.Sub == sub);
        if (existing is not null)
        {
            cache.Set(CacheKey(sub), existing.Id, CacheTtl);
            return (false, existing.Id);
        }

        var user = new User { Id = Guid.NewGuid(), Sub = sub, Email = email, CreatedAt = DateTime.UtcNow };
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync();
            cache.Set(CacheKey(sub), user.Id, CacheTtl);
            return (true, user.Id);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            db.ChangeTracker.Clear();
            var raced = await db.Users.FirstAsync(u => u.Sub == sub);
            cache.Set(CacheKey(sub), raced.Id, CacheTtl);
            return (false, raced.Id);
        }
    }

    private static string CacheKey(string sub) => $"user:sub:{sub}";

    public Task<bool> ExistsAsync(string sub) =>
        db.Users.AnyAsync(u => u.Sub == sub);

    public async Task<UserDto?> GetAsync(string sub) =>
        await db.Users
            .Where(u => u.Sub == sub)
            .Select(u => new UserDto(u.Id, u.Sub, u.Email, u.CreatedAt))
            .FirstOrDefaultAsync();
}
