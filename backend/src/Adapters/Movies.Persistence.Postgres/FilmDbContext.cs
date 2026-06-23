using Microsoft.EntityFrameworkCore;
using Movies.Domain.GenreAggregate;
using Movies.Domain.MovieAggregate;

namespace Movies.Persistence.Postgres;

public class FilmDbContext(DbContextOptions<FilmDbContext> options) : DbContext(options)
{
    public DbSet<Movie> Movies => Set<Movie>();

    public DbSet<Genre> Genres => Set<Genre>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FilmDbContext).Assembly);
    }
}