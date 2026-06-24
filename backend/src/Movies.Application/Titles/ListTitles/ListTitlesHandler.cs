using Microsoft.Extensions.Logging;
using Movies.Application.Abstractions;
using Movies.Application.Constants;

namespace Movies.Application.Titles.ListTitles;

internal sealed class ListTitlesHandler(
    ITitleRepository repository,
    ILogger<ListTitlesHandler> logger) : IListTitlesHandler
{
    public async Task<IReadOnlyList<TitleDto>> Handle(ListTitlesQuery query, CancellationToken cancellationToken)
    {
        var titles = await repository.GetAllAsync(cancellationToken);

        logger.LogInformation(LogMessageConstants.Title.RetrievedAll, titles.Count);

        return titles.Select(title => title.ToDto()).ToList();
    }
}
