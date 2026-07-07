using Cinedex.Application.Auth;
using Cinedex.Domain.UserAggregate;

namespace Cinedex.Application.Abstractions;

// Port for issuing and rotating access/refresh tokens.
public interface ITokenService
{
    // Issues a fresh access token and refresh token pair for the given user.
    Task<AuthTokensDto> IssueTokensAsync(User user, CancellationToken cancellationToken);

    // Validates and rotates a refresh token, returning a new token pair. Throws
    // InvalidCredentialsException when the refresh token is unknown, expired or revoked.
    Task<AuthTokensDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    // Revokes a refresh token so it can no longer be used (e.g. on logout). No-op when the token
    // is unknown or already revoked.
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
}
