using Cinedex.Application.Abstractions;

namespace Cinedex.WebService.IntegrationTests.Fakes;

// Test double for IEmailSender that records the most recent password-reset token so tests can
// complete the reset flow without a real mail provider.
internal sealed class CapturingEmailSender : IEmailSender
{
    public string? LastEmail { get; private set; }

    public string? LastResetToken { get; private set; }

    public Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken)
    {
        this.LastEmail = email;
        this.LastResetToken = resetToken;
        return Task.CompletedTask;
    }
}
