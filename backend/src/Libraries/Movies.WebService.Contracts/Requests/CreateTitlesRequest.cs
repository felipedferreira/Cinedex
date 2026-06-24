using Movies.WebService.Contracts.Enums;

namespace Movies.WebService.Contracts.Requests;

public class CreateTitlesRequest
{
    public required string Title { get; init; } = string.Empty;

    public required TitleType Type { get; init; }

    public required int YearOfRelease { get; init; }

    public string? Description { get; init; }

    public IEnumerable<Guid> GenreIds { get; init; } = Enumerable.Empty<Guid>();
}