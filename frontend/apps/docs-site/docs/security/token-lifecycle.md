---
sidebar_position: 2
---

# Token Lifecycle & Sessions

```
POST /auth/login
  ├─ access token   JWT, HS256, 15 min   (Jwt:AccessTokenMinutes)   → response body
  └─ refresh token  32 random bytes, base64, 7 days   (Jwt:RefreshTokenDays)   → Set-Cookie only
       FamilyId = new Guid v7   (a login starts a new token family)

POST /auth/refresh  (refresh token read from the cookie; no request body)
  ├─ look up by SHA-256 hash
  ├─ reject if the cookie is missing, or the token is revoked or expired  → 401
  │    on rejection the cookie is also cleared, so the browser stops re-sending a dead token
  └─ rotate:
       old.RevokedAtUtc = now
       old.ReplacedByTokenHash = hash(new)
       new.FamilyId = old.FamilyId   (rotation stays in the same family)
       new token pair issued; the new refresh token is written as a fresh Set-Cookie
     (both writes committed in one transaction)

POST /auth/logout  (refresh token read from the cookie; no request body)
  └─ RevokedAtUtc = now, and the cookie is cleared.
     Idempotent: an unknown, already-revoked, or absent cookie is a silent no-op that still clears.
```

## The refresh token cookie

The refresh token is returned to the browser only as a cookie, never in a response body, so a
cross-site scripting defect can't read it. The access token stays in the body; it's short-lived
(15 minutes) and the client attaches it as a bearer header.

```
Set-Cookie: __Secure-cinedex_refresh_token=<raw token>;
            HttpOnly; Secure; SameSite=Strict; Path=/movies-svc/auth; Expires=<token expiry>
```

| Attribute               | Purpose                                                                                                                                                                  |
| ----------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `HttpOnly`              | Unreachable from `document.cookie`.                                                                                                                                      |
| `Secure`                | Required by the `__Secure-` prefix — the browser won't store the cookie unless it was set over HTTPS.                                                                    |
| `SameSite=Strict`       | The browser never attaches the cookie to a cross-site request, which makes CSRF against `/auth/refresh` structurally impossible rather than something to defend against. |
| `Path=/movies-svc/auth` | The cookie rides only on the auth endpoints, so it never appears in logs or traces for ordinary API traffic.                                                             |
| `Expires`               | Taken from the token's own expiry, so the cookie and the database row agree on the lifetime.                                                                             |

`RefreshTokenCookie` owns the cookie name and a single `CookieOptions` factory shared by the set
and the clear, so the two can't drift apart — a cookie is only deleted when the delete call's
attributes match the ones it was set with.

### Deployment constraints

Both of these are load-bearing. Violating the first breaks authentication silently; violating the
second weakens it.

1. **The SPA and the API must be same-site.** `SameSite` is evaluated on registrable domain, not
   origin (ports are excluded, so `localhost:9000` → `localhost:8080` is already same-site). In
   production this means either one registrable domain (`app.cinedex.com` + `api.cinedex.com`) or
   the API served through the SPA's reverse proxy. A `SameSite=Strict` cookie is not sent
   cross-site, so hosting the UI on an unrelated domain breaks refresh entirely, with a bare 401.
2. **No untrusted content on sibling subdomains.** The cookie uses the `__Secure-` prefix rather
   than `__Host-`, because `__Host-` forbids a `Path` and would put the token on every API request.
   The trade-off: `__Secure-` doesn't forbid a `Domain` attribute, so a sibling subdomain could set
   a same-named cookie scoped to the parent domain and shadow this one — session fixation, for a
   refresh token. If untrusted subdomains ever become possible, switch to `__Host-` and accept
   `Path=/`.

## Access token claims

Issued by `JwtTokenService.CreateAccessToken`, signed HS256 with `Jwt:SigningKey`:

`sub` (user id) · `email` · `jti` (Guid v7) · `ClaimTypes.NameIdentifier` · `ClaimTypes.Name` ·
`ClaimTypes.Role` (repeatable — one entry per assigned role)

Roles are re-read from Identity on both issue and refresh, so a role change propagates on the next
refresh — the lag is bounded by the access-token lifetime (15 minutes). The JwtBearer default
`RoleClaimType` is `ClaimTypes.Role`, so `[Authorize(Roles = ...)]` reads these claims without
further configuration.

## Why refresh tokens are hashed

The raw refresh token is returned to the client exactly once, at issue time — as the cookie above —
and is never persisted in raw form. The database stores only `SHA256(token)` as hex, so a dump of
the `auth.refreshTokens` table doesn't yield usable tokens.

Rotation on every use means a stolen refresh token has a bounded window: the moment the legitimate
client refreshes, the stolen token is revoked, and vice versa.

## Token families

Every refresh token carries a `FamilyId`: a Guid v7 minted by `POST /auth/login` and copied
unchanged onto each replacement by `POST /auth/refresh`. One login therefore produces one family,
and the whole rotation chain that follows shares a single indexed value. Two logins by the same
account are two families, so they can be reasoned about — and revoked — independently.

The identifier is derivable by walking `ReplacedByTokenHash` hash by hash, but storing it collapses
that walk into one indexed lookup — a 15-minute access token over a 7-day refresh window means a
continuously-active session can accumulate several hundred rotations, so chain-walking would cost
that many sequential round-trips on the refresh path.

## Reuse response

`POST /auth/refresh` applies one state policy to the presented row:

- An unknown or expired token returns the ordinary generic `401`; expiry takes precedence over
  reuse detection.
- A revoked token without a replacement link — for example one revoked by logout — returns the same
  `401` without raising a new reuse event.
- A known, unexpired token with `ReplacedByTokenHash` is evidence that an already-rotated token was
  replayed. **Every active token in that family is revoked** before the same generic `401` is
  returned.
- An unrevoked token is rotated normally.

Every known, unexpired family is serialized with a PostgreSQL transaction-scoped advisory lock
whose key is derived from `FamilyId`, so rotation and reuse detection can't race each other. Only
the compromised login family is affected — other families for the same user remain valid, and
already-issued access tokens remain valid until their normal (short) expiry.

The public contract stays indistinguishable from every other invalid refresh attempt: RFC 7807
`401 Unauthorized` with the existing generic detail. The reuse event that's logged
(`RefreshTokenReuseDetected`) carries only a revoked-token count — no raw token, hash, family id,
user id, email, or username.

See [Storage & Retention](./storage-and-retention.md#refresh-token-retention) for how long revoked
rows stick around before they're swept, and why that window matters for this detection to work at
all.

## Errors

`InvalidCredentialsException` — thrown for bad logins and for invalid, revoked, or expired refresh
tokens — is translated to `401 Unauthorized` by a dedicated exception handler in the same
chain-of-responsibility pipeline as the validation and not-found handlers. All error responses are
RFC 7807 problem details carrying the request's correlation id.
