using Movies.Domain.Enums;

namespace Movies.Domain.TitleAggregate;

public class Title
{
    public Guid Id { get; init; }

    public required string Name { get; set; }

    public required TitleType Type { get; set; }

    public required int YearOfRelease { get; set; }

    public string? Description { get; set; }

    // Reference to the Genre aggregate by identity only (no navigation, no cross-aggregate FK).
    public List<Guid> GenreIds { get; set; } = [];
}