namespace Cinedex.Application.Auth.Login;

public interface ILoginHandler
{
    Task<AuthTokensDto> HandleAsync(LoginCommand command, CancellationToken cancellationToken);
}
