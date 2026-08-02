namespace Cinedex.Auth.Identity.Persistence.ReadModels;

/// <summary>
/// A refresh-token row as read, detached from any change tracker.
/// </summary>
/// <remarks>
/// <para>
/// Returned instead of the <c>RefreshToken</c> entity so the read side's no-write property is
/// structural rather than a convention: a caller cannot attach one of these to the write context and
/// save it, because it is not an entity.
/// </para>
/// <para>
/// <c>TokenHash</c> is deliberately absent. Every lookup is by hash, so the caller already holds the
/// value; carrying it back would spread secret-adjacent material through call stacks and log scopes
/// for no gain.
/// </para>
/// </remarks>
/// <param name="Id">The row's primary key.</param>
/// <param name="UserId">The user the token was issued to.</param>
/// <param name="FamilyId">The rotation chain the token belongs to. Immutable for the row's lifetime, which is what makes it safe to act on a value read outside the family lock.</param>
/// <param name="ExpiresAtUtc">When the token stops being accepted.</param>
/// <param name="RevokedAtUtc">When the token was revoked, or <see langword="null"/> if it is still active.</param>
/// <param name="ReplacedByTokenHash">The hash of the token that replaced this one, or <see langword="null"/> if it was never rotated. Non-null on a presented token is the reuse signal.</param>
internal sealed record RefreshTokenReadModel(
    Guid Id,
    Guid UserId,
    Guid FamilyId,
    DateTime ExpiresAtUtc,
    DateTime? RevokedAtUtc,
    string? ReplacedByTokenHash);
