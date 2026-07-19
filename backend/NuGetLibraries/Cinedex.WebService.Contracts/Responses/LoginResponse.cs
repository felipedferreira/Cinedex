namespace Cinedex.WebService.Contracts.Responses;

/// <summary>
/// The access token issued by login or refresh. The refresh token is not included: it is returned
/// as an HttpOnly cookie so that scripts running in the browser cannot read it.
/// </summary>
public class LoginResponse
{
    public required string AccessToken { get; init; } = string.Empty;

    public required DateTime ExpiresAtUtc { get; init; }
}