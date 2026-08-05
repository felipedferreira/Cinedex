using FoundryOceanus.Persistence.Abstractions.Transactions;

namespace FoundryOceanus.Persistence.Tests.Fakes;

/// <summary>
/// An <see cref="ITransaction"/> that records commits, rollbacks and disposal.
/// </summary>
public sealed class RecordingTransaction : ITransaction
{
    private readonly Action _onDisposed;

    internal RecordingTransaction(TransactionIsolationLevel isolationLevel, Action onDisposed)
    {
        IsolationLevel = isolationLevel;
        _onDisposed = onDisposed;
    }

    public Guid TransactionId { get; } = Guid.NewGuid();

    public TransactionIsolationLevel IsolationLevel { get; }

    public bool IsCompleted { get; private set; }

    public bool Committed { get; private set; }

    public bool Disposed { get; private set; }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        Committed = true;
        IsCompleted = true;

        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        IsCompleted = true;

        return Task.CompletedTask;
    }

    public Task<ISavepoint> CreateSavepointAsync(string name, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("This fake does not create savepoints.");

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        IsCompleted = true;
        _onDisposed();

        return ValueTask.CompletedTask;
    }
}
