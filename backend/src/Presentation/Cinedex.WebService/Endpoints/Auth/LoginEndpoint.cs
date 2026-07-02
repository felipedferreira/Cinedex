using Cinedex.WebService.Constants;
using Cinedex.WebService.Contracts.Requests;
using Cinedex.WebService.Contracts.Responses;
using FastEndpoints;

namespace Cinedex.WebService.Endpoints.Auth;

internal sealed class LoginEndpoint : Endpoint<LoginRequest, LoginResponse>
{
    public override void Configure()
    {
        Post(ApiConstants.Auth.LoginRoute);
        Tags(ApiConstants.Auth.Tag);
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        // Stub - implementation pending
        var response = new LoginResponse
        {
            AccessToken = "stub-access-token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
        };

        await Send.OkAsync(response, cancellationToken);
    }
}
