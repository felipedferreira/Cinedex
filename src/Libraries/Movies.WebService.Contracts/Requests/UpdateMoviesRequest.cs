using Movies.WebService.Contracts.Enums;

namespace Movies.WebService.Contracts.Requests;

public class UpdateMoviesRequest
{
    public required string Title { get; init; } = string.Empty;

    public required int YearOfRelease { get; init; }

    public required IEnumerable<Genre> Genres { get; init; } = Enumerable.Empty<Genre>();
}