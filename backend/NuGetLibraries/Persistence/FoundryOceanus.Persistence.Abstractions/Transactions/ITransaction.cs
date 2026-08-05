namespace FoundryOceanus.Persistence.Abstractions.Transactions;

/// <summary>
/// A database transaction opened by <see cref="IUnitOfWork.BeginTransactionAsync"/>. The caller owns
/// it and must either commit it or dispose it.
/// </summary>
/// <remarks>
/// <para>
/// Disposing an uncommitted transaction rolls it back. That makes <c>await using</c> the correct way
/// to hold one — an exception on any path between the <c>using</c> and the commit leaves the database
/// untouched, with no <c>finally</c> block to get wrong:
/// </para>
/// <code>
/// await using ITransaction transaction = await unitOfWork.BeginTransactionAsync(cancellationToken: ct);
///
/// await unitOfWork.Repository&lt;IRefreshTokenRepository&gt;().RotateAsync(hash, now, replacement, ct);
/// await unitOfWork.SaveChangesAsync(ct);
/// await transaction.CommitAsync(ct);
/// </code>
/// <para>
/// A transaction is a real database transaction on a real connection, not an ambient scope: writes
/// made through repositories resolved from the same <see cref="IUnitOfWork"/> enlist in it, and
/// nothing else does.
/// </para>
/// </remarks>
public interface ITransaction : IAsyncDisposable
{
    /// <summary>
    /// Gets the provider's identifier for this transaction. Useful as a log correlation key.
    /// </summary>
    Guid TransactionId { get; }

    /// <summary>
    /// Gets the isolation level the transaction was opened at.
    /// </summary>
    /// <remarks>
    /// Reports the level that was requested. When
    /// <see cref="TransactionIsolationLevel.Default"/> was requested this returns
    /// <see cref="TransactionIsolationLevel.Default"/> rather than resolving what the server chose.
    /// </remarks>
    TransactionIsolationLevel IsolationLevel { get; }

    /// <summary>
    /// Gets a value indicating whether this transaction has been committed or rolled back.
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Commits the transaction.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that completes when the transaction has been committed.</returns>
    /// <exception cref="InvalidOperationException">The transaction has already completed.</exception>
    /// <exception cref="Exceptions.PersistenceException">
    /// The commit was rejected by the database — most often a deferred constraint firing at commit
    /// time, or a serialization failure under
    /// <see cref="TransactionIsolationLevel.Serializable"/>.
    /// </exception>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls the transaction back. Does nothing if it has already completed.
    /// </summary>
    /// <remarks>
    /// Tolerating a call on an already-completed transaction is deliberate: it makes this safe to call
    /// from a <c>catch</c> or <c>finally</c> block without first checking
    /// <see cref="IsCompleted"/>. <see cref="CommitAsync"/> does <i>not</i> tolerate it — committing
    /// twice is a bug worth surfacing, while rolling back twice is not.
    /// </remarks>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that completes when the transaction has been rolled back.</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a savepoint that a later <see cref="ISavepoint.RollbackAsync"/> can return to without
    /// abandoning the whole transaction.
    /// </summary>
    /// <remarks>
    /// Savepoints are here because the alternative is worse. Without them, any caller who needs to
    /// undo part of a transaction has to reach past this abstraction for the underlying provider
    /// object — at which point the abstraction is leaking and you are paying its cost without
    /// getting its benefit.
    /// </remarks>
    /// <param name="name">
    /// The savepoint name, unique within this transaction. Must be a valid SQL identifier: it is sent
    /// to the database as an identifier, not as a parameter.
    /// </param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A handle to the savepoint.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid savepoint name.</exception>
    /// <exception cref="InvalidOperationException">The transaction has already completed.</exception>
    Task<ISavepoint> CreateSavepointAsync(string name, CancellationToken cancellationToken = default);
}
