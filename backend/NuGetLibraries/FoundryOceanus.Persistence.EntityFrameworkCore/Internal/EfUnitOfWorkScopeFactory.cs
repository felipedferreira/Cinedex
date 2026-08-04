using FoundryOceanus.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoundryOceanus.Persistence.EntityFrameworkCore.Internal;

/// <summary>
/// <see cref="IUnitOfWorkScopeFactory"/> backed by <see cref="IServiceScopeFactory"/>.
/// </summary>
/// <typeparam name="TContext">The persistence context whose unit of work this factory creates.</typeparam>
internal sealed class EfUnitOfWorkScopeFactory<TContext> : IUnitOfWorkScopeFactory
    where TContext : DbContext
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    // Public on an internal type, which reads oddly but is required: the container's activator only
    // considers public constructors, so an internal one fails at resolution rather than at compile
    // time. The type itself stays internal, so this widens nothing outside the assembly.
    public EfUnitOfWorkScopeFactory(IServiceScopeFactory serviceScopeFactory)
    {
        ArgumentNullException.ThrowIfNull(serviceScopeFactory);

        _serviceScopeFactory = serviceScopeFactory;
    }

    public IUnitOfWorkScope CreateScope()
    {
        AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();

        try
        {
            // Resolved eagerly so a misconfiguration surfaces here, at the call that asked for the
            // scope, rather than later from a property getter that looks like it cannot fail.
            EfUnitOfWork<TContext> unitOfWork = scope.ServiceProvider.GetRequiredService<EfUnitOfWork<TContext>>();

            return new EfUnitOfWorkScope(scope, unitOfWork);
        }
        catch
        {
            // Synchronous disposal on the failure path: this method cannot await, and leaking the
            // scope would be worse than disposing it the blocking way on a path that is already
            // throwing.
            scope.Dispose();
            throw;
        }
    }
}
