namespace Cinedex.Application.Auth.RegisterUser;

public sealed record RegisterUserCommand(string Email, string UserName, string Password);