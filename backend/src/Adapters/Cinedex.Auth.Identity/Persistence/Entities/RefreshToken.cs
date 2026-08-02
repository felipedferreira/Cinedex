namespace Cinedex.Auth.Identity.Persistence.Entities;

// A persisted refresh token. Only a hash of the raw token is stored; the raw value is returned to
// the client once at issue time and never persisted.
internal sealed class RefreshToken
{
    public Guid Id { get; init; }

    public required Guid UserId { get; init; }

    // Identifies the rotation chain a token belongs to: a login mints a new family, and every
    // rotation copies the incoming token's value onto its replacement. Immutable for the row's
    // lifetime — that immutability is what makes it safe to read from a stale AsNoTracking() row.
    public required Guid FamilyId { get; init; }

    public required string TokenHash { get; set; }

    public required DateTime ExpiresAtUtc { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? RevokedAtUtc { get; set; }

    public string? ReplacedByTokenHash { get; set; }
}
