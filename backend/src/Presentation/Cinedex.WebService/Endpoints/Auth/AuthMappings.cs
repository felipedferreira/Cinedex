using Cinedex.Application.Auth;
using Cinedex.Application.Auth.ForgotPassword;
using Cinedex.Application.Auth.Login;
using Cinedex.Application.Auth.Logout;
using Cinedex.Application.Auth.RefreshToken;
using Cinedex.Application.Auth.RegisterUser;
using Cinedex.Application.Auth.ResetPassword;
using Cinedex.WebService.Contracts.Requests;
using Cinedex.WebService.Contracts.Responses;

namespace Cinedex.WebService.Endpoints.Auth;

internal static class AuthMappings
{
    public static RegisterUserCommand ToCommand(this RegisterRequest request) =>
        new(request.Email, request.Username, request.Password);

    public static LoginCommand ToCommand(this LoginRequest request) =>
        new(request.Email, request.Password);

    public static RefreshTokenCommand ToCommand(this RefreshRequest request) =>
        new(request.RefreshToken);

    public static LogoutCommand ToCommand(this LogoutRequest request) =>
        new(request.RefreshToken);

    public static ForgotPasswordCommand ToCommand(this ForgotPasswordRequest request) =>
        new(request.Email);

    public static ResetPasswordCommand ToCommand(this ResetPasswordRequest request) =>
        new(request.Email, request.ResetToken, request.NewPassword);

    public static LoginResponse ToResponse(this AuthTokensDto tokens) => new()
    {
        AccessToken = tokens.AccessToken,
        ExpiresAtUtc = tokens.ExpiresAtUtc,
        RefreshToken = tokens.RefreshToken,
        RefreshTokenExpiresAtUtc = tokens.RefreshTokenExpiresAtUtc,
    };
}
