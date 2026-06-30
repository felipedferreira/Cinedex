namespace Cinedex.Application.Titles.CreateTitle;

public interface ICreateTitleHandler
{
    Task<Guid> HandleAsync(CreateTitleCommand command, CancellationToken cancellationToken);
}