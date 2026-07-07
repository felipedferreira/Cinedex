using Cinedex.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Cinedex.Persistence.Auth.Identity.Services;

// Placeholder email sender: no real delivery is configured yet. Logs that a reset was requested
// without emitting the token value. Replace with a real provider when email delivery is added.
internal sealed class NoOpEmailSender(ILogger<NoOpEmailSender> logger) : IEmailSender
{
    public Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Password reset requested for {Email}. Email delivery is not configured; token was not sent.",
            email);

        return Task.CompletedTask;
    }
}
