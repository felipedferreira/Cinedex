namespace Cinedex.Application.Email;

/// <summary>
/// A plain-text body.
/// </summary>
/// <param name="Content">The plain-text content.</param>
public sealed record PlainTextEmailBody(string Content) : EmailBody;
