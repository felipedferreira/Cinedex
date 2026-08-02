using Cinedex.Application.Auth;
using Cinedex.Domain.UserAggregate;

namespace Cinedex.Application.Abstractions;

/// <summary>
/// Port for issuing and rotating access and refresh tokens.
/// </summary>
/// <remarks>
/// Refresh tokens are returned to the caller in raw form exactly once, at issue time. Only a hash
/// is persisted, so a raw token cannot be recovered afterwards and must be captured from the
/// returned <see cref="AuthTokensDto"/>.
/// </remarks>
public interface ITokenService
{
    /// <summary>
    /// Issues a fresh access token and refresh token pair for the given user, persisting the
    /// refresh token so it can later be rotated or revoked.
    /// </summary>
    /// <param name="user">The user the tokens are issued for.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task producing the new <see cref="AuthTokensDto"/>.</returns>
    Task<AuthTokensDto> IssueTokensAsync(User user, CancellationToken cancellationToken);

    /// <summary>
    /// Validates the supplied refresh token and rotates it, returning a new token pair.
    /// </summary>
    /// <param name="refreshToken">The raw refresh token presented by the client.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task producing the rotated <see cref="AuthTokensDto"/>.</returns>
    /// <remarks>
    /// Rotation revokes the presented token and persists its replacement in a single transaction, so
    /// a successful refresh always invalidates the token that was presented. Presenting a known,
    /// unexpired token that already has a replacement is treated as evidence of reuse: every active
    /// token in that rotation family is revoked before this method reports invalid credentials.
    /// </remarks>
    /// <exception cref="Cinedex.Application.Exceptions.InvalidCredentialsException">
    /// The refresh token is unknown, already revoked, or expired, or the account it was issued for
    /// no longer exists. The same exception is used for all four so callers cannot distinguish them.
    /// </exception>
    Task<AuthTokensDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes a refresh token so it can no longer be used, for example on logout.
    /// </summary>
    /// <param name="refreshToken">The raw refresh token to revoke.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that completes when the token has been revoked.</returns>
    /// <remarks>
    /// Idempotent: an unknown or already-revoked token is a silent no-op rather than an error, so
    /// callers cannot use this method to probe whether a token exists.
    /// </remarks>
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
}
