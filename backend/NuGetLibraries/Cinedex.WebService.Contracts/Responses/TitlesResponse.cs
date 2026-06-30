namespace Cinedex.WebService.Contracts.Responses;

public class TitlesResponse
{
    public required IEnumerable<TitleResponse> Titles { get; init; } = Enumerable.Empty<TitleResponse>();
}