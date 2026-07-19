using Cinedex.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace Cinedex.Auth.Identity;

internal sealed class EmailSenderAdapter : IEmailSender<ApplicationUser>
{
    private readonly IEmailSender _emailSender;

    public EmailSenderAdapter(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public Task SendEmailAsync(ApplicationUser user, string subject, string htmlMessage)
    {
        return _emailSender.SendEmailAsync(user.Email!, subject, htmlMessage);
    }
}
