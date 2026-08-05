using FoundryOceanus.Persistence.Abstractions;
using FoundryOceanus.Persistence.Abstractions.Transactions;

namespace FoundryOceanus.Persistence.Tests.Fakes;

/// <summary>
/// An <see cref="IUnitOfWork"/> that records what was called on it, for testing
/// <see cref="UnitOfWorkExtensions.ExecuteInTransactionAsync"/> without a database.
/// </summary>
public sealed class RecordingUnitOfWork : IUnitOfWork
{
    private RecordingTransaction? _currentTransaction;

    public ITransaction? CurrentTransaction => _currentTransaction;

    public bool HasPendingChanges { get; set; }

    public List<RecordingTransaction> Transactions { get; } = [];

    public int SaveChangesCallCount { get; private set; }

    public int DiscardChangesCallCount { get; private set; }

    public TRepository Repository<TRepository>()
        where TRepository : class, IRepository =>
        throw new NotSupportedException("This fake does not resolve repositories.");

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;

        return Task.FromResult(0);
    }

    public void DiscardChanges() => DiscardChangesCallCount++;

    public Task<ITransaction> BeginTransactionAsync(
        TransactionIsolationLevel isolationLevel = TransactionIsolationLevel.Default,
        CancellationToken cancellationToken = default)
    {
        // Mirrors the real implementation's refusal to nest, so a test that accidentally leaves a
        // transaction open fails here rather than passing for the wrong reason.
        if (_currentTransaction is not null)
        {
            throw new InvalidOperationException("A transaction is already open on this unit of work.");
        }

        var transaction = new RecordingTransaction(isolationLevel, () => _currentTransaction = null);
        _currentTransaction = transaction;
        Transactions.Add(transaction);

        return Task.FromResult<ITransaction>(transaction);
    }
}
