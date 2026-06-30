using Cinedex.Domain.GenreAggregate;

namespace Cinedex.Application.Abstractions;

public interface IGenreRepository
{
    Task<IReadOnlyList<Genre>> GetAllAsync(CancellationToken cancellationToken);

    Task<Genre?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    // Returns the genres matching the supplied ids. Used to validate that referenced genre
    // ids exist and to enrich a title's response with genre details.
    Task<IReadOnlyList<Genre>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task CreateAsync(Genre genre, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(Genre genre, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}