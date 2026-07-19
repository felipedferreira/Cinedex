namespace Cinedex.Application.Auth.RegisterUser;

public interface IRegisterUserHandler
{
    Task<Guid> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken);
}