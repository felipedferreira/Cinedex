using Microsoft.EntityFrameworkCore.Storage;

namespace Cinedex.Auth.Identity.Persistence.Repository;

/// <summary>
/// All persistence for <c>auth."refreshTokens"</c>: every write, and every read, over
/// <see cref="AuthDbContext"/>.
/// </summary>
/// <remarks>
/// Implementations must take the scoped <see cref="AuthDbContext"/>, never an
/// <c>IDbContextFactory</c>. Callers open transactions on that same scoped context and expect
/// these statements to enlist in them; a factory would hand out a second connection and silently
/// put the writes outside the caller's transaction.
/// </remarks>
internal interface IRefreshTokenRepository
{
    /// <summary>
    /// Opens a transaction on the underlying context.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The transaction, which the caller owns and must commit or dispose.</returns>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Takes the advisory lock that serializes refresh operations within one token family.
    /// </summary>
    /// <param name="familyId">The family to lock.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that completes once the lock is held.</returns>
    /// <exception cref="InvalidOperationException">No transaction is open on the context.</exception>
    Task AcquireFamilyLockAsync(Guid familyId, CancellationToken cancellationToken);

    /// <summary>
    /// Finds the token with the supplied hash.
    /// </summary>
    /// <remarks>
    /// Called both as a preflight lookup before a transaction opens and again as the authoritative
    /// re-read once the family's advisory lock is held via <see cref="AcquireFamilyLockAsync"/> —
    /// the second call is what makes rotation and reuse detection correct against a value that
    /// could have changed while the first call's caller waited for the lock.
    /// </remarks>
    /// <param name="tokenHash">The hex-encoded SHA-256 hash of the raw refresh token.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The matching entity, or <see langword="null"/> if no token has that hash.</returns>
    Task<RefreshToken?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts a refresh token and saves.
    /// </summary>
    /// <param name="refreshToken">The token to insert.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that completes when the row has been written.</returns>
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes an active, unexpired token and records what replaced it, in one conditional statement.
    /// </summary>
    /// <remarks>
    /// The condition is what makes concurrent rotation safe: two requests presenting the same token
    /// cannot both match, so exactly one of them can win.
    /// </remarks>
    /// <param name="tokenHash">The hash of the token being rotated away.</param>
    /// <param name="rotatedAtUtc">The rotation instant, used both as the revocation stamp and as the expiry cutoff.</param>
    /// <param name="replacementTokenHash">The hash of the token taking its place.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The number of rows updated: 1 when this caller won the rotation, 0 otherwise.</returns>
    Task<int> RotateAsync(
        string tokenHash,
        DateTime rotatedAtUtc,
        string replacementTokenHash,
        CancellationToken cancellationToken);

    /// <summary>
    /// Revokes every active, unexpired token in one family — the response to detected reuse.
    /// </summary>
    /// <param name="familyId">The compromised family.</param>
    /// <param name="revokedAtUtc">The revocation stamp, also used as the expiry cutoff.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The number of tokens revoked.</returns>
    Task<int> RevokeActiveFamilyAsync(Guid familyId, DateTime revokedAtUtc, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes a single token by hash, if it is still active. Idempotent.
    /// </summary>
    /// <param name="tokenHash">The hash of the token to revoke.</param>
    /// <param name="revokedAtUtc">The revocation stamp.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that completes when the statement has run.</returns>
    Task RevokeByTokenHashAsync(string tokenHash, DateTime revokedAtUtc, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes every active token belonging to one user, whatever family it is in.
    /// </summary>
    /// <param name="userId">The user whose sessions are ending.</param>
    /// <param name="revokedAtUtc">The revocation stamp.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The number of tokens revoked.</returns>
    Task<int> RevokeAllActiveForUserAsync(Guid userId, DateTime revokedAtUtc, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes up to <paramref name="batchSize"/> tokens that expired before the cutoff without ever
    /// having been revoked.
    /// </summary>
    /// <param name="expiredCutoff">Delete unrevoked tokens that expired before this instant.</param>
    /// <param name="batchSize">The maximum number of rows to delete.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The number of rows deleted. A short batch means the predicate has drained.</returns>
    Task<int> DeleteExpiredBatchAsync(DateTime expiredCutoff, int batchSize, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes up to <paramref name="batchSize"/> tokens revoked before the cutoff and already expired.
    /// </summary>
    /// <param name="revokedCutoff">Delete tokens revoked before this instant.</param>
    /// <param name="expiredBefore">Only delete tokens that have also expired before this instant.</param>
    /// <param name="batchSize">The maximum number of rows to delete.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The number of rows deleted. A short batch means the predicate has drained.</returns>
    Task<int> DeleteRevokedBatchAsync(
        DateTime revokedCutoff,
        DateTime expiredBefore,
        int batchSize,
        CancellationToken cancellationToken);
}
