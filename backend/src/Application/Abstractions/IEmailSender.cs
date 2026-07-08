namespace Cinedex.Application.Abstractions;

/// <summary>
/// Port for sending transactional emails. Keeps password-reset flows decoupled from any concrete
/// mail provider.
/// </summary>
/// <remarks>
/// No real mail provider is wired up yet: the adapter registers a no-op implementation, so
/// password reset is not usable end to end. See <c>docs/auth-security-model.md</c>.
/// </remarks>
public interface IEmailSender
{
    /// <summary>
    /// Sends a password-reset message containing the supplied reset token to the given address.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="resetToken">The reset token to embed in the message, as issued by <see cref="IIdentityService.GeneratePasswordResetTokenAsync"/>.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that completes when the message has been handed to the mail provider.</returns>
    Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken);
}
