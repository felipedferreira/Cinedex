using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Cinedex.Auth.Identity.Persistence.Repository;

/// <inheritdoc />
internal sealed class RefreshTokenRepository(AuthDbContext dbContext) : IRefreshTokenRepository
{
    /// <inheritdoc />
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        dbContext.Database.BeginTransactionAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AcquireFamilyLockAsync(Guid familyId, CancellationToken cancellationToken)
    {
        // Advisory transaction locks belong to the PostgreSQL session and transaction that acquire
        // them, so execute the command through the exact connection and transaction EF opened.
        // Reading the ambient transaction rather than taking one as a parameter turns "this only
        // means anything inside a transaction" into an invariant the type system enforces at runtime.
        var transaction = dbContext.Database.CurrentTransaction
            ?? throw new InvalidOperationException(
                "A refresh-token family lock must be acquired inside a transaction; the lock is released when that transaction ends.");

        var connection = (NpgsqlConnection)transaction.GetDbTransaction().Connection!;
        var npgsqlTransaction = (NpgsqlTransaction)transaction.GetDbTransaction();

        // PostgreSQL advisory locks use a 64-bit key. Hash the family UUID into that key so every
        // request for the same family waits on the same logical mutex. The lock is released
        // automatically when the surrounding transaction commits or rolls back.
        //
        // Two UUIDs can theoretically hash to the same key, but that only makes unrelated families
        // wait for one another. It cannot cross family boundaries because the revocation update
        // still filters rows by the complete FamilyId.
        await using var command = new NpgsqlCommand(
            "SELECT pg_advisory_xact_lock(hashtextextended(CAST(@familyId AS text), 0));",
            connection,
            npgsqlTransaction);
        command.Parameters.AddWithValue("familyId", familyId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<RefreshToken?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .AsNoTracking()
            .Where(token => token.TokenHash == tokenHash)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> RotateAsync(
        string tokenHash,
        DateTime rotatedAtUtc,
        string replacementTokenHash,
        CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(token =>
                token.TokenHash == tokenHash &&
                token.RevokedAtUtc == null &&
                token.ExpiresAtUtc > rotatedAtUtc)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAtUtc, rotatedAtUtc)
                    .SetProperty(token => token.ReplacedByTokenHash, replacementTokenHash),
                cancellationToken);

    /// <inheritdoc />
    public Task<int> RevokeActiveFamilyAsync(Guid familyId, DateTime revokedAtUtc, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(token =>
                token.FamilyId == familyId &&
                token.RevokedAtUtc == null &&
                token.ExpiresAtUtc > revokedAtUtc)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedAtUtc, revokedAtUtc),
                cancellationToken);

    // One conditional statement rather than read-then-mutate-then-save. Idempotent either way, but
    // the RevokedAtUtc IS NULL filter also means an earlier revocation's timestamp survives a
    // concurrent logout instead of being overwritten by the later one.

    /// <inheritdoc />
    public Task RevokeByTokenHashAsync(string tokenHash, DateTime revokedAtUtc, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(token => token.TokenHash == tokenHash && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedAtUtc, revokedAtUtc),
                cancellationToken);

    /// <inheritdoc />
    public Task<int> RevokeAllActiveForUserAsync(Guid userId, DateTime revokedAtUtc, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedAtUtc, revokedAtUtc),
                cancellationToken);

    /// <inheritdoc />
    public Task<int> DeleteExpiredBatchAsync(DateTime expiredCutoff, int batchSize, CancellationToken cancellationToken) =>
        DeleteBatchAsync(
            dbContext.RefreshTokens
                .Where(token => token.RevokedAtUtc == null && token.ExpiresAtUtc < expiredCutoff)
                .OrderBy(token => token.ExpiresAtUtc),
            batchSize,
            cancellationToken);

    /// <inheritdoc />
    public Task<int> DeleteRevokedBatchAsync(
        DateTime revokedCutoff,
        DateTime expiredBefore,
        int batchSize,
        CancellationToken cancellationToken) =>
        DeleteBatchAsync(
            dbContext.RefreshTokens
                .Where(token => token.RevokedAtUtc != null
                    && token.RevokedAtUtc < revokedCutoff
                    && token.ExpiresAtUtc < expiredBefore)
                .OrderBy(token => token.RevokedAtUtc),
            batchSize,
            cancellationToken);

    // Deletes one page of the supplied candidates. The batch is chosen by a keyed subquery and the
    // DELETE matches on the primary key, written out rather than left to EF's automatic pushdown so
    // the emitted shape is visible here instead of being an implementation detail of the translator.
    // batchIds stays an IQueryable on purpose: materialising it would round-trip the ids and open a
    // window between the read and the delete.
    //
    // ExecuteDeleteAsync rather than loading the rows and calling RemoveRange, which is the more
    // usual way to delete through EF. The trade, deliberately taken:
    //
    //   What we give up. ExecuteDelete bypasses the whole SaveChanges pipeline — no change tracking,
    //   no interceptors, no concurrency-token checks, no domain events, and no EF-side cascade (the
    //   database's FK rules still apply). It also reports only a row count, never which rows went. In
    //   a codebase that audits or soft-deletes through a SaveChanges interceptor, using it would
    //   silently skip that, with no compile error to warn you. None of that machinery exists here
    //   today, and the retention sweep needs nothing from the rows it removes: it logs counts only,
    //   on purpose, so the log store never becomes a record of who was signed in.
    //
    //   What we get. RemoveRange would first materialise every row — eight columns for up to
    //   BatchSize × MaxBatchesPerRun rows per sweep — purely to throw them away. Worse, the scope is
    //   per sweep, so one DbContext serves every batch: the change tracker would still hold each
    //   previous batch's entities, and DetectChanges is O(tracked), so each successive SaveChanges
    //   would slow down while memory grew, unless we added ChangeTracker.Clear() calls purely to work
    //   around the approach. SaveChanges would also wrap each batch in a transaction spanning
    //   BatchSize individual DELETE statements, holding the same row locks far longer than the one
    //   set-based statement below — the opposite of what a job that must not block issuance or
    //   rotation wants.
    //
    // The rule this follows: RemoveRange expresses "these objects are gone from my model",
    // ExecuteDelete expresses "these rows are gone from the table". Retention is storage
    // housekeeping, not a domain operation, so it is the second kind.
    //
    // Each ExecuteDeleteAsync is also its own implicit transaction — EF does not open one for it the
    // way SaveChangesAsync does — so row locks are taken and released within a single statement
    // rather than held across the sweep. That is what keeps the sweep off the back of concurrent
    // issuance and rotation, and it is why callers must never wrap it in an explicit transaction.
    //
    // Revisit this if RefreshToken ever gains a SaveChanges interceptor, a soft-delete convention, or
    // a concurrency token — this call would bypass all three.
    private Task<int> DeleteBatchAsync(
        IQueryable<RefreshToken> candidates,
        int batchSize,
        CancellationToken cancellationToken)
    {
        IQueryable<Guid> batchIds = candidates
            .Select(token => token.Id)
            .Take(batchSize);

        return dbContext.RefreshTokens
            .Where(token => batchIds.Contains(token.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
