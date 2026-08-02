using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cinedex.WebService.Contracts.Requests;
using Cinedex.WebService.Contracts.Responses;
using Cinedex.WebService.IntegrationTests.Constants;

namespace Cinedex.WebService.IntegrationTests.Auth;

/// <summary>
/// Proves the auth adapter really resolves two connection strings rather than quietly using one, by
/// running a host whose read-only connection goes nowhere and watching which endpoints still work.
/// </summary>
/// <remarks>
/// The fallback case needs no test of its own: every other test in this project runs without
/// <c>ConnectionStrings:ReadOnlyConnection</c> set, so their passing is the assertion that an absent
/// key still resolves to <c>DefaultConnection</c>.
/// </remarks>
public sealed class RefreshTokenConnectionSplitTests(ReadOnlyConnectionFixture fixture)
    : IClassFixture<ReadOnlyConnectionFixture>
{
    private const string Password = "P@ssw0rd!";
    private const string RefreshCookieName = "__Secure-cinedex_refresh_token";

    [Fact]
    public async Task Register_WithReadOnlyConnectionUnreachable_Succeeds()
    {
        var response = await RegisterAsync(NewEmail(), "rosplitregister", Password);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithReadOnlyConnectionUnreachable_Succeeds()
    {
        // Login inserts a refresh token and reads the user through UserManager, all on the write
        // context. A broken read-only connection must not touch it.
        var email = NewEmail();
        Assert.Equal(HttpStatusCode.Created, (await RegisterAsync(email, "rosplitlogin", Password)).StatusCode);

        var response = await PostLoginAsync(email, Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
    }

    [Fact]
    public async Task Refresh_WithReadOnlyConnectionUnreachable_Returns500()
    {
        // Refresh opens with the preflight lookup, the one refresh-token read that goes through
        // IRefreshTokenQueries. With the read-only connection refused it cannot complete, and the
        // failure surfaces as an unhandled-exception 500 rather than the 401 an invalid token gets.
        // That difference is the proof: were the queries still on DefaultConnection, this would
        // rotate the cookie and return 200 exactly as it does everywhere else.
        var email = NewEmail();
        Assert.Equal(HttpStatusCode.Created, (await RegisterAsync(email, "rosplitrefresh", Password)).StatusCode);

        var login = await PostLoginAsync(email, Password);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var refreshCookie = ReadRefreshCookieValue(login);
        Assert.False(string.IsNullOrWhiteSpace(refreshCookie));

        var response = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, refreshCookie);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithReadOnlyConnectionUnreachable_Succeeds()
    {
        // Logout revokes by hash in a single conditional update, so it never reads through the
        // queries — it must stay unaffected.
        var email = NewEmail();
        Assert.Equal(HttpStatusCode.Created, (await RegisterAsync(email, "rosplitlogout", Password)).StatusCode);

        var login = await PostLoginAsync(email, Password);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var refreshCookie = ReadRefreshCookieValue(login);
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);

        // Logout is members-only, so it needs the bearer token as well as the cookie.
        var response = await PostAsync(TestRouteConstants.Auth.LogoutEndpoint, refreshCookie, body!.AccessToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static string NewEmail() => $"rosplit-{Guid.NewGuid():N}@example.com";

    private static string? ReadRefreshCookieValue(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return null;
        }

        var setCookie = values.FirstOrDefault(value =>
            value.StartsWith($"{RefreshCookieName}=", StringComparison.Ordinal));
        if (setCookie is null)
        {
            return null;
        }

        var value = setCookie[(RefreshCookieName.Length + 1)..];
        var separator = value.IndexOf(';', StringComparison.Ordinal);
        return separator < 0 ? value : value[..separator];
    }

    private Task<HttpResponseMessage> RegisterAsync(string email, string username, string password) =>
        fixture.CookielessClient.PostAsJsonAsync(
            TestRouteConstants.Auth.RegisterEndpoint,
            new RegisterRequest { Email = email, Username = username, Password = password });

    private Task<HttpResponseMessage> PostLoginAsync(string email, string password) =>
        fixture.CookielessClient.PostAsJsonAsync(
            TestRouteConstants.Auth.LoginEndpoint,
            new LoginRequest { Email = email, Password = password });

    private Task<HttpResponseMessage> PostAsync(string route, string? refreshCookie, string? accessToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, route);

        if (refreshCookie is not null)
        {
            request.Headers.Add("Cookie", $"{RefreshCookieName}={refreshCookie}");
        }

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return fixture.CookielessClient.SendAsync(request);
    }
}
