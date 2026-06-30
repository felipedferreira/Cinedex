namespace Movies.Application.Titles.GetTitleById;

public interface IGetTitleByIdHandler
{
    Task<TitleDetailsDto> HandleAsync(GetTitleByIdQuery query, CancellationToken cancellationToken);
}