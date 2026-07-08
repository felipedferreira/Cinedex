namespace Cinedex.Application.Configuration;

/// <summary>
/// Points at the single-page app. Used to build user-facing links (e.g. the password-reset link)
/// that are emailed to users. Bound from the <c>Frontend</c> configuration section.
/// </summary>
public sealed class FrontendOptions
{
    /// <summary>The configuration section this binds from.</summary>
    public const string SectionName = "Frontend";

    /// <summary>Gets or sets the SPA's base URL, e.g. <c>https://app.cinedex.com</c>.</summary>
    public string BaseUrl { get; set; } = string.Empty;
}
