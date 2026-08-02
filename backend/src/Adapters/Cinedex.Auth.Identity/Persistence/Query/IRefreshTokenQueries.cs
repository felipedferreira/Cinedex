namespace Cinedex.Auth.Identity.Persistence.Query;

/// <summary>
/// Reads refresh tokens over the read-only connection (<see cref="AuthReadOnlyDbContext"/>).
/// </summary>
/// <remarks>
/// <para>
/// The read half of the refresh-token CQRS split. Only lookups that are correct <i>outside</i> a
/// transaction belong here. A read taken inside the rotation transaction has to run on the write
/// connection, because that is the connection holding the family's advisory lock — running it here
/// would read outside the lock and silently break reuse detection. Those reads live on
/// <see cref="Repository.IRefreshTokenRepository"/> alongside the writes they are part of.
/// </para>
/// <para>
/// Every member returns materialised values rather than <see cref="IQueryable{T}"/>, so no caller
/// ever gets a handle it could call <c>ExecuteUpdate</c> or <c>ExecuteDelete</c> on.
/// </para>
/// </remarks>
internal interface IRefreshTokenQueries
{
    /// <summary>
    /// Finds the token with the supplied hash.
    /// </summary>
    /// <param name="tokenHash">The hex-encoded SHA-256 hash of the raw refresh token.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The matching row, or <see langword="null"/> if no token has that hash.</returns>
    Task<RefreshTokenReadModel?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
}
