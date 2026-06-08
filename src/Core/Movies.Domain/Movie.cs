namespace Movies.Domain;

public class Movie
{
    public Guid Id { get; init; }

    public required string Title { get; set; }

    public required int YearOfRelease { get; set; }

    public string? Description { get; set; }
}
