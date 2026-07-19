namespace Cinedex.Application.Auth.ResetPassword;

public interface IResetPasswordHandler
{
    Task HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken);
}