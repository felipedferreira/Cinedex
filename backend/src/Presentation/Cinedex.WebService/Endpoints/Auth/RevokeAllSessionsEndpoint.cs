using Cinedex.Application.Auth.RevokeAllSessions;
using Cinedex.WebService.Constants;
using Cinedex.WebService.Extensions;
using Cinedex.WebService.Http;
using FastEndpoints;

namespace Cinedex.WebService.Endpoints.Auth;

/// <summary>
/// Ends every session the caller has, on every device — the sign-out-everywhere control.
/// </summary>
/// <remarks>
/// No <c>AllowAnonymous()</c>, which is what secures this: FastEndpoints requires an authenticated
/// caller unless an endpoint opts out. The bearer is also the only input — there is no body, no
/// route parameter and no query string — so the set of sessions this can reach is fixed by the token
/// the service itself signed.
/// </remarks>
internal sealed class RevokeAllSessionsEndpoint(
    IRevokeAllSessionsHandler handler,
    RefreshTokenCookie refreshTokenCookie) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete(ApiConstants.Auth.RevokeAllSessionsRoute);
        Tags(ApiConstants.Auth.Tag);
        Description(b => b.Produces(StatusCodes.Status204NoContent));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        // Unreadable in practice — the endpoint is unreachable without a token this service signed —
        // and not exceptional if it ever were: with no subject there is no scope, so there is nothing
        // to revoke. Skipping the handler and falling through to the same exit keeps "always clear
        // the cookie" true, the shape LogoutEndpoint already uses.
        if (User.TryGetUserId(out var requestingUserId))
        {
            await handler.HandleAsync(new RevokeAllSessionsCommand(requestingUserId), cancellationToken);
        }

        // The caller's own cookie is one of the ones just revoked, so evict it rather than leave the
        // browser re-presenting a corpse. Unconditional, because the endpoint is idempotent and a
        // caller that sent no cookie is unharmed by being told to clear one.
        refreshTokenCookie.Clear(HttpContext.Response);

        await Send.NoContentAsync(cancellationToken);
    }
}
