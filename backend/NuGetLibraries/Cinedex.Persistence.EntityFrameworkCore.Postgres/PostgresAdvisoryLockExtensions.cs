using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cinedex.Persistence.EntityFrameworkCore.Postgres;

/// <summary>
/// Transaction-scoped PostgreSQL advisory locks — an application-level mutex, held in the database,
/// released automatically when the transaction ends.
/// </summary>
/// <remarks>
/// <para>
/// The problem these solve is serialising work that has no row to lock yet. "Only one request may
/// rotate this token family at a time" cannot be expressed as <c>SELECT … FOR UPDATE</c> when the
/// deciding read might return nothing; an advisory lock gives you a name to synchronise on instead of
/// a row.
/// </para>
/// <para>
/// <b>These hang off <see cref="DbContext"/> rather than off <c>IUnitOfWork</c>, and that placement
/// is the design.</b> An advisory lock is a database mechanism, not a business concept, so it belongs
/// in the adapter that already knows it is talking to PostgreSQL. If application-layer code finds
/// itself wanting one, the operation it is trying to protect has leaked out of the adapter — express
/// it as a repository method named for what the domain is doing (<c>RotateAsync</c>), and take the
/// lock inside that method. Needing a <see cref="DbContext"/> to call these is what keeps that
/// boundary honest.
/// </para>
/// <para>
/// <b>Every lock here is transaction-scoped</b> (<c>pg_advisory_xact_lock</c>, not
/// <c>pg_advisory_lock</c>). Session-scoped locks are deliberately not offered: with connection
/// pooling, the session outlives the request, so a session lock that is not explicitly released —
/// because an exception skipped the release, say — stays held on a pooled connection and is
/// eventually handed to an unrelated request. Transaction-scoped locks cannot be leaked that way. The
/// commit or rollback releases them, and there is always a commit or rollback.
/// </para>
/// <para>
/// <b>Keys share one namespace per database.</b> PostgreSQL advisory locks are keyed on a single
/// 64-bit space covering the whole database, so two unrelated features whose keys collide will block
/// each other. Prefix your string keys with what they are protecting —
/// <c>$"refresh-token-family:{familyId}"</c> rather than <c>familyId.ToString()</c> — and collisions
/// stop being something you have to think about.
/// </para>
/// </remarks>
public static class PostgresAdvisoryLockExtensions
{
    // hashtextextended runs in PostgreSQL rather than in .NET, and that is not an optimisation.
    // string.GetHashCode is randomised per process on .NET Core: the same key would hash differently
    // in every instance of the service, so two instances would take two different locks and neither
    // would ever wait for the other. The bug would be invisible on one machine and would appear only
    // once the service scaled out. PostgreSQL's hash is stable across processes, versions and
    // machines, which is the property this actually needs.
    private const string AcquireByTextSql =
        "SELECT pg_advisory_xact_lock(hashtextextended(@key, 0));";

    private const string TryAcquireByTextSql =
        "SELECT pg_try_advisory_xact_lock(hashtextextended(@key, 0));";

    private const string AcquireByIdSql =
        "SELECT pg_advisory_xact_lock(@key);";

    private const string TryAcquireByIdSql =
        "SELECT pg_try_advisory_xact_lock(@key);";

    /// <summary>
    /// Takes an advisory lock on a text key, waiting until it is available.
    /// </summary>
    /// <remarks>
    /// Blocks — in the database, holding a connection — until whoever holds the lock commits or rolls
    /// back. Waiting is the point, so that concurrent callers queue rather than interleave, but it
    /// does mean a caller that holds the lock for a long time makes everyone else wait for it too.
    /// Take the lock as late as possible and keep the transaction short.
    /// </remarks>
    /// <param name="dbContext">
    /// The context whose transaction the lock belongs to. Use the same instance the surrounding unit
    /// of work saves through.
    /// </param>
    /// <param name="key">
    /// The lock's name. Prefix it with what it protects to avoid colliding with unrelated locks.
    /// </param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that completes once the lock is held.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dbContext"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">
    /// No transaction is open on <paramref name="dbContext"/>. A transaction-scoped lock taken outside
    /// a transaction is released immediately, so this is always a bug rather than a degraded mode.
    /// </exception>
    public static async Task AcquireTransactionLockAsync(
        this DbContext dbContext,
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        await ExecuteAsync(dbContext, AcquireByTextSql, key, cancellationToken);
    }

    /// <summary>
    /// Takes an advisory lock on a numeric key, waiting until it is available.
    /// </summary>
    /// <remarks>
    /// Use this when you already have a stable 64-bit identifier and do not need hashing. It is the
    /// same lock space as the text overload — a text key that happens to hash to this number is this
    /// lock.
    /// </remarks>
    /// <param name="dbContext">The context whose transaction the lock belongs to.</param>
    /// <param name="key">The lock identifier.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that completes once the lock is held.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dbContext"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No transaction is open on <paramref name="dbContext"/>.</exception>
    public static async Task AcquireTransactionLockAsync(
        this DbContext dbContext,
        long key,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(dbContext, AcquireByIdSql, key, cancellationToken);
    }

    /// <summary>
    /// Attempts to take an advisory lock on a text key, returning immediately if it is held elsewhere.
    /// </summary>
    /// <remarks>
    /// The non-blocking variant, for work that is better skipped than queued — a periodic sweep that
    /// only one instance needs to run, for instance, where a second instance finding the lock taken
    /// should simply move on rather than wait to repeat work that is already happening.
    /// </remarks>
    /// <param name="dbContext">The context whose transaction the lock belongs to.</param>
    /// <param name="key">The lock's name.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns><see langword="true"/> if the lock was taken; <see langword="false"/> if held elsewhere.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dbContext"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">No transaction is open on <paramref name="dbContext"/>.</exception>
    public static async Task<bool> TryAcquireTransactionLockAsync(
        this DbContext dbContext,
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return await ExecuteScalarAsync(dbContext, TryAcquireByTextSql, key, cancellationToken);
    }

    /// <summary>
    /// Attempts to take an advisory lock on a numeric key, returning immediately if it is held
    /// elsewhere.
    /// </summary>
    /// <param name="dbContext">The context whose transaction the lock belongs to.</param>
    /// <param name="key">The lock identifier.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns><see langword="true"/> if the lock was taken; <see langword="false"/> if held elsewhere.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dbContext"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No transaction is open on <paramref name="dbContext"/>.</exception>
    public static async Task<bool> TryAcquireTransactionLockAsync(
        this DbContext dbContext,
        long key,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteScalarAsync(dbContext, TryAcquireByIdSql, key, cancellationToken);
    }

    private static async Task ExecuteAsync(
        DbContext dbContext,
        string sql,
        object key,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = CreateCommand(dbContext, sql, key);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> ExecuteScalarAsync(
        DbContext dbContext,
        string sql,
        object key,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = CreateCommand(dbContext, sql, key);

        object? result = await command.ExecuteScalarAsync(cancellationToken);

        return result is true;
    }

    private static DbCommand CreateCommand(DbContext dbContext, string sql, object key)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        // Reading the ambient transaction instead of accepting one as a parameter turns "this only
        // means something inside a transaction" from a comment into an invariant that fails loudly.
        IDbContextTransaction transaction = dbContext.Database.CurrentTransaction
            ?? throw new InvalidOperationException(
                "An advisory lock must be taken inside a transaction. These locks are released when the transaction "
                + "ends, so one taken outside a transaction is released by the statement that took it — the call would "
                + "appear to succeed while protecting nothing. Open a transaction on the unit of work first.");

        DbCommand command = dbContext.Database.GetDbConnection().CreateCommand();

        try
        {
            command.CommandText = sql;

            // Binding the command to the transaction is what puts the lock on the same PostgreSQL
            // session that will later commit or roll back. Without it the command would run outside
            // the transaction, and the lock would be released before the work it guards began.
            command.Transaction = transaction.GetDbTransaction();

            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = "key";
            parameter.Value = key;
            command.Parameters.Add(parameter);

            return command;
        }
        catch
        {
            command.Dispose();
            throw;
        }
    }
}
