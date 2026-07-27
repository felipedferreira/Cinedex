using System.Reflection;

namespace Cinedex.Application.Email;

// Binary assets embedded in this assembly and attached to outgoing mail. A missing resource throws
// while composing, which happens on the request thread before the message is enqueued: the caller
// gets a 500 while the unknown-email path still answers 202, so a rename here would turn
// password/forgot into an account-enumeration oracle rather than fail quietly. Lazy<T> caches the
// thrown exception, so it would stay broken for the life of the process. The EmbeddedResource
// LogicalName in the csproj and CinedexEmailLayoutTests are the guards.
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
