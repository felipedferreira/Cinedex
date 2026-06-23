namespace Movies.WebService.Contracts.Requests;

public class CreateMoviesRequest
{
    public required string Title { get; init; } = string.Empty;

    public required int YearOfRelease { get; init; }

    public string? Description { get; init; }

    public IEnumerable<Guid> GenreIds { get; init; } = Enumerable.Empty<Guid>();
}