using Microsoft.Extensions.Logging;
using Movies.Application.Abstractions;

namespace Movies.Application.Titles.ListTitles;

internal sealed class ListTitlesHandler(
    ITitleRepository repository,
    ILogger<ListTitlesHandler> logger) : IListTitlesHandler
{
    public async Task<IReadOnlyList<TitleDto>> HandleAsync(ListTitlesQuery query, CancellationToken cancellationToken)
    {
        var titles = await repository.GetAllAsync(cancellationToken);

        logger.LogInformation("Retrieved {TitleCount} titles.", titles.Count);

        return titles.Select(title => title.ToDto()).ToList();
    }
}
