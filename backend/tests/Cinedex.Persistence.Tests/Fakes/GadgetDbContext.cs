using Microsoft.EntityFrameworkCore;

namespace Cinedex.Persistence.Tests.Fakes;

/// <summary>
/// A second context, so multi-context registration can be exercised.
/// </summary>
public sealed class GadgetDbContext(DbContextOptions<GadgetDbContext> options) : DbContext(options)
{
    public DbSet<Widget> Gadgets => Set<Widget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Widget>().ToTable("gadgets").HasKey(entity => entity.Id);
}
