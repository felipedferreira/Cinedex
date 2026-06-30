namespace Cinedex.WebService.Contracts.Requests;

public class UpdateGenreRequest
{
    public required string Name { get; init; } = string.Empty;
}