using Cinedex.Application.Email;

namespace Cinedex.WebService.IntegrationTests.Email;

public sealed class CinedexEmailLayoutTests
{
    [Fact]
    public void HtmlEmailBody_WithoutInlineImages_ExposesEmptyCollection()
    {
        var body = new HtmlEmailBody("<p>hello</p>", "hello");

        Assert.Empty(body.InlineImages);
    }

    [Fact]
    public void HtmlEmailBody_WithInlineImage_ExposesIt()
    {
        var image = new InlineImage("logo", "image/png", new byte[] { 1, 2, 3 });

        var body = new HtmlEmailBody("<p>hello</p>", "hello") { InlineImages = [image] };

        Assert.Equal("logo", Assert.Single(body.InlineImages).ContentId);
    }

    [Fact]
    public void EmailAssets_Logo_ResolvesFromTheAssembly()
    {
        var logo = EmailAssets.Logo();

        Assert.Equal("cinedex-logo", logo.ContentId);
        Assert.Equal("image/png", logo.MediaType);
        Assert.NotEmpty(logo.Content.ToArray());
    }

    [Fact]
    public void Render_EncodesAmpersandsInTheButtonHref()
    {
        var body = CinedexEmailLayout.Render(SampleContent());

        Assert.Contains(
            "href=\"https://localhost:9000/reset-password?email=a%40b.com&amp;token=abc123\"",
            body.Content,
            StringComparison.Ordinal);
        Assert.DoesNotContain("com&token=", body.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_ReferencesTheAttachedLogoByContentId()
    {
        var body = CinedexEmailLayout.Render(SampleContent());

        var logo = Assert.Single(body.InlineImages);
        Assert.Contains($"cid:{logo.ContentId}", body.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_KeepsTheRawUrlInThePlainTextFallback()
    {
        var body = CinedexEmailLayout.Render(SampleContent());

        Assert.Contains(
            "https://localhost:9000/reset-password?email=a%40b.com&token=abc123",
            body.PlainTextFallback!,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WithFootnote_RendersTheFootnoteText()
    {
        var body = CinedexEmailLayout.Render(SampleContent());

        Assert.Contains("This link expires in 1 hour.", body.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WithoutFootnote_OmitsTheFootnoteText()
    {
        var content = SampleContent() with { FootnoteHtml = null };

        var body = CinedexEmailLayout.Render(content);

        Assert.DoesNotContain("This link expires in 1 hour.", body.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_ProducesTheExpectedStructuralPieces()
    {
        var body = CinedexEmailLayout.Render(SampleContent());

        Assert.Contains("background-color:#4d181b", body.Content, StringComparison.Ordinal);
        Assert.Contains("background-color:#c44b43", body.Content, StringComparison.Ordinal);
        Assert.Contains("background-color:#e0776f", body.Content, StringComparison.Ordinal);
        Assert.Contains("width=\"160\" height=\"32\"", body.Content, StringComparison.Ordinal);
    }

    private static EmailLayoutContent SampleContent() => new(
        Heading: "Reset your password",
        IntroHtml: "We received a request to reset the password for your Cinedex account.",
        ButtonLabel: "Reset password",
        ButtonUrl: "https://localhost:9000/reset-password?email=a%40b.com&token=abc123",
        FootnoteHtml: "This link expires in 1 hour.",
        PlainTextBody: "Reset your password: https://localhost:9000/reset-password?email=a%40b.com&token=abc123");
}
