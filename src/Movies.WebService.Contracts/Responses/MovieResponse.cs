namespace Movies.WebService.Contracts.Responses;

public class MovieResponse
{
    public required Guid Id { get; init; }

    public required string Title { get; init; } = string.Empty;

    public required int YearOfRelease { get; init; }

    public required IEnumerable<string> Genres { get; init; } = Enumerable.Empty<string>();
}