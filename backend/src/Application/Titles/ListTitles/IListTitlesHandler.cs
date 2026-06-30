namespace Movies.Application.Titles.ListTitles;

public interface IListTitlesHandler
{
    Task<IReadOnlyList<TitleDto>> HandleAsync(ListTitlesQuery query, CancellationToken cancellationToken);
}