namespace FoundryOceanus.Persistence.Abstractions.Transactions;

/// <summary>
/// A named point inside a <see cref="ITransaction"/> that work can be rolled back to without
/// abandoning the transaction itself.
/// </summary>
/// <remarks>
/// <para>
/// Disposing a savepoint does <b>not</b> roll back to it and does not release it. A savepoint is a
/// marker, not a resource: it costs nothing to leave in place, and it disappears when the enclosing
/// transaction ends. Disposal is offered only so the type composes with <c>await using</c> in code
/// that holds one alongside other disposables.
/// </para>
/// <para>
/// This is the opposite of the <see cref="ITransaction"/> convention on purpose. A transaction that
/// falls out of scope uncommitted almost always means something went wrong, so rolling it back is the
/// safe reading. A savepoint that falls out of scope usually means the work it guarded succeeded, so
/// rolling it back would be actively wrong.
/// </para>
/// </remarks>
public interface ISavepoint : IAsyncDisposable
{
    /// <summary>
    /// Gets the savepoint's name, as supplied to <see cref="ITransaction.CreateSavepointAsync"/>.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Discards every change made since the savepoint was created. The transaction stays open and can
    /// still be committed.
    /// </summary>
    /// <remarks>
    /// Rolling back to a savepoint undoes the <i>database's</i> view of that work. It does not undo
    /// your in-memory object graph: entities the discarded statements loaded or modified are still
    /// tracked, and saving again will try to write them back. When the rolled-back work touched
    /// tracked entities rather than set-based statements, follow this with
    /// <see cref="IUnitOfWork.DiscardChanges"/> and re-read what you still need.
    /// </remarks>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that completes when the savepoint has been rolled back to.</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases the savepoint, keeping the work done since it was created.
    /// </summary>
    /// <remarks>
    /// Rarely needed — an unreleased savepoint is harmless and is cleaned up when the transaction
    /// ends. It is worth calling in a long transaction that creates savepoints in a loop, where the
    /// server would otherwise accumulate them.
    /// </remarks>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that completes when the savepoint has been released.</returns>
    Task ReleaseAsync(CancellationToken cancellationToken = default);
}
