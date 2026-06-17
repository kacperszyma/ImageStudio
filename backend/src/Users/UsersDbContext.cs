using Microsoft.EntityFrameworkCore;

namespace Users;

internal sealed class UsersDbContext(DbContextOptions<UsersDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("users");
        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Sub);
            e.Property(x => x.Sub).HasMaxLength(128);
        });
    }
}
