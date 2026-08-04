using FoundryOceanus.Persistence.Abstractions;

namespace Cinedex.Persistence.Tests.Fakes;

/// <summary>
/// A domain-named repository port, shaped the way the library expects them to be written.
/// </summary>
public interface IWidgetRepository : IRepository
{
    Task AddAsync(Widget widget, CancellationToken cancellationToken);

    Task<Widget?> FindByNameAsync(string name, CancellationToken cancellationToken);

    Task<int> CountAsync(CancellationToken cancellationToken);

    Task<int> SetQuantityAsync(string name, int quantity, CancellationToken cancellationToken);
}
