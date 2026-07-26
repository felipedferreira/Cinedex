using Cinedex.Application.Email;

namespace Cinedex.Application.Abstractions;

/// <summary>
/// Port for handing a transactional email off for out-of-band delivery. Unlike
/// <see cref="IEmailSender"/>, which performs the delivery, this port only accepts the message and
/// returns; delivery happens later on a background worker and its outcome is never reported back.
/// </summary>
/// <remarks>
/// The method is deliberately synchronous, returns <see langword="void"/>, and never throws, so a
/// caller can neither await delivery nor branch on its outcome. <c>POST /auth/password/forgot</c>
/// depends on that: any observable difference between the known-account and unknown-account paths —
/// latency included — is an account-enumeration oracle.
/// </remarks>
public interface IEmailDispatcher
{
    /// <summary>
    /// Accepts <paramref name="message"/> for delivery and returns immediately.
    /// </summary>
    /// <param name="message">The fully composed message to deliver.</param>
    void Enqueue(EmailMessage message);
}
