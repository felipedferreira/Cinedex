namespace Cinedex.Application.Email;

/// <summary>
/// The body of an <see cref="EmailMessage"/>. A closed set of variants (<see cref="HtmlEmailBody"/>,
/// <see cref="PlainTextEmailBody"/>); adapters pattern-match to translate it to the wire format.
/// </summary>
public abstract record EmailBody;