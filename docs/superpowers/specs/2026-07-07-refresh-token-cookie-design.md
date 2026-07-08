# Refresh Token as an HttpOnly Cookie

**Date:** 2026-07-07
**Status:** Approved, not yet implemented

Move the refresh token out of the JSON response body and into a hardened cookie, so that
JavaScript running in the browser can never read it.

## Problem

`POST /movies-svc/auth/login` returns the raw refresh token in its response body:

```json
{ "accessToken": "...", "expiresAtUtc": "...",
  "refreshToken": "...", "refreshTokenExpiresAtUtc": "..." }
```

Any client that receives it must store it somewhere reachable from JavaScript — `localStorage`,
`sessionStorage`, or a JS-readable cookie. A single XSS defect then yields a seven-day credential,
which survives page reloads and cannot be revoked by clearing the access token. The refresh token
is the long-lived secret in this system; it is the one credential that must not be reachable from
script.

`POST /movies-svc/auth/refresh` and `POST /movies-svc/auth/logout` currently accept the refresh
token in their request bodies, which only works if the client can read it.

## Goals

- The refresh token is never present in any response body.
- The refresh token is never readable by JavaScript.
- Cross-site request forgery against `/auth/refresh` is structurally impossible, not merely
  defended against.
- The change is verifiable end to end by the existing integration test suite.

## Non-goals

- **CORS configuration.** Still required unless the reverse-proxy topology is adopted — `SameSite`
  is evaluated on registrable domain, but CORS is evaluated on *origin*, and `app.cinedex.com` →
  `api.cinedex.com` is same-site yet cross-origin. Only proxying makes the two same-origin and
  removes the need. Either way, out of scope here: the SPA has no code that calls the API.
- **The reverse proxy itself.** See [Follow-ups](#follow-ups).
- **Frontend work.** `frontend/cinadex-ui` contains no authentication code today — no `fetch`, no
  token handling. There is no client to update.
- **Refresh-token reuse detection.** A pre-existing known gap, unchanged by this work.

## Decisions

### The cookie is the only transport

`RefreshToken` is removed from `LoginResponse`. `RefreshRequest` and `LogoutRequest` are deleted;
`/auth/refresh` and `/auth/logout` read the cookie instead.

*Rejected:* keeping a request-body fallback for native clients. A fallback means the endpoint has
two trust paths, and a bug in cookie handling silently degrades to the weaker one. There are no
native clients today. Add the fallback when one exists, deliberately.

### `SameSite=Strict`, and a same-site deployment requirement

`SameSite` is evaluated on registrable domain, not origin — ports are excluded. `localhost:9000`
(the SPA) and `localhost:8080` (the API) are therefore already the same site, and a `Strict` cookie
is sent between them.

With `Strict`, the browser never attaches the cookie to a request initiated by another site. CSRF
against `/auth/refresh` is not defended against; it cannot occur. No CSRF token, no double-submit
cookie, no `Origin` allowlist.

The cost is a hard deployment constraint, recorded below.

*Rejected:* `SameSite=None; Secure`. It permits any deployment topology, but reopens CSRF on
`/auth/refresh` and requires a separate defense. More machinery, more ways to be wrong.

Note that `Lax` and `Strict` are equivalent for these endpoints — both block cookies on cross-site
`POST`, and `/auth/refresh` and `/auth/logout` are `POST`-only. `Strict` is chosen because it says
what is meant.

### `__Secure-` prefix with `Path` scoping

```
__Secure-cinedex_refresh_token
```

The `__Secure-` prefix makes the browser reject the cookie unless it was set over HTTPS. Scoping
`Path=/movies-svc/auth` means the cookie is attached only to the auth endpoints, so it never
appears in request logs, traces, or proxy headers for ordinary API traffic.

*Rejected:* the `__Host-` prefix. It is strictly stronger — it additionally forbids a `Domain`
attribute and pins the cookie to the exact host — but it *requires* `Path=/`, which would attach
the refresh token to every request to the API. `__Host-` and `Path` scoping cannot be combined.
Exposure reduction was preferred over domain pinning.

**Residual risk, accepted:** without `__Host-`, a sibling subdomain can set a cookie of the same
name scoped to `Domain=cinedex.com` and shadow this one. For a *refresh* token that is session
fixation, not merely a nuisance: the victim's browser would then present the attacker's refresh
token, and the victim would be issued access tokens for the attacker's account. This is a
deployment constraint — see below.

### Cookie handling lives in the Presentation layer

A static `RefreshTokenCookie` helper next to the auth endpoints owns the cookie name and a single
`CookieOptions` factory shared by append and delete. A cookie is deleted only when the delete call's
`Path`, `Domain`, `Secure`, and `SameSite` match those it was set with; sharing one factory makes
that mismatch unrepresentable.

`AuthTokensDto` and every Application handler are unchanged. The Application layer still *produces*
the refresh token; the Presentation layer decides it travels as a cookie. Nothing below Presentation
learns what a cookie is.

*Rejected:* a FastEndpoints post-processor that strips the token and sets the cookie globally. For
three call sites, the indirection costs more than it saves, and nothing at the endpoint would
indicate a cookie is set.

*Rejected:* an `ICookieService` port in Application. `HttpResponse` is not a domain concept, and
Application references no ASP.NET Core packages today.

### `RefreshTokenExpiresAtUtc` is dropped, not retained

The value is not secret, and keeping it would cost nothing mechanically. But the SPA cannot read
the `HttpOnly` cookie's expiry, so a client that wants a "your session ends at" hint needs this
field added back deliberately, with a reason. Shipping a field no caller uses is how contracts rot.

## Cookie contract

```
Set-Cookie: __Secure-cinedex_refresh_token=<raw token>;
            HttpOnly;
            Secure;
            SameSite=Strict;
            Path=/movies-svc/auth;
            Expires=<AuthTokensDto.RefreshTokenExpiresAtUtc>
```

| Attribute | Value | Why |
|---|---|---|
| `HttpOnly` | set | Unreachable from `document.cookie`. The point of the change. |
| `Secure` | set | Required by the `__Secure-` prefix. Browsers permit this on `http://localhost`, which is treated as a secure context. |
| `SameSite` | `Strict` | Eliminates CSRF on `/auth/refresh`. |
| `Path` | `/movies-svc/auth` | Derived from `ApiConstants`, never hardcoded. Keeps the token off ordinary API requests. |
| `Expires` | token's real expiry | Cookie and database agree on lifetime rather than both reading `Jwt:RefreshTokenDays`. |
| `IsEssential` | `true` | A future cookie-consent policy must not suppress it. |

The base path comes from `app.UsePathBase(ApiConstants.BasePath)`, so inside the app `Request.Path`
is `/auth/refresh`. The cookie `Path` must be the *browser-visible* path, `/movies-svc/auth`, and is
built as `$"{ApiConstants.BasePath}/{ApiConstants.Auth.Route}"`.

## Endpoint contracts

| Endpoint | Reads | Writes | Response body |
|---|---|---|---|
| `POST /auth/login` | — | sets cookie | `{ accessToken, expiresAtUtc }` |
| `POST /auth/refresh` | cookie | sets rotated cookie | `{ accessToken, expiresAtUtc }` |
| `POST /auth/logout` | cookie | clears cookie | none (`204`) |

`RefreshEndpoint` and `LogoutEndpoint` become `EndpointWithoutRequest`. `/auth/logout` continues to
require a bearer token; every other auth endpoint stays anonymous.

## Error handling

| Situation | Behavior |
|---|---|
| Refresh, no cookie | Endpoint throws `InvalidCredentialsException` → `401` RFC 7807 via the existing handler chain. Indistinguishable from an invalid token. |
| Refresh, invalid / expired / revoked cookie | Handler throws; endpoint catches, **clears the cookie**, rethrows → `401`. The token is known-dead; leaving it means every later request re-sends a corpse. |
| Logout, no cookie | Handler is not called (no pointless database round-trip). Cookie cleared, `204`. Preserves the endpoint's documented idempotency. |
| Logout, unknown or already-revoked cookie | Existing silent no-op. Cookie cleared, `204`. |

No new exception types. `InvalidCredentialsException` already maps to `401` through
`InvalidCredentialsExceptionHandler`, carrying the request's correlation id.

## Testing

`WebApplicationFixture` currently calls `CreateClient()` with defaults: `BaseAddress` is
`http://localhost` and `HandleCookies` is `true`. .NET's `CookieContainer` refuses to send a
`Secure` cookie over `http://`, so every refresh and logout test would fail with a `401` unrelated
to the code under test.

Two fixture changes:

- `Client` gets `BaseAddress = new Uri("https://localhost")`. `TestServer` performs no real TLS; this
  only flips `Request.IsHttps`.
- A second client with `HandleCookies = false` is added, for tests that must replay a specific
  cookie value rather than whatever the cookie jar holds.

| Test | Asserts |
|---|---|
| `Login_SetsHardenedRefreshCookie` | `Set-Cookie` carries the name, `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/movies-svc/auth`. |
| `Login_ResponseBodyOmitsRefreshToken` | The raw JSON has no `refreshToken` property. Asserted against `JsonDocument`, not the typed DTO — a typed assertion cannot detect a property that is still being serialized. |
| `Refresh_WithCookie_RotatesCookie` | New `Set-Cookie` value differs from the presented one. |
| `Refresh_WithRotatedCookie_Returns401` | Replaying the pre-rotation value is rejected. |
| `Refresh_WithoutCookie_Returns401` | Missing cookie is a `401`, not a `500`. |
| `Refresh_WithInvalidCookie_ClearsCookie` | `401` **and** a `Set-Cookie` that expires it. |
| `Logout_RevokesTokenAndClearsCookie` | `204`, expired `Set-Cookie`, and the token no longer refreshes. |
| `Logout_WithoutBearerToken_Returns401` | Unchanged behavior. |
| `Logout_WithoutCookie_Returns204` | Idempotency preserved. |

## Breaking changes

`Cinedex.WebService.Contracts` (`IsPackable=true`, published) loses:

- `LoginResponse.RefreshToken`
- `LoginResponse.RefreshTokenExpiresAtUtc`
- `RefreshRequest` (deleted)
- `LogoutRequest` (deleted)

**The package version stays at `0.4.1`.** This breaking change ships unversioned, by decision. It is
tolerable only because nothing consumes the package yet. If it is ever published to a feed that
others depend on, this becomes a defect.

Nothing else breaks: the two deleted request types were referenced only by their endpoints, the two
`ToCommand` overloads in `AuthMappings`, and the integration tests. There are no FastEndpoints
request validators — validation lives on the Application commands — and there is no `LogoutValidator`
at all.

## Deployment constraints

These are load-bearing. Violating either silently breaks authentication or weakens it.

1. **The SPA and the API must be same-site.** Either one registrable domain
   (`app.cinedex.com` + `api.cinedex.com`), or the API served through the SPA's reverse proxy. A
   `SameSite=Strict` cookie is not sent cross-site, so hosting the UI on an unrelated domain breaks
   refresh entirely — silently, with a `401`.

   Same-site is not the same as same-origin. The two-subdomain option satisfies the cookie but is
   still cross-origin, so it additionally needs CORS with `AllowCredentials` and a `fetch` that
   sets `credentials: "include"`. The reverse-proxy option satisfies both at once and needs neither.

2. **No untrusted content on sibling subdomains.** Because the cookie uses `__Secure-` rather than
   `__Host-`, a sibling subdomain can shadow it with a `Domain`-scoped cookie of the same name. For a
   refresh token this is session fixation. If untrusted subdomains ever become a possibility, switch
   to `__Host-` and accept `Path=/`.

## Follow-ups

Out of scope here, but required before the flow works from a browser:

- `frontend/cinadex-ui/nginx.conf` — proxy `/movies-svc/` to the API, making UI and API same-origin.
- `frontend/cinadex-ui/vite.config.ts` — `server.proxy` for the dev server on `:9000`.
- Once proxied, the CORS known gap in `auth-security-model.md` dissolves rather than being solved.

## Files touched

```
backend/src/Presentation/Cinedex.WebService/Endpoints/Auth/RefreshTokenCookie.cs   (new)
backend/src/Presentation/Cinedex.WebService/Endpoints/Auth/LoginEndpoint.cs
backend/src/Presentation/Cinedex.WebService/Endpoints/Auth/RefreshEndpoint.cs
backend/src/Presentation/Cinedex.WebService/Endpoints/Auth/LogoutEndpoint.cs
backend/src/Presentation/Cinedex.WebService/Endpoints/Auth/AuthMappings.cs
backend/NuGetLibraries/Cinedex.WebService.Contracts/Responses/LoginResponse.cs
backend/NuGetLibraries/Cinedex.WebService.Contracts/Requests/RefreshRequest.cs    (deleted)
backend/NuGetLibraries/Cinedex.WebService.Contracts/Requests/LogoutRequest.cs     (deleted)
backend/tests/Cinedex.WebService.IntegrationTests/WebApplicationFixture.cs
backend/tests/Cinedex.WebService.IntegrationTests/Endpoints/Auth/AuthEndpointTests.cs
docs/auth-security-model.md
CHANGELOG.md
```
