using System.Net;
using System.Net.Http.Json;
using Cinedex.WebService.Contracts.Requests;
using Cinedex.WebService.Contracts.Responses;
using Cinedex.WebService.IntegrationTests.Constants;

namespace Cinedex.WebService.IntegrationTests.Auth;

[Collection(WebApplicationCollection.Name)]
public sealed class AuthEndpointTests(WebApplicationFixture fixture)
{
    [Fact]
    public async Task Register_WithValidRequest_Returns201()
    {
        var request = new RegisterRequest
        {
            Email = "new.user@example.com",
            Username = "newuser",
            Password = "P@ssw0rd!",
        };

        var response = await fixture.Client.PostAsJsonAsync(TestRouteConstants.Auth.RegisterEndpoint, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidRequest_Returns200WithAccessToken()
    {
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "P@ssw0rd!",
        };

        var response = await fixture.Client.PostAsJsonAsync(TestRouteConstants.Auth.LoginEndpoint, request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.True(login.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Logout_Returns204()
    {
        var response = await fixture.Client.PostAsync(TestRouteConstants.Auth.LogoutEndpoint, content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_WithValidRequest_Returns202()
    {
        var request = new ForgotPasswordRequest { Email = "user@example.com" };

        var response = await fixture.Client.PostAsJsonAsync(TestRouteConstants.Auth.ForgotPasswordEndpoint, request);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithValidRequest_Returns204()
    {
        var request = new ResetPasswordRequest
        {
            Email = "user@example.com",
            ResetToken = "stub-reset-token",
            NewPassword = "N3wP@ssw0rd!",
        };

        var response = await fixture.Client.PostAsJsonAsync(TestRouteConstants.Auth.ResetPasswordEndpoint, request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
