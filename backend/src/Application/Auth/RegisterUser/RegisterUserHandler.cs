using Cinedex.Application.Abstractions;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Cinedex.Application.Auth.RegisterUser;

internal sealed class RegisterUserHandler(
    IIdentityService identityService,
    IValidator<RegisterUserCommand> validator,
    ILogger<RegisterUserHandler> logger) : IRegisterUserHandler
{
    public async Task<Guid> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        logger.LogInformation("Registering user {Email}.", command.Email);

        var user = await identityService.RegisterAsync(
            command.Email,
            command.UserName,
            command.Password,
            cancellationToken);

        logger.LogInformation("Registered user {UserId}.", user.Id);

        return user.Id;
    }
}
