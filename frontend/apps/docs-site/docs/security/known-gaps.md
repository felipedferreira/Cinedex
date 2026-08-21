---
sidebar_position: 5
---

# Known Gaps

These are deliberate scope cuts, not oversights — but they're load-bearing if you're about to build
on top of this system.

- **Migrations are not applied by the web service.** The `Cinedex.DatabaseMigrator` project applies
  both database contexts, and Docker Compose gates the web service on it completing successfully;
  the integration-test fixture migrates itself. Nothing in the web service migrates either context,
  so a bare `dotnet run` against a fresh database still needs the migrator or an explicit
  `dotnet ef database update`.
- **The forgot-password response still carries a small timing signal.** Queueing the reset email
  removed the large one — the SMTP conversation that only the known-account path performed. What
  remains is one to three orders of magnitude below normal network and pipeline jitter, so
  exploiting it needs a large sample from a low-noise vantage point rather than a single
  observation. See [Password reset](./password-reset.md#a-residual-timing-signal-and-why-its-acceptable)
  for the detail. Closing it entirely would need a constant-time response — padding every response
  to a fixed latency floor, or moving token generation off the request path as well.
- **Revoking sessions does not revoke access tokens (SES-07).** `DELETE /auth/sessions/all`,
  `POST /auth/logout`, a password reset, and the reuse response all revoke _refresh_ tokens
  immediately — but access tokens are stateless JWTs, validated by signature and expiry alone, with
  nothing consulted at request time that revocation could change. **An access token already issued
  stays valid until it expires**, up to `Jwt:AccessTokenMinutes` (15 by default, 5 minimum) after it
  was minted. So "sign out everywhere" closes the ability to _obtain_ new access tokens instantly,
  while leaving a window of at most that long in which an already-stolen one still works.
  Closing the window means giving up statelessness — a per-request revocation check (a denylist of
  `jti` values, or a per-user "tokens issued before" watermark, both needing a lookup on every
  authenticated request), which is the trade JWT bearer exists to avoid. The mitigation actually in
  place is the short lifetime: the 5-to-15-minute range enforced by `JwtOptions` validation is what
  bounds the exposure, which is why raising `AccessTokenMinutes` is capped rather than free.
- **No rate limiting anywhere in the service.** `POST /auth/password/forgot` is anonymous and
  unthrottled, so nothing caps the sample size an attacker could collect against the residual
  timing signal above, and nothing stops them enqueuing reset emails to a known address in a loop.
  This is the highest-value next step for that endpoint.
- **No endpoint yet enforces roles.** The `User`, `Moderator`, and `Administrator` roles are seeded
  and the access token carries them, but no endpoint restricts by role — Genre and Title endpoints
  require _authentication_, not a particular role, so any logged-in account can edit the catalog.
  Bootstrapping the first `Administrator` is also manual (direct SQL) — there's no config-driven
  admin seed yet.
- **No CORS configuration.** Docker Compose serves the SPA and API through the HTTPS Nginx reverse
  proxy on the same origin, and the SPA's dev server proxies the API through the same origin too —
  so browser auth flows are same-origin in both local modes and don't need CORS. A deployment that
  splits the API and SPA onto different origins would need credentialed CORS, or an equivalent
  reverse proxy — see
  [Token lifecycle → Deployment constraints](./token-lifecycle.md#deployment-constraints).
- **No email confirmation, external logins, or two-factor authentication.**

These aren't hidden — they're the actual next items on the list, and most of them already have a
natural place to land once they're picked up: see
[How this was built](./how-this-was-built.md) for how the work so far has been sequenced.
