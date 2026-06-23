using Microsoft.EntityFrameworkCore;

namespace GenerationManager;

internal sealed class GenerationManagerDbContext(DbContextOptions<GenerationManagerDbContext> options)
    : DbContext(options)
{
    public DbSet<GenerationJob> Jobs => Set<GenerationJob>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("generation_manager");
        b.Entity<GenerationJob>(e =>
        {
            e.ToTable("generation_jobs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>();
            // Webhook correlates on this; unique keeps Fal's retries idempotent.
            // Postgres allows many NULLs, so jobs not yet submitted don't collide.
            e.HasIndex(x => x.FalRequestId).IsUnique().HasDatabaseName("uq_jobs_fal_request");
        });
    }
}
