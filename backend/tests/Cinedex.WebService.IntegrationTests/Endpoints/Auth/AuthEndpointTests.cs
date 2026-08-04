using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cinedex.Application.Auth;
using Cinedex.Application.Email;
using Cinedex.WebService.IntegrationTests.Constants;
using Cinedex.WebService.IntegrationTests.Fakes;
using FoundryOceanus.WebService.Contracts.Requests;
using FoundryOceanus.WebService.Contracts.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    public async Task Refresh_WithRotatedCookie_RevokesFamilyAndReturnsExisting401Contract()
    {
        var email = NewEmail();
        await RegisterAsync(email, "rotateduser", Password);
        var (_, refreshCookie) = await LoginAsync(email, Password);
        var familyId = await GetTokenFamilyIdAsync(refreshCookie);

        var rotation = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, refreshCookie);
        Assert.Equal(HttpStatusCode.OK, rotation.StatusCode);
        var replacement = ReadRefreshCookieValue(rotation);
        Assert.False(string.IsNullOrWhiteSpace(replacement));

        var reuse = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, refreshCookie);
        var unknown = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, "not-a-real-token");

        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        Assert.Equal(string.Empty, ReadRefreshCookieValue(reuse));
        await AssertSamePublicUnauthorizedContractAsync(unknown, reuse);
        Assert.Equal(0L, await CountActiveTokensInFamilyAsync(familyId));

        var replacementAttempt = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, replacement);
        Assert.Equal(HttpStatusCode.Unauthorized, replacementAttempt.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithSameCookieConcurrently_AllowsOnlyOneRotation()
    {
        var email = NewEmail();
        await RegisterAsync(email, "concurrentrefresh", Password);
        var (_, refreshCookie) = await LoginAsync(email, Password);
        var familyId = await GetTokenFamilyIdAsync(refreshCookie);

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 8)
                .Select(_ => PostAsync(TestRouteConstants.Auth.RefreshEndpoint, refreshCookie)));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Equal(responses.Length - 1, responses.Count(response => response.StatusCode == HttpStatusCode.Unauthorized));

        var rotatedCookie = Assert.Single(
            responses.Select(ReadRefreshCookieValue),
            value => !string.IsNullOrWhiteSpace(value));
        Assert.NotEqual(refreshCookie, rotatedCookie);
        Assert.Equal(0L, await CountActiveTokensInFamilyAsync(familyId));

        var replacementAttempt = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, rotatedCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, replacementAttempt.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithReusedAncestor_RevokesTheActiveTailAcrossALongerChain()
    {
        var email = NewEmail();
        await RegisterAsync(email, "reusechain", Password);
        var (_, original) = await LoginAsync(email, Password);
        var familyId = await GetTokenFamilyIdAsync(original);

        var firstRotation = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, original);
        var firstReplacement = ReadRefreshCookieValue(firstRotation);
        Assert.False(string.IsNullOrWhiteSpace(firstReplacement));

        var secondRotation = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, firstReplacement);
        var activeTail = ReadRefreshCookieValue(secondRotation);
        Assert.False(string.IsNullOrWhiteSpace(activeTail));

        var reuse = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, original);

        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        Assert.Equal(0L, await CountActiveTokensInFamilyAsync(familyId));

        var tailAttempt = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, activeTail);
        Assert.Equal(HttpStatusCode.Unauthorized, tailAttempt.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithReuseInOneFamily_LeavesAnotherFamilyForTheSameUserValid()
    {
        var email = NewEmail();
        await RegisterAsync(email, "familyisolation", Password);
        var (_, compromisedOriginal) = await LoginAsync(email, Password);
        var (_, independentFamily) = await LoginAsync(email, Password);

        var rotation = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, compromisedOriginal);
        Assert.Equal(HttpStatusCode.OK, rotation.StatusCode);

        var reuse = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, compromisedOriginal);
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        var independentRefresh = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, independentFamily);
        Assert.Equal(HttpStatusCode.OK, independentRefresh.StatusCode);
    }

    [Fact]
    public async Task Refresh_WhenAncestorReuseRacesTailRotation_LeavesNoActiveFamilyToken()
    {
        var email = NewEmail();
        await RegisterAsync(email, "reuserace", Password);
        var (_, original) = await LoginAsync(email, Password);
        var familyId = await GetTokenFamilyIdAsync(original);

        var initialRotation = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, original);
        var activeTail = ReadRefreshCookieValue(initialRotation);
        Assert.False(string.IsNullOrWhiteSpace(activeTail));

        var responses = await Task.WhenAll(
            PostAsync(TestRouteConstants.Auth.RefreshEndpoint, original),
            PostAsync(TestRouteConstants.Auth.RefreshEndpoint, activeTail));

        Assert.Equal(HttpStatusCode.Unauthorized, responses[0].StatusCode);
        Assert.Contains(
            responses[1].StatusCode,
            new[] { HttpStatusCode.OK, HttpStatusCode.Unauthorized });
        Assert.Equal(0L, await CountActiveTokensInFamilyAsync(familyId));

        var racedReplacement = ReadRefreshCookieValue(responses[1]);
        if (!string.IsNullOrWhiteSpace(racedReplacement))
        {
            var replacementAttempt = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, racedReplacement);
            Assert.Equal(HttpStatusCode.Unauthorized, replacementAttempt.StatusCode);
        }
    }

    [Fact]
    public async Task Refresh_WithReuse_EmitsPiiSafeStructuredSecurityEvent()
    {
        var email = NewEmail();
        const string username = "securityevent";
        await RegisterAsync(email, username, Password);
        var (login, original) = await LoginAsync(email, Password);
        var userId = new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken).Subject;
        var familyId = await GetTokenFamilyIdAsync(original);

        var rotation = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, original);
        var replacement = ReadRefreshCookieValue(rotation);
        Assert.False(string.IsNullOrWhiteSpace(replacement));

        fixture.LoggerProvider.Clear();
        var reuse = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, original);

        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        CapturedLogEntry securityEvent = Assert.Single(
            fixture.LoggerProvider.Entries,
            entry => entry.EventId.Id == 1001);
        Assert.Equal(LogLevel.Warning, securityEvent.Level);
        Assert.Equal("RefreshTokenReuseDetected", securityEvent.EventId.Name);
        Assert.Equal(
            "Cinedex.Auth.Identity.Services.JwtTokenService",
            securityEvent.Category);

        var eventProperties = securityEvent.State
            .Where(property => property.Key != "{OriginalFormat}")
            .ToArray();
        var revokedCount = Assert.Single(eventProperties);
        Assert.Equal("RevokedTokenCount", revokedCount.Key);
        Assert.Equal(1, Assert.IsType<int>(revokedCount.Value));

        var emittedText = securityEvent.Message + "|" + string.Join(
            "|",
            securityEvent.State.Select(property => $"{property.Key}={property.Value}"));
        var sensitiveValues = new[]
        {
            email,
            username,
            userId,
            familyId.ToString(),
            original,
            Uri.UnescapeDataString(original),
            HashRefreshToken(original),
            replacement!,
            Uri.UnescapeDataString(replacement!),
            HashRefreshToken(replacement!),
        };

        foreach (var sensitiveValue in sensitiveValues)
        {
            Assert.DoesNotContain(sensitiveValue, emittedText, StringComparison.OrdinalIgnoreCase);
        }

        fixture.LoggerProvider.Clear();
        var repeatedReuse = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, original);
        Assert.Equal(HttpStatusCode.Unauthorized, repeatedReuse.StatusCode);

        CapturedLogEntry repeatedEvent = Assert.Single(
            fixture.LoggerProvider.Entries,
            entry => entry.EventId.Id == 1001);
        var repeatedEventProperties = repeatedEvent.State
            .Where(property => property.Key != "{OriginalFormat}")
            .ToArray();
        var repeatedRevokedCount = Assert.Single(repeatedEventProperties);
        Assert.Equal("RevokedTokenCount", repeatedRevokedCount.Key);
        Assert.Equal(0, Assert.IsType<int>(repeatedRevokedCount.Value));
    }

    [Fact]
    public async Task Refresh_WithNonReuseFailures_DoesNotEmitReuseSecurityEvent()
    {
        fixture.LoggerProvider.Clear();
        var unknown = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, "not-a-real-token");
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        AssertNoReuseSecurityEvent();

        var expiredEmail = NewEmail();
        await RegisterAsync(expiredEmail, "expirednonreuse", Password);
        var (_, expiredToken) = await LoginAsync(expiredEmail, Password);
        var expiredRotation = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, expiredToken);
        var expiredFamilyTail = ReadRefreshCookieValue(expiredRotation);
        Assert.False(string.IsNullOrWhiteSpace(expiredFamilyTail));
        await ExpireTokenAsync(expiredToken);
        fixture.LoggerProvider.Clear();
        var expired = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, expiredToken);
        Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
        AssertNoReuseSecurityEvent();

        var expiredFamilyTailRefresh = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, expiredFamilyTail);
        Assert.Equal(HttpStatusCode.OK, expiredFamilyTailRefresh.StatusCode);

        var logoutEmail = NewEmail();
        await RegisterAsync(logoutEmail, "logoutnonreuse", Password);
        var (logoutLogin, loggedOutToken) = await LoginAsync(logoutEmail, Password);
        var logout = await PostAsync(
            TestRouteConstants.Auth.LogoutEndpoint,
            loggedOutToken,
            logoutLogin.AccessToken);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        fixture.LoggerProvider.Clear();
        var loggedOut = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, loggedOutToken);
        Assert.Equal(HttpStatusCode.Unauthorized, loggedOut.StatusCode);
        AssertNoReuseSecurityEvent();

        var familyEmail = NewEmail();
        await RegisterAsync(familyEmail, "familyrevoked", Password);
        var (_, familyOriginal) = await LoginAsync(familyEmail, Password);
        var familyRotation = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, familyOriginal);
        var familyTail = ReadRefreshCookieValue(familyRotation);
        Assert.False(string.IsNullOrWhiteSpace(familyTail));
        var familyReuse = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, familyOriginal);
        Assert.Equal(HttpStatusCode.Unauthorized, familyReuse.StatusCode);

        fixture.LoggerProvider.Clear();
        var alreadyFamilyRevoked = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, familyTail);
        Assert.Equal(HttpStatusCode.Unauthorized, alreadyFamilyRevoked.StatusCode);
        AssertNoReuseSecurityEvent();
    }

    [Fact]
    public async Task Login_WithValidCredentials_StartsANewTokenFamily()
    {
        var email = NewEmail();
        await RegisterAsync(email, "familyuser", Password);

        var (_, refreshCookie) = await LoginAsync(email, Password);

        var familyId = await GetTokenFamilyIdAsync(refreshCookie);
        Assert.NotEqual(Guid.Empty, familyId);
        Assert.Equal(1L, await CountTokensInFamilyAsync(familyId));
    }

    [Fact]
    public async Task Refresh_WithCookie_PreservesTheTokenFamily()
    {
        var email = NewEmail();
        await RegisterAsync(email, "familyrotate", Password);
        var (_, refreshCookie) = await LoginAsync(email, Password);
        var originalFamilyId = await GetTokenFamilyIdAsync(refreshCookie);

        var response = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, refreshCookie);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rotated = ReadRefreshCookieValue(response);
        Assert.False(string.IsNullOrWhiteSpace(rotated));
        Assert.Equal(originalFamilyId, await GetTokenFamilyIdAsync(rotated!));
    }

    [Fact]
    public async Task Refresh_TwiceInSequence_PreservesTheTokenFamilyAcrossTheChain()
    {
        var email = NewEmail();
        await RegisterAsync(email, "familychain", Password);
        var (_, refreshCookie) = await LoginAsync(email, Password);
        var originalFamilyId = await GetTokenFamilyIdAsync(refreshCookie);

        var first = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, refreshCookie);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstRotated = ReadRefreshCookieValue(first);
        Assert.False(string.IsNullOrWhiteSpace(firstRotated));

        var second = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, firstRotated);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondRotated = ReadRefreshCookieValue(second);
        Assert.False(string.IsNullOrWhiteSpace(secondRotated));

        Assert.Equal(originalFamilyId, await GetTokenFamilyIdAsync(firstRotated!));
        Assert.Equal(originalFamilyId, await GetTokenFamilyIdAsync(secondRotated!));

        // The revoked ancestors stay in the family, which is what lets a family-wide revocation see
        // the whole chain rather than only its tail.
        Assert.Equal(3L, await CountTokensInFamilyAsync(originalFamilyId));
    }

    [Fact]
    public async Task Login_TwiceForTheSameUser_StartsSeparateTokenFamilies()
    {
        var email = NewEmail();
        await RegisterAsync(email, "twofamilies", Password);

        var (_, firstCookie) = await LoginAsync(email, Password);
        var (_, secondCookie) = await LoginAsync(email, Password);

        var firstFamilyId = await GetTokenFamilyIdAsync(firstCookie);
        var secondFamilyId = await GetTokenFamilyIdAsync(secondCookie);

        // Two devices are two sessions: revoking one must not be able to reach the other.
        Assert.NotEqual(firstFamilyId, secondFamilyId);
        Assert.Equal(1L, await CountTokensInFamilyAsync(firstFamilyId));
        Assert.Equal(1L, await CountTokensInFamilyAsync(secondFamilyId));
    }

    [Fact]
    public async Task Refresh_WithSameCookieConcurrently_RevokesTheCompromisedFamily()
    {
        var email = NewEmail();
        await RegisterAsync(email, "concurrentfamily", Password);
        var (_, refreshCookie) = await LoginAsync(email, Password);
        var originalFamilyId = await GetTokenFamilyIdAsync(refreshCookie);

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 8)
                .Select(_ => PostAsync(TestRouteConstants.Auth.RefreshEndpoint, refreshCookie)));

        var rotatedCookie = Assert.Single(
            responses.Select(ReadRefreshCookieValue),
            value => !string.IsNullOrWhiteSpace(value));

        // One request creates the replacement. The next request to acquire the family lock observes
        // the replacement link and revokes that winner before returning its indistinguishable 401.
        Assert.Equal(originalFamilyId, await GetTokenFamilyIdAsync(rotatedCookie!));
        Assert.Equal(2L, await CountTokensInFamilyAsync(originalFamilyId));
        Assert.Equal(0L, await CountActiveTokensInFamilyAsync(originalFamilyId));

        var replacementAttempt = await PostAsync(TestRouteConstants.Auth.RefreshEndpoint, rotatedCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, replacementAttempt.StatusCode);
    }

    [Fact]
    public async Task AuthDatabase_RefreshTokenFamilyIndex_ExistsAndIsNotUnique()
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
              AND t.relname = 'refreshTokens'
              AND ix.relname = 'IX_refreshTokens_familyId';
            """,
            connection);

        var isUnique = await command.ExecuteScalarAsync();

        // A non-null scalar proves the index exists under that exact name; false proves a family may
        // hold many tokens.
        Assert.False(Assert.IsType<bool>(isUnique));
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
    public async Task ForgotPassword_WhileDeliveryIsStalled_RespondsBeforeTheEmailIsSent()
    {
        var email = NewEmail();
        await RegisterAsync(email, "queueduser", Password);

        fixture.EmailSender.BlockDelivery();
        try
        {
            // If the handler still awaited delivery inline, this request could not complete while the
            // sender is stalled: the WaitAsync below would time out instead of yielding a 202.
            var forgot = await fixture.CookielessClient
                .PostAsJsonAsync(
                    TestRouteConstants.Auth.ForgotPasswordEndpoint,
                    new ForgotPasswordRequest { Email = email })
                .WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Equal(HttpStatusCode.Accepted, forgot.StatusCode);
        }
        finally
        {
            fixture.EmailSender.ResumeDelivery();
        }

        // Stalled, not dropped: the message still arrives once the sender is released.
        var message = await fixture.EmailSender.WaitForMessageAsync(email);
        Assert.False(string.IsNullOrWhiteSpace(ExtractResetToken(message)));
    }

    [Fact]
    public async Task ForgotPassword_WithKnownEmail_SendsTheBrandedEmail()
    {
        var email = NewEmail();
        await RegisterAsync(email, "brandeduser", Password);

        var forgot = await fixture.CookielessClient.PostAsJsonAsync(
            TestRouteConstants.Auth.ForgotPasswordEndpoint,
            new ForgotPasswordRequest { Email = email });
        Assert.Equal(HttpStatusCode.Accepted, forgot.StatusCode);

        // The only test that exercises EmailAssets.Logo() on the real request path: the branded
        // layout references the logo by cid: and carries the bytes with the message.
        var message = await fixture.EmailSender.WaitForMessageAsync(email);
        var html = Assert.IsType<HtmlEmailBody>(message.Body);
        Assert.Contains("cid:cinedex-logo", html.Content, StringComparison.Ordinal);
        Assert.Single(html.InlineImages);

        // The expiry copy is formatted from the same policy that configures the token lifespan.
        var expiryNotice = $"This link expires in {PasswordResetTokenPolicy.LifespanDescription}.";
        Assert.Contains(expiryNotice, html.Content, StringComparison.Ordinal);
        Assert.Contains(expiryNotice, html.PlainTextFallback!, StringComparison.Ordinal);
    }

    [Fact]
    public void PasswordResetToken_AsConfigured_ExpiresAfterThePolicyLifespan()
    {
        var options = fixture.Services
            .GetRequiredService<IOptions<DataProtectionTokenProviderOptions>>();

        Assert.Equal(PasswordResetTokenPolicy.Lifespan, options.Value.TokenLifespan);
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
        // a real recipient's client would follow the link. Delivery is queued and runs after the
        // response, so wait for the message addressed to this test's unique account rather than
        // reading whatever happened to be captured last.
        var message = await fixture.EmailSender.WaitForMessageAsync(email);
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

    // Recomputed here rather than referenced from the adapter, which is internal: pinning the hash in
    // the test means a change to how tokens are keyed breaks a test rather than silently decoupling
    // the cookie from its row.
    //
    // The value must be unescaped first. Set-Cookie carries the token URL-encoded and the server
    // hashes what request.Cookies decoded, so hashing the wire form would never match a stored row —
    // a base64 32-byte token always ends in '=', which travels as %3D.
    private static string HashRefreshToken(string refreshCookieValue) =>
        Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(Uri.UnescapeDataString(refreshCookieValue))));

    private static async Task AssertSamePublicUnauthorizedContractAsync(
        HttpResponseMessage expected,
        HttpResponseMessage actual)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, expected.StatusCode);
        Assert.Equal(expected.StatusCode, actual.StatusCode);
        Assert.Equal(expected.Content.Headers.ContentType?.MediaType, actual.Content.Headers.ContentType?.MediaType);

        using var expectedDocument = JsonDocument.Parse(await expected.Content.ReadAsStringAsync());
        using var actualDocument = JsonDocument.Parse(await actual.Content.ReadAsStringAsync());

        foreach (var propertyName in new[] { "type", "title", "status", "detail", "instance" })
        {
            Assert.Equal(
                expectedDocument.RootElement.GetProperty(propertyName).GetRawText(),
                actualDocument.RootElement.GetProperty(propertyName).GetRawText());
        }
    }

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

    private void AssertNoReuseSecurityEvent() =>
        Assert.DoesNotContain(fixture.LoggerProvider.Entries, entry => entry.EventId.Id == 1001);

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

    private async Task<Guid> GetTokenFamilyIdAsync(string rawRefreshToken)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            SELECT "familyId"
            FROM auth."refreshTokens"
            WHERE "tokenHash" = @tokenHash;
            """,
            connection);
        command.Parameters.AddWithValue("tokenHash", HashRefreshToken(rawRefreshToken));

        var familyId = await command.ExecuteScalarAsync();

        Assert.NotNull(familyId);
        return Assert.IsType<Guid>(familyId);
    }

    private async Task<long> CountTokensInFamilyAsync(Guid familyId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            SELECT COUNT(*)
            FROM auth."refreshTokens"
            WHERE "familyId" = @familyId;
            """,
            connection);
        command.Parameters.AddWithValue("familyId", familyId);

        return (long)(await command.ExecuteScalarAsync() ?? 0L);
    }

    private async Task<long> CountActiveTokensInFamilyAsync(Guid familyId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            SELECT COUNT(*)
            FROM auth."refreshTokens"
            WHERE "familyId" = @familyId
              AND "revokedAtUtc" IS NULL
              AND "expiresAtUtc" > CURRENT_TIMESTAMP;
            """,
            connection);
        command.Parameters.AddWithValue("familyId", familyId);

        return (long)(await command.ExecuteScalarAsync() ?? 0L);
    }

    private async Task ExpireTokenAsync(string rawRefreshToken)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            UPDATE auth."refreshTokens"
            SET "expiresAtUtc" = CURRENT_TIMESTAMP - INTERVAL '1 minute'
            WHERE "tokenHash" = @tokenHash;
            """,
            connection);
        command.Parameters.AddWithValue("tokenHash", HashRefreshToken(rawRefreshToken));

        Assert.Equal(1, await command.ExecuteNonQueryAsync());
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
