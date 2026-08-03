using Cinedex.Persistence.Abstractions.Exceptions;
using Npgsql;

namespace Cinedex.Persistence.EntityFrameworkCore.Postgres;

/// <summary>
/// Classifies PostgreSQL failures by SQLSTATE.
/// </summary>
/// <remarks>
/// <para>
/// This class is the reason the application layer never has to know that <c>"23505"</c> means a
/// duplicate. It is also, deliberately, the only place that knowledge lives — when Npgsql changes
/// how it reports something, or PostgreSQL adds a code worth reacting to, this is the file that
/// changes and nothing downstream does.
/// </para>
/// <para>
/// <b>It claims every PostgreSQL error, including ones it cannot name.</b> A code with no specific
/// mapping becomes an <see cref="UnclassifiedPersistenceException"/> rather than being passed along
/// for someone else to handle, because "passed along" means an <c>NpgsqlException</c> arriving in
/// application code that was written not to know PostgreSQL exists. The original is kept as the inner
/// exception, so nothing is lost for logs or debugging.
/// </para>
/// <para>
/// The SQLSTATE constants come from Npgsql's own <see cref="PostgresErrorCodes"/>, which already
/// carries all 238 of them. Re-declaring the handful used here would have been a second list to keep
/// in step with PostgreSQL for no benefit — the same "wrapping what the vendor already does well is
/// pure tax" argument that decides what else does and does not belong in these packages.
/// </para>
/// </remarks>
public sealed class NpgsqlExceptionTranslator : IPersistenceExceptionTranslator
{
    /// <inheritdoc />
    public PersistenceException? Translate(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        // Checked before anything else. Npgsql surfaces a cancelled statement as an
        // OperationCanceledException that can have a PostgresException (57014) hanging off it, and
        // classifying that as a database failure would turn every graceful shutdown into a logged
        // error — and, worse, make it retryable.
        if (exception is OperationCanceledException)
        {
            return null;
        }

        // Entity Framework wraps provider failures in DbUpdateException, so the code we need is
        // usually one or two levels down rather than on the exception we were handed.
        if (FindInnermost<PostgresException>(exception) is { } postgresException)
        {
            return TranslateSqlState(postgresException, exception);
        }

        // No SQLSTATE: a connection-level failure that never reached the server. Npgsql already
        // knows which of these are worth another attempt, so defer to it rather than maintaining a
        // second opinion here.
        if (FindInnermost<NpgsqlException>(exception) is { IsTransient: true })
        {
            return new TransientPersistenceException(
                "The connection to PostgreSQL failed before the statement completed. The operation can be retried.",
                exception);
        }

        return null;
    }

    private static TException? FindInnermost<TException>(Exception exception)
        where TException : Exception
    {
        TException? found = null;

        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is TException match)
            {
                found = match;
            }
        }

        return found;
    }

    private static PersistenceException TranslateSqlState(PostgresException postgresException, Exception original) =>
        postgresException.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation => new DuplicateKeyException(
                DescribeConstraint("A unique constraint was violated", postgresException),
                postgresException.ConstraintName,
                original),

            // An EXCLUDE constraint says "no two rows may relate this way" — overlapping bookings for
            // one room, say. Structurally that is a uniqueness rule with a richer predicate, so it maps
            // here rather than growing a fourth constraint exception that would be handled identically.
            PostgresErrorCodes.ExclusionViolation => new DuplicateKeyException(
                DescribeConstraint("An exclusion constraint was violated", postgresException),
                postgresException.ConstraintName,
                original),

            PostgresErrorCodes.ForeignKeyViolation => new ReferentialIntegrityException(
                DescribeConstraint("A foreign key constraint was violated", postgresException),
                postgresException.ConstraintName,
                original),

            PostgresErrorCodes.SerializationFailure => new TransientPersistenceException(
                "The transaction was aborted to preserve serializability (SQLSTATE 40001). This is normal at "
                + "RepeatableRead and Serializable isolation: PostgreSQL detected that concurrent transactions could "
                + "not have run one after another, and picked this one to redo. Retry the whole transaction.",
                original),

            PostgresErrorCodes.DeadlockDetected => new TransientPersistenceException(
                "The transaction was chosen as a deadlock victim (SQLSTATE 40P01). Retrying usually succeeds, but "
                + "repeated deadlocks mean two code paths take the same locks in opposite orders — fix the ordering "
                + "rather than relying on the retry.",
                original),

            PostgresErrorCodes.StatementCompletionUnknown => new TransientPersistenceException(
                "The connection dropped while the transaction's outcome was still unknown (SQLSTATE 40003). Retrying "
                + "is safe only if the operation is idempotent: the previous attempt may have committed.",
                original),

            PostgresErrorCodes.LockNotAvailable => new TransientPersistenceException(
                "A lock requested with NOWAIT was already held (SQLSTATE 55P03).",
                original),

            // Not transient, on purpose. This is nearly always statement_timeout firing on a query that
            // is too slow, and retrying a too-slow query is how a slow endpoint becomes a busy one.
            PostgresErrorCodes.QueryCanceled => new UnclassifiedPersistenceException(
                "The statement was cancelled by the server (SQLSTATE 57014), typically by statement_timeout. This is "
                + "not marked transient: the usual cause is a query that needs an index or a narrower predicate, and "
                + "retrying it unchanged would only repeat the timeout.",
                original),

            PostgresErrorCodes.NotNullViolation => new UnclassifiedPersistenceException(
                DescribeColumn("A NOT NULL constraint was violated", postgresException),
                original),

            PostgresErrorCodes.CheckViolation => new UnclassifiedPersistenceException(
                DescribeConstraint("A CHECK constraint was violated", postgresException),
                original),

            _ => new UnclassifiedPersistenceException(
                $"PostgreSQL rejected the operation with SQLSTATE {postgresException.SqlState}: "
                + $"{postgresException.MessageText}",
                original),
        };

    private static string DescribeConstraint(string summary, PostgresException exception)
    {
        string constraint = string.IsNullOrEmpty(exception.ConstraintName)
            ? "an unnamed constraint"
            : $"constraint '{exception.ConstraintName}'";

        string table = string.IsNullOrEmpty(exception.TableName)
            ? string.Empty
            : $" on table '{exception.TableName}'";

        return $"{summary}: {constraint}{table}.";
    }

    private static string DescribeColumn(string summary, PostgresException exception)
    {
        string column = string.IsNullOrEmpty(exception.ColumnName)
            ? "an unnamed column"
            : $"column '{exception.ColumnName}'";

        string table = string.IsNullOrEmpty(exception.TableName)
            ? string.Empty
            : $" on table '{exception.TableName}'";

        return $"{summary}: {column}{table}.";
    }
}
