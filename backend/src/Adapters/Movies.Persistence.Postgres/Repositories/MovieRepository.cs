using Microsoft.EntityFrameworkCore;
using Movies.Application.Abstractions;
using Movies.Domain.Movies;

namespace Movies.Persistence.Postgres.Repositories;

internal sealed class MovieRepository(FilmDbContext dbContext) : IMovieRepository
{
    public async Task<IReadOnlyList<Movie>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Movies.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<Movie?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Movies.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<Movie> CreateAsync(Movie movie, CancellationToken cancellationToken)
    {
        dbContext.Movies.Add(movie);
        await dbContext.SaveChangesAsync(cancellationToken);

        return movie;
    }

    public async Task<bool> UpdateAsync(Movie movie, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Movies
            .FirstOrDefaultAsync(m => m.Id == movie.Id, cancellationToken);

        if (existing is null)
        {
            return false;
        }

        existing.Title = movie.Title;
        existing.YearOfRelease = movie.YearOfRelease;
        existing.Description = movie.Description;
        existing.GenreIds = movie.GenreIds;

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
