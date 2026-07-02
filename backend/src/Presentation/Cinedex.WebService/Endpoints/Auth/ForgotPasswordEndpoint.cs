using Cinedex.WebService.Constants;
using Cinedex.WebService.Contracts.Requests;
using FastEndpoints;

namespace Cinedex.WebService.Endpoints.Auth;

internal sealed class ForgotPasswordEndpoint : Endpoint<ForgotPasswordRequest, EmptyResponse>
{
    public override void Configure()
    {
        Post(ApiConstants.Auth.ForgotPasswordRoute);
        Tags(ApiConstants.Auth.Tag);
        AllowAnonymous();
        Description(b => b.Produces(StatusCodes.Status202Accepted));
    }

    public override async Task HandleAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        // Stub - implementation pending
        await Send.ResultAsync(TypedResults.Accepted((string?)null));
    }
}
