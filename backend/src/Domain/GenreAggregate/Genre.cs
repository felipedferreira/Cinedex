namespace Cinedex.Domain.GenreAggregate;

public class Genre
{
    private Genre()
    {
        // Required by EF Core for materialization.
    }

    public Guid Id { get; init; }

    public required string Name { get; set; }

    public static Genre Create(string name)
    {
        return Create(Guid.CreateVersion7(), name);
    }

    public static Genre Create(Guid id, string name)
    {
        return new Genre
        {
            Id = id,
            Name = name,
        };
    }
}