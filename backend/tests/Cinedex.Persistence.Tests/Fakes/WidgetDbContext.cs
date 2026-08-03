using Microsoft.EntityFrameworkCore;

namespace Cinedex.Persistence.Tests.Fakes;

/// <summary>
/// Minimal context for exercising the unit of work against a real database.
/// </summary>
public sealed class WidgetDbContext(DbContextOptions<WidgetDbContext> options) : DbContext(options)
{
    public DbSet<Widget> Widgets => Set<Widget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Widget>(widget =>
        {
            widget.ToTable("widgets");
            widget.HasKey(entity => entity.Id);

            // The unique index is the point of this table: it gives the tests a real constraint to
            // violate, so translation is verified against PostgreSQL's actual SQLSTATE rather than
            // against a hand-built exception.
            widget.HasIndex(entity => entity.Name).IsUnique().HasDatabaseName("ix_widgets_name");
            widget.Property(entity => entity.Name).IsRequired().HasMaxLength(200);
        });
    }
}
