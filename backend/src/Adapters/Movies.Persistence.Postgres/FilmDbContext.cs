using Microsoft.EntityFrameworkCore;
using Movies.Domain.GenreAggregate;
using Movies.Domain.TitleAggregate;
using Movies.Persistence.Postgres.Constants;

namespace Movies.Persistence.Postgres;

public class FilmDbContext(DbContextOptions<FilmDbContext> options) : DbContext(options)
{
    public DbSet<Title> Titles => Set<Title>();

    public DbSet<Genre> Genres => Set<Genre>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DatabaseConstants.CatalogSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FilmDbContext).Assembly);
    }
}
