using Cinedex.Application.Auth;
using Cinedex.Application.Auth.RefreshToken;
using Cinedex.Application.Exceptions;
using Cinedex.WebService.Constants;
using Cinedex.WebService.Contracts.Responses;
using Cinedex.WebService.Http;
using FastEndpoints;

namespace Cinedex.WebService.Endpoints.Auth;

internal sealed class RefreshEndpoint(IRefreshTokenHandler handler) : EndpointWithoutRequest<LoginResponse>
{
    public override void Configure()
    {
        Post(ApiConstants.Auth.RefreshRoute);
        Tags(ApiConstants.Auth.Tag);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        // Identical message to the one the token service throws, so a missing cookie is
        // indistinguishable from an invalid one.
        var refreshToken = RefreshTokenCookie.Read(HttpContext.Request)
            ?? throw new InvalidCredentialsException("The refresh token is invalid or has expired.");

        AuthTokensDto tokens;
        try
        {
            tokens = await handler.HandleAsync(new RefreshTokenCommand(refreshToken), cancellationToken);
        }
        catch (InvalidCredentialsException)
        {
            // The presented token is known-dead. Evict it so the browser stops re-sending a corpse.
            // Registered via OnStarting rather than written now: rethrowing runs UseExceptionHandler,
            // which calls Response.Clear() and would wipe a header set here. OnStarting callbacks fire
            // at header-flush time, after the exception handler has written its 401.
            var response = HttpContext.Response;
            response.OnStarting(() =>
            {
                RefreshTokenCookie.Clear(response);
                return Task.CompletedTask;
            });
            throw;
        }

        RefreshTokenCookie.Append(HttpContext.Response, tokens.RefreshToken, tokens.RefreshTokenExpiresAtUtc);

        await Send.OkAsync(tokens.ToResponse(), cancellationToken);
    }
}