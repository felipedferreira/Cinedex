using Microsoft.EntityFrameworkCore;
using Movies.Application.Abstractions;
using Movies.Domain;

namespace Movies.Persistence.Postgres.Repositories;

internal sealed class MovieRepository(MoviesDbContext dbContext) : IMovieRepository
{
    public async Task<IReadOnlyList<Movie>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Movies.AsNoTracking().Include(m => m.Genres).ToListAsync(cancellationToken);

    public async Task<Movie?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Movies.AsNoTracking().Include(m => m.Genres).FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<Movie> CreateAsync(Movie movie, CancellationToken cancellationToken)
    {
        // Any genres already present on movie.Genres are tracked (loaded via the genre
        // repository in the same scope), so EF only writes the movie_genres join rows.
        dbContext.Movies.Add(movie);
        await dbContext.SaveChangesAsync(cancellationToken);

        return movie;
    }

    public async Task<bool> UpdateAsync(Movie movie, CancellationToken cancellationToken)
    {
        // Load the tracked aggregate so the many-to-many junction can be reconciled.
        var existing = await dbContext.Movies
            .Include(m => m.Genres)
            .FirstOrDefaultAsync(m => m.Id == movie.Id, cancellationToken);

        if (existing is null)
        {
            return false;
        }

        existing.Title = movie.Title;
        existing.YearOfRelease = movie.YearOfRelease;
        existing.Description = movie.Description;

        existing.Genres.Clear();
        foreach (var genre in movie.Genres)
        {
            existing.Genres.Add(genre);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var rowsAffected = await dbContext.Movies
            .Where(m => m.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return rowsAffected > 0;
    }
}
