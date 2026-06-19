namespace Movies.WebService.Contracts.Requests;

public class CreateGenreRequest
{
    public required string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}
