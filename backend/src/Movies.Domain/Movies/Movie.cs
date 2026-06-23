namespace Movies.Domain.Movies;

public class Movie
{
    public Guid Id { get; init; }

    public required string Title { get; set; }

    public required int YearOfRelease { get; set; }

    public string? Description { get; set; }

    // Reference to the Genre aggregate by identity only (no navigation, no cross-aggregate FK).
    public List<Guid> GenreIds { get; set; } = [];
}
