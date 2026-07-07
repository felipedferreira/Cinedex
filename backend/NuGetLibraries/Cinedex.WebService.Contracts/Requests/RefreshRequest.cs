namespace Cinedex.WebService.Contracts.Requests;

public class RefreshRequest
{
    public required string RefreshToken { get; init; } = string.Empty;
}
