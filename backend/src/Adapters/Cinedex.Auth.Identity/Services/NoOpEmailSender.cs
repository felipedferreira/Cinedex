using Cinedex.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Cinedex.Auth.Identity.Services;

/// <summary>
/// Placeholder <see cref="IEmailSender"/> that performs no delivery. Logs that a reset was
/// requested, without emitting the token value.
/// </summary>
/// <remarks>
/// This is the implementation registered by <c>AddAuthenticationAdapter</c>, so password reset is
/// not usable end to end. Replace it with a real mail provider when email delivery is added.
/// </remarks>
internal sealed class NoOpEmailSender(ILogger<NoOpEmailSender> logger) : IEmailSender
{
    /// <inheritdoc />
    /// <remarks>
    /// Logs the request and returns <see cref="Task.CompletedTask"/>. No message is sent, and
    /// <paramref name="resetToken"/> is deliberately never written to the log.
    /// </remarks>
    public Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken)
    {
        // TODO: Implement real email delivery. Until a mail provider is wired up here, the reset
        // token never reaches the user and POST /auth/password/reset can only be exercised with a
        // token obtained out of band. Tracked as a known gap in docs/auth-security-model.md.
        logger.LogInformation(
            "Password reset requested for {Email}. Email delivery is not configured; token was not sent.",
            email);

        return Task.CompletedTask;
    }
}
