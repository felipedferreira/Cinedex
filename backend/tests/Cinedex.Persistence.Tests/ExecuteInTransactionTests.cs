using Cinedex.Persistence.Abstractions;
using Cinedex.Persistence.Abstractions.Exceptions;
using Cinedex.Persistence.Abstractions.Transactions;
using Cinedex.Persistence.Tests.Fakes;

namespace Cinedex.Persistence.Tests;

public sealed class ExecuteInTransactionTests
{
    [Fact]
    public async Task ExecuteInTransaction_WhenOperationSucceeds_SavesThenCommits()
    {
        var unitOfWork = new RecordingUnitOfWork();

        int result = await unitOfWork.ExecuteInTransactionAsync(_ => Task.FromResult(42));

        Assert.Equal(42, result);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        RecordingTransaction transaction = Assert.Single(unitOfWork.Transactions);
        Assert.True(transaction.Committed);
        Assert.True(transaction.Disposed);
    }

    [Fact]
    public async Task ExecuteInTransaction_WhenOperationThrows_DisposesWithoutCommitting()
    {
        var unitOfWork = new RecordingUnitOfWork();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => unitOfWork.ExecuteInTransactionAsync<int>(
                _ => throw new InvalidOperationException("Business rule failed.")));

        RecordingTransaction transaction = Assert.Single(unitOfWork.Transactions);
        Assert.False(transaction.Committed);

        // Disposal without a commit is what rolls back. It is the property that makes every early
        // exit safe, including the ones nobody wrote a catch for.
        Assert.True(transaction.Disposed);
    }

    [Fact]
    public async Task ExecuteInTransaction_WithNonTransientFailure_DoesNotRetry()
    {
        var unitOfWork = new RecordingUnitOfWork();
        int attempts = 0;

        await Assert.ThrowsAsync<DuplicateKeyException>(
            () => unitOfWork.ExecuteInTransactionAsync<int>(
                _ =>
                {
                    attempts++;
                    throw new DuplicateKeyException("Duplicate.");
                },
                maxRetries: 5));

        // Replaying a duplicate key would fail identically every time. Retrying it would turn one
        // failed request into six.
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task ExecuteInTransaction_WithTransientFailure_RetriesInAFreshTransaction()
    {
        var unitOfWork = new RecordingUnitOfWork();
        int attempts = 0;

        int result = await unitOfWork.ExecuteInTransactionAsync(
            _ =>
            {
                attempts++;

                return attempts < 3
                    ? throw new TransientPersistenceException("Serialization failure.")
                    : Task.FromResult(7);
            },
            TransactionIsolationLevel.Serializable,
            maxRetries: 3);

        Assert.Equal(7, result);
        Assert.Equal(3, attempts);

        // PostgreSQL aborts the whole transaction on a serialization failure, so a retry has to begin
        // a new one — reusing the aborted transaction would fail on every subsequent statement.
        Assert.Equal(3, unitOfWork.Transactions.Count);
        Assert.All(unitOfWork.Transactions.Take(2), transaction => Assert.False(transaction.Committed));
        Assert.True(unitOfWork.Transactions[2].Committed);
    }

    [Fact]
    public async Task ExecuteInTransaction_BetweenRetries_DiscardsPendingChanges()
    {
        // Without this, the second attempt would still be tracking entities the first attempt loaded
        // against a snapshot the database has already thrown away.
        var unitOfWork = new RecordingUnitOfWork();
        int attempts = 0;

        await unitOfWork.ExecuteInTransactionAsync(
            _ =>
            {
                attempts++;

                return attempts < 2
                    ? throw new TransientPersistenceException("Serialization failure.")
                    : Task.CompletedTask;
            },
            maxRetries: 1);

        Assert.Equal(1, unitOfWork.DiscardChangesCallCount);
    }

    [Fact]
    public async Task ExecuteInTransaction_WhenRetriesRunOut_ThrowsTheTransientFailure()
    {
        var unitOfWork = new RecordingUnitOfWork();
        int attempts = 0;

        await Assert.ThrowsAsync<TransientPersistenceException>(
            () => unitOfWork.ExecuteInTransactionAsync(
                _ =>
                {
                    attempts++;
                    throw new TransientPersistenceException("Serialization failure.");
                },
                maxRetries: 2));

        // maxRetries counts retries, not total attempts: one initial run plus two more.
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task ExecuteInTransaction_WithNegativeMaxRetries_Throws()
    {
        var unitOfWork = new RecordingUnitOfWork();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => unitOfWork.ExecuteInTransactionAsync(_ => Task.CompletedTask, maxRetries: -1));
    }

    [Fact]
    public async Task ExecuteInTransaction_PassesTheRequestedIsolationLevelThrough()
    {
        var unitOfWork = new RecordingUnitOfWork();

        await unitOfWork.ExecuteInTransactionAsync(
            _ => Task.CompletedTask,
            TransactionIsolationLevel.Serializable);

        Assert.Equal(TransactionIsolationLevel.Serializable, Assert.Single(unitOfWork.Transactions).IsolationLevel);
    }
}
