using Cinedex.Application.Auth;
using Cinedex.Application.Auth.ForgotPassword;
using Cinedex.Application.Auth.Login;
using Cinedex.Application.Auth.RegisterUser;
using Cinedex.Application.Auth.ResetPassword;
using Oceanus.WebService.Contracts.Requests;
using Oceanus.WebService.Contracts.Responses;

namespace Cinedex.WebService.Endpoints.Auth;

internal static class AuthMappings
{
    public static RegisterUserCommand ToCommand(this RegisterRequest request) =>
        new(request.Email, request.Username, request.Password);

    public static LoginCommand ToCommand(this LoginRequest request) =>
        new(request.Email, request.Password);

    public static ForgotPasswordCommand ToCommand(this ForgotPasswordRequest request) =>
        new(request.Email);

    public static ResetPasswordCommand ToCommand(this ResetPasswordRequest request) =>
        new(request.Email, request.ResetToken, request.NewPassword);

    // The refresh token is deliberately absent: it travels only as an HttpOnly cookie.
    public static LoginResponse ToResponse(this AuthTokensDto tokens) => new()
    {
        AccessToken = tokens.AccessToken,
        ExpiresAtUtc = tokens.ExpiresAtUtc,
    };
}