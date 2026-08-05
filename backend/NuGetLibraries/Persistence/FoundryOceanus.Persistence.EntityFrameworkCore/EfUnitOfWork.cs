using System.Data;
using FoundryOceanus.Persistence.Abstractions;
using FoundryOceanus.Persistence.Abstractions.Exceptions;
using FoundryOceanus.Persistence.Abstractions.Transactions;
using FoundryOceanus.Persistence.EntityFrameworkCore.DependencyInjection;
using FoundryOceanus.Persistence.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace FoundryOceanus.Persistence.EntityFrameworkCore;

/// <summary>
/// <see cref="IUnitOfWork"/> over an Entity Framework <see cref="DbContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// Scoped, and dependent on being scoped. The <typeparamref name="TContext"/> it receives is the same
/// instance the container injects into repositories resolved from the same scope, and that shared
/// instance is what makes a single <see cref="SaveChangesAsync"/> flush all of their writes and a
/// single transaction cover all of their statements.
/// </para>
/// <para>
/// Application-layer code should depend on <see cref="IUnitOfWork"/>, never on this class:
/// naming it means naming <typeparamref name="TContext"/>, which is an Entity Framework type, and the
/// dependency would point the wrong way. Adapters may name it freely — that is their job.
/// </para>
/// </remarks>
/// <typeparam name="TContext">The Entity Framework context this unit of work saves through.</typeparam>
public sealed class EfUnitOfWork<TContext> : IUnitOfWork
    where TContext : DbContext
{
    private readonly TContext _dbContext;

    private readonly IServiceProvider _serviceProvider;

    private readonly CompositePersistenceExceptionTranslator _exceptionTranslator;

    private readonly UnitOfWorkRegistration<TContext> _registration;

    private readonly HashSet<Type> _verifiedRepositoryTypes = [];

    private EfTransaction? _currentTransaction;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfUnitOfWork{TContext}"/> class.
    /// </summary>
    /// <param name="dbContext">The scoped persistence context.</param>
    /// <param name="serviceProvider">
    /// The scope's service provider, used to resolve repositories. It must be the same scope the
    /// context came from — otherwise repositories would be built against a different context, which
    /// is the failure <see cref="IDbContextBound"/> exists to catch.
    /// </param>
    /// <param name="exceptionTranslator">Turns provider failures into persistence exceptions.</param>
    /// <param name="registration">What was registered for this context at startup.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public EfUnitOfWork(
        TContext dbContext,
        IServiceProvider serviceProvider,
        CompositePersistenceExceptionTranslator exceptionTranslator,
        UnitOfWorkRegistration<TContext> registration)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(exceptionTranslator);
        ArgumentNullException.ThrowIfNull(registration);

        _dbContext = dbContext;
        _serviceProvider = serviceProvider;
        _exceptionTranslator = exceptionTranslator;
        _registration = registration;
    }

    /// <inheritdoc />
    /// <remarks>
    /// A committed or rolled-back transaction reads as no transaction, even if the caller has not
    /// disposed it yet. Callers use this to assert that they are inside one — an advisory lock is the
    /// example — and a completed transaction would satisfy that check while protecting nothing.
    /// </remarks>
    public ITransaction? CurrentTransaction => _currentTransaction is { IsCompleted: false }
        ? _currentTransaction
        : null;

    /// <inheritdoc />
    /// <remarks>
    /// Runs change detection over the tracked graph, so this is O(tracked entities) rather than a
    /// field read. Cheap for a request-sized unit of work; worth not calling in a loop.
    /// </remarks>
    public bool HasPendingChanges => _dbContext.ChangeTracker.HasChanges();

    /// <inheritdoc />
    public TRepository Repository<TRepository>()
        where TRepository : class, IRepository
    {
        TRepository? repository = _serviceProvider.GetService<TRepository>();

        if (repository is null)
        {
            throw new InvalidOperationException(BuildUnregisteredRepositoryMessage(typeof(TRepository)));
        }

        if (_registration.ValidateRepositoryContext)
        {
            VerifySharedContext(repository);
        }

        return repository;
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            // Translating here rather than in an exception filter. A filter reads better and avoids
            // unwinding for exceptions we do not classify, but .NET swallows anything a filter throws
            // — so a custom translator with a bug in it would silently classify nothing, and the
            // original exception would propagate with no sign of why. That is the failure mode this
            // library least wants to have.
            PersistenceException? translated = _exceptionTranslator.Translate(exception);

            if (translated is null)
            {
                // Unrecognised — a cancellation, a bug in generated SQL. Propagates untouched rather
                // than being relabelled as a persistence failure it is not. `throw;` keeps the
                // original stack trace.
                throw;
            }

            throw translated;
        }
    }

    /// <inheritdoc />
    public void DiscardChanges() => _dbContext.ChangeTracker.Clear();

    /// <inheritdoc />
    public async Task<ITransaction> BeginTransactionAsync(
        TransactionIsolationLevel isolationLevel = TransactionIsolationLevel.Default,
        CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is { IsCompleted: false })
        {
            throw new InvalidOperationException(
                "A transaction is already open on this unit of work. Databases do not nest transactions, so a second "
                + "one here could only be a pretence — an inner 'rollback' that left the outer transaction's writes in "
                + "place. Use ITransaction.CreateSavepointAsync for partial rollback, or restructure so the transaction "
                + "is opened once at the boundary that owns it.");
        }

        // A committed transaction the caller never disposed is not a reason to refuse a new one — but
        // the provider object behind it still holds resources, so clean it up rather than leaking it
        // until finalisation. Disposal clears _currentTransaction through the callback below.
        if (_currentTransaction is not null)
        {
            await _currentTransaction.DisposeAsync();
        }

        // Reading through Database.CurrentTransaction as well catches a transaction opened directly on
        // the context, behind this unit of work's back. Rare, but if it happens the transaction we
        // returned would not be the one actually in force.
        if (_dbContext.Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException(
                $"A transaction is already open on the underlying {typeof(TContext).Name}, but it was not opened through "
                + "this unit of work. Open transactions on one or the other, not both — otherwise commit and rollback "
                + "are being driven from two places that cannot see each other.");
        }

        IDbContextTransaction transaction = isolationLevel == TransactionIsolationLevel.Default
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : await _dbContext.Database.BeginTransactionAsync(ToAdoIsolationLevel(isolationLevel), cancellationToken);

        _currentTransaction = new EfTransaction(
            transaction,
            isolationLevel,
            _exceptionTranslator,
            onDisposed: () => _currentTransaction = null);

        return _currentTransaction;
    }

    private static IsolationLevel ToAdoIsolationLevel(TransactionIsolationLevel isolationLevel) =>
        isolationLevel switch
        {
            TransactionIsolationLevel.ReadCommitted => IsolationLevel.ReadCommitted,
            TransactionIsolationLevel.RepeatableRead => IsolationLevel.RepeatableRead,
            TransactionIsolationLevel.Serializable => IsolationLevel.Serializable,
            _ => throw new ArgumentOutOfRangeException(
                nameof(isolationLevel),
                isolationLevel,
                "Unsupported isolation level."),
        };

    private string BuildUnregisteredRepositoryMessage(Type repositoryType)
    {
        string registered = _registration.RepositoryTypes.Count == 0
            ? "no repositories are registered for it"
            : $"the repositories registered for it are: {string.Join(", ", _registration.RepositoryTypes.Select(type => type.Name).Order())}";

        return $"'{repositoryType.Name}' is not registered with the unit of work for {typeof(TContext).Name} — {registered}. "
            + $"Register it at startup with .AddRepository<{repositoryType.Name}, YourImplementation>() inside the "
            + $"AddUnitOfWork<{typeof(TContext).Name}>(...) call. If it is registered against a different persistence "
            + "context, resolve it from that context's unit of work instead: a repository can only take part in "
            + "transactions belonging to the context it was built against.";
    }

    private void VerifySharedContext<TRepository>(TRepository repository)
        where TRepository : class, IRepository
    {
        // Once per repository type per scope. The container caches scoped instances, so a type that
        // checked out the first time cannot check out differently on a later call within the scope.
        if (!_verifiedRepositoryTypes.Add(typeof(TRepository)))
        {
            return;
        }

        // Repositories that do not implement IDbContextBound cannot be inspected, so they are trusted
        // rather than rejected. Rejecting them would make the base class mandatory, which would rule
        // out repositories built on anything but this library's own conventions.
        if (repository is not IDbContextBound bound)
        {
            return;
        }

        if (!ReferenceEquals(bound.DbContext, _dbContext))
        {
            throw new InvalidOperationException(
                $"'{repository.GetType().Name}' is bound to a different {typeof(TContext).Name} instance than this unit of "
                + "work. Its writes would not be flushed by SaveChangesAsync and would not enlist in transactions opened "
                + "here — they would commit on their own connection, and a rollback would leave them in place. The usual "
                + $"cause is a repository resolving its own context (an IDbContextFactory<{typeof(TContext).Name}>, or a "
                + "manually constructed context) instead of taking the scoped one as a constructor dependency. The other "
                + "is resolving the repository from a different dependency-injection scope than the unit of work.");
        }
    }
}
