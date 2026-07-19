using Cinedex.Application.Email;

namespace Cinedex.Application.Abstractions;

/// <summary>
/// Port for delivering transactional emails. A thin transport: it delivers a fully composed
/// <see cref="EmailMessage"/> and knows nothing about what the message is for. Composition (subject,
/// body, links) is an application concern and happens before the message reaches this port.
/// </summary>
/// <remarks>
/// No real mail provider is wired up yet: the <c>Cinedex.Email.Smtp</c> adapter registers a no-op
/// implementation, so email is not delivered end to end. See <c>docs/auth-security-model.md</c>.
/// </remarks>
public interface IEmailSender
{
    /// <summary>
    /// Delivers the supplied message.
    /// </summary>
    /// <param name="message">The fully composed message to deliver.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that completes when the message has been handed to the mail provider.</returns>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}