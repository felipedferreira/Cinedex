using Cinedex.Persistence.Abstractions.Exceptions;
using Cinedex.Persistence.Abstractions.Transactions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cinedex.Persistence.EntityFrameworkCore.Internal;

/// <summary>
/// <see cref="ITransaction"/> over an <see cref="IDbContextTransaction"/>.
/// </summary>
internal sealed class EfTransaction : ITransaction
{
    private readonly IDbContextTransaction _transaction;

    private readonly IPersistenceExceptionTranslator _exceptionTranslator;

    private readonly Action _onDisposed;

    private bool _disposed;

    internal EfTransaction(
        IDbContextTransaction transaction,
        TransactionIsolationLevel isolationLevel,
        IPersistenceExceptionTranslator exceptionTranslator,
        Action onDisposed)
    {
        _transaction = transaction;
        _exceptionTranslator = exceptionTranslator;
        _onDisposed = onDisposed;
        IsolationLevel = isolationLevel;
    }

    public Guid TransactionId => _transaction.TransactionId;

    public TransactionIsolationLevel IsolationLevel { get; }

    public bool IsCompleted { get; private set; }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsCompleted)
        {
            throw new InvalidOperationException(
                "This transaction has already been committed or rolled back. Committing twice is a bug: the second "
                + "call cannot mean anything, and the writes it appears to confirm were settled by the first.");
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            // Commit is a second place failures surface, not just a formality: deferred constraints
            // fire here, and so does a serialization failure under Serializable that nothing before
            // this point had a reason to detect. Either way the transaction is over.
            IsCompleted = true;

            PersistenceException? translated = _exceptionTranslator.Translate(exception);

            if (translated is null)
            {
                throw;
            }

            throw translated;
        }

        IsCompleted = true;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Tolerated rather than an error — see the interface remarks. This is what lets callers put a
        // rollback in a catch block without first testing IsCompleted, which is the shape that gets
        // written anyway and would otherwise throw over the original exception on its way out.
        if (IsCompleted)
        {
            return;
        }

        IsCompleted = true;
        await _transaction.RollbackAsync(cancellationToken);
    }

    public async Task<ISavepoint> CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ValidateSavepointName(name);

        if (IsCompleted)
        {
            throw new InvalidOperationException(
                "Cannot create a savepoint on a transaction that has already been committed or rolled back.");
        }

        await _transaction.CreateSavepointAsync(name, cancellationToken);

        return new EfSavepoint(_transaction, name);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Disposing without committing rolls back. That is the property that makes `await using` the
        // right way to hold a transaction: every path out of the block that is not an explicit commit
        // leaves the database untouched, including the ones nobody wrote a catch for.
        try
        {
            await _transaction.DisposeAsync();
        }
        finally
        {
            IsCompleted = true;
            _onDisposed();
        }
    }

    // The name reaches the database as an identifier rather than as a parameter, because SAVEPOINT
    // takes no parameters. Entity Framework delimits it, so this is not the only thing standing
    // between a caller and an injected statement — but a rejected name with an explanation beats a
    // quoting bug in a provider we do not control, and callers get a clear error instead of a
    // syntax error from PostgreSQL.
    private static void ValidateSavepointName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // 63 bytes is PostgreSQL's identifier limit (NAMEDATALEN - 1); longer names are silently
        // truncated, which would make two distinct savepoints collide.
        if (name.Length > 63)
        {
            throw new ArgumentException(
                $"Savepoint name '{name}' is longer than the 63-character identifier limit.",
                nameof(name));
        }

        if (!char.IsLetter(name[0]) && name[0] != '_')
        {
            throw new ArgumentException(
                $"Savepoint name '{name}' must start with a letter or underscore.",
                nameof(name));
        }

        foreach (char character in name)
        {
            if (!char.IsLetterOrDigit(character) && character != '_')
            {
                throw new ArgumentException(
                    $"Savepoint name '{name}' may only contain letters, digits and underscores.",
                    nameof(name));
            }
        }
    }
}
