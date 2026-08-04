# Cinedex Backend

.NET 10 hexagonal (ports & adapters) solution. Warnings are errors (StyleCop + .NET analyzers, configured in `Directory.Build.props`); package versions are centralized in `Directory.Packages.props` (test-only package versions live in `Directory.Build.props`). Solution file is `Cinedex.slnx` (XML solution format).

## Commands (from `backend/`)

```bash
dotnet build          # also runs the embedded Vite build (see below)
dotnet test           # integration tests — REQUIRES Docker running (Testcontainers Postgres)
dotnet test --filter "GenreEndpointTests"
dotnet run --project src/Presentation/Cinedex.WebService   # https://localhost:7201
dotnet run --project aspire/Cinedex.AppHost                # whole stack via Aspire (needs Docker)
.\coverage.ps1 -Open  # HTML coverage report (needs dotnet-reportgenerator-globaltool installed once)
```

First build on a fresh clone: run `npm ci` in `src/Presentation/Cinedex.WebService/` first. The csproj's `BuildFrontend` target runs `npx vite build` before every build to populate `wwwroot/` (the service's landing/changelog pages). Skip it with `-p:SkipFrontendBuild=true`. The same target refreshes `backend/CHANGELOG.md` from the root `CHANGELOG.md` — never edit the backend copy by hand; commit the diff the build produces.

## Architecture

Dependencies point inward: WebService → Adapters → Application → Domain.

- `src/Domain` — pure entities (`Title`, `Genre`, `User`); no framework dependencies allowed.
- `src/Application` — vertical-slice use cases + ports under `Abstractions/`. Each use case folder holds `<Name>Command`/`<Name>Query`, `<Name>Handler`, `I<Name>Handler`, and (for writes) `<Name>Validator` (FluentValidation).
- `src/Adapters/Cinedex.Persistence.Postgres` — `FilmDbContext`, `catalog` schema (titles, genres), Fluent API configs (domain stays EF-free).
- `src/Adapters/Cinedex.Auth.Identity` — ASP.NET Core Identity + JWT issuance, `auth` schema. Refresh-token persistence goes through `IRefreshTokenRepository` (`Persistence/Repository/`), backed by `AuthDbContext`; the persisted `RefreshToken` entity lives under `Persistence/Entities`.
- `src/Adapters/Cinedex.Email.Smtp` — MailKit SMTP implementation of the `IEmailSender` port, plus the delivery queue: `ChannelEmailDispatcher` (the `IEmailDispatcher` port) enqueues, and the `EmailDeliveryWorker` background service drains it. Request-path code must depend on `IEmailDispatcher`, never `IEmailSender` — awaiting SMTP inline reopens an account-enumeration timing oracle on `password/forgot`.
- `src/Presentation/Cinedex.WebService` — FastEndpoints (one class per endpoint), exception-handler chain (`ExceptionHandlers/`, registration order matters, `DefaultExceptionHandler` last), health checks (`/health/live`, `/health/ready`), OpenTelemetry → Seq.
- `NuGetLibraries/FoundryOceanus.WebService.Contracts` — shared request/response DTOs (the packable API contract).
- `NuGetLibraries/FoundryOceanus.Persistence.*` — three packable libraries providing a unit of work over EF Core. **Nothing in `src/` consumes them yet** — they ship standalone; migrating `FilmDbContext`/`AuthDbContext` onto them is separate, unstarted work. The split enforces dependency direction: `.Abstractions` (the ports — `IUnitOfWork`, `ITransaction`, `IRepository`, the `PersistenceException` hierarchy) deliberately has **zero PackageReference entries**, so an application project referencing it cannot reach EF; `.EntityFrameworkCore` implements them; `.EntityFrameworkCore.Postgres` adds SQLSTATE translation and `pg_advisory_xact_lock` helpers. Register with `AddNpgsqlUnitOfWork<TContext>(...)` **after** `AddDbContext` — a non-scoped context is rejected, since the whole design rests on the unit of work and its repositories sharing one `DbContext` per scope. Full rationale in each package's `README.md`.
- `aspire/Cinedex.AppHost` — Aspire orchestration for local dev (Postgres + Mailpit containers, the three hosts as processes). **No ServiceDefaults project on purpose**: `AddObservability` already consumes the `OTEL_EXPORTER_OTLP_*` variables the AppHost injects, and health endpoints already exist, so the only things ServiceDefaults would add are service discovery and HTTP resilience — neither of which this codebase uses. The `Aspire.AppHost.Sdk` version is pinned inline in the csproj because central package management does not cover MSBuild SDKs; keep it in step with the `Aspire.Hosting.*` versions in `Directory.Packages.props`.

The web service runs under its `aspire` launch profile (`Properties/launchSettings.json`), which is HTTP-only on port 5187. **Do not remove that profile or point the AppHost at `launchProfileName: null`.** Visual Studio launches Aspire project resources itself and reads a web project's URLs from its launch profile; without one, Kestrel binds 5000/5001 while Aspire's proxy forwards elsewhere, and the resource hangs at `Running (Unhealthy)`. `dotnet run` does not show this — there the orchestrator injects the port itself.

## AppHost per-developer config

Committed defaults are in `aspire/Cinedex.AppHost/appsettings.json`. Override them **without editing a tracked file** via either channel — User Secrets win, because the host adds them after `appsettings.Development.json`:

```bash
# from aspire/Cinedex.AppHost/
dotnet user-secrets set "Features:EnableDatabaseMigrationsSvc" "false"
# or: cp appsettings.Development.json.example appsettings.Development.json   (git-ignored)
```

Both channels only apply in **Development**, which the launch profiles in `Properties/launchSettings.json` set. `dotnet run` and the Rider configuration both go through a profile, so they pick the overrides up; running `bin/…/Cinedex.AppHost.exe` directly does not — it defaults to Production, where the host skips user secrets, so you get the committed defaults and Aspire regenerates the Postgres password instead of reading the one matching the data volume.

`Features:EnableDatabaseMigrationsSvc` (default `true`) controls whether the migrator runs. Set it to `false` and the resource is **omitted from the graph entirely** — the web service and scheduler worker then wait on Postgres directly instead of on the migrator. Faster startup, but nothing applies migrations, so turn it back on for one run after pulling a new migration or when starting against an empty database. Note `dotnet user-secrets list` here also shows the Postgres password Aspire generated on first run — deleting it makes Aspire generate a new one that won't match the `cinedex-aspire-pgdata` volume.

`Features:EnableMailpitSvc` (default `true`), same pattern, controls whether the Mailpit container runs. `false` omits it from the graph; the web service still boots (`Smtp__Username`/`Smtp__Password` are set unconditionally so `SmtpOptions.ValidateOnStart` passes, and `Smtp__Host`/`Smtp__Port` fall back to the web service's own `appsettings.Development.json` default of `localhost:1025`) but outgoing email has nowhere to land — `EmailDeliveryWorker` logs the failure and moves on rather than crashing. Turn it back on to actually read a password-reset email at `http://localhost:8025`.

The `.gitignore` rule for `appsettings.Development.json` is scoped to this one project path on purpose — the web service's `appsettings.Development.json` is tracked.

Handler conventions: use cases expose `HandleAsync(...)`; create handlers return the new `Guid` (presentation builds the `Location` header); update/delete handlers return `Task`; repository create ports return `Task`, not the saved entity.

Routes: base path `/movies-svc`; catalog resources are `titles` and `genres`; auth is `auth/register|login|refresh|logout|password/forgot|password/reset`. All route strings live in `Constants/ApiConstants.cs` — the entity is `Title`, not `Movie`, despite the `/movies-svc` base path.

Scalar API docs and the OpenAPI JSON are served only when `Features:ApiDocumentationEnabled` is true — `false` in `appsettings.json` (production default); Development settings and compose turn it on. If `/api-docs/v1` 404s, check this flag first.

## EF Core migrations — two contexts, always pass `--context`

Two DbContexts share one database with separate schemas and `__EFMigrationsHistory` tables. Every `dotnet ef` command MUST pass `--context`, or it fails with "More than one DbContext was found". **Nothing applies migrations automatically** — except the integration-test fixture, which migrates itself, and the Aspire AppHost, which runs `Cinedex.DatabaseMigrator` to completion before starting anything else. Everywhere else a fresh database needs both:

```bash
dotnet ef database update --context FilmDbContext \
  --project src/Adapters/Cinedex.Persistence.Postgres \
  --startup-project src/Presentation/Cinedex.WebService

dotnet ef database update --context AuthDbContext \
  --project src/Adapters/Cinedex.Auth.Identity \
  --startup-project src/Presentation/Cinedex.WebService
```

Use the same shape with `migrations add <Name>`. The connection string resolves from the WebService's User Secrets in Development (`dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."` from the WebService directory), or pass `--connection "..."` explicitly (e.g. to target the compose Postgres on `localhost:5432`).

## Auth

JWT bearer (15-minute HS256 access token) + rotating 7-day refresh token stored hashed and delivered as a `Secure` cookie. **All catalog endpoints require authentication**; only auth and health endpoints are anonymous. Roles (`User`/`Moderator`/`Administrator`, constants in `RoleNames`) are seeded and issued as token claims — registration assigns `User` — but no endpoint restricts by role yet. `Jwt:SigningKey` in `appsettings.json` is a dev-only placeholder — override per environment (`Jwt__SigningKey`/User Secrets). Full model and known gaps: `docs/auth-security-model.md`.

## Testing

- `tests/Cinedex.Persistence.Tests` covers the `Cinedex.Persistence.*` libraries. Everything outside `Integration/` runs without Docker (`dotnet test --filter "FullyQualifiedName!~Integration"`); the `Integration/` folder shares one `postgres:17-alpine` Testcontainer via `PostgresCollection` and runs sequentially, because several tests assert on total row counts or contend for one advisory-lock key.
- `SmtpEmailSenderTests` starts a pinned Mailpit Testcontainer on random host ports and verifies authenticated HTML/plain-text delivery through the real MailKit adapter and Mailpit API.

- xUnit integration tests in `tests/Cinedex.WebService.IntegrationTests`. `WebApplicationFixture` starts a `postgres:17-alpine` Testcontainer, migrates both contexts, and exposes three clients: `Client` (cookie jar), `CookielessClient` (for tests presenting a specific refresh cookie), and `AuthenticatedClient` (pre-authenticated bearer — use it for catalog endpoints, which are members-only).
- Test base address is `https://localhost` because `CookieContainer` refuses to send a `Secure` cookie over http.
- Web-service endpoint tests replace `IEmailSender` with `CapturingEmailSender` so password-reset tokens are captured, not sent. Delivery is queued, so tests must `await fixture.EmailSender.WaitForMessageAsync(email)` rather than read a property synchronously; `BlockDelivery()`/`ResumeDelivery()` gate delivery for tests that assert the response precedes it.
- Naming: `Action_Condition_Result`, e.g. `GetMovie_WithUnknownId_ReturnsNotFound`.
