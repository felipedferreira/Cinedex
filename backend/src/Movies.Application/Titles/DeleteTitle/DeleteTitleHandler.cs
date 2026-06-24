using Microsoft.Extensions.Logging;
using Movies.Application.Abstractions;
using Movies.Application.Constants;
using Movies.Application.Exceptions;
using Movies.Domain.TitleAggregate;

namespace Movies.Application.Titles.DeleteTitle;

internal sealed class DeleteTitleHandler(
    ITitleRepository repository,
    ILogger<DeleteTitleHandler> logger) : IDeleteTitleHandler
{
    public async Task Handle(DeleteTitleCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation(LogMessageConstants.Title.Deleting, command.Id);

        var deleted = await repository.DeleteAsync(command.Id, cancellationToken);

        if (!deleted)
        {
            logger.LogWarning(LogMessageConstants.Title.NotFoundForDeletion, command.Id);
            throw new EntityNotFoundException(nameof(Title), command.Id);
        }

        logger.LogInformation(LogMessageConstants.Title.Deleted, command.Id);
    }
}
