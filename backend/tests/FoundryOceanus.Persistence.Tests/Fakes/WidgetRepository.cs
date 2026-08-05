using FoundryOceanus.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FoundryOceanus.Persistence.Tests.Fakes;

/// <summary>
/// Repository built the recommended way: derived from <see cref="EfRepository{TContext}"/>, taking
/// the scoped context by constructor injection.
/// </summary>
public sealed class WidgetRepository(
    WidgetDbContext dbContext,
    CompositePersistenceExceptionTranslator exceptionTranslator)
    : EfRepository<WidgetDbContext>(dbContext, exceptionTranslator), IWidgetRepository
{
    // Deliberately does not save. Deciding when to write is the unit of work's job, and a repository
    // that saves on every call makes a multi-step transaction impossible to express.
    public Task AddAsync(Widget widget, CancellationToken cancellationToken)
    {
        DbContext.Widgets.Add(widget);

        return Task.CompletedTask;
    }

    public Task<Widget?> FindByNameAsync(string name, CancellationToken cancellationToken) =>
        ExecuteAsync(
            token => DbContext.Widgets
                .AsNoTracking()
                .FirstOrDefaultAsync(widget => widget.Name == name, token),
            cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken) =>
        ExecuteAsync(token => DbContext.Widgets.CountAsync(token), cancellationToken);

    // Set-based, so it never passes through SaveChanges â€” which is exactly why it goes through
    // ExecuteAsync: without that wrapper its failures would arrive as raw Npgsql exceptions.
    public Task<int> SetQuantityAsync(string name, int quantity, CancellationToken cancellationToken) =>
        ExecuteAsync(
            token => DbContext.Widgets
                .Where(widget => widget.Name == name)
                .ExecuteUpdateAsync(setters => setters.SetProperty(widget => widget.Quantity, quantity), token),
            cancellationToken);
}
