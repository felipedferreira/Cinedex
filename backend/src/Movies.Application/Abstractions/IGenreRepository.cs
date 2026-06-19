using Movies.Domain;

namespace Movies.Application.Abstractions;

public interface IGenreRepository
{
    Task<IReadOnlyList<Genre>> GetAllAsync(CancellationToken cancellationToken);

    Task<Genre?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    // Returns the genres matching the supplied ids as tracked entities so they can be
    // linked to a movie via the junction table without being re-inserted.
    Task<IReadOnlyList<Genre>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<Genre> CreateAsync(Genre genre, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(Genre genre, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
