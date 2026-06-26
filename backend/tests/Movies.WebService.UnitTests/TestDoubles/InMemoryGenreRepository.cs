using Movies.Application.Abstractions;
using Movies.Domain.GenreAggregate;

namespace Movies.WebService.UnitTests.TestDoubles;

internal sealed class InMemoryGenreRepository : IGenreRepository
{
    private readonly List<Genre> genres = [];

    public InMemoryGenreRepository(params Genre[] genres)
    {
        this.genres.AddRange(genres);
    }

    public int CreateCallCount { get; private set; }

    public int UpdateCallCount { get; private set; }

    public int DeleteCallCount { get; private set; }

    public bool UpdateResult { get; set; } = true;

    public bool DeleteResult { get; set; } = true;

    public Genre? LastCreated { get; private set; }

    public Genre? LastUpdated { get; private set; }

    public IReadOnlyList<Guid> LastRequestedIds { get; private set; } = [];

    public Task<IReadOnlyList<Genre>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Genre>>(genres.ToList());

    public Task<Genre?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(genres.FirstOrDefault(genre => genre.Id == id));

    public Task<IReadOnlyList<Genre>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        LastRequestedIds = ids.ToList();

        IReadOnlyList<Genre> matchingGenres = genres
            .Where(genre => ids.Contains(genre.Id))
            .ToList();

        return Task.FromResult(matchingGenres);
    }

    public Task CreateAsync(Genre genre, CancellationToken cancellationToken)
    {
        CreateCallCount++;
        LastCreated = genre;
        genres.Add(genre);

        return Task.CompletedTask;
    }

    public Task<bool> UpdateAsync(Genre genre, CancellationToken cancellationToken)
    {
        UpdateCallCount++;
        LastUpdated = genre;

        if (!UpdateResult)
        {
            return Task.FromResult(false);
        }

        genres.RemoveAll(existing => existing.Id == genre.Id);
        genres.Add(genre);

        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        DeleteCallCount++;

        return Task.FromResult(DeleteResult);
    }
}
