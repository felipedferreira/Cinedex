using Cinedex.Application.Abstractions;
using Cinedex.Application.Email;

namespace Cinedex.WebService.IntegrationTests.Fakes;

// Test double for IEmailSender that records the most recent message so tests can inspect what would
// have been sent (and complete the reset flow) without a real mail provider.
internal sealed class CapturingEmailSender : IEmailSender
{
    public EmailMessage? LastMessage { get; private set; }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        this.LastMessage = message;
        return Task.CompletedTask;
    }
}
