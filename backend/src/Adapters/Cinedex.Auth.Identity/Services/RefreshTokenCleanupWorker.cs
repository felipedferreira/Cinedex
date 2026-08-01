using System.Diagnostics;
using Cinedex.Auth.Identity.Entities;
using Cinedex.Auth.Identity.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinedex.Auth.Identity.Services;

/// <summary>
/// Deletes refresh-token rows that have passed their retention window, in bounded batches.
/// </summary>
/// <remarks>
/// <para>
/// Nothing else in the system ever deletes a refresh token — a rotation revokes its predecessor and
/// inserts a replacement, so without this worker the table and its indexes only grow forever.
/// </para>
/// <para>
/// There are two categories of "dead" row, deleted on two different schedules
/// (<see cref="RefreshTokenCleanupOptions.ExpiredRetention"/> and
/// <see cref="RefreshTokenCleanupOptions.ReuseDetectionWindow"/>), because they are dead for
/// different reasons and each needs a different amount of extra time before deletion is safe:
/// </para>
/// <para>
/// <b>Expired, never revoked.</b> A row whose <c>ExpiresAtUtc</c> has already passed is inert the
/// instant that happens — <see cref="JwtTokenService.RefreshAsync"/> rejects an expired token
/// before it looks at anything else, so no code path treats an expired-but-unrevoked row specially.
/// The extra day of retention past expiry is not protecting any behavior; it is pure
/// operational slack for clock skew between this worker's host and the web service's, and for
/// having the row still queryable for a day if someone needs to check when a session actually
/// ended.
/// </para>
/// <para>
/// <b>Revoked.</b> A revoked row is the <i>only</i> evidence the system has that a token was
/// rotated — and, once a family-wide reuse response is built, the only trigger for recognising that
/// a stolen, already-rotated token is being replayed rather than presenting an unknown one. Deleting
/// it too soon destroys that evidence before it can ever be used. So a revoked row must outlive the
/// window in which an attacker could plausibly replay the token it replaced, which is bounded by the
/// token's own lifetime (<c>Jwt:RefreshTokenDays</c>). The default keeps it for double that lifetime
/// as margin — this buffer is a security boundary, not a courtesy, and shortening it narrows the
/// window in which reuse can still be detected.
/// </para>
/// </remarks>
internal sealed class RefreshTokenCleanupWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<RefreshTokenCleanupOptions> options,
    ILogger<RefreshTokenCleanupWorker> logger) : BackgroundService
{
    private readonly RefreshTokenCleanupOptions _options = options.Value;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(this._options.Interval);

        try
        {
            // Sweep once on startup rather than idling for a full interval first: a worker that has
            // just been redeployed should not leave an existing backlog sitting for another ten
            // minutes. The run is bounded either way, so an immediate sweep costs nothing.
            await this.SweepAsync(stoppingToken);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await this.SweepAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown. Nothing needs draining: every batch is its own committed transaction, so
            // stopping mid-sweep just leaves the remainder for the next process to pick up.
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        try
        {
            var startedAt = Stopwatch.GetTimestamp();
            var now = DateTime.UtcNow;

            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            // Expired and never revoked. Unreachable by every code path: rotation rejects an expired
            // token outright, and family-wide revocation only ever targets unexpired live tails.
            var expiredCutoff = now - this._options.ExpiredRetention;
            var (expiredDeleted, remainingBudget) = await this.DeleteBatchesAsync(
                dbContext.RefreshTokens
                    .Where(token => token.RevokedAtUtc == null && token.ExpiresAtUtc < expiredCutoff)
                    .OrderBy(token => token.ExpiresAtUtc),
                dbContext,
                this._options.MaxBatchesPerRun,
                cancellationToken);

            // Revoked long enough ago that replaying the token can no longer be meaningful. The
            // ExpiresAtUtc clause is belt-and-braces: with the default windows a revoked row is
            // necessarily expired, but that stops holding if Jwt:RefreshTokenDays is raised past the
            // reuse-detection window, and the invariant should be enforced rather than assumed.
            var revokedCutoff = now - this._options.ReuseDetectionWindow;
            var (revokedDeleted, _) = await this.DeleteBatchesAsync(
                dbContext.RefreshTokens
                    .Where(token => token.RevokedAtUtc != null
                        && token.RevokedAtUtc < revokedCutoff
                        && token.ExpiresAtUtc < now)
                    .OrderBy(token => token.RevokedAtUtc),
                dbContext,
                remainingBudget,
                cancellationToken);

            if (expiredDeleted + revokedDeleted > 0)
            {
                // Counts and timing only — a token hash or user id here would turn the log store into
                // a record of who was signed in when.
                logger.LogInformation(
                    "Refresh-token cleanup deleted {ExpiredCount} expired and {RevokedCount} revoked token(s) in {ElapsedMilliseconds} ms.",
                    expiredDeleted,
                    revokedDeleted,
                    (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown cancelled the sweep. Let ExecuteAsync end the loop.
            throw;
        }
        catch (Exception exception)
        {
            // Anything escaping here completes ExecuteAsync's task, and the default
            // BackgroundServiceExceptionBehavior (StopHost) would take the whole process down. A
            // transient database failure must never do that — the next tick simply retries.
            logger.LogError(exception, "A refresh-token cleanup sweep failed.");
        }
    }

    // Deletes candidates a page at a time, stopping when the predicate drains or the batch budget
    // runs out. Each ExecuteDeleteAsync is its own implicit transaction — EF does not open one for
    // it the way SaveChangesAsync does — so row locks are taken and released within a single
    // statement rather than held across the sweep. That is what keeps this off the back of
    // concurrent issuance and rotation, and it is why the sweep must never be wrapped in an
    // explicit transaction.
    //
    // The batch is chosen by a keyed subquery and the DELETE matches on the primary key, written
    // out rather than left to EF's automatic pushdown so the emitted shape is visible here instead
    // of being an implementation detail of the translator. batchIds stays an IQueryable on purpose:
    // materialising it would round-trip the ids and open a window between the read and the delete.
    private async Task<(int Deleted, int RemainingBudget)> DeleteBatchesAsync(
        IQueryable<RefreshToken> candidates,
        AuthDbContext dbContext,
        int batchBudget,
        CancellationToken cancellationToken)
    {
        var deleted = 0;

        while (batchBudget > 0)
        {
            IQueryable<Guid> batchIds = candidates
                .Select(token => token.Id)
                .Take(this._options.BatchSize);

            var batchDeleted = await dbContext.RefreshTokens
                .Where(token => batchIds.Contains(token.Id))
                .ExecuteDeleteAsync(cancellationToken);

            deleted += batchDeleted;
            batchBudget--;

            // A short batch means the predicate is drained; anything else is the cap doing its job.
            if (batchDeleted < this._options.BatchSize)
            {
                break;
            }
        }

        return (deleted, batchBudget);
    }
}
