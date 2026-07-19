namespace Cinedex.Application.Email;

/// <summary>
/// A file attached to an <see cref="EmailMessage"/>.
/// </summary>
/// <param name="FileName">The attachment's file name.</param>
/// <param name="ContentType">The MIME content type (e.g. <c>application/pdf</c>).</param>
/// <param name="Content">The raw attachment bytes.</param>
public sealed record EmailAttachment(string FileName, string ContentType, ReadOnlyMemory<byte> Content);