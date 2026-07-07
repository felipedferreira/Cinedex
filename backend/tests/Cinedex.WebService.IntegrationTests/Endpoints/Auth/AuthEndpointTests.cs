using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cinedex.WebService.Contracts.Requests;
using Cinedex.WebService.Contracts.Responses;
using Cinedex.WebService.IntegrationTests.Constants;

namespace Cinedex.WebService.IntegrationTests.Auth;

[Collection(WebApplicationCollection.Name)]
public sealed class AuthEndpointTests(WebApplicationFixture fixture)
{
    private const string Password = "P@ssw0rd!";
    private const string NewPassword = "N3wP@ssw0rd!";

    [Fact]
    public async Task Register_WithValidRequest_Returns201()
    {
        var response = await RegisterAsync(NewEmail(), "newuser", Password);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400()
    {
        var email = NewEmail();
        var first = await RegisterAsync(email, "firstuser", Password);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await RegisterAsync(email, "seconduser", Password);

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400()
    {
        var response = await RegisterAsync(NewEmail(), "weakuser", "short");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        var email = NewEmail();
        await RegisterAsync(email, "loginuser", Password);

        var login = await LoginAsync(email, Password);

        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(login.RefreshToken));
        Assert.True(login.ExpiresAtUtc > DateTime.UtcNow);
        Assert.True(login.RefreshTokenExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var email = NewEmail();
        await RegisterAsync(email, "wrongpw", Password);

        var response = await fixture.Client.PostAsJsonAsync(
            TestRouteConstants.Auth.LoginEndpoint,
            new LoginRequest { Email = email, Password = "Wr0ng@Pass!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownUser_Returns401()
    {
        var response = await fixture.Client.PostAsJsonAsync(
            TestRouteConstants.Auth.LoginEndpoint,
            new LoginRequest { Email = NewEmail(), Password = Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithValidToken_RotatesTokensAndInvalidatesOld()
    {
        var email = NewEmail();
        await RegisterAsync(email, "refreshuser", Password);
        var login = await LoginAsync(email, Password);

        var refreshResponse = await fixture.Client.PostAsJsonAsync(
            TestRouteConstants.Auth.RefreshEndpoint,
            new RefreshRequest { RefreshToken = login.RefreshToken });

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var rotated = await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(rotated);
        Assert.NotEqual(login.RefreshToken, rotated.RefreshToken);

        // The original refresh token must no longer be accepted after rotation.
        var reuse = await fixture.Client.PostAsJsonAsync(
            TestRouteConstants.Auth.RefreshEndpoint,
            new RefreshRequest { RefreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_Returns401()
    {
        var response = await fixture.Client.PostAsJsonAsync(
            TestRouteConstants.Auth.RefreshEndpoint,
            new RefreshRequest { RefreshToken = "not-a-real-token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutBearerToken_Returns401()
    {
        var response = await fixture.Client.PostAsJsonAsync(
            TestRouteConstants.Auth.LogoutEndpoint,
            new LogoutRequest { RefreshToken = "anything" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var email = NewEmail();
        await RegisterAsync(email, "logoutuser", Password);
        var login = await LoginAsync(email, Password);

        var logout = new HttpRequestMessage(HttpMethod.Post, TestRouteConstants.Auth.LogoutEndpoint)
        {
            Content = JsonContent.Create(new LogoutRequest { RefreshToken = login.RefreshToken }),
        };
        logout.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var logoutResponse = await fixture.Client.SendAsync(logout);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        // The revoked refresh token can no longer be exchanged.
        var refresh = await fixture.Client.PostAsJsonAsync(
            TestRouteConstants.Auth.RefreshEndpoint,
            new RefreshRequest { RefreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_WithUnknownEmail_Returns202()
    {
        var response = await fixture.Client.PostAsJsonAsync(
            TestRouteConstants.Auth.ForgotPasswordEndpoint,
            new ForgotPasswordRequest { Email = NewEmail() });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ChangesPassword()
    {
        var email = NewEmail();
        await RegisterAsync(email, "resetuser", Password);

        var forgot = await fixture.Client.PostAsJsonAsync(
            TestRouteConstants.Auth.ForgotPasswordEndpoint,
            new ForgotPasswordRequest { Email = email });
        Assert.Equal(HttpStatusCode.Accepted, forgot.StatusCode);

        var resetToken = fixture.EmailSender.LastResetToken;
        Assert.False(string.IsNullOrWhiteSpace(resetToken));

        var reset = await fixture.Client.PostAsJsonAsync(
            TestRouteConstants.Auth.ResetPasswordEndpoint,
            new ResetPasswordRequest { Email = email, ResetToken = resetToken!, NewPassword = NewPassword });
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        // New password works, old password is rejected.
        var loginNew = await LoginAsync(email, NewPassword);
        Assert.False(string.IsNullOrWhiteSpace(loginNew.AccessToken));

        var loginOld = await fixture.Client.PostAsJsonAsync(
            TestRouteConstants.Auth.LoginEndpoint,
            new LoginRequest { Email = email, Password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, loginOld.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_Returns400()
    {
        var email = NewEmail();
        await RegisterAsync(email, "badreset", Password);

        var response = await fixture.Client.PostAsJsonAsync(
            TestRouteConstants.Auth.ResetPasswordEndpoint,
            new ResetPasswordRequest { Email = email, ResetToken = "invalid-token", NewPassword = NewPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private async Task<HttpResponseMessage> RegisterAsync(string email, string username, string password) =>
        await fixture.Client.PostAsJsonAsync(
            TestRouteConstants.Auth.RegisterEndpoint,
            new RegisterRequest { Email = email, Username = username, Password = password });

    private async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var response = await fixture.Client.PostAsJsonAsync(
            TestRouteConstants.Auth.LoginEndpoint,
            new LoginRequest { Email = email, Password = password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        return login;
    }
}
