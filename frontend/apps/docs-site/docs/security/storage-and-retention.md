---
sidebar_position: 3
---

# Storage & Retention

## Storage

All Identity and refresh-token tables live in a dedicated **`auth` schema**. Because `AuthDbContext`
and the catalog's `FilmDbContext` share one physical database, `AuthDbContext` also gets its own
`__EFMigrationsHistory` table inside the `auth` schema, so the two migration histories can't
collide.

`AuthDbContext` derives from `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`, so the
usual Identity role tables (`AspNetRoles`, `AspNetUserRoles`, `AspNetRoleClaims`) live in the `auth`
schema alongside users. Three roles are seeded:

| Role            | Purpose                                                                                       |
| --------------- | --------------------------------------------------------------------------------------------- |
| `User`          | Baseline role. Every account created by `POST /auth/register` is placed here automatically.   |
| `Moderator`     | Reserved for future moderation surfaces (e.g. Title/Genre curation). No endpoint uses it yet. |
| `Administrator` | Full access. Bootstrapping the first Administrator is currently manual — assign via SQL.      |

The `refreshTokens` table carries `familyId` under a non-unique index, alongside a unique index on
the token hash and one on the user id.

## Refresh-token retention

Nothing on the request path ever deletes a refresh token — a rotation revokes its predecessor and
inserts a replacement, so the table only grows. A background `RefreshTokenCleanupWorker` sweeps it
on an interval, deleting in bounded batches.

Two retention windows, because the two kinds of dead row are dead for different reasons:

| Category               | Deleted when                                            | Default                 |
| ---------------------- | ------------------------------------------------------- | ----------------------- |
| Expired, never revoked | past its expiry by more than `ExpiredRetention`         | 1 day past expiry       |
| Revoked                | past its revocation by more than `ReuseDetectionWindow` | 14 days past revocation |

Neither window shortens a session — a token that hasn't yet expired is never touched by either
category, no matter how long it's sat unused.

- **`ExpiredRetention` (1 day) is not a security boundary.** An expired-and-never-revoked row is
  inert the instant it expires — rotation already rejects it before checking anything else. The
  day of retention is pure operational slack: room for clock skew, and the row stays queryable for
  a day if someone needs to check when a session ended.
- **`ReuseDetectionWindow` (14 days) is a security boundary.** A revoked row is the only evidence
  that a token was rotated, and the only trigger that lets the system recognize a stolen,
  already-rotated token being replayed (see
  [Token lifecycle → Reuse response](./token-lifecycle.md#reuse-response)). Delete it too soon and
  that evidence is gone before it can be used. The window has to outlast the period an attacker
  could plausibly still be replaying the token it replaced — hence roughly double the 7-day default refresh
  token lifetime, as margin.

Operational notes:

- Work per sweep is capped (10,000 rows by default); a backlog drains across successive sweeps.
- Each batch is its own transaction — row locks are held for one statement, never across a sweep.
- A sweep runs at startup, not after the first interval, so a redeployed worker starts reclaiming
  immediately.
- The sweep logs counts and elapsed time only — never a token hash, user id, or family id.

## Identity options

- `RequireUniqueEmail = true`
- Lockout after 5 failed attempts, for 5 minutes.
- **Password policy** — Identity is the single authority for password strength (application-layer
  validation only checks the input is non-empty and at most 256 characters). The policy: minimum
  length 8, at least one digit, one uppercase letter, one lowercase letter, and one non-alphanumeric
  character. Enforced explicitly at registration and password reset, not left to framework
  defaults.

## Configuration

The signing key must match on both the issuing and validating sides.

| Key                      | Default                 | Notes                                                       |
| ------------------------ | ----------------------- | ----------------------------------------------------------- |
| `Jwt:Issuer`             | `https://cinedex.local` |                                                             |
| `Jwt:Audience`           | `cinedex-api`           |                                                             |
| `Jwt:SigningKey`         | dev placeholder         | Minimum 32 bytes for HS256 — see the warning below.         |
| `Jwt:AccessTokenMinutes` | `15`                    | Configurable; must be between 5 and 15 minutes (inclusive). |
| `Jwt:RefreshTokenDays`   | `7`                     | Configurable; must be between 1 and 7 days (inclusive).     |

| Key                                        | Default       | Notes                                                                                   |
| ------------------------------------------ | ------------- | --------------------------------------------------------------------------------------- |
| `RefreshTokenCleanup:Interval`             | `00:10:00`    | Time between sweeps.                                                                    |
| `RefreshTokenCleanup:ExpiredRetention`     | `1.00:00:00`  | How long an expired, never-revoked row is kept past expiry.                             |
| `RefreshTokenCleanup:ReuseDetectionWindow` | `14.00:00:00` | How long a revoked row is kept past revocation. Keep well above `Jwt:RefreshTokenDays`. |
| `RefreshTokenCleanup:BatchSize`            | `500`         | Rows per delete statement.                                                              |
| `RefreshTokenCleanup:MaxBatchesPerRun`     | `20`          | Batches per sweep.                                                                      |

> ⚠️ **The `Jwt:SigningKey` in `appsettings.json` is a committed, dev-only placeholder.** It is
> public and must never be used outside local development. Override it per environment via the
> `Jwt__SigningKey` environment variable or .NET User Secrets.
