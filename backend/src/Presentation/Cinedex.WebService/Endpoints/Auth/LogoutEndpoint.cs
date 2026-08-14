using Cinedex.Application.Auth.Logout;
using Cinedex.WebService.Constants;
using Cinedex.WebService.Extensions;
using Cinedex.WebService.Http;
using FastEndpoints;

namespace Cinedex.WebService.Endpoints.Auth;

internal sealed class LogoutEndpoint(ILogoutHandler handler, RefreshTokenCookie refreshTokenCookie) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post(ApiConstants.Auth.LogoutRoute);
        Tags(ApiConstants.Auth.Tag);
        Description(b => b.Produces(StatusCodes.Status204NoContent));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var refreshToken = refreshTokenCookie.Read(HttpContext.Request);

        // Neither condition is exceptional. A client that logs out twice presents no cookie the
        // second time, and the endpoint is unreachable without a token this service signed, so the
        // subject is always readable in practice. Neither changes the response, so neither gets a
        // branch of its own: the single exit below is what keeps "always clear the cookie" true.
        if (refreshToken is not null && User.TryGetUserId(out var requestingUserId))
        {
            await handler.HandleAsync(new LogoutCommand(requestingUserId, refreshToken), cancellationToken);
        }

        // Always clear, even when no cookie was presented: logout is idempotent.
        refreshTokenCookie.Clear(HttpContext.Response);

        await Send.NoContentAsync(cancellationToken);
    }
}
