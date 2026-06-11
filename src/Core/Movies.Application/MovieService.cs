using Movies.Application.Abstractions;
using Movies.Domain;

namespace Movies.Application;

internal sealed class MovieService(IMovieRepository repository) : IMovieService
{
    public Task<IReadOnlyList<Movie>> GetAllAsync(CancellationToken cancellationToken) =>
        repository.GetAllAsync(cancellationToken);

    public Task<Movie?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, cancellationToken);

    public Task<Movie> CreateAsync(Movie movie, CancellationToken cancellationToken) =>
        repository.CreateAsync(movie, cancellationToken);

    public Task<bool> UpdateAsync(Movie movie, CancellationToken cancellationToken) =>
        repository.UpdateAsync(movie, cancellationToken);

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        repository.DeleteAsync(id, cancellationToken);
}
