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
}
