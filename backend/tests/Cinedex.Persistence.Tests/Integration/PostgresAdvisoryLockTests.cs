using Cinedex.Persistence.Abstractions;
using Cinedex.Persistence.Abstractions.Transactions;
using Cinedex.Persistence.EntityFrameworkCore.Postgres;
using Cinedex.Persistence.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace Cinedex.Persistence.Tests.Integration;

/// <summary>
/// Advisory-lock behaviour against real PostgreSQL.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class PostgresAdvisoryLockTests(PostgresFixture fixture)
{
    [Fact]
    public async Task TryAcquire_WhenHeldByAnotherTransaction_ReturnsFalse()
    {
        string key = UniqueKey();

        await using ServiceProvider provider = fixture.BuildServiceProvider();

        await using AsyncServiceScope holder = provider.CreateAsyncScope();
        var holderUnitOfWork = holder.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var holderContext = holder.ServiceProvider.GetRequiredService<WidgetDbContext>();

        await using ITransaction holderTransaction = await holderUnitOfWork.BeginTransactionAsync();
        Assert.True(await holderContext.TryAcquireTransactionLockAsync(key, CancellationToken.None));

        // A second scope means a second connection, which is the only way to observe contention —
        // the same session would simply re-acquire its own lock and report success.
        await using AsyncServiceScope contender = provider.CreateAsyncScope();
        var contenderUnitOfWork = contender.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var contenderContext = contender.ServiceProvider.GetRequiredService<WidgetDbContext>();

        await using ITransaction contenderTransaction = await contenderUnitOfWork.BeginTransactionAsync();

        Assert.False(await contenderContext.TryAcquireTransactionLockAsync(key, CancellationToken.None));
    }

    [Fact]
    public async Task TryAcquire_AfterTheHoldingTransactionEnds_Succeeds()
    {
        // The property that makes transaction-scoped locks safe: they are released by the transaction
        // ending, with no release call to forget on an exception path.
        string key = UniqueKey();

        await using ServiceProvider provider = fixture.BuildServiceProvider();

        await using (AsyncServiceScope holder = provider.CreateAsyncScope())
        {
            var unitOfWork = holder.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var dbContext = holder.ServiceProvider.GetRequiredService<WidgetDbContext>();

            await using ITransaction transaction = await unitOfWork.BeginTransactionAsync();
            Assert.True(await dbContext.TryAcquireTransactionLockAsync(key, CancellationToken.None));
        }

        await using AsyncServiceScope next = provider.CreateAsyncScope();
        var nextUnitOfWork = next.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var nextContext = next.ServiceProvider.GetRequiredService<WidgetDbContext>();

        await using ITransaction nextTransaction = await nextUnitOfWork.BeginTransactionAsync();

        Assert.True(await nextContext.TryAcquireTransactionLockAsync(key, CancellationToken.None));
    }

    [Fact]
    public async Task Acquire_OutsideATransaction_Throws()
    {
        // A transaction-scoped lock taken outside a transaction is released by the statement that took
        // it — the call would appear to succeed while protecting nothing.
        await using ServiceProvider provider = fixture.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => dbContext.AcquireTransactionLockAsync(UniqueKey(), CancellationToken.None));

        Assert.Contains("transaction", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Acquire_SerialisesConcurrentCallersOnTheSameKey()
    {
        // The behaviour the blocking overload exists for: the second caller waits rather than
        // proceeding in parallel, so the guarded section runs one at a time.
        string key = UniqueKey();

        await using ServiceProvider provider = fixture.BuildServiceProvider();

        await using AsyncServiceScope holder = provider.CreateAsyncScope();
        var holderUnitOfWork = holder.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var holderContext = holder.ServiceProvider.GetRequiredService<WidgetDbContext>();

        ITransaction holderTransaction = await holderUnitOfWork.BeginTransactionAsync();
        await holderContext.AcquireTransactionLockAsync(key, CancellationToken.None);

        await using AsyncServiceScope contender = provider.CreateAsyncScope();
        var contenderUnitOfWork = contender.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var contenderContext = contender.ServiceProvider.GetRequiredService<WidgetDbContext>();

        await using ITransaction contenderTransaction = await contenderUnitOfWork.BeginTransactionAsync();
        Task waiting = contenderContext.AcquireTransactionLockAsync(key, CancellationToken.None);

        // Still blocked while the first transaction holds the lock. Asserting on a short wait rather
        // than on an exact instant: the point is that it does not complete, not how fast it does not.
        Task firstToFinish = await Task.WhenAny(waiting, Task.Delay(TimeSpan.FromMilliseconds(250)));
        Assert.NotSame(waiting, firstToFinish);

        await holderTransaction.CommitAsync(CancellationToken.None);
        await holderTransaction.DisposeAsync();

        await waiting.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task TryAcquire_WithNumericKey_UsesTheSameLockSpace()
    {
        long key = Random.Shared.NextInt64();

        await using ServiceProvider provider = fixture.BuildServiceProvider();

        await using AsyncServiceScope holder = provider.CreateAsyncScope();
        var holderUnitOfWork = holder.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var holderContext = holder.ServiceProvider.GetRequiredService<WidgetDbContext>();

        await using ITransaction holderTransaction = await holderUnitOfWork.BeginTransactionAsync();
        Assert.True(await holderContext.TryAcquireTransactionLockAsync(key, CancellationToken.None));

        await using AsyncServiceScope contender = provider.CreateAsyncScope();
        var contenderUnitOfWork = contender.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var contenderContext = contender.ServiceProvider.GetRequiredService<WidgetDbContext>();

        await using ITransaction contenderTransaction = await contenderUnitOfWork.BeginTransactionAsync();

        Assert.False(await contenderContext.TryAcquireTransactionLockAsync(key, CancellationToken.None));
    }

    private static string UniqueKey() => $"cinedex-test:{Guid.NewGuid():N}";
}
