namespace Cinedex.Application.Email;

/// <summary>
/// An image embedded in an HTML email body and referenced from the markup by a
/// <c>cid:</c> URI, so it renders without a remote request.
/// </summary>
/// <param name="ContentId">The Content-ID the markup references (without the <c>cid:</c> scheme).</param>
/// <param name="MediaType">The MIME media type, for example <c>image/png</c>.</param>
/// <param name="Content">The raw image bytes.</param>
public sealed record InlineImage(string ContentId, string MediaType, ReadOnlyMemory<byte> Content);
