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
    /// Ends the session a refresh token belongs to — the logout path — provided that token was
    /// issued to <paramref name="ownerId"/>.
    /// </summary>
    /// <param name="ownerId">The user the token must belong to for anything to be revoked.</param>
    /// <param name="refreshToken">The raw refresh token presented by the client.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The number of tokens revoked, which is zero whenever nothing matched.</returns>
    /// <remarks>
    /// The session, not the presented token, is what ends: every active token in the same rotation
    /// family is revoked, so a rotation that landed just before this call cannot leave a live
    /// successor behind.
    /// <para>
    /// Ownership is enforced within the operation rather than by a check the caller makes first, so
    /// there is no window in which the two could disagree, and no reading of the token's owner is
    /// ever returned. Idempotent: an unknown token, another user's token, and an already-ended
    /// session all revoke nothing and report zero, so callers cannot use this method to probe
    /// whether a token exists or who holds it.
    /// </para>
    /// </remarks>
    Task<int> RevokeSessionAsync(Guid ownerId, string refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// Ends every session the user has, on every device — the sign-out-everywhere path.
    /// </summary>
    /// <param name="userId">The user whose sessions are ending.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The number of tokens revoked, which is zero when nothing was live.</returns>
    /// <remarks>
    /// Scoped by user id alone. Nothing is presented and nothing is matched on, so — unlike
    /// <see cref="RevokeSessionAsync"/>, which needs a token and enforces ownership of it — the caller
    /// must have established whose sessions these are before calling: pass the subject of a validated
    /// access token, never a value read off the request.
    /// <para>
    /// Idempotent: a second call finds nothing active and reports zero. Note the bound this does not
    /// cross — access tokens are stateless and are not revoked, so one already issued stays valid
    /// until its own expiry (at most <c>Jwt:AccessTokenMinutes</c>).
    /// </para>
    /// </remarks>
    Task<int> RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Reports whether a refresh token exists and was issued to somebody other than the requester.
    /// </summary>
    /// <param name="requestingUserId">The user asking, whose own tokens are not a mismatch.</param>
    /// <param name="refreshToken">The raw refresh token presented by the client.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns><see langword="true"/> only when the token is known and its owner is somebody else.</returns>
    /// <remarks>
    /// Diagnostic only, for logging an anomaly that <see cref="RevokeSessionAsync"/> has already
    /// acted on; never call it to decide whether to revoke, which would reintroduce exactly the
    /// check-then-act window that method exists to avoid. It answers one question and reveals
    /// nothing else: <see langword="false"/> covers both "unknown" and "yours", so it cannot be
    /// used to probe whether a token exists.
    /// </remarks>
    Task<bool> RefreshTokenBelongsToAnotherUserAsync(
        Guid requestingUserId,
        string refreshToken,
        CancellationToken cancellationToken);
}
