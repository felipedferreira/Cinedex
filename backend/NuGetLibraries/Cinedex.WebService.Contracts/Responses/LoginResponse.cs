namespace Cinedex.WebService.Contracts.Responses;

public class LoginResponse
{
    public required string AccessToken { get; init; } = string.Empty;

    public required DateTime ExpiresAtUtc { get; init; }
}
