namespace Movies.Application.Titles.DeleteTitle;

public interface IDeleteTitleHandler
{
    Task HandleAsync(DeleteTitleCommand command, CancellationToken cancellationToken);
}