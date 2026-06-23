namespace Movies.Domain.Genres;

public class Genre
{
    public Guid Id { get; init; }

    public required string Name { get; set; }

    public string? Description { get; set; }
}
