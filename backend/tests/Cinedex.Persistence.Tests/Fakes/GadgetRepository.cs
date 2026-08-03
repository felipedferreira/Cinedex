using Cinedex.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cinedex.Persistence.Tests.Fakes;

/// <summary>
/// Implementation of <see cref="IGadgetRepository"/> over the second context.
/// </summary>
public sealed class GadgetRepository(
    WidgetDbContext dbContext,
    CompositePersistenceExceptionTranslator exceptionTranslator)
    : EfRepository<WidgetDbContext>(dbContext, exceptionTranslator), IGadgetRepository
{
    public Task<int> CountAsync(CancellationToken cancellationToken) =>
        ExecuteAsync(token => DbContext.Widgets.CountAsync(token), cancellationToken);
}
