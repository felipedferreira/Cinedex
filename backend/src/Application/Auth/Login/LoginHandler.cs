using Cinedex.Application.Abstractions;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Cinedex.Application.Auth.Login;

internal sealed class LoginHandler(
    IIdentityService identityService,
    ITokenService tokenService,
    IValidator<LoginCommand> validator,
    ILogger<LoginHandler> logger) : ILoginHandler
{
    public async Task<AuthTokensDto> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var user = await identityService.ValidateCredentialsAsync(
            command.Email,
            command.Password,
            cancellationToken);

        logger.LogInformation("User {UserId} logged in.", user.Id);

        return await tokenService.IssueTokensAsync(user, cancellationToken);
    }
}
