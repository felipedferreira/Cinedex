namespace Cinedex.Application.Auth.ForgotPassword;

public interface IForgotPasswordHandler
{
    Task HandleAsync(ForgotPasswordCommand command, CancellationToken cancellationToken);
}
