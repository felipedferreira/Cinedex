namespace Cinedex.Application.Titles.UpdateTitle;

public interface IUpdateTitleHandler
{
    Task HandleAsync(UpdateTitleCommand command, CancellationToken cancellationToken);
}