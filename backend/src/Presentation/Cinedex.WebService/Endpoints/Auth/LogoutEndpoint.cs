using Cinedex.WebService.Constants;
using FastEndpoints;

namespace Cinedex.WebService.Endpoints.Auth;

internal sealed class LogoutEndpoint : EndpointWithoutRequest<EmptyResponse>
{
    public override void Configure()
    {
        Post(ApiConstants.Auth.LogoutRoute);
        Tags(ApiConstants.Auth.Tag);
        AllowAnonymous();
        Description(b => b.Produces(StatusCodes.Status204NoContent));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        // Stub - implementation pending
        await Send.NoContentAsync(cancellationToken);
    }
}
