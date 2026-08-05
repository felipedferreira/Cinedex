using FoundryOceanus.Persistence.Abstractions;

namespace FoundryOceanus.Persistence.Tests.Fakes;

/// <summary>
/// A second repository port, used to prove that repeated registration calls accumulate.
/// </summary>
public interface IGadgetRepository : IRepository
{
    Task<int> CountAsync(CancellationToken cancellationToken);
}
