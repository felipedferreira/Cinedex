using Cinedex.Application.Auth.Login;
using Cinedex.WebService.Constants;
using Cinedex.WebService.Http;
using FastEndpoints;
using FoundryOceanus.WebService.Contracts.Requests;
using FoundryOceanus.WebService.Contracts.Responses;

namespace Cinedex.WebService.Endpoints.Auth;

internal sealed class LoginEndpoint(ILoginHandler handler, RefreshTokenCookie refreshTokenCookie)
    : Endpoint<LoginRequest, LoginResponse>
{
    public override void Configure()
    {
        Post(ApiConstants.Auth.LoginRoute);
        Tags(ApiConstants.Auth.Tag);
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var tokens = await handler.HandleAsync(request.ToCommand(), cancellationToken);

        // The refresh token leaves the service only as an HttpOnly cookie, never in the body.
        refreshTokenCookie.Append(HttpContext.Response, tokens.RefreshToken, tokens.RefreshTokenExpiresAtUtc);

        await Send.OkAsync(tokens.ToResponse(), cancellationToken);
    }
}
