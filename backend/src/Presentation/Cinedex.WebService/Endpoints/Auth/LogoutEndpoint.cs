using Cinedex.Application.Auth.Logout;
using Cinedex.WebService.Constants;
using FastEndpoints;

namespace Cinedex.WebService.Endpoints.Auth;

internal sealed class LogoutEndpoint(ILogoutHandler handler) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post(ApiConstants.Auth.LogoutRoute);
        Tags(ApiConstants.Auth.Tag);
        Description(b => b.Produces(StatusCodes.Status204NoContent));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var refreshToken = RefreshTokenCookie.Read(HttpContext.Request);
        if (refreshToken is not null)
        {
            await handler.HandleAsync(new LogoutCommand(refreshToken), cancellationToken);
        }

        // Always clear, even when no cookie was presented: logout is idempotent.
        RefreshTokenCookie.Clear(HttpContext.Response);

        await Send.NoContentAsync(cancellationToken);
    }
}
