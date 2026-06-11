using Movies.Domain;

namespace Movies.Application.Abstractions;

public interface IMovieService
{
    Task<IReadOnlyList<Movie>> GetAllAsync(CancellationToken cancellationToken);

    Task<Movie?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Movie> CreateAsync(Movie movie, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(Movie movie, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
