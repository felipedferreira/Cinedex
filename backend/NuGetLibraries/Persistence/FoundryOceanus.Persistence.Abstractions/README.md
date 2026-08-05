# FoundryOceanus.Persistence.Abstractions

Storage-agnostic ports for persistence: a unit of work, transactions, savepoints, and a repository
marker. No Entity Framework, no Npgsql, **no package references at all**.

This is the package your application and domain projects reference. The implementation lives in
[`FoundryOceanus.Persistence.EntityFrameworkCore`](../FoundryOceanus.Persistence.EntityFrameworkCore/README.md),
which your composition root references and your business logic never does.

---

## Why this exists

The usual justification for hiding an ORM is "so we can swap Entity Framework for Dapper". That is
the weakest argument available, and it is false besides — almost nobody swaps ORMs, and a team that
did would find their abstraction fit the old ORM's shape too closely to help. If that were the only
reason, this package would not be worth its ceremony.

The reasons that hold up:

**Dependency direction.** If your domain references `DbContext`, business logic depends on
infrastructure. Defining the port in the core and implementing it outward flips the arrow. That is
why this package has zero dependencies: it is the one property that cannot be preserved by
convention, only by there being nothing to violate.

**Semantic narrowing.** `DbContext` exposes around forty public members, including
`Database.ExecuteSqlRaw`, `ChangeTracker`, and every `DbSet<T>` as a fully composable `IQueryable`.
`IUnitOfWork` exposes six members and no query surface. Nobody *can* compose an ad-hoc query in a
command handler, because the type does not permit it. That is a constraint, not a wrapper.

**Policy in one place.** Retry classification, isolation-level defaults, and error translation live
behind the port. Without it, every call site decides for itself whether SQLSTATE 40001 is worth
retrying, and half of them decide wrong.

**Blast radius.** Not "swap the ORM" but "EF 10 changed this behaviour" — one file changes instead of
two hundred.

### And the cost

Abstractions that mirror the vendor API one-to-one are pure tax. Leaky ones are worse than none: you
pay the ceremony *and* keep the coupling. The test is whether the interface speaks your domain's
vocabulary or the ORM's.

This package tries to stay on the right side of that test, and where it does not, it says so:
`SaveChangesAsync` keeps Entity Framework's name deliberately, because the alternative — calling it
`CommitAsync` next to `ITransaction.CommitAsync` — would be two verbs meaning different things under
one name. Where the vendor already does something well, this library uses it rather than re-wrapping
it: PostgreSQL SQLSTATE constants come from Npgsql's own `PostgresErrorCodes`, which already has all
238 of them.

---

## What is in the box

| Type | Purpose |
| --- | --- |
| `IUnitOfWork` | Resolve repositories, save, and open transactions. Six members. |
| `ITransaction` | A real database transaction. Commit, roll back, create savepoints. |
| `ISavepoint` | A point inside a transaction to roll back to without abandoning it. |
| `TransactionIsolationLevel` | The four levels PostgreSQL actually implements. |
| `IRepository` | Marker constraining what `Repository<T>()` will hand out. |
| `IUnitOfWorkScopeFactory` | Explicit scopes for background services. |
| `UnitOfWorkExtensions` | `ExecuteInTransactionAsync`, with optional retry. |
| `PersistenceException` and subclasses | Provider failures, classified. |

---

## The shape of a repository

Repositories are yours. This package deliberately does **not** ship an `IRepository<TEntity>` with
`GetAll`/`Add`/`Update`/`Delete`, because a generic CRUD repository is the ORM's vocabulary wearing a
different name — callers end up composing queries against it, and persistence knowledge is back in
the application layer with an extra layer of indirection paid for.

Write one interface per aggregate, named for the domain:

```csharp
public interface IRefreshTokenRepository : IRepository
{
    Task<RefreshToken?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<int> RotateAsync(
        string tokenHash,
        DateTime rotatedAtUtc,
        string replacementTokenHash,
        CancellationToken cancellationToken);

    Task<int> RevokeActiveFamilyAsync(Guid familyId, DateTime revokedAtUtc, CancellationToken cancellationToken);
}
```

Read those method names out loud. If they sound like things your product does, it is a repository. If
they sound like things a database does, it is a thin wrapper and it is costing more than it returns.

---

## Usage

### Resolving repositories

```csharp
public sealed class RotateRefreshTokenHandler(IUnitOfWork unitOfWork)
{
    public async Task<string> HandleAsync(string presentedHash, CancellationToken cancellationToken)
    {
        var tokens = unitOfWork.Repository<IRefreshTokenRepository>();

        RefreshToken? existing = await tokens.FindByTokenHashAsync(presentedHash, cancellationToken);
        // ...
    }
}
```

Constructor injection works too, and is better when a class always uses one repository. Resolving
through the unit of work is better when a class coordinates several inside one transaction, where
listing them as constructor parameters would hide that they are related.

### Transactions

```csharp
await using ITransaction transaction = await unitOfWork.BeginTransactionAsync(
    cancellationToken: cancellationToken);

await unitOfWork.Repository<ITitleRepository>().CreateAsync(title, cancellationToken);
await unitOfWork.Repository<IAuditRepository>().RecordAsync(entry, cancellationToken);

await unitOfWork.SaveChangesAsync(cancellationToken);
await transaction.CommitAsync(cancellationToken);
```

**Disposing an uncommitted transaction rolls it back.** That is what makes `await using` the correct
way to hold one: every path out of the block that is not an explicit commit leaves the database
untouched, including the ones nobody wrote a `catch` for.

Or let the extension method handle the ceremony:

```csharp
await unitOfWork.ExecuteInTransactionAsync(
    async token =>
    {
        await unitOfWork.Repository<ITitleRepository>().CreateAsync(title, token);
        await unitOfWork.Repository<IAuditRepository>().RecordAsync(entry, token);
    },
    cancellationToken: cancellationToken);
```

### Save is not commit

`SaveChangesAsync` writes pending changes. `ITransaction.CommitAsync` ends the transaction.

- Inside a transaction, saving writes statements that stay invisible to other sessions — and
  revocable — until commit.
- Outside a transaction, the provider wraps the save in its own single-use transaction. The batch is
  still all-or-nothing; it is simply committed for you.

So a single-statement write needs no explicit transaction. Reach for one when two or more writes must
succeed or fail together.

### Isolation levels and retries

`TransactionIsolationLevel` offers four values, not `System.Data.IsolationLevel`'s seven. The missing
three are fictions on PostgreSQL: `ReadUncommitted` is silently treated as `ReadCommitted`,
`Snapshot` does not exist under that name, and `Chaos` means nothing at all. Offering values the
database will quietly reinterpret is how an abstraction starts lying to the people reading it.

Above `ReadCommitted`, PostgreSQL aborts transactions under contention rather than blocking. That is
normal, not a fault — and it means those levels require a retry:

```csharp
await unitOfWork.ExecuteInTransactionAsync(
    async token =>
    {
        // Re-read inside the delegate: a retry must not reuse state loaded before the call.
        int balance = await unitOfWork.Repository<IAccountRepository>().GetBalanceAsync(id, token);
        await unitOfWork.Repository<IAccountRepository>().DebitAsync(id, amount, token);
    },
    TransactionIsolationLevel.Serializable,
    maxRetries: 3,
    cancellationToken);
```

**Retries require an operation that is safe to run more than once.** It must re-read the state it
depends on rather than closing over values loaded before the call, and it must have no side effects
outside the database — no email sent, no message published, no counter incremented in memory. Between
attempts, `DiscardChanges()` detaches everything the failed attempt was tracking, so an operation
handed already-loaded entities would find nothing tracking them on the second pass.

### Handling failures

Provider exceptions are translated, so the application layer never catches an Npgsql type or compares
a SQLSTATE string:

```csharp
try
{
    await unitOfWork.SaveChangesAsync(cancellationToken);
}
catch (DuplicateKeyException exception) when (exception.ConstraintName == "ix_users_email")
{
    throw new EmailAlreadyRegisteredException();
}
```

| Exception | Means | `IsTransient` |
| --- | --- | --- |
| `DuplicateKeyException` | Unique constraint or index violated | `false` |
| `ReferentialIntegrityException` | Foreign key violated in either direction | `false` |
| `ConcurrencyConflictException` | Row changed or deleted by someone else first | `false` |
| `TransientPersistenceException` | Serialization failure, deadlock, dropped connection | `true` |
| `UnclassifiedPersistenceException` | A write failed for a reason no translator named | `false` |

`UnclassifiedPersistenceException` exists so `catch (PersistenceException)` is a complete backstop.
Without it, an unrecognised failure would arrive as whatever the ORM threw — the exact coupling this
package prevents, appearing only on the paths nobody tested.

Catching `DuplicateKeyException` is the *correct* implementation of a uniqueness rule, not a fallback
for a forgotten check. Checking "does this email exist?" before inserting is a race; the constraint is
the only real guarantee.

### Background services

A background service is a singleton and cannot take a scoped `IUnitOfWork` as a constructor
dependency. Injecting `IServiceScopeFactory` works but puts the container back in front of code that
had stopped knowing about it. Use `IUnitOfWorkScopeFactory` instead:

```csharp
public sealed class RefreshTokenCleanupWorker(IUnitOfWorkScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using IUnitOfWorkScope scope = scopeFactory.CreateScope();

            await scope.UnitOfWork
                .Repository<IRefreshTokenRepository>()
                .DeleteExpiredBatchAsync(DateTime.UtcNow, batchSize: 500, stoppingToken);

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
```

One scope per iteration, disposed each time. Holding a single context for the process's lifetime
leaves the change tracker accumulating entities until it is the slowest thing in the service.

---

## Rules worth knowing

1. **Scoped, always.** A unit of work and its repositories share one persistence context per scope.
   That is what makes one `SaveChangesAsync` flush all their writes and one transaction cover all
   their statements.
2. **Transactions do not nest.** Databases have no nested transactions, and pretending otherwise
   means an inner "rollback" that silently does nothing. Use savepoints for partial rollback.
3. **Rolling back to a savepoint does not detach entities.** It undoes the database's view of the
   work; your object graph is unchanged. Follow it with `DiscardChanges()` when the discarded work
   touched tracked entities.
4. **Do not put raw SQL behind this port.** Code that needs it belongs in an adapter, where it can
   depend on the provider honestly rather than tunnelling through a port that claims not to know what
   a database is.

---

## Related packages

- [`FoundryOceanus.Persistence.EntityFrameworkCore`](../FoundryOceanus.Persistence.EntityFrameworkCore/README.md) —
  the implementation, plus registration and repository base class.
- [`FoundryOceanus.Persistence.EntityFrameworkCore.Postgres`](../FoundryOceanus.Persistence.EntityFrameworkCore.Postgres/README.md) —
  SQLSTATE translation and advisory locks.
