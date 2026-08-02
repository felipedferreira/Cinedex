namespace Cinedex.Auth.Identity.Options;

// Retention policy for the refresh-token cleanup sweep. Two windows rather than one because the two
// kinds of dead row are dead for different reasons: an expired-but-never-revoked row is simply
// unreachable by every code path, while a revoked row is the only thing that makes a replayed token
// recognisable as reuse rather than as an unknown token — so it has to outlive its own expiry.
internal sealed class RefreshTokenCleanupOptions
{
    public const string SectionName = "RefreshTokenCleanup";

    // How long to wait between sweeps.
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(10);

    // How long an expired, never-revoked row is kept past its expiry. This is NOT a security
    // boundary — the row is already unreachable by every code path the moment it expires. The value
    // is pure operational slack: room for clock skew between this worker's host and the web
    // service's, and for the row to still be queryable for a day if someone needs to check when a
    // session ended. Safe to keep short.
    public TimeSpan ExpiredRetention { get; set; } = TimeSpan.FromDays(1);

    // How long a revoked row is kept past its revocation. This IS a security boundary: the revoked
    // row is the only evidence that lets the reuse response recognise a replayed, already-
    // rotated token as a compromise rather than as an unknown token. It must comfortably exceed
    // Jwt:RefreshTokenDays, or a token stolen early in its life could stop being recognisable as
    // reuse before an attacker even had a chance to replay it. Shortening this narrows that
    // detection window — do not treat it as a knob for saving space the way ExpiredRetention is.
    public TimeSpan ReuseDetectionWindow { get; set; } = TimeSpan.FromDays(14);

    // Rows deleted per statement. Bounds how long any single delete holds row locks.
    public int BatchSize { get; set; } = 500;

    // Batches per sweep. Bounds total work per tick so a large backlog drains over several sweeps
    // instead of turning one sweep into a long-running job.
    public int MaxBatchesPerRun { get; set; } = 20;
}
