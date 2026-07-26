using Cinedex.Application.Abstractions;
using Cinedex.Application.Email;
using Cinedex.Application.Exceptions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cinedex.Email.Smtp;

/// <summary>
/// Drains <see cref="ChannelEmailDispatcher"/> and delivers each message through
/// <see cref="IEmailSender"/>, off the HTTP request path. The queue and this worker are
/// transport-agnostic; only <see cref="IEmailSender"/> knows this adapter speaks SMTP.
/// </summary>
internal sealed class EmailDeliveryWorker(
    ChannelEmailDispatcher dispatcher,
    IEmailSender emailSender,
    ILogger<EmailDeliveryWorker> logger) : BackgroundService
{
    // How long shutdown may spend finishing queued deliveries. Kept below Docker's 10-second default
    // stop grace period and well below HostOptions.ShutdownTimeout (30 s), so the drain finishes on
    // its own terms rather than being killed part-way.
    private static readonly TimeSpan DrainTimeout = TimeSpan.FromSeconds(5);

    // Deliveries must never observe a request's CancellationToken: it is already cancelled by the
    // time the response has been written, and SmtpEmailSender's "when (exception is not
    // OperationCanceledException)" filter lets a cancellation escape past every catch. This source is
    // owned by the worker and cancelled only when the shutdown drain window expires.
    private readonly CancellationTokenSource _deliveryCts = new();

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Completing the writer makes ReadAllAsync finish once the backlog is empty, so ExecuteAsync
        // returns on its own instead of being torn off mid-delivery. Enqueues racing with shutdown
        // get false back from TryWrite and are logged as drops rather than throwing.
        dispatcher.CompleteWriting();
        this._deliveryCts.CancelAfter(DrainTimeout);

        await base.StopAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        this._deliveryCts.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // stoppingToken is deliberately ignored: it fires at the start of shutdown, and the drain in
        // StopAsync depends on this loop outliving it. _deliveryCts is what bounds the drain.
        await foreach (var message in dispatcher.DequeueAllAsync(CancellationToken.None))
        {
            if (this._deliveryCts.IsCancellationRequested)
            {
                logger.LogWarning(
                    "Shutdown drain window expired; {PendingCount} outgoing email(s) were not delivered.",
                    dispatcher.PendingCount + 1);
                return;
            }

            await this.DeliverAsync(message);
        }
    }

    private async Task DeliverAsync(EmailMessage message)
    {
        try
        {
            await emailSender.SendAsync(message, this._deliveryCts.Token);
        }
        catch (EmailDeliveryException exception)
        {
            // The recipient address and body are deliberately excluded: this log must not become a
            // record of who requested a password reset, even during a mail-provider outage.
            logger.LogError(exception, "Background delivery of an email with tags {Tags} failed.", message.Tags);
        }
        catch (OperationCanceledException)
        {
            // The drain window expired mid-delivery. SmtpEmailSender's exception filter lets
            // OperationCanceledException through, so it arrives here raw, not as EmailDeliveryException.
            logger.LogWarning("Shutdown cancelled an in-flight email delivery with tags {Tags}.", message.Tags);
        }
        catch (Exception exception)
        {
            // Anything escaping here completes ExecuteAsync's task, and the default
            // BackgroundServiceExceptionBehavior (StopHost) would take the whole process down. One bad
            // message must never do that, so the loop swallows everything and keeps draining.
            logger.LogError(exception, "Unexpected failure delivering an email with tags {Tags}.", message.Tags);
        }
    }
}
