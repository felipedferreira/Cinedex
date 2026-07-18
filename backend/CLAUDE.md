# Cinedex Backend

.NET 10 hexagonal (ports & adapters) solution. Warnings are errors (StyleCop + .NET analyzers, configured in `Directory.Build.props`); package versions are centralized in `Directory.Packages.props` (test-only package versions live in `Directory.Build.props`). Solution file is `Cinedex.slnx` (XML solution format).

## Commands (from `backend/`)

```bash
dotnet build          # also runs the embedded Vite build (see below)
dotnet test           # integration tests — REQUIRES Docker running (Testcontainers Postgres)
dotnet test --filter "GenreEndpointTests"
dotnet run --project src/Presentation/Cinedex.WebService   # https://localhost:7201
.\coverage.ps1 -Open  # HTML coverage report (needs dotnet-reportgenerator-globaltool installed once)
```

First build on a fresh clone: run `npm ci` in `src/Presentation/Cinedex.WebService/` first. The csproj's `BuildFrontend` target runs `npx vite build` before every build to populate `wwwroot/` (the service's landing/changelog pages). Skip it with `-p:SkipFrontendBuild=true`. The same target refreshes `backend/CHANGELOG.md` from the root `CHANGELOG.md` — never edit the backend copy by hand; commit the diff the build produces.

## Architecture

Dependencies point inward: WebService → Adapters → Application → Domain.

- `src/Domain` — pure entities (`Title`, `Genre`, `User`); no framework dependencies allowed.
- `src/Application` — vertical-slice use cases + ports under `Abstractions/`. Each use case folder holds `<Name>Command`/`<Name>Query`, `<Name>Handler`, `I<Name>Handler`, and (for writes) `<Name>Validator` (FluentValidation).
- `src/Adapters/Cinedex.Persistence.Postgres` — `FilmDbContext`, `catalog` schema (titles, genres), Fluent API configs (domain stays EF-free).
- `src/Adapters/Cinedex.Auth.Identity` — ASP.NET Core Identity + JWT issuance, `AuthDbContext`, `auth` schema.
- `src/Adapters/Cinedex.Email.Smtp` — `IEmailSender` port implementation (currently `NoOpEmailSender`).
- `src/Presentation/Cinedex.WebService` — FastEndpoints (one class per endpoint), exception-handler chain (`ExceptionHandlers/`, registration order matters, `DefaultExceptionHandler` last), health checks (`/health/live`, `/health/ready`), OpenTelemetry → Seq.
- `NuGetLibraries/Cinedex.WebService.Contracts` — shared request/response DTOs (the packable API contract).

Handler conventions: use cases expose `HandleAsync(...)`; create handlers return the new `Guid` (presentation builds the `Location` header); update/delete handlers return `Task`; repository create ports return `Task`, not the saved entity.

Routes: base path `/movies-svc`; catalog resources are `titles` and `genres`; auth is `auth/register|login|refresh|logout|password/forgot|password/reset`. All route strings live in `Constants/ApiConstants.cs` — the entity is `Title`, not `Movie`, despite the `/movies-svc` base path.

Scalar API docs and the OpenAPI JSON are served only when `Features:ApiDocumentationEnabled` is true — `false` in `appsettings.json` (production default); Development settings and compose turn it on. If `/api-docs/v1` 404s, check this flag first.

## EF Core migrations — two contexts, always pass `--context`

Two DbContexts share one database with separate schemas and `__EFMigrationsHistory` tables. Every `dotnet ef` command MUST pass `--context`, or it fails with "More than one DbContext was found". **Nothing applies migrations automatically** (only the integration-test fixture migrates itself); a fresh database needs both:

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

JWT bearer (15-minute HS256 access token) + rotating 7-day refresh token stored hashed and delivered as a `Secure` cookie. **All catalog endpoints require authentication**; only auth and health endpoints are anonymous. `Jwt:SigningKey` in `appsettings.json` is a dev-only placeholder — override per environment (`Jwt__SigningKey`/User Secrets). Full model and known gaps: `docs/auth-security-model.md`.

## Testing

- xUnit integration tests in `tests/Cinedex.WebService.IntegrationTests`. `WebApplicationFixture` starts a `postgres:17-alpine` Testcontainer, migrates both contexts, and exposes three clients: `Client` (cookie jar), `CookielessClient` (for tests presenting a specific refresh cookie), and `AuthenticatedClient` (pre-authenticated bearer — use it for catalog endpoints, which are members-only).
- Test base address is `https://localhost` because `CookieContainer` refuses to send a `Secure` cookie over http.
- `CapturingEmailSender` replaces `IEmailSender` so password-reset tokens are captured, not sent.
- Naming: `Action_Condition_Result`, e.g. `GetMovie_WithUnknownId_ReturnsNotFound`.
