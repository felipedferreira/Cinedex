using System.Collections.Concurrent;
using Cinedex.Application.Abstractions;
using Cinedex.Application.Email;

namespace Cinedex.WebService.IntegrationTests.Fakes;

// Test double for IEmailSender that records every message it is handed, so tests can inspect what
// would have been sent (and complete the reset flow) without a real mail provider.
//
// Delivery now happens on a background worker, after the HTTP response has been written, so tests
// must wait for their message rather than read it synchronously. Messages are kept rather than
// consumed and are looked up by recipient, so a test can never observe another test's email: every
// endpoint test generates a unique address.
internal sealed class CapturingEmailSender : IEmailSender
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(25);

    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    // Non-null while a test is deliberately stalling delivery.
    private TaskCompletionSource? _gate;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var gate = Volatile.Read(ref this._gate);
        if (gate is not null)
        {
            await gate.Task.WaitAsync(cancellationToken);
        }

        this._messages.Enqueue(message);
    }

    // Stalls every delivery until ResumeDelivery is called.
    internal void BlockDelivery() =>
        Volatile.Write(ref this._gate, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));

    // Releases a stall started by BlockDelivery. Safe to call when not stalled.
    internal void ResumeDelivery() => Interlocked.Exchange(ref this._gate, null)?.TrySetResult();

    // Waits for the message addressed to the supplied recipient. Delivery is queued and happens after
    // the response, so callers must wait rather than read.
    internal async Task<EmailMessage> WaitForMessageAsync(string recipientAddress, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? DefaultTimeout);

        while (true)
        {
            var message = this._messages.FirstOrDefault(candidate =>
                string.Equals(candidate.To.Address, recipientAddress, StringComparison.OrdinalIgnoreCase));
            if (message is not null)
            {
                return message;
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException(
                    $"No email addressed to '{recipientAddress}' was delivered before the wait timed out.");
            }

            await Task.Delay(PollInterval);
        }
    }
}
