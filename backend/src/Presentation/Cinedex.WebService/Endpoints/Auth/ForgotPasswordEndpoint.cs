using Cinedex.Application.Auth.ForgotPassword;
using Cinedex.WebService.Constants;
using Cinedex.WebService.Contracts.Requests;
using FastEndpoints;

namespace Cinedex.WebService.Endpoints.Auth;

internal sealed class ForgotPasswordEndpoint(IForgotPasswordHandler handler) : Endpoint<ForgotPasswordRequest, EmptyResponse>
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
        await handler.HandleAsync(request.ToCommand(), cancellationToken);

        await Send.ResultAsync(TypedResults.Accepted((string?)null));
    }
}