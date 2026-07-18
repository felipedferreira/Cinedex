namespace Cinedex.Application.Auth;

/// <summary>
/// An issued access token plus its rotating refresh token, with UTC expiries.
/// </summary>
public sealed record AuthTokensDto(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
