# Cinedex.Persistence.EntityFrameworkCore.Postgres

PostgreSQL support for the Cinedex unit of work: SQLSTATE-aware exception translation and
transaction-scoped advisory locks.

Depends on `Npgsql`, **not** on `Npgsql.EntityFrameworkCore.PostgreSQL`. This package reads
`PostgresException.SqlState` and issues advisory-lock statements over an ADO command; it never
configures a `DbContext`, so it has no business pinning which version of the EF provider you use.
Bring your own provider package.

---

## Setup

Use `AddNpgsqlUnitOfWork` in place of `AddUnitOfWork`:

```csharp
services.AddDbContext<AuthDbContext>(options => options
    .UseNpgsql(connectionString)
    .UseCamelCaseNamingConvention());

services.AddNpgsqlUnitOfWork<AuthDbContext>(uow => uow
    .AddRepository<IRefreshTokenRepository, RefreshTokenRepository>());
```

Everything `AddUnitOfWork` does, plus `NpgsqlExceptionTranslator` at the **front** of the translator
chain. That position is load-bearing: the Entity Framework translator's catch-all claims any
remaining `DbUpdateException`, so a provider translator registered after it would never see a
SQLSTATE.

Without this package on PostgreSQL, two things break quietly:

- A unique-constraint violation arrives as `UnclassifiedPersistenceException` instead of
  `DuplicateKeyException`, so the handler that turns it into a 409 never fires.
- A serialization failure is not recognised as transient, so `ExecuteInTransactionAsync`'s retries
  never fire — silently, exactly when they matter.

---

## Exception translation

| SQLSTATE | Condition | Becomes | Transient |
| --- | --- | --- | --- |
| `23505` | `unique_violation` | `DuplicateKeyException` | no |
| `23P01` | `exclusion_violation` | `DuplicateKeyException` | no |
| `23503` | `foreign_key_violation` | `ReferentialIntegrityException` | no |
| `23502` | `not_null_violation` | `UnclassifiedPersistenceException` | no |
| `23514` | `check_violation` | `UnclassifiedPersistenceException` | no |
| `40001` | `serialization_failure` | `TransientPersistenceException` | **yes** |
| `40P01` | `deadlock_detected` | `TransientPersistenceException` | **yes** |
| `40003` | `statement_completion_unknown` | `TransientPersistenceException` | **yes** |
| `55P03` | `lock_not_available` | `TransientPersistenceException` | **yes** |
| `57014` | `query_canceled` | `UnclassifiedPersistenceException` | no |
| anything else | | `UnclassifiedPersistenceException` | no |

`DuplicateKeyException` and `ReferentialIntegrityException` carry `ConstraintName`, which is how a
handler distinguishes two unique constraints on one table without parsing a message. Matching on it
couples your code to a name defined in a migration — treat that name as part of your schema's
contract if you do.

A few of these deserve their reasoning stated:

**`23P01` maps to `DuplicateKeyException`.** An exclusion constraint says "no two rows may relate this
way" — overlapping bookings for one room. Structurally that is uniqueness with a richer predicate, and
a fourth constraint exception handled identically to the first would be surface without meaning.

**`57014` is not marked transient.** It is nearly always `statement_timeout` firing on a query that is
too slow, and retrying a too-slow query is how a slow endpoint becomes a busy one. The fix is an index
or a narrower predicate.

**`40003` is transient but the message says to be careful.** The connection dropped while the
transaction's outcome was unknown, so the previous attempt may have committed. Retrying is only safe
if the operation is idempotent.

**Unmapped codes are still claimed.** They become `UnclassifiedPersistenceException` rather than
passing through, because passing through means an `NpgsqlException` arriving in application code
written not to know PostgreSQL exists. The original is preserved as `InnerException`.

**Connection failures with no SQLSTATE** are classified by deferring to Npgsql's own
`NpgsqlException.IsTransient`, rather than maintaining a second opinion about which ones are worth
retrying.

The SQLSTATE constants come from Npgsql's `PostgresErrorCodes`, which already carries all 238 of them.
Re-declaring a handful would have been a second list to keep in step with PostgreSQL for no benefit —
the same reasoning that decides what else does and does not belong in these packages.

---

## Advisory locks

An application-level mutex held in the database. The problem they solve is serialising work that has
no row to lock yet: "only one request may rotate this token family at a time" cannot be expressed as
`SELECT … FOR UPDATE` when the deciding read might return nothing.

```csharp
internal sealed class RefreshTokenRepository(
    AuthDbContext dbContext,
    CompositePersistenceExceptionTranslator translator)
    : EfRepository<AuthDbContext>(dbContext, translator), IRefreshTokenRepository
{
    public async Task<int> RotateAsync(Guid familyId, string tokenHash, CancellationToken cancellationToken)
    {
        // Callers queue on the family rather than interleaving. Released when the caller's
        // transaction ends — there is no release call to forget on an exception path.
        await DbContext.AcquireTransactionLockAsync($"refresh-token-family:{familyId}", cancellationToken);

        // ... the re-read and conditional update that the lock makes correct
    }
}
```

### Four rules

**1. They hang off `DbContext`, not `IUnitOfWork`, and that placement is the design.** An advisory
lock is a database mechanism, not a business concept, so it belongs in the adapter that already knows
it is talking to PostgreSQL. If application-layer code wants one, the operation it is protecting has
leaked out of the adapter — express it as a repository method named for what the domain is doing
(`RotateAsync`) and take the lock inside. Needing a `DbContext` to call these is what keeps that
boundary honest.

**2. Every lock is transaction-scoped.** `pg_advisory_xact_lock`, never `pg_advisory_lock`.
Session-scoped locks are deliberately not offered: with connection pooling the session outlives the
request, so a session lock that is not explicitly released — because an exception skipped the release
— stays held on a pooled connection and is eventually handed to an unrelated request. Transaction
locks cannot leak that way. The commit or rollback releases them, and there is always one.

**3. Taking one outside a transaction throws.** It would be released by the statement that took it, so
the call would appear to succeed while protecting nothing.

**4. Keys share one namespace per database.** PostgreSQL keys advisory locks on a single 64-bit space
covering the whole database, so two unrelated features whose keys collide block each other. Prefix
your keys with what they protect — `$"refresh-token-family:{familyId}"`, not `familyId.ToString()`.

### Blocking or not

| Method | Behaviour |
| --- | --- |
| `AcquireTransactionLockAsync(string \| long)` | Waits until the lock is free. Callers queue. |
| `TryAcquireTransactionLockAsync(string \| long)` | Returns `false` immediately if held elsewhere. |

The `Try` variants suit work better skipped than queued — a periodic sweep only one instance needs to
run, where a second instance finding it taken should move on rather than wait to repeat work already
happening.

### Why hashing happens in PostgreSQL

Text keys are hashed with `hashtextextended(@key, 0)` **in the database**, not in .NET. This is not an
optimisation and it is not optional.

`string.GetHashCode()` is randomised per process on .NET Core. Hashing in .NET would give the same key
a different value in every instance of your service, so two instances would take two different locks
and neither would ever wait for the other. The bug is invisible on one machine and appears only once
you scale out — the worst possible failure mode for a mutual-exclusion primitive. PostgreSQL's hash is
stable across processes, versions and machines.

Two distinct keys can still hash to the same 64-bit value. That only makes unrelated callers wait for
each other; it cannot make a lock fail to exclude, because everyone using a given key hashes it the
same way.

---

## Isolation levels

`TransactionIsolationLevel` offers exactly what PostgreSQL implements — `ReadCommitted`,
`RepeatableRead`, `Serializable`, and `Default` — and deliberately omits `ReadUncommitted`, which
PostgreSQL silently treats as `ReadCommitted`.

At `RepeatableRead` and `Serializable`, PostgreSQL aborts transactions under contention rather than
blocking. Pair those levels with retries:

```csharp
await unitOfWork.ExecuteInTransactionAsync(
    async token => { /* re-read, then write */ },
    TransactionIsolationLevel.Serializable,
    maxRetries: 3,
    cancellationToken);
```

Choosing `Serializable` without a retry policy converts a concurrency-control mechanism into an
intermittent 500.

---

## Related packages

- [`Cinedex.Persistence.Abstractions`](../Cinedex.Persistence.Abstractions/README.md) — the ports.
- [`Cinedex.Persistence.EntityFrameworkCore`](../Cinedex.Persistence.EntityFrameworkCore/README.md) —
  registration, repository base class, translation pipeline.
