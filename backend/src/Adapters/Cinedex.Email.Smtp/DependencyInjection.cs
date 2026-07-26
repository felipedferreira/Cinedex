using Cinedex.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;

namespace Cinedex.Email.Smtp;

public static class DependencyInjection
{
    public static IServiceCollection AddEmailAdapter(this IServiceCollection services)
    {
        services.AddOptions<SmtpOptions>()
            .BindConfiguration(SmtpOptions.SectionName)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Host),
                $"{SmtpOptions.SectionName}:Host is required.")
            .Validate(
                options => options.Port is > 0 and <= 65_535,
                $"{SmtpOptions.SectionName}:Port must be between 1 and 65535.")
            .Validate(
                options =>
                    options.FromAddress.Contains('@', StringComparison.Ordinal) &&
                    MailboxAddress.TryParse(options.FromAddress, out _),
                $"{SmtpOptions.SectionName}:FromAddress must be a valid email address.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Username),
                $"{SmtpOptions.SectionName}:Username is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Password),
                $"{SmtpOptions.SectionName}:Password is required.")
            .Validate(
                options => Enum.IsDefined(options.SecureSocketOptions),
                $"{SmtpOptions.SectionName}:SecureSocketOptions is not supported.")
            .ValidateOnStart();

        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        return services;
    }
}
