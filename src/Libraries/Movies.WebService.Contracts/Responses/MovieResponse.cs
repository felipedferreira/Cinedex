using Movies.WebService.Contracts.Enums;

namespace Movies.WebService.Contracts.Responses;

public class MovieResponse
{
    public required Guid Id { get; init; }

    public required string Title { get; init; } = string.Empty;

    public required int YearOfRelease { get; init; }

    public required IEnumerable<Genre> Genres { get; init; } = Enumerable.Empty<Genre>();
}