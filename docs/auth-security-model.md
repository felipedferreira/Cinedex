# Auth & Security Model

How authentication works in Cinedex: JWT access tokens, rotating refresh tokens, and where
ASP.NET Core Identity is allowed to live.

## Layering

Identity is a framework detail, so it is confined to a single adapter behind application-layer
ports. The domain and application layers never reference ASP.NET Core Identity.

| Layer | Type | Responsibility |
|-------|------|----------------|
| Domain | `UserAggregate/User` | Framework-free user aggregate. No password hashes, no tokens. |
| Application | `IIdentityService`, `ITokenService`, `IEmailSender` | Ports the use cases depend on. |
| Application | `Auth/{Register,Login,Logout,Refresh,ForgotPassword,ResetPassword}` | One handler slice per use case, each with a FluentValidation validator. |
| Adapter | `Cinedex.Persistence.Auth.Identity` | Implements the ports with Identity + EF Core. `ApplicationUser : IdentityUser<Guid>` maps to the domain `User`. |
| Presentation | `Extensions/AuthenticationExtensions` | JWT bearer validation, authorization middleware. |

## Endpoints

All routes are relative to the `/movies-svc` base path.

| Method & route | Auth | Behavior |
|---|---|---|
| `POST /movies-svc/auth/register` | Anonymous | Create a user. `201 Created`. |
| `POST /movies-svc/auth/login` | Anonymous | Validate credentials; returns an access token + refresh token. |
| `POST /movies-svc/auth/refresh` | Anonymous | Exchange a refresh token for a rotated pair. |
| `POST /movies-svc/auth/logout` | **Bearer** | Revoke the refresh token supplied in the body. `204 No Content`. |
| `POST /movies-svc/auth/password/forgot` | Anonymous | Always `202 Accepted`. |
| `POST /movies-svc/auth/password/reset` | Anonymous | Reset the password with a valid reset token. `204 No Content`. |

`logout` is the only endpoint that requires a bearer token — every other auth endpoint calls
`AllowAnonymous()`. The existing Genre and Title endpoints are currently anonymous as well.

## Token lifecycle

```
POST /auth/login
  ├─ access token   JWT, HS256, 15 min   (Jwt:AccessTokenMinutes)
  └─ refresh token  32 random bytes, base64, 7 days   (Jwt:RefreshTokenDays)

POST /auth/refresh  (present the refresh token)
  ├─ look up by SHA-256 hash
  ├─ reject if missing, revoked, or expired  → 401
  └─ rotate:
       old.RevokedAtUtc = now
       old.ReplacedByTokenHash = hash(new)
       new token pair issued
     (both writes committed in one SaveChangesAsync)

POST /auth/logout  (present the refresh token)
  └─ RevokedAtUtc = now.  Idempotent: an unknown or already-revoked token is a silent no-op.
```

### Access token claims

Issued by `JwtTokenService.CreateAccessToken`, signed HS256 with `Jwt:SigningKey`:

`sub` (user id) · `email` · `jti` (Guid v7) · `ClaimTypes.NameIdentifier` · `ClaimTypes.Name`

Validation (`AuthenticationExtensions.AddJwtAuthentication`) checks issuer, audience, signing key,
and lifetime, with a 30-second clock skew.

### Why refresh tokens are hashed

The raw refresh token is returned to the client exactly once, at issue time, and is never
persisted. The database stores only `SHA256(token)` as hex. A dump of the `auth.refreshTokens`
table therefore does not yield usable tokens.

Rotation on every use means a stolen refresh token has a bounded window: the moment the legitimate
client refreshes, the stolen token is revoked, and vice versa.

## Storage

All Identity and refresh-token tables live in a dedicated **`auth` schema**, set via
`HasDefaultSchema("auth")` in `AuthDbContext`. Because `AuthDbContext` and the catalog's
`FilmDbContext` share one physical database, `AuthDbContext` also gets its own
`__EFMigrationsHistory` table inside the `auth` schema so the two migration histories cannot
collide.

`AuthDbContext` derives from `IdentityUserContext<ApplicationUser, Guid>` rather than
`IdentityDbContext`, so **no role tables are created**. Adding roles later means switching the base
class and generating a migration.

### Identity options

Configured in `DependencyInjection.AddAuthenticationAdapter`:

- `RequireUniqueEmail = true`
- Minimum password length: 8
- Lockout after 5 failed attempts, for 5 minutes

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

The reset token is generated by Identity's default token providers and handed to `IEmailSender`.
The only registered implementation is `NoOpEmailSender`, so **reset emails are not actually
delivered**. Password reset is not usable end-to-end until a real `IEmailSender` is wired up.

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
- **No email delivery.** `IEmailSender` is a no-op (see above).
- **No roles or policies.** `AddAuthorization()` is registered with no policies; Genre and Title
  endpoints are anonymous.
- **No CORS configuration anywhere in the backend.** The SPA is served from `:9000` and the API
  from `:8080`. The first cross-origin `fetch` from the frontend will fail until CORS is
  configured or the UI is served through a reverse proxy.
- **No email confirmation, external logins, or 2FA.**
