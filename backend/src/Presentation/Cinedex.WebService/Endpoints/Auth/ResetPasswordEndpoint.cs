using Cinedex.Application.Auth.ResetPassword;
using Cinedex.WebService.Constants;
using FastEndpoints;
using FoundryOceanus.WebService.Contracts.Requests;

namespace Cinedex.WebService.Endpoints.Auth;

internal sealed class ResetPasswordEndpoint(IResetPasswordHandler handler) : Endpoint<ResetPasswordRequest, EmptyResponse>
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
        await handler.HandleAsync(request.ToCommand(), cancellationToken);

        await Send.NoContentAsync(cancellationToken);
    }
}