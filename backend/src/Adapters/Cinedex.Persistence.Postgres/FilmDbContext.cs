using Cinedex.Domain.GenreAggregate;
using Cinedex.Domain.TitleAggregate;
using Cinedex.Persistence.Postgres.Constants;
using Cinedex.Persistence.Postgres.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cinedex.Persistence.Postgres;

public class FilmDbContext(DbContextOptions<FilmDbContext> options) : DbContext(options)
{
    public DbSet<Title> Titles => Set<Title>();

    public DbSet<Genre> Genres => Set<Genre>();

    internal DbSet<TitleGenre> TitleGenres => Set<TitleGenre>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DatabaseConstants.CatalogSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FilmDbContext).Assembly);
    }
}