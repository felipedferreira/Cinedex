using Cinedex.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Cinedex.Email.Smtp;

public static class DependencyInjection
{
    public static IServiceCollection AddEmailAdapter(this IServiceCollection services)
    {
        // TODO: swap NoOpEmailSender for a MailKit SmtpEmailSender once email delivery is wired up.
        services.AddSingleton<IEmailSender, NoOpEmailSender>();

        return services;
    }
}
