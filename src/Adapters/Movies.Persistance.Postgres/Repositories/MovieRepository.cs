using Microsoft.EntityFrameworkCore;
using Movies.Application.Abstractions;
using Movies.Domain;

namespace Movies.Persistance.Postgres.Repositories;

internal sealed class MovieRepository(MoviesDbContext dbContext) : IMovieRepository
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
        var rowsAffected = await dbContext.Movies
            .Where(m => m.Id == movie.Id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(m => m.Title, movie.Title)
                    .SetProperty(m => m.YearOfRelease, movie.YearOfRelease)
                    .SetProperty(m => m.Description, movie.Description),
                cancellationToken);

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var rowsAffected = await dbContext.Movies
            .Where(m => m.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return rowsAffected > 0;
    }
}
