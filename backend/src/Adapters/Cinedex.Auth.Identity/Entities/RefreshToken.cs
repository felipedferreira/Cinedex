namespace Cinedex.Auth.Identity.Entities;

// A persisted refresh token. Only a hash of the raw token is stored; the raw value is returned to
// the client once at issue time and never persisted.
internal sealed class RefreshToken
{
    public Guid Id { get; init; }

    public required Guid UserId { get; init; }

    public required string TokenHash { get; set; }

    public required DateTime ExpiresAtUtc { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? RevokedAtUtc { get; set; }

    public string? ReplacedByTokenHash { get; set; }
}