namespace Cinedex.Application.Email;

/// <summary>The content slots the branded email shell renders around.</summary>
/// <param name="Heading">The headline, plain text.</param>
/// <param name="IntroHtml">The explanatory paragraph. Trusted markup — never interpolate user data.</param>
/// <param name="ButtonLabel">The call-to-action label, plain text.</param>
/// <param name="ButtonUrl">The call-to-action target. HTML-encoded before it reaches the markup.</param>
/// <param name="FootnoteHtml">Optional accent line under the button, for example an expiry notice.</param>
/// <param name="PlainTextBody">The complete plain-text alternative.</param>
internal sealed record EmailLayoutContent(
    string Heading,
    string IntroHtml,
    string ButtonLabel,
    string ButtonUrl,
    string? FootnoteHtml,
    string PlainTextBody);
