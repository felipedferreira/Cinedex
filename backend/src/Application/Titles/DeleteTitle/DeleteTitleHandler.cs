using Cinedex.Application.Abstractions;
using Cinedex.Application.Exceptions;
using Cinedex.Domain.TitleAggregate;
using Microsoft.Extensions.Logging;

namespace Cinedex.Application.Titles.DeleteTitle;

internal sealed class DeleteTitleHandler(
    ITitleRepository repository,
    ILogger<DeleteTitleHandler> logger) : IDeleteTitleHandler
{
    public async Task HandleAsync(DeleteTitleCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting title {TitleId}.", command.Id);

        var deleted = await repository.DeleteAsync(command.Id, cancellationToken);

        if (!deleted)
        {
            logger.LogWarning("Title {TitleId} was not found for deletion.", command.Id);
            throw new EntityNotFoundException(nameof(Title), command.Id);
        }

        logger.LogInformation("Deleted title {TitleId}.", command.Id);
    }
}