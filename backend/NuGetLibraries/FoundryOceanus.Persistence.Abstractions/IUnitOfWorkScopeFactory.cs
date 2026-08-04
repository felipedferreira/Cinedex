namespace FoundryOceanus.Persistence.Abstractions;

/// <summary>
/// Creates unit-of-work scopes for code that runs outside a request.
/// </summary>
/// <remarks>
/// <para>
/// A background service is a singleton, so it cannot take a scoped <see cref="IUnitOfWork"/> as a
/// constructor dependency — and if it forces one anyway it gets a single context that lives for the
/// process's lifetime, accumulating tracked entities until the change tracker is the slowest thing
/// in the service. Each iteration needs its own scope.
/// </para>
/// <para>
/// The usual answer to that is to inject <c>IServiceScopeFactory</c> and resolve out of it, which
/// works but puts the container back in front of code that was supposed to have stopped knowing
/// about it. This interface is the same mechanism, named for what the caller is actually asking for.
/// </para>
/// <example>
/// <code>
/// protected override async Task ExecuteAsync(CancellationToken stoppingToken)
/// {
///     while (!stoppingToken.IsCancellationRequested)
///     {
///         await using IUnitOfWorkScope scope = _scopeFactory.CreateScope();
///
///         await scope.UnitOfWork
///             .Repository&lt;IRefreshTokenRepository&gt;()
///             .DeleteExpiredBatchAsync(cutoff, batchSize, stoppingToken);
///
///         await Task.Delay(_interval, stoppingToken);
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public interface IUnitOfWorkScopeFactory
{
    /// <summary>
    /// Creates a scope holding a fresh <see cref="IUnitOfWork"/> and its own persistence context.
    /// </summary>
    /// <returns>The new scope. The caller owns it and must dispose it.</returns>
    IUnitOfWorkScope CreateScope();
}
