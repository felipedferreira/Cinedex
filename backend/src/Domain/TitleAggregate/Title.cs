using Movies.Domain.Enums;

namespace Movies.Domain.TitleAggregate;

public class Title
{
    private readonly HashSet<Guid> _genreIds = [];

    private Title()
    {
        // Required by EF Core for materialization.
    }

    public Guid Id { get; init; }

    public required string Name { get; set; }

    public required TitleType Type { get; set; }

    public required int YearOfRelease { get; set; }

    public string? Description { get; set; }

    // Reference to the Genre aggregate by identity only (no navigation, no cross-aggregate FK).
    public IReadOnlySet<Guid> GenreIds => _genreIds;

    public static Title Create(
        string name,
        TitleType type,
        int yearOfRelease,
        string? description,
        IEnumerable<Guid> genreIds)
    {
        return Create(
            Guid.CreateVersion7(),
            name,
            type,
            yearOfRelease,
            description,
            genreIds);
    }

    public static Title Create(
        Guid id,
        string name,
        TitleType type,
        int yearOfRelease,
        string? description,
        IEnumerable<Guid> genreIds)
    {
        var title = new Title
        {
            Id = id,
            Name = name,
            Type = type,
            YearOfRelease = yearOfRelease,
            Description = description,
        };

        title.AddGenres(genreIds);

        return title;
    }

    public void AddGenre(Guid genreId)
    {
        if (genreId == Guid.Empty)
        {
            throw new ArgumentException("Genre id cannot be empty.", nameof(genreId));
        }

        _genreIds.Add(genreId);
    }

    public void AddGenres(IEnumerable<Guid> genreIdsToAdd)
    {
        foreach (var genreId in genreIdsToAdd)
        {
            AddGenre(genreId);
        }
    }

    public bool RemoveGenre(Guid genreId)
    {
        return _genreIds.Remove(genreId);
    }

    public void ReplaceGenres(IEnumerable<Guid> replacementGenreIds)
    {
        _genreIds.Clear();
        AddGenres(replacementGenreIds);
    }
}
