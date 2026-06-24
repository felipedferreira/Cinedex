using Movies.WebService.Contracts.Enums;

namespace Movies.WebService.Contracts.Responses;

public class TitleResponse
{
    public required Guid Id { get; init; }

    public required string Title { get; init; } = string.Empty;

    public required TitleType Type { get; init; }

    public required int YearOfRelease { get; init; }

    public string? Description { get; init; }
}