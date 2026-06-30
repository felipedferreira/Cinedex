using Cinedex.WebService.Contracts.Enums;

namespace Cinedex.WebService.Contracts.Responses;

public class TitleDetailsResponse
{
    public required Guid Id { get; init; }

    public required string Title { get; init; } = string.Empty;

    public required TitleType Type { get; init; }

    public required int YearOfRelease { get; init; }

    public string? Description { get; init; }

    public IEnumerable<GenreResponse> Genres { get; init; } = Enumerable.Empty<GenreResponse>();
}