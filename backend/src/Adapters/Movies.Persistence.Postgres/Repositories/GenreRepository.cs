using Microsoft.EntityFrameworkCore;
using Movies.Application.Abstractions;
using Movies.Domain;

namespace Movies.Persistence.Postgres.Repositories;

internal sealed class GenreRepository(MoviesDbContext dbContext) : IGenreRepository
{
    public async Task<IReadOnlyList<Genre>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Genres.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<Genre?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Genres.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Genre>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        // Tracked (no AsNoTracking) so the returned genres can be attached to a movie's
        // junction collection without EF attempting to insert them as new rows.
        return await dbContext.Genres.Where(g => ids.Contains(g.Id)).ToListAsync(cancellationToken);
    }

    public async Task<Genre> CreateAsync(Genre genre, CancellationToken cancellationToken)
    {
        dbContext.Genres.Add(genre);
        await dbContext.SaveChangesAsync(cancellationToken);

        return genre;
    }

    public async Task<bool> UpdateAsync(Genre genre, CancellationToken cancellationToken)
    {
        var rowsAffected = await dbContext.Genres
            .Where(g => g.Id == genre.Id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(g => g.Name, genre.Name)
                    .SetProperty(g => g.Description, genre.Description),
                cancellationToken);

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var rowsAffected = await dbContext.Genres
            .Where(g => g.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return rowsAffected > 0;
    }
}
