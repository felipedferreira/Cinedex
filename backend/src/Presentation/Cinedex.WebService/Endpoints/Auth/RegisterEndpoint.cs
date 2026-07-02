using Cinedex.WebService.Constants;
using Cinedex.WebService.Contracts.Requests;
using FastEndpoints;

namespace Cinedex.WebService.Endpoints.Auth;

internal sealed class RegisterEndpoint : Endpoint<RegisterRequest, EmptyResponse>
{
    public override void Configure()
    {
        Post(ApiConstants.Auth.RegisterRoute);
        Tags(ApiConstants.Auth.Tag);
        AllowAnonymous();
        Description(b => b.Produces(StatusCodes.Status201Created));
    }

    public override async Task HandleAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        // Stub - implementation pending
        await Send.ResultAsync(TypedResults.Created());
    }
}
