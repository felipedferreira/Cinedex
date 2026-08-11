using System.IdentityModel.Tokens.Jwt;
using Cinedex.Application.Auth.Logout;
using Cinedex.WebService.Constants;
using Cinedex.WebService.Http;
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
        var subClaim = User.FindFirst(JwtRegisteredClaimNames.Sub);
        if (subClaim is null)
        {
            await Send.NoContentAsync(cancellationToken);
            return;
        }

        if (!Guid.TryParseExact(subClaim.Value, "D", out var requestingUserId))
        {
            await Send.NoContentAsync(cancellationToken);
            return;
        }

        var refreshToken = RefreshTokenCookie.Read(HttpContext.Request);
        if (refreshToken is not null)
        {
            await handler.HandleAsync(new LogoutCommand(requestingUserId, refreshToken), cancellationToken);
        }

        // Always clear, even when no cookie was presented: logout is idempotent.
        RefreshTokenCookie.Clear(HttpContext.Response);

        await Send.NoContentAsync(cancellationToken);
    }
}
