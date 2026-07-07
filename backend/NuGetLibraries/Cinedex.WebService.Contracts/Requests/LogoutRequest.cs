namespace Cinedex.WebService.Contracts.Requests;

public class LogoutRequest
{
    public required string RefreshToken { get; init; } = string.Empty;
}
