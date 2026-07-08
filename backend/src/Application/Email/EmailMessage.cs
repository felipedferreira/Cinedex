namespace Cinedex.Application.Email;

/// <summary>
/// A transactional email to be delivered by an <see cref="Abstractions.IEmailSender"/>. Composed in
/// the application layer; the adapter only delivers it.
/// </summary>
public sealed record EmailMessage
{
    /// <summary>Gets the recipient.</summary>
    public required EmailRecipient To { get; init; }

    /// <summary>Gets the subject line.</summary>
    public required string Subject { get; init; }

    /// <summary>Gets the body. Its concrete type selects the format (HTML, plain text, …).</summary>
    public required EmailBody Body { get; init; }

    /// <summary>Gets the attachments, if any.</summary>
    public IReadOnlyList<EmailAttachment> Attachments { get; init; } = [];

    /// <summary>Gets the classification tags (e.g. "password-reset") for logging and analytics.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}
