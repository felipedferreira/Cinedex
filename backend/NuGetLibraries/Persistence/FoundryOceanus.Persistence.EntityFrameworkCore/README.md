# FoundryOceanus.Persistence.EntityFrameworkCore

Entity Framework Core implementation of the ports in
[`FoundryOceanus.Persistence.Abstractions`](../FoundryOceanus.Persistence.Abstractions/README.md): unit of work,
real database transactions, savepoints, and repository resolution that guarantees a shared context.

Reference this from your **composition root and adapters**. Application and domain projects reference
only the abstractions package — that separation is the point, and it is the one thing a code review
should check.

On PostgreSQL, use
[`FoundryOceanus.Persistence.EntityFrameworkCore.Postgres`](../FoundryOceanus.Persistence.EntityFrameworkCore.Postgres/README.md)
instead of registering this directly. It adds SQLSTATE translation and advisory locks on top.

---

## Setup

```csharp
// 1. Register the context as usual. Scoped — which AddDbContext does by default.
services.AddDbContext<AuthDbContext>(options => options
    .UseNpgsql(connectionString)
    .UseCamelCaseNamingConvention());

// 2. Register the unit of work and the repositories it can hand out.
services.AddUnitOfWork<AuthDbContext>(uow => uow
    .AddRepository<IRefreshTokenRepository, RefreshTokenRepository>()
    .AddRepository<IUserRepository, UserRepository>());
```

That registers:

| Service | Lifetime | Notes |
| --- | --- | --- |
| `EfUnitOfWork<TContext>` | Scoped | Concrete, always unambiguous. Adapter-layer use only. |
| `IUnitOfWork` | Scoped | Keyed on `typeof(TContext)`, plus unkeyed for the first context registered. |
| `IUnitOfWorkScopeFactory` | Scoped | Same keying. |
| Your repositories | Scoped | Via `TryAddScoped`, so an existing registration wins. |
| `CompositePersistenceExceptionTranslator` | Singleton | |
| `EntityFrameworkExceptionTranslator` | Singleton | Appended to the translator chain. |

Registering repositories here does not make `Repository<T>()` the only way to reach them —
constructor injection keeps working exactly as before.

### Call order

Call `AddUnitOfWork` **after** `AddDbContext` where you can. If the context is already registered,
its lifetime is checked and a wrong one throws immediately with an explanation. If it is registered
afterwards the check cannot run, and a misconfiguration surfaces later and less clearly. The reverse
order is not an error, just less helpful.

### Calling it twice

Additive. Two modules can each contribute their own repositories for one context and both end up in
the same registration.

---

## Multiple contexts

The first context registered claims the unkeyed `IUnitOfWork`. A second is registered under a key
only — adding one must not silently repoint everything that already asks for `IUnitOfWork`.

```csharp
services.AddNpgsqlUnitOfWork<FilmDbContext>(uow => uow
    .AddRepository<ITitleRepository, TitleRepository>());          // becomes the default

services.AddNpgsqlUnitOfWork<AuthDbContext>(uow => uow
    .WithKey(PersistenceKeys.Auth)
    .AddRepository<IRefreshTokenRepository, RefreshTokenRepository>());
```

Consumers of the second ask for it by key:

```csharp
public sealed class LogoutHandler(
    [FromKeyedServices(PersistenceKeys.Auth)] IUnitOfWork unitOfWork);
```

Choose your own key — a constant in the application layer, not `typeof(AuthDbContext)` — so consuming
code never has to name an Entity Framework type:

```csharp
public static class PersistenceKeys
{
    public const string Auth = "auth";
}
```

`AsDefault()` moves the unkeyed registration to a context of your choosing. Calling it from two
contexts throws at startup rather than letting registration order decide, because a default unit of
work that depends on ordering is the kind of thing that works in tests and writes to the wrong
database in production. Two contexts asking for the same `WithKey(...)` value throws for the same
reason — the second registration would otherwise be dropped and every consumer of that key would
resolve the first context.

Re-stating a claim your own context already holds is not a conflict: a context configured across two
modules can take its key or the default in either call.

---

## Writing repositories

Two options, both valid.

### Plain class

Take the context as a constructor dependency. Nothing else is required:

```csharp
internal sealed class GenreRepository(FilmDbContext dbContext) : IGenreRepository
{
    public async Task<Genre?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Genres.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
}
```

### Deriving from `EfRepository<TContext>`

Adds two things worth having:

1. It implements `IDbContextBound`, so the unit of work can **verify** the shared-context invariant
   rather than trust it.
2. It offers `ExecuteAsync`, which applies exception translation to statements that never pass
   through `SaveChanges`.

```csharp
internal sealed class RefreshTokenRepository(
    AuthDbContext dbContext,
    CompositePersistenceExceptionTranslator translator)
    : EfRepository<AuthDbContext>(dbContext, translator), IRefreshTokenRepository
{
    public Task<RefreshToken?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        ExecuteAsync(
            token => DbContext.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(entity => entity.TokenHash == tokenHash, token),
            cancellationToken);

    // Set-based: compiles to one UPDATE … WHERE and never reaches SaveChanges, which is why it needs
    // the ExecuteAsync wrapper to have its failures translated.
    public Task<int> RotateAsync(
        string tokenHash,
        DateTime rotatedAtUtc,
        string replacementTokenHash,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            token => DbContext.RefreshTokens
                .Where(entity => entity.TokenHash == tokenHash && entity.RevokedAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(entity => entity.RevokedAtUtc, rotatedAtUtc)
                        .SetProperty(entity => entity.ReplacedByTokenHash, replacementTokenHash),
                    token),
            cancellationToken);
}
```

**Do not call `SaveChangesAsync` inside a repository.** Deciding when to write is the unit of work's
job; a repository that saves on every call makes a multi-step transaction impossible to express.

---

## The invariant that matters

**A unit of work and every repository it hands out share one `DbContext` instance.** That is what
makes a single `SaveChangesAsync` flush all their writes, and what makes their statements enlist in a
transaction opened on the unit of work.

It holds automatically when a repository takes the context as a constructor dependency and the
container resolves both from the same scope. It breaks — silently, and usually only under load — when
a repository gets a context some other way:

```csharp
// Broken. The factory hands out a fresh context on a second connection, so this repository's writes
// commit on their own regardless of what the surrounding transaction decides. Nothing throws.
// The rollback simply does not cover them.
internal sealed class BrokenRepository(IDbContextFactory<AuthDbContext> factory) : IRefreshTokenRepository
{
    private readonly AuthDbContext _dbContext = factory.CreateDbContext();
}
```

Repositories deriving from `EfRepository<TContext>` are checked for this at resolution time, and a
mismatch throws with an explanation instead of corrupting data quietly. Repositories that do not
implement `IDbContextBound` cannot be inspected, so they are trusted rather than rejected —
requiring the base class would rule out repositories built on other conventions.

The check costs one reference comparison per repository type per scope.
`DisableRepositoryContextValidation()` turns it off; performance is not a reason to.

Lifetime is guarded too: a `TContext` registered as `Singleton` or `Transient` throws at registration.
Singleton would share one context across concurrent requests — Entity Framework contexts are not
thread-safe — and transient would give every repository a separate context on a separate connection.

---

## Exception translation

Translators are consulted in order, first non-null wins.

```
NpgsqlExceptionTranslator        (from the .Postgres package — reads SQLSTATE)
EntityFrameworkExceptionTranslator (this package — DbUpdateConcurrencyException, then a catch-all)
```

Order is load-bearing. The Entity Framework translator's last rule turns any remaining
`DbUpdateException` into an `UnclassifiedPersistenceException`, so if it ran first no provider
translator would ever see a SQLSTATE — no `DuplicateKeyException` to answer with a 409, and
serialization failures no longer marked transient, so `ExecuteInTransactionAsync` would stop retrying
without saying so.

That is too quiet a failure to leave to registration order, so it is not left to it:
`CompositePersistenceExceptionTranslator` sorts the catch-all last whatever order the registrations
happened in. Provider and hand-written translators keep their order relative to each other, so a
custom translator registered by hand still runs before the catch-all and after Npgsql's.

That catch-all rule exists on purpose: letting an unrecognised `DbUpdateException` through would mean
an Entity Framework type reaching the application layer on precisely the paths nobody anticipated,
which is where a leaky abstraction does its damage.

### Adding your own

```csharp
public sealed class TenantQuotaTranslator : IPersistenceExceptionTranslator
{
    public PersistenceException? Translate(Exception exception) =>
        exception is PostgresException { SqlState: "P0001", MessageText: "quota_exceeded" }
            ? new QuotaExceededException("Tenant storage quota exceeded.", exception)
            : null;   // null means "not mine" — the next translator gets a turn
}

// Register before AddUnitOfWork so it is consulted first.
services.TryAddEnumerable(
    ServiceDescriptor.Singleton<IPersistenceExceptionTranslator, TenantQuotaTranslator>());
```

Return `null` for anything you do not recognise, and always for `OperationCanceledException` —
cancellation is the caller's token doing its job, and translating it turns a clean shutdown into a
logged error.

---

## Where translation applies

| Path | Translated? |
| --- | --- |
| `IUnitOfWork.SaveChangesAsync` | Yes |
| `ITransaction.CommitAsync` | Yes — deferred constraints and serialization failures surface here |
| `EfRepository.ExecuteAsync(...)` | Yes |
| A repository calling `ToListAsync` directly, without the wrapper | **No** |

Entity Framework provides no hook for replacing an exception thrown by a query, so the wrapper is how
direct statements opt in. Reads that cannot fail in an interesting way are fine without it.

---

## Testing

Test against a real database. The in-memory provider has no transactions, no savepoints, no isolation
levels and no SQLSTATEs, so a suite that passes against it is asserting that the fake works.
[Testcontainers](https://testcontainers.com/) makes this cheap; this repository's own
`FoundryOceanus.Persistence.Tests` uses `postgres:17-alpine` and is worth copying.

For unit-testing a handler, `IUnitOfWork` is a six-member interface — implementing a recording fake
by hand takes a few minutes and needs no mocking framework. There is one in the test project.

---

## Related packages

- [`FoundryOceanus.Persistence.Abstractions`](../FoundryOceanus.Persistence.Abstractions/README.md) — the ports, and
  the reasoning behind them.
- [`FoundryOceanus.Persistence.EntityFrameworkCore.Postgres`](../FoundryOceanus.Persistence.EntityFrameworkCore.Postgres/README.md) —
  PostgreSQL specifics.
