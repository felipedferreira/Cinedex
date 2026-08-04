using FoundryOceanus.Persistence.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FoundryOceanus.Persistence.EntityFrameworkCore.Internal;

/// <summary>
/// <see cref="IUnitOfWorkScope"/> over a dependency-injection scope.
/// </summary>
internal sealed class EfUnitOfWorkScope : IUnitOfWorkScope
{
    private readonly AsyncServiceScope _scope;

    internal EfUnitOfWorkScope(AsyncServiceScope scope, IUnitOfWork unitOfWork)
    {
        _scope = scope;
        UnitOfWork = unitOfWork;
    }

    public IUnitOfWork UnitOfWork { get; }

    // Disposing the dependency-injection scope is what disposes the DbContext inside it. Skipping
    // this in a background worker leaks a context and a pooled connection per iteration, which
    // presents as connection-pool exhaustion hours later rather than as anything resembling its
    // cause.
    public ValueTask DisposeAsync() => _scope.DisposeAsync();
}
