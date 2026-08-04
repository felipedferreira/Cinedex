using Cinedex.Persistence.Tests.Fakes;
using FoundryOceanus.Persistence.Abstractions;
using FoundryOceanus.Persistence.Abstractions.Exceptions;
using FoundryOceanus.Persistence.Abstractions.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Cinedex.Persistence.Tests.Integration;

/// <summary>
/// Serializable isolation and the retry path, against real PostgreSQL.
/// </summary>
/// <remarks>
/// These are the tests that justify the whole transient-failure classification. A serialization
/// failure cannot be simulated convincingly — it depends on PostgreSQL's own predicate locking and
/// surfaces as SQLSTATE 40001 from either the save or the commit — so the only way to know the
/// classification and the retry loop line up is to provoke a real one.
/// </remarks>
[Collection(PostgresCollection.Name)]
public sealed class SerializationFailureTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Serializable_WithWriteSkew_SurfacesAsATransientFailure()
    {
        // The canonical dangerous structure: both transactions read the whole table, then both write
        // to it. Neither read sees the other's insert, so no serial order could have produced this
        // outcome, and PostgreSQL aborts whichever commits second.
        await fixture.ResetAsync();

        await using ServiceProvider provider = fixture.BuildServiceProvider();

        await using AsyncServiceScope firstScope = provider.CreateAsyncScope();
        await using AsyncServiceScope secondScope = provider.CreateAsyncScope();

        var first = firstScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var second = secondScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await using ITransaction firstTransaction =
            await first.BeginTransactionAsync(TransactionIsolationLevel.Serializable);
        await using ITransaction secondTransaction =
            await second.BeginTransactionAsync(TransactionIsolationLevel.Serializable);

        // Both snapshots are taken before either writes.
        await first.Repository<IWidgetRepository>().CountAsync(CancellationToken.None);
        await second.Repository<IWidgetRepository>().CountAsync(CancellationToken.None);

        await first.Repository<IWidgetRepository>()
            .AddAsync(new Widget { Id = Guid.NewGuid(), Name = UniqueName() }, CancellationToken.None);
        await first.SaveChangesAsync(CancellationToken.None);
        await firstTransaction.CommitAsync(CancellationToken.None);

        await second.Repository<IWidgetRepository>()
            .AddAsync(new Widget { Id = Guid.NewGuid(), Name = UniqueName() }, CancellationToken.None);

        // 40001 can be raised by either statement depending on when PostgreSQL notices the cycle, so
        // the assertion covers the pair rather than guessing which one.
        PersistenceException exception = await Assert.ThrowsAnyAsync<PersistenceException>(async () =>
        {
            await second.SaveChangesAsync(CancellationToken.None);
            await secondTransaction.CommitAsync(CancellationToken.None);
        });

        Assert.True(
            exception.IsTransient,
            $"A serialization failure must be classified as transient, but got {exception.GetType().Name}: {exception.Message}");
    }

    [Fact]
    public async Task ExecuteInTransaction_AfterATransientFailure_CommitsOnTheRetryAgainstRealPostgres()
    {
        // The unit tests prove the retry loop reacts to IsTransient, and the test above proves a real
        // 40001 sets it. What is left is that a retry works against a real database — that the
        // second attempt gets a genuinely new transaction and that its write actually commits.
        //
        // The failure is raised by the operation rather than provoked by contention on purpose.
        // Racing two connections and hoping PostgreSQL aborts the right one would make this test's
        // outcome depend on scheduling; a test that sometimes exercises nothing is worse than no test,
        // because it reports success either way.
        await fixture.ResetAsync();

        await using ServiceProvider provider = fixture.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        string name = UniqueName();
        int attempts = 0;

        await unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                attempts++;

                var repository = unitOfWork.Repository<IWidgetRepository>();
                await repository.AddAsync(new Widget { Id = Guid.NewGuid(), Name = name }, token);

                if (attempts == 1)
                {
                    throw new TransientPersistenceException("Simulated serialization failure.");
                }
            },
            TransactionIsolationLevel.Serializable,
            maxRetries: 2,
            CancellationToken.None);

        Assert.Equal(2, attempts);

        // Exactly one row. The first attempt's insert was rolled back with its transaction, and
        // DiscardChanges detached the entity so the retry did not re-send it alongside its own.
        await using AsyncServiceScope verifying = provider.CreateAsyncScope();
        int rows = await verifying.ServiceProvider.GetRequiredService<IUnitOfWork>()
            .Repository<IWidgetRepository>()
            .CountAsync(CancellationToken.None);

        Assert.Equal(1, rows);
    }

    private static string UniqueName() => $"widget-{Guid.NewGuid():N}";
}
