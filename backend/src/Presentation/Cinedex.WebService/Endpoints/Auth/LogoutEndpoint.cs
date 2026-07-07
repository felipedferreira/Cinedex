using Cinedex.Application.Auth.Logout;
using Cinedex.WebService.Constants;
using Cinedex.WebService.Contracts.Requests;
using FastEndpoints;

namespace Cinedex.WebService.Endpoints.Auth;

internal sealed class LogoutEndpoint(ILogoutHandler handler) : Endpoint<LogoutRequest>
{
    public override void Configure()
    {
        Post(ApiConstants.Auth.LogoutRoute);
        Tags(ApiConstants.Auth.Tag);
        Description(b => b.Produces(StatusCodes.Status204NoContent));
    }

    public override async Task HandleAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(request.ToCommand(), cancellationToken);

        await Send.NoContentAsync(cancellationToken);
    }
}
