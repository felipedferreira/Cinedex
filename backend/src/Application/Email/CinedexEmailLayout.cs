using System.Net;

namespace Cinedex.Application.Email;

/// <summary>The "Marquee" shell: header band, crimson rule, body card, filled call to action.</summary>
//
// Deliberately 2005-era markup — tables, inline styles, solid hex. Mail clients do not support
// flexbox or grid, Outlook's Word engine ignores rgba and drops backgrounds on block elements, and
// web fonts do not load. See the design spec before modernising any of it.
//
// No caller data is interpolated into the markup beyond the URL, which is HTML-encoded, so the body
// is injection-free by construction rather than by careful escaping.
internal static class CinedexEmailLayout
{
    /// <summary>Renders the branded shell around <paramref name="content"/>, logo attached.</summary>
    /// <param name="content">The content slots to render into the shell.</param>
    /// <returns>An <see cref="HtmlEmailBody"/> with the rendered markup, plain-text fallback, and logo attached.</returns>
    public static HtmlEmailBody Render(EmailLayoutContent content)
    {
        var logo = EmailAssets.Logo();
        var href = WebUtility.HtmlEncode(content.ButtonUrl);
        var heading = WebUtility.HtmlEncode(content.Heading);
        var buttonLabel = WebUtility.HtmlEncode(content.ButtonLabel);
        var footnote = content.FootnoteHtml is null
            ? string.Empty
            : $"""<p style="margin:24px 0 0;color:#e0776f;font-size:13px">{content.FootnoteHtml}</p>""";

        var html = $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width,initial-scale=1" />
            <meta name="color-scheme" content="dark" />
            <title>{heading}</title>
            </head>
            <body style="margin:0;padding:24px 0;background-color:#23090b">
            <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%" style="background-color:#23090b">
            <tr><td align="center" bgcolor="#23090b">
            <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="560" style="width:560px;max-width:560px;font-family:-apple-system,'Segoe UI',Roboto,Helvetica,Arial,sans-serif">
            <tr><td align="center" style="background-color:#4d181b;padding:22px 32px">
            <img src="cid:{logo.ContentId}" width="160" height="32" alt="Cinedex" style="display:block;border:0;outline:none;text-decoration:none;color:#f3ece6;font-size:20px" />
            </td></tr>
            <tr><td style="background-color:#c44b43;height:3px;font-size:0;line-height:0">&nbsp;</td></tr>
            <tr><td style="background-color:#2f0e11;padding:36px 32px 30px">
            <p style="margin:0 0 14px;color:#f3ece6;font-size:26px;line-height:1.25">{heading}</p>
            <p style="margin:0 0 26px;color:#c29d97;font-size:15px;line-height:1.65">{content.IntroHtml}</p>
            <table role="presentation" cellpadding="0" cellspacing="0" border="0"><tr>
            <td style="background-color:#e0776f;border-radius:6px;padding:14px 34px"><a href="{href}" style="display:block;color:#23090b;font-size:15px;text-decoration:none">{buttonLabel}</a></td>
            </tr></table>
            {footnote}
            </td></tr>
            <tr><td style="background-color:#2f0e11;padding:0 32px 28px">
            <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%">
            <tr><td style="border-top:1px solid #3d1518;padding-top:18px">
            <p style="margin:0 0 10px;color:#c29d97;font-size:12px;line-height:1.6">If the button does not work, paste this into your browser:</p>
            <p style="margin:0;color:#8f6b67;font-size:11px;word-break:break-all">{href}</p>
            </td></tr></table>
            </td></tr>
            <tr><td align="center" style="padding:18px 32px 24px">
            <p style="margin:0;color:#8f6b67;font-size:12px;line-height:1.6">Didn't request this? Ignore this email &#8212; your password won't change.</p>
            </td></tr>
            </table>
            </td></tr>
            </table>
            </body>
            </html>
            """;

        return new HtmlEmailBody(html, content.PlainTextBody) { InlineImages = [logo] };
    }
}
