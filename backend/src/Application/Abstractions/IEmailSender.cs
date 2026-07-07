namespace Cinedex.Application.Abstractions;

// Port for sending transactional emails. Keeps password-reset flows decoupled from any concrete
// mail provider.
public interface IEmailSender
{
    // Sends a password-reset message containing the supplied reset token to the given address.
    Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken);
}
