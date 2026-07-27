using System.Reflection;

namespace Cinedex.Application.Email;

// Binary assets embedded in this assembly and attached to outgoing mail. A missing resource would
// throw while composing, and EmailDeliveryWorker logs delivery failures without surfacing them, so
// a rename here would silently stop password-reset email. CinedexEmailLayoutTests is the guard.
internal static class EmailAssets
{
    /// <summary>The Content-ID the layout markup references as <c>cid:cinedex-logo</c>.</summary>
    public const string LogoContentId = "cinedex-logo";

    /// <summary>The media type of the embedded logo.</summary>
    public const string LogoMediaType = "image/png";

    private const string LogoResourceName = "Cinedex.Application.Email.Assets.cinedex-logo.png";

    private static readonly Lazy<ReadOnlyMemory<byte>> LogoBytes = new(LoadLogo);

    /// <summary>Gets the Cinedex wordmark as an inline image ready to attach to an HTML body.</summary>
    /// <returns>An <see cref="InlineImage"/> for the embedded logo, referenced by <see cref="LogoContentId"/>.</returns>
    public static InlineImage Logo() => new(LogoContentId, LogoMediaType, LogoBytes.Value);

    private static ReadOnlyMemory<byte> LoadLogo()
    {
        var assembly = typeof(EmailAssets).GetTypeInfo().Assembly;
        using var stream = assembly.GetManifestResourceStream(LogoResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded email asset '{LogoResourceName}' was not found in assembly " +
                $"'{assembly.GetName().Name}'. Check the EmbeddedResource LogicalName in the csproj.");

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
