using Microsoft.EntityFrameworkCore;
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
        await db.SaveChangesAsync();
        return (true, user.Id);
    }

    public Task<bool> ExistsAsync(string sub) =>
        db.Users.AnyAsync(u => u.Sub == sub);

    public async Task<UserDto?> GetAsync(string sub) =>
        await db.Users
            .Where(u => u.Sub == sub)
            .Select(u => new UserDto(u.Id, u.Sub, u.Email, u.CreatedAt))
            .FirstOrDefaultAsync();
}
