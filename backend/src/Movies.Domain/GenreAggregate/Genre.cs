namespace Movies.Domain.GenreAggregate;

public class Genre
{
    public Guid Id { get; init; }

    public required string Name { get; set; }
}