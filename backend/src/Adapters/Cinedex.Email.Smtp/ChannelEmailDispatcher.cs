using System.Threading.Channels;
using Cinedex.Application.Abstractions;
using Cinedex.Application.Email;
using Microsoft.Extensions.Logging;

namespace Cinedex.Email.Smtp;

/// <summary>
/// In-process, bounded, non-durable <see cref="IEmailDispatcher"/>. Writes to a
/// <see cref="Channel{T}"/> that <see cref="EmailDeliveryWorker"/> drains. Queued messages live only
/// in memory: a crash loses them.
/// </summary>
internal sealed class ChannelEmailDispatcher(ILogger<ChannelEmailDispatcher> logger) : IEmailDispatcher
{
    // Bounded because POST /auth/password/forgot is anonymous and unthrottled: an unbounded queue
    // behind it is a memory-exhaustion vector. 1,000 pending messages costs about a megabyte and is
    // far past any legitimate burst — reaching it means SMTP is down or the endpoint is being
    // flooded. FullMode.Wait paired with TryWrite gives a non-blocking write that *reports* overflow;
    // DropWrite would return true and discard silently.
    private const int Capacity = 1_000;

    private readonly Channel<EmailMessage> _channel = Channel.CreateBounded<EmailMessage>(
        new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });

    internal int PendingCount => this._channel.Reader.Count;

    /// <inheritdoc />
    public void Enqueue(EmailMessage message)
    {
        // TryWrite is O(1), never blocks, and never throws: it returns false when the queue is full
        // and when the writer has been completed during shutdown. Both drop the message rather than
        // propagate, so no caller can observe the failure in its latency or its response.
        if (this._channel.Writer.TryWrite(message))
        {
            return;
        }

        // The recipient address is deliberately excluded — this log must not become a record of who
        // requested a password reset.
        logger.LogWarning(
            "Dropped an outgoing email with tags {Tags}: the delivery queue is full or closed ({Capacity} message capacity). The recipient will have to retry.",
            message.Tags,
            Capacity);
    }

    internal void CompleteWriting() => this._channel.Writer.TryComplete();

    internal IAsyncEnumerable<EmailMessage> DequeueAllAsync(CancellationToken cancellationToken) =>
        this._channel.Reader.ReadAllAsync(cancellationToken);
}
