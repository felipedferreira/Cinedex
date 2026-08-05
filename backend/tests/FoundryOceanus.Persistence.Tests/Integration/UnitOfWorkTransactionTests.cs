using FoundryOceanus.Persistence.Abstractions;
using FoundryOceanus.Persistence.Abstractions.Exceptions;
using FoundryOceanus.Persistence.Abstractions.Transactions;
using FoundryOceanus.Persistence.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace FoundryOceanus.Persistence.Tests.Integration;

/// <summary>
/// Transaction behaviour against real PostgreSQL.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class UnitOfWorkTransactionTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Commit_PersistsWritesMadeThroughARepository()
    {
        string name = UniqueName();

        await using (ServiceProvider provider = fixture.BuildServiceProvider())
        await using (AsyncServiceScope scope = provider.CreateAsyncScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await using ITransaction transaction = await unitOfWork.BeginTransactionAsync();
            await unitOfWork.Repository<IWidgetRepository>()
                .AddAsync(new Widget { Id = Guid.NewGuid(), Name = name }, CancellationToken.None);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        Assert.NotNull(await FindAsync(name));
    }

    [Fact]
    public async Task Dispose_WithoutCommit_RollsBack()
    {
        // The property the whole `await using` convention rests on. If disposal did not roll back,
        // every early return and every unhandled exception would leave a partial write behind.
        string name = UniqueName();

        await using (ServiceProvider provider = fixture.BuildServiceProvider())
        await using (AsyncServiceScope scope = provider.CreateAsyncScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await using ITransaction transaction = await unitOfWork.BeginTransactionAsync();
            await unitOfWork.Repository<IWidgetRepository>()
                .AddAsync(new Widget { Id = Guid.NewGuid(), Name = name }, CancellationToken.None);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }

        Assert.Null(await FindAsync(name));
    }

    [Fact]
    public async Task SaveChanges_WithDuplicateName_ThrowsDuplicateKeyExceptionCarryingTheConstraint()
    {
        // End to end: PostgreSQL raises 23505, Npgsql wraps it, EF wraps that, and the application
        // layer still catches a DuplicateKeyException it can turn into a 409.
        string name = UniqueName();
        await InsertAsync(name);

        await using ServiceProvider provider = fixture.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.Repository<IWidgetRepository>()
            .AddAsync(new Widget { Id = Guid.NewGuid(), Name = name }, CancellationToken.None);

        DuplicateKeyException exception = await Assert.ThrowsAsync<DuplicateKeyException>(
            () => unitOfWork.SaveChangesAsync(CancellationToken.None));

        Assert.Equal("ix_widgets_name", exception.ConstraintName);
    }

    [Fact]
    public async Task Repository_WithSetBasedStatement_TranslatesFailuresToo()
    {
        // ExecuteUpdate never passes through SaveChanges, so without EfRepository.ExecuteAsync this
        // would surface as a raw Npgsql exception.
        string first = UniqueName();
        string second = UniqueName();
        await InsertAsync(first);
        await InsertAsync(second);

        await using ServiceProvider provider = fixture.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        int updated = await unitOfWork.Repository<IWidgetRepository>()
            .SetQuantityAsync(first, 5, CancellationToken.None);

        Assert.Equal(1, updated);
    }

    [Fact]
    public async Task BeginTransaction_WhileOneIsOpen_Throws()
    {
        await using ServiceProvider provider = fixture.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await using ITransaction transaction = await unitOfWork.BeginTransactionAsync();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => unitOfWork.BeginTransactionAsync());

        Assert.Contains("savepoint", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BeginTransaction_AfterThePreviousOneIsDisposed_Succeeds()
    {
        // The unit of work has to forget a disposed transaction, or a retry loop would trip the
        // no-nesting rule on its second attempt.
        await using ServiceProvider provider = fixture.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await using (await unitOfWork.BeginTransactionAsync())
        {
        }

        Assert.Null(unitOfWork.CurrentTransaction);

        await using ITransaction second = await unitOfWork.BeginTransactionAsync();
        Assert.NotNull(second);
    }

    [Fact]
    public async Task CurrentTransaction_AfterCommit_ReadsAsNoTransaction()
    {
        // Callers assert on this to check they are inside a transaction â€” an advisory lock is the
        // example. A committed transaction satisfying that check would protect nothing.
        await using ServiceProvider provider = fixture.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await using ITransaction transaction = await unitOfWork.BeginTransactionAsync();
        await transaction.CommitAsync(CancellationToken.None);

        Assert.Null(unitOfWork.CurrentTransaction);
    }

    [Fact]
    public async Task BeginTransaction_AfterACommitThatWasNeverDisposed_Succeeds()
    {
        // Committing without disposing is a resource leak worth fixing, but it is not a reason to
        // refuse the next transaction â€” the previous one is over.
        await using ServiceProvider provider = fixture.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        ITransaction first = await unitOfWork.BeginTransactionAsync();
        await first.CommitAsync(CancellationToken.None);

        await using ITransaction second = await unitOfWork.BeginTransactionAsync();

        string name = UniqueName();
        await unitOfWork.Repository<IWidgetRepository>()
            .AddAsync(new Widget { Id = Guid.NewGuid(), Name = name }, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        await second.CommitAsync(CancellationToken.None);

        Assert.NotNull(await FindAsync(name));
    }

    [Fact]
    public async Task Commit_AfterCommit_Throws()
    {
        await using ServiceProvider provider = fixture.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await using ITransaction transaction = await unitOfWork.BeginTransactionAsync();
        await transaction.CommitAsync(CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => transaction.CommitAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Rollback_AfterCompletion_IsTolerated()
    {
        // Asymmetric with commit on purpose: this is what lets callers put a rollback in a catch block
        // without first testing IsCompleted, instead of throwing over the original exception.
        await using ServiceProvider provider = fixture.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await using ITransaction transaction = await unitOfWork.BeginTransactionAsync();
        await transaction.RollbackAsync(CancellationToken.None);

        await transaction.RollbackAsync(CancellationToken.None);

        Assert.True(transaction.IsCompleted);
    }

    [Fact]
    public async Task Savepoint_RollbackDiscardsOnlyTheWorkAfterIt()
    {
        string kept = UniqueName();
        string discarded = UniqueName();

        await using (ServiceProvider provider = fixture.BuildServiceProvider())
        await using (AsyncServiceScope scope = provider.CreateAsyncScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var repository = unitOfWork.Repository<IWidgetRepository>();

            await using ITransaction transaction = await unitOfWork.BeginTransactionAsync();

            await repository.AddAsync(
                new Widget { Id = Guid.NewGuid(), Name = kept },
                CancellationToken.None);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);

            ISavepoint savepoint = await transaction.CreateSavepointAsync(
                "before_optional_work",
                CancellationToken.None);

            await repository.AddAsync(
                new Widget { Id = Guid.NewGuid(), Name = discarded },
                CancellationToken.None);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);

            await savepoint.RollbackAsync(CancellationToken.None);

            // The rollback undid the database's view of the second insert but left the entity tracked.
            // Saving again without discarding would try to re-insert it â€” which is exactly the caveat
            // documented on ISavepoint.RollbackAsync.
            unitOfWork.DiscardChanges();

            await transaction.CommitAsync(CancellationToken.None);
        }

        Assert.NotNull(await FindAsync(kept));
        Assert.Null(await FindAsync(discarded));
    }

    [Theory]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("1_leading_digit")]
    [InlineData("drop\"table")]
    public async Task CreateSavepoint_WithInvalidName_Throws(string name)
    {
        await using ServiceProvider provider = fixture.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await using ITransaction transaction = await unitOfWork.BeginTransactionAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => transaction.CreateSavepointAsync(name, CancellationToken.None));
    }

    [Fact]
    public async Task Repository_BoundToADifferentContext_IsRejected()
    {
        // Without this check the detached repository's writes would commit on their own connection,
        // outside the transaction, and nothing would report it.
        await using ServiceProvider provider = fixture.BuildServiceProvider(useDetachedRepository: true);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() => unitOfWork.Repository<IWidgetRepository>());

        Assert.Contains("different", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Repository_WhenNotRegistered_ExplainsHowToRegisterIt()
    {
        await using ServiceProvider provider = fixture.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() => unitOfWork.Repository<IGadgetRepository>());

        Assert.Contains("AddRepository", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ScopeFactory_GivesEachScopeItsOwnUnitOfWork()
    {
        await using ServiceProvider provider = fixture.BuildServiceProvider();

        var scopeFactory = provider.GetRequiredService<IUnitOfWorkScopeFactory>();

        await using IUnitOfWorkScope first = scopeFactory.CreateScope();
        await using IUnitOfWorkScope second = scopeFactory.CreateScope();

        Assert.NotSame(first.UnitOfWork, second.UnitOfWork);

        string name = UniqueName();
        await first.UnitOfWork.ExecuteInTransactionAsync(
            token => first.UnitOfWork.Repository<IWidgetRepository>()
                .AddAsync(new Widget { Id = Guid.NewGuid(), Name = name }, token),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(await second.UnitOfWork.Repository<IWidgetRepository>()
            .FindByNameAsync(name, CancellationToken.None));
    }

    private static string UniqueName() => $"widget-{Guid.NewGuid():N}";

    private async Task InsertAsync(string name)
    {
        await using ServiceProvider provider = fixture.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.Repository<IWidgetRepository>()
            .AddAsync(new Widget { Id = Guid.NewGuid(), Name = name }, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<Widget?> FindAsync(string name)
    {
        await using ServiceProvider provider = fixture.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<IUnitOfWork>()
            .Repository<IWidgetRepository>()
            .FindByNameAsync(name, CancellationToken.None);
    }
}
