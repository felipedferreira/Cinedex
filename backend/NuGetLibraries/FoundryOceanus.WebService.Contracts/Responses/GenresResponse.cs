namespace FoundryOceanus.WebService.Contracts.Responses;

public class GenresResponse
{
    public required IEnumerable<GenreResponse> Genres { get; init; } = Enumerable.Empty<GenreResponse>();
}