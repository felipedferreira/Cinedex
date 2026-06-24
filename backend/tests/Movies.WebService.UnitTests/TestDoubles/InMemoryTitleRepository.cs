using Movies.Application.Abstractions;
using Movies.Domain.TitleAggregate;

namespace Movies.WebService.UnitTests.TestDoubles;

internal sealed class InMemoryTitleRepository : ITitleRepository
{
    private readonly List<Title> titles = [];

    public InMemoryTitleRepository(params Title[] titles)
    {
        this.titles.AddRange(titles);
    }

    public Guid CreatedId { get; set; } = Guid.NewGuid();

    public int CreateCallCount { get; private set; }

    public int UpdateCallCount { get; private set; }

    public int DeleteCallCount { get; private set; }

    public bool UpdateResult { get; set; } = true;

    public bool DeleteResult { get; set; } = true;

    public Title? LastCreated { get; private set; }

    public Title? LastUpdated { get; private set; }

    public Task<IReadOnlyList<Title>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Title>>(titles.ToList());

    public Task<Title?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(titles.FirstOrDefault(title => title.Id == id));

    public Task<Title> CreateAsync(Title title, CancellationToken cancellationToken)
    {
        CreateCallCount++;
        LastCreated = title;

        var created = new Title
        {
            Id = CreatedId,
            Name = title.Name,
            Type = title.Type,
            YearOfRelease = title.YearOfRelease,
            Description = title.Description,
            GenreIds = title.GenreIds.ToList(),
        };

        titles.Add(created);

        return Task.FromResult(created);
    }

    public Task<bool> UpdateAsync(Title title, CancellationToken cancellationToken)
    {
        UpdateCallCount++;
        LastUpdated = title;

        if (!UpdateResult)
        {
            return Task.FromResult(false);
        }

        titles.RemoveAll(existing => existing.Id == title.Id);
        titles.Add(title);

        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        DeleteCallCount++;

        return Task.FromResult(DeleteResult);
    }
}
