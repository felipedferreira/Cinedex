using FoundryOceanus.Persistence.Abstractions.Transactions;

namespace FoundryOceanus.Persistence.Abstractions;

/// <summary>
/// One business transaction's worth of persistence: the repositories it touches, the point at which
/// its changes are written, and the database transaction they are written inside.
/// </summary>
/// <remarks>
/// <para>
/// This interface exists to be small. Entity Framework's <c>DbContext</c> exposes roughly forty
/// public members — <c>Database.ExecuteSqlRaw</c>, <c>ChangeTracker</c>, <c>Entry</c>,
/// <c>Model</c>, and every <c>DbSet</c> you have declared, each of them a fully composable
/// <c>IQueryable</c>. Any application-layer type holding one can do all of that, and over a long
/// enough timeline some of them will. Five members cannot. That narrowing is the point of this type;
/// it is not a portability layer, and swapping ORMs is not the reason it is here.
/// </para>
/// <para>
/// <b>Lifetime.</b> Scoped — one per request, or one per iteration of a background worker. The unit
/// of work and every repository it hands out share a single underlying <c>DbContext</c>, which is
/// what makes <see cref="SaveChangesAsync"/> flush all of their pending writes together and what
/// makes their statements enlist in a transaction opened here. Resolving a unit of work outside a
/// scope, or holding one across requests, breaks both guarantees.
/// </para>
/// <para>
/// <b>What is deliberately absent.</b> There is no query surface. You cannot get an
/// <c>IQueryable</c>, an entity set, or a change tracker from here, because handing the application
/// layer a composable query is handing it the ORM with extra steps. Queries live behind repository
/// methods named for what the domain is asking. There is also no raw-SQL escape hatch: code that
/// genuinely needs one belongs in an adapter, where it can depend on the provider honestly, rather
/// than tunnelling through a port that claims not to know what a database is.
/// </para>
/// <example>
/// The common case — several writes, one transaction:
/// <code>
/// await using ITransaction transaction = await unitOfWork.BeginTransactionAsync(cancellationToken: ct);
///
/// await unitOfWork.Repository&lt;ITitleRepository&gt;().CreateAsync(title, ct);
/// await unitOfWork.Repository&lt;IAuditRepository&gt;().RecordAsync(entry, ct);
///
/// await unitOfWork.SaveChangesAsync(ct);
/// await transaction.CommitAsync(ct);
/// </code>
/// </example>
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>
    /// Gets the transaction currently open on this unit of work, or <see langword="null"/> if there
    /// is none.
    /// </summary>
    /// <remarks>
    /// Mostly useful for asserting an invariant. Some operations are only correct inside a
    /// transaction — a PostgreSQL transaction-scoped advisory lock is meaningless outside one,
    /// because it is released the moment the implicit single-statement transaction ends — and code
    /// implementing them can check here rather than trusting its callers.
    /// </remarks>
    ITransaction? CurrentTransaction { get; }

    /// <summary>
    /// Gets a value indicating whether this unit of work is holding changes that have not been saved.
    /// </summary>
    /// <remarks>
    /// Reflects tracked entity changes only. Set-based statements a repository issues directly — the
    /// kind that compile to a single <c>UPDATE … WHERE</c> — take effect immediately and are never
    /// pending, so this can be <see langword="false"/> while the current transaction still has
    /// uncommitted work in it.
    /// </remarks>
    bool HasPendingChanges { get; }

    /// <summary>
    /// Resolves a repository that shares this unit of work's persistence context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This resolves from the same dependency-injection scope the unit of work was created in, so a
    /// repository obtained here is the same instance the container would inject into a constructor.
    /// Both are valid. Constructor injection is better when a class always uses a repository;
    /// resolving here is better when a class coordinates several repositories inside one transaction
    /// and listing them all as constructor parameters would obscure that they are related.
    /// </para>
    /// <para>
    /// The repository must have been registered with the unit of work at startup. Registration is
    /// what lets the container guarantee the shared context; resolving an arbitrary type that happens
    /// to implement <see cref="IRepository"/> would not.
    /// </para>
    /// </remarks>
    /// <typeparam name="TRepository">
    /// The repository interface to resolve — your own domain-named interface, not a generic
    /// <c>IRepository&lt;TEntity&gt;</c>. See <see cref="IRepository"/> for why that distinction is
    /// enforced rather than suggested.
    /// </typeparam>
    /// <returns>The repository instance for this scope.</returns>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="TRepository"/> was not registered with this unit of work, or it was
    /// registered but resolved against a different persistence context — meaning its writes would
    /// silently fall outside this unit of work's transaction.
    /// </exception>
    TRepository Repository<TRepository>()
        where TRepository : class, IRepository;

    /// <summary>
    /// Writes every pending change to the database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is not a commit. When a transaction is open, saving writes the statements inside it and
    /// they remain invisible to other sessions, and revocable, until
    /// <see cref="ITransaction.CommitAsync"/>. When no transaction is open, the provider wraps the
    /// save in its own single-use transaction, so the batch is still all-or-nothing — it is simply
    /// committed for you.
    /// </para>
    /// <para>
    /// The name is the one piece of the underlying ORM's vocabulary kept on purpose. Calling it
    /// <c>CommitAsync</c> would read better in isolation and would be worse in practice: alongside
    /// <see cref="ITransaction.CommitAsync"/> it invites the reading that saving ends the
    /// transaction. Two verbs that mean different things should not share a name.
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The number of rows affected.</returns>
    /// <exception cref="Exceptions.ConcurrencyConflictException">
    /// A row this unit of work expected to change was changed or deleted by someone else first.
    /// </exception>
    /// <exception cref="Exceptions.PersistenceException">
    /// The database rejected the write — a unique or foreign-key constraint, or a transient failure
    /// such as a serialization conflict or deadlock.
    /// </exception>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Abandons every pending change, returning this unit of work to the state it had after its last
    /// successful save.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Affects in-memory state only. It does not roll back a transaction and it does not undo
    /// statements that already reached the database.
    /// </para>
    /// <para>
    /// The case that needs it is retrying a failed transaction. A rollback undoes the database's
    /// work but leaves this unit of work still holding the entities the failed attempt loaded and
    /// modified; saving again would replay them against a snapshot that no longer exists. Discarding
    /// first is what makes the second attempt a genuinely fresh one, and it is why
    /// <see cref="UnitOfWorkExtensions"/>.<c>ExecuteInTransactionAsync</c> calls it between retries.
    /// </para>
    /// </remarks>
    void DiscardChanges();

    /// <summary>
    /// Opens a database transaction that subsequent work on this unit of work enlists in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A real transaction on a real connection — not an ambient scope and not a distributed one. It
    /// covers writes made through this unit of work and through repositories resolved from it, and
    /// nothing else.
    /// </para>
    /// <para>
    /// The caller owns the returned transaction: commit it, or dispose it to roll back. Nesting is
    /// not supported, because the underlying database does not have nested transactions and
    /// pretending otherwise would mean an inner "rollback" that silently did nothing. Use
    /// <see cref="ITransaction.CreateSavepointAsync"/> for partial rollback within a transaction.
    /// </para>
    /// </remarks>
    /// <param name="isolationLevel">
    /// The isolation level to open at. Levels above <see cref="TransactionIsolationLevel.ReadCommitted"/>
    /// can abort under contention and require the caller to retry.
    /// </param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The open transaction.</returns>
    /// <exception cref="InvalidOperationException">
    /// A transaction is already open on this unit of work.
    /// </exception>
    Task<ITransaction> BeginTransactionAsync(
        TransactionIsolationLevel isolationLevel = TransactionIsolationLevel.Default,
        CancellationToken cancellationToken = default);
}
