using Oceanus.WebService.Contracts.Enums;

namespace Oceanus.WebService.Contracts.Requests;

public class UpdateTitlesRequest
{
    public required string Title { get; init; } = string.Empty;

    public required TitleType Type { get; init; }

    public required int YearOfRelease { get; init; }

    public string? Description { get; init; }

    public IEnumerable<Guid> GenreIds { get; init; } = Enumerable.Empty<Guid>();
}