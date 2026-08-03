using System.Diagnostics;
using Cinedex.Auth.Identity.Options;
using Cinedex.Auth.Identity.Persistence.Repository;
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
/// rotated — and the family-wide reuse response's only trigger for recognising that
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
        // The effective policy, once, at startup. Without this the first troubleshooting question —
        // "it is not deleting anything, is that a bug or is retention just longer than I think?" —
        // cannot be answered from the logs at all.
        logger.LogDebug(
            "Refresh-token cleanup started. Sweeping every {Interval}, deleting unrevoked tokens {ExpiredRetention} past expiry and revoked tokens {ReuseDetectionWindow} past revocation, at most {BatchSize} row(s) per batch and {MaxBatchesPerRun} batch(es) per sweep.",
            this._options.Interval,
            this._options.ExpiredRetention,
            this._options.ReuseDetectionWindow,
            this._options.BatchSize,
            this._options.MaxBatchesPerRun);

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

        logger.LogInformation("Refresh-token cleanup stopped.");
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        try
        {
            var startedAt = Stopwatch.GetTimestamp();
            var now = DateTime.UtcNow;
            var expiredCutoff = now - this._options.ExpiredRetention;
            var revokedCutoff = now - this._options.ReuseDetectionWindow;

            // The cutoffs the predicates will actually use — the fastest way to see why a given row
            // survived a sweep.
            logger.LogDebug(
                "Refresh-token cleanup sweep starting. Deleting unrevoked tokens that expired before {ExpiredCutoff:o}, and revoked tokens revoked before {RevokedCutoff:o}.",
                expiredCutoff,
                revokedCutoff);

            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            var refreshTokens = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

            // Expired and never revoked. Unreachable by every code path: rotation rejects an expired
            // token outright, and family-wide revocation only ever targets unexpired live tails.
            var (expiredDeleted, budgetAfterExpired) = await this.DeleteBatchesAsync(
                (batchSize, ct) => refreshTokens.DeleteExpiredBatchAsync(expiredCutoff, batchSize, ct),
                this._options.MaxBatchesPerRun,
                cancellationToken);

            logger.LogDebug(
                "Refresh-token cleanup deleted {DeletedCount} expired token(s) across {BatchCount} batch(es).",
                expiredDeleted,
                this._options.MaxBatchesPerRun - budgetAfterExpired);

            // Revoked long enough ago that replaying the token can no longer be meaningful. The
            // ExpiresAtUtc clause is belt-and-braces: with the default windows a revoked row is
            // necessarily expired, but that stops holding if Jwt:RefreshTokenDays is raised past the
            // reuse-detection window, and the invariant should be enforced rather than assumed.
            var (revokedDeleted, remainingBudget) = await this.DeleteBatchesAsync(
                (batchSize, ct) => refreshTokens.DeleteRevokedBatchAsync(revokedCutoff, now, batchSize, ct),
                budgetAfterExpired,
                cancellationToken);

            logger.LogDebug(
                "Refresh-token cleanup deleted {DeletedCount} revoked token(s) across {BatchCount} batch(es).",
                revokedDeleted,
                budgetAfterExpired - remainingBudget);

            // Emitted on every sweep, including empty ones. A job that logs only when it finds work
            // is indistinguishable from a job that is not running at all — which is precisely the
            // question this line exists to answer. Counts and timing only: a token hash or user id
            // here would turn the log store into a record of who was signed in when.
            logger.LogInformation(
                "Refresh-token cleanup swept {DeletedCount} token(s) in {ElapsedMilliseconds} ms ({ExpiredCount} expired, {RevokedCount} revoked) using {BatchCount} of {MaxBatchesPerRun} available batch(es).",
                expiredDeleted + revokedDeleted,
                (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
                expiredDeleted,
                revokedDeleted,
                this._options.MaxBatchesPerRun - remainingBudget,
                this._options.MaxBatchesPerRun);

            if (remainingBudget == 0)
            {
                // The sweep stopped because it ran out of budget rather than out of rows, so a
                // backlog is probably still waiting. Harmless once (the next sweep continues), but
                // sustained repetition means the drain rate is below the accrual rate.
                logger.LogWarning(
                    "Refresh-token cleanup used its entire budget of {MaxBatchesPerRun} batch(es); rows are likely still pending and will drain on later sweeps. If this repeats every sweep, raise RefreshTokenCleanup:BatchSize or MaxBatchesPerRun, or shorten Interval.",
                    this._options.MaxBatchesPerRun);
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

    // Runs one of the repository's batch deletes a page at a time, stopping when the predicate
    // drains or the batch budget runs out. Only the pacing lives here — which rows a batch selects,
    // and why the delete is set-based rather than a tracked RemoveRange, belongs to the repository
    // and is documented there.
    //
    // The budget is what bounds a sweep. Without it a large backlog would be drained in one
    // unbounded run, competing with issuance and rotation for the same rows for as long as it took.
    private async Task<(int Deleted, int RemainingBudget)> DeleteBatchesAsync(
        Func<int, CancellationToken, Task<int>> deleteBatchAsync,
        int batchBudget,
        CancellationToken cancellationToken)
    {
        var deleted = 0;

        while (batchBudget > 0)
        {
            var batchDeleted = await deleteBatchAsync(this._options.BatchSize, cancellationToken);

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
