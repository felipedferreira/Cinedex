using FoundryOceanus.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cinedex.Persistence.Tests.Fakes;

/// <summary>
/// A repository doing the thing the shared-context check exists to catch: holding a context of its
/// own rather than the scoped one.
/// </summary>
/// <remarks>
/// This is the <c>IDbContextFactory</c> mistake in its most direct form. Its writes would commit on a
/// separate connection, outside whatever transaction the unit of work opened — and without the check,
/// nothing anywhere would say so.
/// </remarks>
public sealed class DetachedWidgetRepository : EfRepository<WidgetDbContext>, IWidgetRepository
{
    public DetachedWidgetRepository(
        IDbContextFactory<WidgetDbContext> contextFactory,
        CompositePersistenceExceptionTranslator exceptionTranslator)
        : base(contextFactory.CreateDbContext(), exceptionTranslator)
    {
    }

    public Task AddAsync(Widget widget, CancellationToken cancellationToken)
    {
        DbContext.Widgets.Add(widget);

        return Task.CompletedTask;
    }

    public Task<Widget?> FindByNameAsync(string name, CancellationToken cancellationToken) =>
        DbContext.Widgets.AsNoTracking().FirstOrDefaultAsync(widget => widget.Name == name, cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken) =>
        DbContext.Widgets.CountAsync(cancellationToken);

    public Task<int> SetQuantityAsync(string name, int quantity, CancellationToken cancellationToken) =>
        DbContext.Widgets
            .Where(widget => widget.Name == name)
            .ExecuteUpdateAsync(setters => setters.SetProperty(widget => widget.Quantity, quantity), cancellationToken);
}
