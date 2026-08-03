using Cinedex.Persistence.Abstractions.Transactions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cinedex.Persistence.EntityFrameworkCore.Internal;

/// <summary>
/// <see cref="ISavepoint"/> over an Entity Framework transaction's savepoint support.
/// </summary>
internal sealed class EfSavepoint : ISavepoint
{
    private readonly IDbContextTransaction _transaction;

    internal EfSavepoint(IDbContextTransaction transaction, string name)
    {
        _transaction = transaction;
        Name = name;
    }

    public string Name { get; }

    public Task RollbackAsync(CancellationToken cancellationToken = default) =>
        _transaction.RollbackToSavepointAsync(Name, cancellationToken);

    public Task ReleaseAsync(CancellationToken cancellationToken = default) =>
        _transaction.ReleaseSavepointAsync(Name, cancellationToken);

    // Deliberately does nothing. A savepoint holds no resource of its own — it is a label inside a
    // transaction that disappears when the transaction ends — so there is nothing to release, and
    // rolling back here would silently undo work that reached this point by succeeding.
    // ISavepoint is IAsyncDisposable only so it composes with `await using` alongside other
    // disposables; see the remarks on that interface.
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
