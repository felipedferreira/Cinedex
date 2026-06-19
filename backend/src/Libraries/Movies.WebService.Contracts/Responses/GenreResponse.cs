namespace Movies.WebService.Contracts.Responses;

public class GenreResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}
