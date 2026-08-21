namespace Cinedex.Application.Auth.RevokeAllSessions;

public interface IRevokeAllSessionsHandler
{
    Task HandleAsync(RevokeAllSessionsCommand command, CancellationToken cancellationToken);
}
