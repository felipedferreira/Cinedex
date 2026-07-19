namespace Cinedex.Application.Auth.Logout;

public interface ILogoutHandler
{
    Task HandleAsync(LogoutCommand command, CancellationToken cancellationToken);
}