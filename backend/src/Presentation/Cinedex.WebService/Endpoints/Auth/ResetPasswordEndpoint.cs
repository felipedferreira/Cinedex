using Cinedex.WebService.Constants;
using Cinedex.WebService.Contracts.Requests;
using FastEndpoints;

namespace Cinedex.WebService.Endpoints.Auth;

internal sealed class ResetPasswordEndpoint : Endpoint<ResetPasswordRequest, EmptyResponse>
{
    public override void Configure()
    {
        Post(ApiConstants.Auth.ResetPasswordRoute);
        Tags(ApiConstants.Auth.Tag);
        AllowAnonymous();
        Description(b => b.Produces(StatusCodes.Status204NoContent));
    }

    public override async Task HandleAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        // Stub - implementation pending
        await Send.NoContentAsync(cancellationToken);
    }
}
