namespace Cinedex.Application.Email;

/// <summary>
/// An HTML body, with an optional plain-text fallback for clients that cannot render HTML.
/// </summary>
/// <param name="Content">The HTML content.</param>
/// <param name="PlainTextFallback">An optional plain-text alternative sent alongside the HTML.</param>
public sealed record HtmlEmailBody(string Content, string? PlainTextFallback = null) : EmailBody
{
    /// <summary>Gets the images embedded in the markup, each referenced by a <c>cid:</c> URI.</summary>
    public IReadOnlyList<InlineImage> InlineImages { get; init; } = [];
}