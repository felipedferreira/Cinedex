using Cinedex.Persistence.Abstractions.Exceptions;
using Cinedex.Persistence.Abstractions.Transactions;

namespace Cinedex.Persistence.Abstractions;

/// <summary>
/// Convenience wrappers over <see cref="IUnitOfWork"/> for the begin/save/commit/rollback shape that
/// otherwise gets retyped at every call site.
/// </summary>
/// <remarks>
/// These are extension methods rather than interface members on purpose. They are composed entirely
/// from the interface's own operations, so every implementation gets them for free and none can get
/// them subtly wrong — and <see cref="IUnitOfWork"/> stays as small as it is meant to be.
/// </remarks>
public static class UnitOfWorkExtensions
{
    private const int BaseRetryDelayMilliseconds = 20;

    private const int MaxRetryDelayMilliseconds = 500;

    /// <summary>
    /// Runs an operation inside a transaction, saving and committing on success and rolling back on
    /// failure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Equivalent to opening a transaction, running <paramref name="operation"/>, calling
    /// <see cref="IUnitOfWork.SaveChangesAsync"/>, and committing — with the rollback path handled.
    /// Saving here is unconditional and harmless: if <paramref name="operation"/> already saved, the
    /// second call finds nothing pending and does not touch the database.
    /// </para>
    /// <para>
    /// <b>On retries.</b> With <paramref name="maxRetries"/> above zero, a
    /// <see cref="PersistenceException"/> whose <see cref="PersistenceException.IsTransient"/> is
    /// <see langword="true"/> causes the whole thing to run again in a brand-new transaction, after a
    /// short randomised backoff. This is only correct if <paramref name="operation"/> can run more
    /// than once: it must re-read the state it depends on rather than closing over values loaded
    /// before the call, and it must not have side effects outside the database — no email sent, no
    /// message published, no counter incremented in memory. An operation that reads inside the
    /// delegate and only writes through the unit of work satisfies this. One that was handed
    /// already-loaded entities does not, because
    /// <see cref="IUnitOfWork.DiscardChanges"/> detaches them between attempts and the second attempt
    /// would be writing objects nothing is tracking.
    /// </para>
    /// <para>
    /// Retries are worth requesting when running at
    /// <see cref="TransactionIsolationLevel.RepeatableRead"/> or
    /// <see cref="TransactionIsolationLevel.Serializable"/>, where an abort under contention is
    /// expected behaviour rather than a fault. At
    /// <see cref="TransactionIsolationLevel.ReadCommitted"/> transient aborts are rare enough that
    /// the default of zero is usually right.
    /// </para>
    /// </remarks>
    /// <typeparam name="TResult">The operation's result type.</typeparam>
    /// <param name="unitOfWork">The unit of work to run against.</param>
    /// <param name="operation">
    /// The work to perform. Receives the cancellation token so it does not have to close over one.
    /// </param>
    /// <param name="isolationLevel">The isolation level to open the transaction at.</param>
    /// <param name="maxRetries">
    /// How many additional attempts to make after a transient failure. Zero — the default — means the
    /// operation runs at most once.
    /// </param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The operation's result.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="unitOfWork"/> or <paramref name="operation"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxRetries"/> is negative.</exception>
    public static async Task<TResult> ExecuteInTransactionAsync<TResult>(
        this IUnitOfWork unitOfWork,
        Func<CancellationToken, Task<TResult>> operation,
        TransactionIsolationLevel isolationLevel = TransactionIsolationLevel.Default,
        int maxRetries = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        for (int attempt = 0; ; attempt++)
        {
            try
            {
                // Disposal is what rolls back, and it happens while the exception unwinds — after the
                // catch filter below has been evaluated but before its body runs. So by the time a
                // retry calls DiscardChanges, the failed transaction is already gone and the next
                // BeginTransactionAsync will not trip the no-nesting rule.
                await using ITransaction transaction =
                    await unitOfWork.BeginTransactionAsync(isolationLevel, cancellationToken);

                TResult result = await operation(cancellationToken);

                await unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return result;
            }
            catch (PersistenceException exception) when (exception.IsTransient && attempt < maxRetries)
            {
                unitOfWork.DiscardChanges();
                await Task.Delay(NextRetryDelay(attempt), cancellationToken);
            }
        }
    }

    /// <summary>
    /// Runs an operation inside a transaction, saving and committing on success and rolling back on
    /// failure.
    /// </summary>
    /// <remarks>
    /// The result-free overload. Every caveat on the generic version applies here too — in
    /// particular, asking for retries commits you to an operation that is safe to run more than once.
    /// </remarks>
    /// <param name="unitOfWork">The unit of work to run against.</param>
    /// <param name="operation">
    /// The work to perform. Receives the cancellation token so it does not have to close over one.
    /// </param>
    /// <param name="isolationLevel">The isolation level to open the transaction at.</param>
    /// <param name="maxRetries">
    /// How many additional attempts to make after a transient failure. Zero — the default — means the
    /// operation runs at most once.
    /// </param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that completes when the transaction has been committed.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="unitOfWork"/> or <paramref name="operation"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxRetries"/> is negative.</exception>
    public static Task ExecuteInTransactionAsync(
        this IUnitOfWork unitOfWork,
        Func<CancellationToken, Task> operation,
        TransactionIsolationLevel isolationLevel = TransactionIsolationLevel.Default,
        int maxRetries = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return unitOfWork.ExecuteInTransactionAsync<object?>(
            async token =>
            {
                await operation(token);
                return null;
            },
            isolationLevel,
            maxRetries,
            cancellationToken);
    }

    // Exponential backoff with jitter, capped. The jitter is the part that matters: a serialization
    // failure means two transactions collided, and retrying both after the same fixed delay just
    // schedules the same collision again.
    private static TimeSpan NextRetryDelay(int attempt)
    {
        int ceiling = attempt >= 16
            ? MaxRetryDelayMilliseconds
            : Math.Min(BaseRetryDelayMilliseconds << attempt, MaxRetryDelayMilliseconds);

        return TimeSpan.FromMilliseconds(Random.Shared.Next(ceiling / 2, ceiling + 1));
    }
}
