# Private Catalog Authentication — Design

**Date:** 2026-07-15
**Branch:** `asp-net-identity-auth`
**Status:** Approved

## Goal

The Cinedex catalog is members-only. Every Title and Genre endpoint — reads and writes — requires
an authenticated user. No role restrictions: any logged-in account can both browse and edit the
catalog. Anonymous requests to any catalog endpoint receive `401 Unauthorized`.

## Current state

Fifteen of the sixteen endpoints call `AllowAnonymous()`; the exception is `POST /auth/logout`,
which already requires a bearer token. JWT bearer authentication and the authorization middleware are fully wired
(`AuthenticationExtensions.AddJwtAuthentication`); the access token carries the claims the
authorization layer needs. The frontend SPA is still a scaffold with no API calls, so nothing
outside the backend consumes these endpoints yet.

## Approach

Rely on FastEndpoints' secure-by-default behavior: an endpoint that does not call
`AllowAnonymous()` requires an authenticated user. The API-side change is therefore only the
removal of the `AllowAnonymous()` call from the 10 catalog endpoints.

Alternatives considered and rejected:

- **ASP.NET fallback policy** (`AuthorizationOptions.FallbackPolicy`) — redundant with
  FastEndpoints' default and introduces a second source of truth for auth behavior.
- **Global FastEndpoints configurator** (`Config.Endpoints.Configurator`) — a mechanism for
  forcing settings across endpoints, but the framework default is already the desired behavior;
  there is nothing to force.

## Changes

### Endpoints (Presentation)

Remove `AllowAnonymous()` from `Configure()` in:

| Titles | Genres |
|---|---|
| `GetAllTitlesEndpoint` | `GetAllGenresEndpoint` |
| `GetTitleByIdEndpoint` | `GetGenreByIdEndpoint` |
| `CreateTitleEndpoint` | `CreateGenreEndpoint` |
| `UpdateTitleEndpoint` | `UpdateGenreEndpoint` |
| `DeleteTitleEndpoint` | `DeleteGenreEndpoint` |

Auth endpoints are unchanged: `register`, `login`, `refresh`, `password/forgot`, and
`password/reset` remain anonymous by necessity; `logout` already requires a bearer token.

### Integration tests

- `WebApplicationFixture` registers a dedicated test user and logs in once during
  `InitializeAsync`, then exposes an `AuthenticatedClient`: an `HttpClient` whose default
  `Authorization` header carries the bearer token. The 15-minute default access-token lifetime
  (configurable from 5 to 15 minutes through `Jwt:AccessTokenMinutes`) covers a
  test run.
- Genre and Title test classes (`GenreEndpointTests`, `CreateTitleEndpointTests`,
  `TitleGenreEndpointTests`) switch from `Client` to `AuthenticatedClient`.
- New contract tests assert that an unauthenticated request returns `401` for one representative
  catalog read (`GET /titles`) and one write (`POST /titles`).

### Documentation

Update `docs/auth-security-model.md`:

- The endpoint table gains the 10 catalog routes marked **Bearer** (or states the rule that all
  non-auth endpoints require a bearer token).
- The "no endpoint yet enforces roles" known-gap entry is reworded: catalog endpoints now require
  authentication, but roles remain unused — that part stays a documented gap.

## Out of scope

- Frontend auth flow (login page, token storage, attaching bearer headers) — the SPA has no API
  calls yet.
- Role enforcement (`Moderator` / `Administrator` on writes) and admin bootstrapping.
- CORS, email delivery, refresh-token reuse detection — unchanged known gaps.

## Success criteria

- Anonymous requests to any Title or Genre endpoint return `401`.
- The same requests with a valid bearer token behave exactly as before.
- Full integration-test suite passes.
