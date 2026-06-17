using Microsoft.EntityFrameworkCore;

namespace Generation;

internal sealed class GenerationDbContext(DbContextOptions<GenerationDbContext> options)
    : DbContext(options)
{
    public DbSet<GenerationInstance> Generations => Set<GenerationInstance>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("generation");
        b.Entity<GenerationInstance>(e =>
        {
            e.ToTable("generations");
            e.HasKey(x => x.Id);
        });
    }
}