namespace Movies.Domain;

public class Genre
{
    public Guid Id { get; init; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public ICollection<Movie> Movies { get; } = new List<Movie>();
}
