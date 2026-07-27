# Auth & Security Model

How authentication works in Cinedex: JWT access tokens, rotating refresh tokens, and where
ASP.NET Core Identity is allowed to live.

## Layering

Identity is a framework detail, so it is confined to a single adapter behind application-layer
ports. The domain and application layers never reference ASP.NET Core Identity.

| Layer | Type | Responsibility |
|-------|------|----------------|
| Domain | `UserAggregate/User` | Framework-free user aggregate. No password hashes, no tokens. |
| Application | `IIdentityService`, `ITokenService`, `IEmailSender`, `IEmailDispatcher` | Ports the use cases depend on. `IEmailSender` delivers; `IEmailDispatcher` only queues, and is what request-path code uses. |
| Application | `Auth/{Register,Login,Logout,Refresh,ForgotPassword,ResetPassword}` | One handler slice per use case, each with a FluentValidation validator. |
| Adapter | `Cinedex.Auth.Identity` | Implements `IIdentityService` and `ITokenService`: ASP.NET Core Identity for accounts, JWT for tokens, EF Core for hashed refresh-token storage. `ApplicationUser : IdentityUser<Guid>` maps to the domain `User`. |
| Adapter | `Cinedex.Email.Smtp` | Implements `IEmailSender` with MailKit, plus `IEmailDispatcher` (`ChannelEmailDispatcher`) and the `EmailDeliveryWorker` background service that drains the queue. Email delivery is a messaging concern, not authentication, so it lives in its own adapter. |
| Presentation | `Extensions/AuthenticationExtensions` | JWT bearer validation, authorization middleware. |
| Presentation | `Http/RefreshTokenCookie` | Reads, sets, and clears the HttpOnly refresh-token cookie. Keeps the cookie a transport detail the Application layer never sees. |

## Endpoints

All routes are relative to the `/movies-svc` base path.

| Method & route | Auth | Behavior |
|---|---|---|
| `POST /movies-svc/auth/register` | Anonymous | Create a user. `201 Created`. |
| `POST /movies-svc/auth/login` | Anonymous | Validate credentials; returns an access token + refresh token. |
| `POST /movies-svc/auth/refresh` | Anonymous | Exchange a refresh token for a rotated pair. |
| `POST /movies-svc/auth/logout` | **Bearer** | Revoke the refresh token supplied in the cookie. `204 No Content`. |
| `POST /movies-svc/auth/password/forgot` | Anonymous | Always `202 Accepted`. |
| `POST /movies-svc/auth/password/reset` | Anonymous | Reset the password with a valid reset token. `204 No Content`. |

The catalog is members-only: **every Genre and Title endpoint requires a bearer token**, reads and
writes alike. Those endpoints simply omit `AllowAnonymous()` — FastEndpoints requires an
authenticated user by default — so the anonymous surface is exactly the five auth endpoints above.
Anonymous catalog requests receive `401 Unauthorized`
(pinned by `CatalogAuthorizationTests`).

## Token lifecycle

```
POST /auth/login
  ├─ access token   JWT, HS256, 15 min   (Jwt:AccessTokenMinutes)   → response body
  └─ refresh token  32 random bytes, base64, 7 days   (Jwt:RefreshTokenDays)   → Set-Cookie only

POST /auth/refresh  (refresh token read from the cookie; no request body)
  ├─ look up by SHA-256 hash
  ├─ reject if the cookie is missing, or the token is revoked or expired  → 401
  │    on rejection the cookie is also cleared, so the browser stops re-sending a dead token
  └─ rotate:
       old.RevokedAtUtc = now
       old.ReplacedByTokenHash = hash(new)
       new token pair issued; the new refresh token is written as a fresh Set-Cookie
     (both writes committed in one SaveChangesAsync)

POST /auth/logout  (refresh token read from the cookie; no request body)
  └─ RevokedAtUtc = now, and the cookie is cleared.
     Idempotent: an unknown, already-revoked, or absent cookie is a silent no-op that still clears.
```

### The refresh token cookie

The refresh token is returned to the browser only as a cookie, never in a response body, so a
cross-site scripting defect cannot read it. The access token remains in the body; it is short-lived
(15 minutes) and the client needs to attach it as a bearer header.

```
Set-Cookie: __Secure-cinedex_refresh_token=<raw token>;
            HttpOnly; Secure; SameSite=Strict; Path=/movies-svc/auth; Expires=<token expiry>
```

| Attribute | Purpose |
|---|---|
| `HttpOnly` | Unreachable from `document.cookie`. The reason for the change. |
| `Secure` | Required by the `__Secure-` prefix; the browser refuses to store the cookie unless it was set over HTTPS. |
| `SameSite=Strict` | The browser never attaches the cookie to a cross-site request, which makes CSRF against `/auth/refresh` structurally impossible rather than something to defend against. |
| `Path=/movies-svc/auth` | The cookie rides only on the auth endpoints, so it never appears in logs or traces for ordinary API traffic. |
| `Expires` | Taken from the token's own expiry, so the cookie and the database row agree on the lifetime. |

`RefreshTokenCookie` (Presentation layer) owns the cookie name and a single `CookieOptions` factory
shared by the set and the clear, so the two cannot drift apart — a cookie is only deleted when the
delete call's attributes match the ones it was set with. The Application layer is unchanged: it still
produces the refresh token and does not know it travels as a cookie.

On a failed refresh the endpoint clears the cookie via `HttpContext.Response.OnStarting`. A direct
clear would be lost, because rethrowing runs `UseExceptionHandler`, which calls `Response.Clear()`
before writing the 401; the `OnStarting` callback runs later, at header-flush time.

#### Deployment constraints

Both are load-bearing. Violating the first breaks authentication silently; violating the second
weakens it.

1. **The SPA and the API must be same-site.** `SameSite` is evaluated on registrable domain, not
   origin (ports are excluded, so `localhost:9000` → `localhost:8080` is already same-site). In
   production this means either one registrable domain (`app.cinedex.com` + `api.cinedex.com`) or the
   API served through the SPA's reverse proxy. A `SameSite=Strict` cookie is not sent cross-site, so
   hosting the UI on an unrelated domain breaks refresh entirely, with a bare 401. Same-site is not
   the same as same-origin: the two-subdomain option is still cross-origin and additionally needs
   CORS with credentials, whereas the reverse-proxy option needs neither.
2. **No untrusted content on sibling subdomains.** The cookie uses the `__Secure-` prefix rather than
   `__Host-`, because `__Host-` forbids a `Path` and would put the token on every API request. The
   trade-off is that `__Secure-` does not forbid a `Domain` attribute, so a sibling subdomain can set
   a same-named cookie scoped to the parent domain and shadow this one. For a refresh token that is
   session fixation. If untrusted subdomains ever become possible, switch to `__Host-` and accept
   `Path=/`.

### Access token claims

Issued by `JwtTokenService.CreateAccessToken`, signed HS256 with `Jwt:SigningKey`:

`sub` (user id) · `email` · `jti` (Guid v7) · `ClaimTypes.NameIdentifier` · `ClaimTypes.Name` ·
`ClaimTypes.Role` (repeatable — one entry per assigned role)

Roles are re-read from Identity on both issue and refresh, so a role change propagates on the next
refresh — the lag is bounded by the access-token lifetime (15 min).

Validation (`AuthenticationExtensions.AddJwtAuthentication`) checks issuer, audience, signing key,
and lifetime, with a 30-second clock skew. The JwtBearer default `RoleClaimType` is `ClaimTypes.Role`,
so `[Authorize(Roles = ...)]` reads these claims without further configuration.

### Why refresh tokens are hashed

The raw refresh token is returned to the client exactly once, at issue time — as the cookie above —
and is never persisted in raw form. The database stores only `SHA256(token)` as hex. A dump of the
`auth.refreshTokens` table therefore does not yield usable tokens.

Rotation on every use means a stolen refresh token has a bounded window: the moment the legitimate
client refreshes, the stolen token is revoked, and vice versa.

## Storage

All Identity and refresh-token tables live in a dedicated **`auth` schema**, set via
`HasDefaultSchema("auth")` in `AuthDbContext`. Because `AuthDbContext` and the catalog's
`FilmDbContext` share one physical database, `AuthDbContext` also gets its own
`__EFMigrationsHistory` table inside the `auth` schema so the two migration histories cannot
collide.

`AuthDbContext` derives from `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`, so the
usual Identity role tables (`AspNetRoles`, `AspNetUserRoles`, `AspNetRoleClaims`) live in the `auth`
schema alongside users. Three roles are seeded via `HasData` in `RoleConfiguration` and applied by the
`AddIdentityRoles` migration:

| Role | Purpose |
|---|---|
| `User` | Baseline role. Every account created by `POST /auth/register` is placed here automatically by `IdentityService.RegisterAsync`. |
| `Moderator` | Reserved for future moderation surfaces (e.g., Title/Genre curation). No endpoint uses it yet. |
| `Administrator` | Full access. Bootstrapping the first Administrator is manual — assign via SQL against `auth."AspNetUserRoles"`, or add a config-driven seed later. |

Role names live as constants in `RoleNames`; reference them from `[Authorize(Roles = ...)]` rather
than string literals.

### Identity options

Configured in `DependencyInjection.AddAuthenticationAdapter`:

- `RequireUniqueEmail = true`
- Lockout after 5 failed attempts, for 5 minutes
- **Password policy** — Identity is the single authority for password strength; the application-layer
  FluentValidation only checks the input is non-empty and at most 256 characters. The policy:
  - minimum length 8
  - at least one digit, one uppercase, one lowercase, and one non-alphanumeric character

  All rules are defined in `PasswordPolicyConstants` and applied explicitly in
  `AddAuthenticationAdapter` (not left to framework defaults), so the policy is visible in one place
  and enforced at registration and password reset.

## Configuration

The `Jwt` section is bound to `JwtOptions` and read by both the adapter (token issuance) and the
presentation layer (token validation) — the signing key must match on both sides.

| Key | Default | Notes |
|---|---|---|
| `Jwt:Issuer` | `https://cinedex.local` | |
| `Jwt:Audience` | `cinedex-api` | |
| `Jwt:SigningKey` | dev placeholder | **See below.** Minimum 32 bytes for HS256. |
| `Jwt:AccessTokenMinutes` | `15` | |
| `Jwt:RefreshTokenDays` | `7` | |

> ⚠️ **The `Jwt:SigningKey` in `appsettings.json` is a committed, dev-only placeholder.** It is
> public and must never be used outside local development. Override it per environment via the
> `Jwt__SigningKey` environment variable or .NET User Secrets, the same way
> `ConnectionStrings:DefaultConnection` is handled. `AddJwtAuthentication` throws at startup if
> the key is absent, but it cannot tell a real key from the placeholder.

## Password reset

`POST /auth/password/forgot` **always returns `202 Accepted`**, whether or not the email
corresponds to a real account. This is deliberate: a `404` for unknown emails would turn the
endpoint into an account-enumeration oracle.

The reset token is generated by Identity's default token providers — `DataProtectorTokenProvider`,
which produces a stateless, data-protection-encrypted token rather than a stored one. It carries the
user id and a snapshot of the account's `SecurityStamp`, so a token stops working once the password
(or any other stamp-changing property) changes, and **expires one hour after issue**. That lifespan is
set explicitly in `AddAuthenticationAdapter` via `DataProtectionTokenProviderOptions.TokenLifespan`,
overriding Identity's one-day default; it applies to every token from the `Default` provider, which
today means password reset only. The value itself lives in `PasswordResetTokenPolicy` in
`Cinedex.Application`, which the reset email also formats its "expires in" sentence from, so the
configured expiry and the copy the recipient reads cannot drift apart. Note that the `email` query
parameter in the reset link is only a lookup convenience — the token alone is the proof of
authorization. The token is stateless rather than stored, so it is not single-use: it stops working
once a reset *succeeds* (the `SecurityStamp` changes) or once it expires, but until then the same
link can be followed again.

`ForgotPasswordHandler` composes the reset email — subject, the reset link built from the token and
the configured `Frontend:BaseUrl`, and the branded HTML body that `CinedexEmailLayout` renders
around them (with the Cinedex logo attached as an inline `cid:` image and a plain-text alternative
alongside) — into an `EmailMessage`, then hands that to the `IEmailDispatcher` port. Composition
is an application concern; the `SmtpEmailSender` implementation in the `Cinedex.Email.Smtp` adapter
is a thin MailKit transport that only delivers. It uses the configured `Smtp` section, requires
SMTP username/password authentication, and supports plain text and HTML with a plain-text
alternative.

Delivery happens **off the request path**. The handler hands the composed message to the
`IEmailDispatcher` port, which returns immediately; `ChannelEmailDispatcher` writes it to a bounded
in-memory channel, and `EmailDeliveryWorker` — a `BackgroundService` in the same adapter — drains the
channel and calls `IEmailSender`. This is what keeps the endpoint's *latency* identical for known and
unknown accounts. Before it, the known-account path additionally awaited a four-round-trip SMTP
conversation that the unknown path skipped, so the two were distinguishable from outside despite both
returning `202 Accepted`. Consequences worth knowing:

- **The queue is in-process and not durable.** A crash, or overflow of the 1,000-message queue, loses
  queued email; the drop is logged (without the recipient address) and the user must request another
  reset. A deliberate trade — an unbounded queue behind an anonymous endpoint is a memory-exhaustion
  vector, and a dropped reset is recoverable where an out-of-memory process is not.
- **Delivery failures never reach the caller.** `EmailDeliveryException` is caught and logged by
  `EmailDeliveryWorker`, without the recipient address or body. `202 Accepted` means the request was
  accepted, not that mail was sent.
- **Deliveries never observe the request's `CancellationToken`,** which is already cancelled once the
  response completes. The worker owns a separate token, cancelled only when the shutdown drain window
  expires.
- **Shutdown drains the queue.** `StopAsync` completes the channel writer and lets the worker finish
  the backlog within a five-second window (inside Docker's ten-second default stop grace), so a
  redeploy does not silently swallow an in-flight reset email.

## Errors

`InvalidCredentialsException` (thrown for bad logins and for invalid, revoked, or expired refresh
tokens) is translated to `401 Unauthorized` by `InvalidCredentialsExceptionHandler`, which
participates in the same chain-of-responsibility `IExceptionHandler` pipeline as the validation and
not-found handlers. All error responses are RFC 7807 problem details carrying the request's
correlation id.

## Known gaps

These are deliberate scope cuts, not oversights — but they are load-bearing if you are about to
build on this.

- **Migrations are not applied at startup.** `AuthDbInitializer.MigrateAsync` exists but is only
  called from the integration-test fixture. Nothing in `Program.cs` migrates either context. See
  the [backend README](../backend/README.md#migrations).
- **No refresh-token reuse detection.** `ReplacedByTokenHash` records the rotation chain, but
  nothing reads it. Presenting an already-revoked token returns `401` without revoking the rest of
  the chain — so a stolen-then-rotated token cannot be detected as a compromise.
- **The forgot-password response still carries a small timing signal.** Queueing the reset email
  removed the large one — the four-round-trip SMTP conversation that only the known-account path
  performed, which MailKit lets run for up to two minutes against an unresponsive relay. What remains
  is that a known account additionally runs `UserManager.GeneratePasswordResetTokenAsync` (an
  AES + HMAC-SHA256 `DataProtectorTokenProvider` operation over already-loaded user state, tens of
  microseconds) after the `FindByEmailAsync` lookup both paths share. That is one to three orders of
  magnitude below normal network and pipeline jitter, so exploiting it needs a large sample from a
  low-noise vantage point rather than a single observation — a narrow residue, not a closed hole.
  Closing it entirely would require a constant-time response: padding every response to a fixed
  latency floor, or moving token generation off the request path as well.
- **No rate limiting anywhere in the service.** `POST /auth/password/forgot` is anonymous and
  unthrottled, so nothing caps the sample size an attacker can collect against the residual timing
  signal above, and nothing stops them enqueuing reset emails to a known address in a loop. This is
  the higher-value next step for this endpoint.
- **No endpoint yet enforces roles.** The `User`, `Moderator`, and `Administrator` roles are seeded
  and the access token carries them, but no endpoint restricts by role — Genre and Title endpoints
  require authentication, not a particular role, so any logged-in account can edit the catalog.
  Bootstrapping the first `Administrator` is also manual (SQL against `auth."AspNetUserRoles"`);
  no `Auth:BootstrapAdminEmail` seed exists.
- **No CORS configuration.** Docker Compose serves the SPA and API through the HTTPS Nginx reverse
  proxy at `https://localhost:9000`, and `npm run dev` serves HTTPS with a Vite `/movies-svc`
  proxy to the backend's HTTPS development profile. Browser auth flows are same-origin in both
  local modes and do not need CORS. Non-local deployments that split the API and SPA origins must
  add credentialed CORS or provide an equivalent reverse proxy (see [Deployment constraints](#deployment-constraints)).
- **No email confirmation, external logins, or 2FA.**
