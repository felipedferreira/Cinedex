using MailKit.Security;

namespace Cinedex.Email.Smtp;

internal sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public string? FromName { get; set; }

    public SecureSocketOptions SecureSocketOptions { get; set; } = SecureSocketOptions.Auto;
}
