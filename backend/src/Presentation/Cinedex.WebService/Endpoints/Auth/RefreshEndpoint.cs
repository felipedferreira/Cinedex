using Cinedex.Application.Auth.RefreshToken;
using Cinedex.WebService.Constants;
using Cinedex.WebService.Contracts.Requests;
using Cinedex.WebService.Contracts.Responses;
using FastEndpoints;

namespace Cinedex.WebService.Endpoints.Auth;

internal sealed class RefreshEndpoint(IRefreshTokenHandler handler) : Endpoint<RefreshRequest, LoginResponse>
{
    public override void Configure()
    {
        Post(ApiConstants.Auth.RefreshRoute);
        Tags(ApiConstants.Auth.Tag);
        AllowAnonymous();
    }

    public override async Task HandleAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var tokens = await handler.HandleAsync(request.ToCommand(), cancellationToken);

        await Send.OkAsync(tokens.ToResponse(), cancellationToken);
    }
}
