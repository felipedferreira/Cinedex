using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cinedex.Application.Email;
using Cinedex.WebService.Contracts.Requests;
using Cinedex.WebService.Contracts.Responses;
using Cinedex.WebService.IntegrationTests.Constants;
using Npgsql;

namespace Cinedex.WebService.IntegrationTests.Auth;

[Collection(WebApplicationCollection.Name)]
public sealed class AuthEndpointTests(WebApplicationFixture fixture)
{
    private const string Password = "P@ssw0rd!";
    private const string NewPassword = "N3wP@ssw0rd!";

    // Asserted here rather than referenced from the web project: the cookie name is part of the
    // HTTP contract, so a rename should break a test rather than silently ship.
    private const string RefreshCookieName = "__Secure-cinedex_refresh_token";

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
    public async Task Register_WithDuplicateEmailConcurrently_AllowsOnlyOneAccount()
    {
        var email = NewEmail();

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 8)
                .Select(index => RegisterAsync(email, $"duplicate{index}", Password)));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Equal(responses.Length - 1, responses.Count(response => response.StatusCode == HttpStatusCode.BadRequest));
    }

    [Fact]
    public async Task Register_WhenDefaultRoleIsMissing_DoesNotPersistUser()
    {
        var email = NewEmail();
        var username = $"missingrole{Guid.NewGuid():N}";

        await DeleteUserRoleAsync();
        try
        {
            var response = await RegisterAsync(email, username, Password);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal(0, await CountUsersByEmailAsync(email));
        }
        finally
        {
            await RestoreUserRoleAsync();
        }
    }

    [Fact]
    public async Task AuthDatabase_EmailIndex_IsUnique()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            SELECT i.indisunique
            FROM pg_class AS t
            JOIN pg_namespace AS n ON n.oid = t.relnamespace
            JOIN pg_index AS i ON i.indrelid = t.oid
            JOIN pg_class AS ix ON ix.oid = i.indexrelid
            WHERE n.nspname = 'auth'
              AND t.relname = 'AspNetUsers'
              AND ix.relname = 'EmailIndex';
            """,
            connection);

        var isUnique = await command.ExecuteScalarAsync();

        Assert.True(Assert.IsType<bool>(isUnique));
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400()
    {
        var response = await RegisterAsync(NewEmail(), "weakuser", "short");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithLongButSimplePassword_Returns400()
    {
        // Long enough (passes the length rule) but no digit, uppercase, or special character.
        // Rejection proves the complexity policy is enforced, not just length.
        var response = await RegisterAsync(NewEmail(), "simplepw", "alllowercaseletters");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_SetsHardenedRefreshCookie()
    {
        var email = NewEmail();
        await RegisterAsync(email, "cookieuser", Password);

        var response = await PostLoginAsync(email, Password);

        var setCookie = GetRefreshSetCookie(response);
        Assert.NotNull(setCookie);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"path={TestRouteConstants.MoviesServiceBasePath}/auth", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_ResponseBodyOmitsRefreshToken()
    {
        var email = NewEmail();
        await RegisterAsync(email, "bodyuser", Password);

        var response = await PostLoginAsync(email, Password);

        // Asserted against the raw JSON, not the typed DTO: a typed assertion cannot detect a
        // property that is still being serialized.
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.TryGetProperty("accessToken", out _));
        Assert.False(document.RootElement.TryGetProperty("refreshToken", out _));
        Assert.False(document.RootElement.TryGetProperty("refreshTokenExpiresAtUtc", out _));
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAccessToken()
    {
        var email = NewEmail();
        await RegisterAsync(email, "loginuser", Password);

        var (body, refreshCookie) = await LoginAsync(email, Password);

        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.True(body.ExpiresAtUtc > DateTime.UtcNow);
        Assert.False(string.IsNullOrWhiteSpace(refreshCookie));
    }

    [Fact]
    public async Task Login_AccessTokenCarriesUserRoleClaim()
    {
        var email = NewEmail();
        await RegisterAsync(email, "roleuser", Password);

        var (body, _) = await LoginAsync(email, Password);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.AccessToken);
        var roles = jwt.Claims
            .Where(claim => claim.Type == ClaimTypes.Role || claim.Type == "role")
            .Select(claim => claim.Value)
            .ToList();

        Assert.Contains("User", roles);
    }

    [Fact]
    public async Task Login_WithEmptyFields_Returns400WithSpecificMessages()
    {
        // The message strings are a client contract — they surface in the 400 body under
        // errors.Email / errors.Password. Pinning them here catches accidental wording drift.
        var response = await fixture.CookielessClient.PostAsJsonAsync(
            TestRouteConstants.Auth.LoginEndpoint,
            new LoginRequest { Email = string.Empty, Password = string.Empty });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = document.RootElement.GetProperty("errors");

        // Dictionary keys are camelCased in the response body ("email" / "password").
        // The empty-email case also triggers the EmailAddress rule (CascadeMode.Continue is the
        // default), so Contains rather than Equal is the right assertion.
        Assert.Contains(
            "Email must not be empty.",
            errors.GetProperty("email").EnumerateArray().Select(element => element.GetString()));
        Assert.Contains(
            "Password must not be empty.",
            errors.GetProperty("password").EnumerateArray().Select(element => element.GetString()));
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var email = NewEmail();
        await RegisterAsync(email, "wrongpw", Password);

        var response = await fixture.CookielessClient.PostAsJsonAsync(
            TestRouteConstants.Auth.LoginEndpoint,
            new LoginRequest { Email = email, Password = "Wr0ng@Pass!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownUser_Returns401()
    {
        var response = await fixture.CookielessClient.PostAsJsonAsync(
            TestRouteConstants.Auth.LoginEndpoint,
            new LoginRequest { Email = NewEmail(), Password = Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithCookie_RotatesCookie()
    {
        var email = NewEmail();
        await RegisterAsync(email, "refreshuser", Password);
        var (_, refreshCookie) = await LoginAsync(email, Password);

        var response = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, refreshCookie);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rotated = ReadRefreshCookieValue(response);
        Assert.False(string.IsNullOrWhiteSpace(rotated));
        Assert.NotEqual(refreshCookie, rotated);
    }

    [Fact]
    public async Task Refresh_WithRotatedCookie_Returns401()
    {
        var email = NewEmail();
        await RegisterAsync(email, "rotateduser", Password);
        var (_, refreshCookie) = await LoginAsync(email, Password);

        var rotation = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, refreshCookie);
        Assert.Equal(HttpStatusCode.OK, rotation.StatusCode);

        // The pre-rotation token must no longer be accepted.
        var reuse = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, refreshCookie);

        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithSameCookieConcurrently_AllowsOnlyOneRotation()
    {
        var email = NewEmail();
        await RegisterAsync(email, "concurrentrefresh", Password);
        var (_, refreshCookie) = await LoginAsync(email, Password);

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 8)
                .Select(_ => PostAsync(TestRouteConstants.Auth.RefreshEndpoint, refreshCookie)));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Equal(responses.Length - 1, responses.Count(response => response.StatusCode == HttpStatusCode.Unauthorized));

        var rotatedCookie = Assert.Single(
            responses.Select(ReadRefreshCookieValue),
            value => !string.IsNullOrWhiteSpace(value));
        Assert.NotEqual(refreshCookie, rotatedCookie);
    }

    [Fact]
    public async Task Refresh_WithoutCookie_Returns401()
    {
        var response = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, refreshCookie: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithInvalidCookie_Returns401AndClearsCookie()
    {
        var response = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, "not-a-real-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(string.Empty, ReadRefreshCookieValue(response));
    }

    [Fact]
    public async Task Logout_WithoutBearerToken_Returns401()
    {
        var response = await PostAsync(TestRouteConstants.Auth.LogoutEndpoint, "anything");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesTokenAndClearsCookie()
    {
        var email = NewEmail();
        await RegisterAsync(email, "logoutuser", Password);
        var (body, refreshCookie) = await LoginAsync(email, Password);

        var logout = await PostAsync(TestRouteConstants.Auth.LogoutEndpoint, refreshCookie, body.AccessToken);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(string.Empty, ReadRefreshCookieValue(logout));

        // The revoked refresh token can no longer be exchanged.
        var refresh = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, refreshCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutCookie_Returns204()
    {
        var email = NewEmail();
        await RegisterAsync(email, "nocookielogout", Password);
        var (body, _) = await LoginAsync(email, Password);

        var logout = await PostAsync(TestRouteConstants.Auth.LogoutEndpoint, refreshCookie: null, body.AccessToken);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_WithUnknownEmail_Returns202()
    {
        var response = await fixture.CookielessClient.PostAsJsonAsync(
            TestRouteConstants.Auth.ForgotPasswordEndpoint,
            new ForgotPasswordRequest { Email = NewEmail() });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ChangesPassword()
    {
        var email = NewEmail();
        await RegisterAsync(email, "resetuser", Password);
        var (_, refreshCookie) = await LoginAsync(email, Password);

        var forgot = await fixture.CookielessClient.PostAsJsonAsync(
            TestRouteConstants.Auth.ForgotPasswordEndpoint,
            new ForgotPasswordRequest { Email = email });
        Assert.Equal(HttpStatusCode.Accepted, forgot.StatusCode);

        // The raw token is embedded in the reset link inside the composed email; extract it the way
        // a real recipient's client would follow the link.
        var message = fixture.EmailSender.LastMessage;
        Assert.NotNull(message);
        var resetToken = ExtractResetToken(message);
        Assert.False(string.IsNullOrWhiteSpace(resetToken));

        var reset = await fixture.CookielessClient.PostAsJsonAsync(
            TestRouteConstants.Auth.ResetPasswordEndpoint,
            new ResetPasswordRequest { Email = email, ResetToken = resetToken, NewPassword = NewPassword });
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        // Password recovery revokes refresh tokens issued before the reset.
        var refresh = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, refreshCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

        // New password works, old password is rejected.
        var (loginNew, _) = await LoginAsync(email, NewPassword);
        Assert.False(string.IsNullOrWhiteSpace(loginNew.AccessToken));

        var loginOld = await fixture.CookielessClient.PostAsJsonAsync(
            TestRouteConstants.Auth.LoginEndpoint,
            new LoginRequest { Email = email, Password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, loginOld.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_Returns400()
    {
        var email = NewEmail();
        await RegisterAsync(email, "badreset", Password);

        var response = await fixture.CookielessClient.PostAsJsonAsync(
            TestRouteConstants.Auth.ResetPasswordEndpoint,
            new ResetPasswordRequest { Email = email, ResetToken = "invalid-token", NewPassword = NewPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";

    // Pulls the raw reset token out of the composed email's reset link, the way a recipient's mail
    // client would when they click through.
    private static string ExtractResetToken(EmailMessage message)
    {
        var text = message.Body switch
        {
            HtmlEmailBody html => html.PlainTextFallback ?? html.Content,
            PlainTextEmailBody plain => plain.Content,
            _ => throw new InvalidOperationException("Unexpected email body type."),
        };

        var match = Regex.Match(text, "token=([^&\\s\"]+)");
        Assert.True(match.Success, "reset token not found in email body");
        return Uri.UnescapeDataString(match.Groups[1].Value);
    }

    /// <summary>Returns the raw <c>Set-Cookie</c> header for the refresh cookie, or null.</summary>
    private static string? GetRefreshSetCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(value => value.StartsWith($"{RefreshCookieName}=", StringComparison.Ordinal))
            : null;

    /// <summary>
    /// Returns the refresh cookie's value: the raw token, or <see cref="string.Empty"/> when the
    /// server cleared it. Null when no refresh cookie was set at all.
    /// </summary>
    private static string? ReadRefreshCookieValue(HttpResponseMessage response)
    {
        var setCookie = GetRefreshSetCookie(response);
        if (setCookie is null)
        {
            return null;
        }

        var value = setCookie[(RefreshCookieName.Length + 1)..];
        var separator = value.IndexOf(';', StringComparison.Ordinal);
        return separator < 0 ? value : value[..separator];
    }

    private async Task DeleteUserRoleAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            DELETE FROM auth."AspNetRoles"
            WHERE "normalizedName" = 'USER';
            """,
            connection);

        await command.ExecuteNonQueryAsync();
    }

    private async Task RestoreUserRoleAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            DELETE FROM auth."AspNetRoles"
            WHERE "normalizedName" = 'USER';

            INSERT INTO auth."AspNetRoles" (id, "concurrencyStamp", name, "normalizedName")
            VALUES (
                'a5f0c1a0-1000-7000-8000-000000000001',
                'f6b1c2a1-2000-7000-8000-000000000001',
                'User',
                'USER');
            """,
            connection);

        await command.ExecuteNonQueryAsync();
    }

    private async Task<long> CountUsersByEmailAsync(string email)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            SELECT COUNT(*)
            FROM auth."AspNetUsers"
            WHERE "normalizedEmail" = @email;
            """,
            connection);
        command.Parameters.AddWithValue("email", email.ToUpperInvariant());

        return (long)(await command.ExecuteScalarAsync() ?? 0L);
    }

    private Task<HttpResponseMessage> RegisterAsync(string email, string username, string password) =>
        fixture.CookielessClient.PostAsJsonAsync(
            TestRouteConstants.Auth.RegisterEndpoint,
            new RegisterRequest { Email = email, Username = username, Password = password });

    private Task<HttpResponseMessage> PostLoginAsync(string email, string password) =>
        fixture.CookielessClient.PostAsJsonAsync(
            TestRouteConstants.Auth.LoginEndpoint,
            new LoginRequest { Email = email, Password = password });

    private async Task<(LoginResponse Body, string RefreshCookie)> LoginAsync(string email, string password)
    {
        var response = await PostLoginAsync(email, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);

        var refreshCookie = ReadRefreshCookieValue(response);
        Assert.False(string.IsNullOrWhiteSpace(refreshCookie));

        return (body, refreshCookie!);
    }

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