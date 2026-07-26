# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **SMTP email delivery** - Replaced `NoOpEmailSender` with a MailKit-based `SmtpEmailSender` requiring username/password authentication and supporting configurable TLS and HTML/plain-text bodies. Docker Compose now connects the web service to Mailpit so password-reset emails are captured end to end during development. An integration test starts a pinned Mailpit Testcontainer and verifies authenticated delivery through the real adapter.

---

## [0.6.0] - 2026-07-18 Authentication & Authorization

### Added
- **Authentication & authorization via ASP.NET Core Identity** - Replaces the previously stubbed auth endpoints with a real implementation. Users can register, log in, refresh their session, log out, and reset their password. Login issues a JWT access token plus a rotating refresh token; protected endpoints are guarded by JWT bearer middleware. Identity is confined to a new persistence adapter behind application-layer ports, so the domain and application layers stay framework-free. See the [Auth & Security Model](https://github.com/felipedferreira/Cinedex/blob/main/docs/auth-security-model.md)
  - **Domain** — `User` aggregate in `Cinedex.Domain/UserAggregate/`, mirroring the existing Genre/Title aggregates
  - **Application** — `IIdentityService` / `ITokenService` / `IEmailSender` ports, `InvalidCredentialsException`, `AuthTokensDto`, and register/login/logout/refresh/forgot/reset handler slices with FluentValidation validators
  - **Adapter** — new `Cinedex.Auth.Identity` project: `AuthDbContext` (an `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`), `ApplicationUser : IdentityUser<Guid>`, hashed and rotating refresh-token storage, `JwtTokenService`, `IdentityService`, and the initial EF migration. All tables live in a dedicated `auth` schema with its own `__EFMigrationsHistory` table
  - **Presentation** — JWT bearer authentication and authorization middleware, DI wiring, an `InvalidCredentialsExceptionHandler` mapping to HTTP 401, real endpoints wired to the handlers, and the `Jwt` configuration section
  - **Endpoints** — `POST /movies-svc/auth/{register,login,refresh,logout,password/forgot,password/reset}`. Adds `refresh`; `logout` now requires a bearer token. `password/forgot` always returns `202` to avoid account enumeration
  - **Refresh token as an HttpOnly cookie** — `login` and `refresh` return the refresh token only as a `__Secure-cinedex_refresh_token` cookie (`HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/movies-svc/auth`), never in the response body, so a browser XSS defect cannot read it. `refresh` and `logout` read the cookie instead of a request body, and an invalid `refresh` clears it. `LoginResponse` now carries only the access token; the `RefreshRequest` / `LogoutRequest` bodies are gone. **Deployment constraint:** the UI and API must be same-site (shared domain or a reverse proxy) or the cookie is never sent. See the [Auth & Security Model](https://github.com/felipedferreira/Cinedex/blob/main/docs/auth-security-model.md)
  - **Role-based access control** — Three roles seeded via EF `HasData` (`RoleConfiguration` / `SeedRoleConstants`): `User`, `Moderator`, `Administrator`. `POST /auth/register` automatically places every new account in `User` (`IdentityService.RegisterAsync`). `RoleManager<IdentityRole<Guid>>` is registered via `.AddRoles<IdentityRole<Guid>>()`. The access token carries one `ClaimTypes.Role` claim per assignment, re-read from Identity on both issue and refresh, so `[Authorize(Roles = ...)]` works without further configuration once an endpoint opts in. Role names live in `RoleNames`. Bootstrapping the first `Administrator` is manual (no endpoint yet); no endpoint currently enforces roles — see Known gaps
  - **Members-only catalog** — every Genre and Title endpoint (reads and writes) requires a bearer token; anonymous catalog requests get `401`. The endpoints simply omit `AllowAnonymous()` and rely on FastEndpoints' secure-by-default behavior. No role restrictions yet: any logged-in account can browse and edit the catalog
  - **Tests** — auth integration tests exercising real register/login/refresh/logout/reset flows against Testcontainers Postgres, including the refresh-cookie attributes, that the token is absent from the login body, and that the access token carries a `role: User` claim; `CatalogAuthorizationTests` pins the 401 contract for anonymous catalog access, and the test fixture exposes an `AuthenticatedClient` (registers and logs in a fixture user) that the catalog tests use
  - ⚠️ `Jwt:SigningKey` in `appsettings.json` is a dev-only placeholder; override it via `Jwt__SigningKey` or User Secrets outside development
  - Known gaps: roles are seeded and issued in tokens but no endpoint restricts by role yet (Genre/Title endpoints require authentication, not a particular role), the first `Administrator` must be assigned manually, email delivery is a no-op so reset emails are not sent (see `Cinedex.Email.Smtp` below), no refresh-token reuse detection, and no CORS or reverse proxy yet (required before the browser can call the API cross-origin)
- **Mailpit dev mail sink (Docker Compose)** - Added a [Mailpit](https://mailpit.axllent.org/) service to `compose.yaml` that captures outgoing email in a web UI at http://localhost:8025 instead of delivering it, ready for the auth flows (e.g. password reset) once a real `SmtpEmailSender` replaces `NoOpEmailSender`. SMTP AUTH is enabled with developer-controlled credentials: `MP_SMTP_AUTH_ACCEPT_ANY` stays `false` and Mailpit validates against a password file generated at container start from the git-ignored `MAILPIT_SMTP_USER` / `MAILPIT_SMTP_PASSWORD` in `.env` (added to `.env.example`), so no credentials are committed. Ports are published on IPv4 loopback (`8025` UI, `1025` SMTP); documented for fresh setups in the [backend README](https://github.com/felipedferreira/Cinedex/blob/main/backend/README.md#-email-mailpit-dev-mail-sink)
- **`Cinedex.Email.Smtp` email adapter** - Extracted the `IEmailSender` implementation out of the auth adapter into its own driven adapter, since email delivery is a messaging concern rather than an authentication one. Holds `NoOpEmailSender` for now; the planned replacement is a MailKit-based `SmtpEmailSender` (MailKit is the recommended modern SMTP client — the built-in `System.Net.Mail.SmtpClient` is obsolete — and can target any relay via config). The `IEmailSender` port stays in `Cinedex.Application`; registered via `AddEmailAdapter`
- **Documentation directory** - `docs/` for design documentation that is versioned with the code and reviewed in the same pull request as the change it describes; `docs/README.md` indexes it
- **`EntityNotFoundException`** - Domain exception in `Cinedex.Application/Exceptions/` thrown by handlers when a requested entity does not exist; carries `entityName` and `id` for a self-describing message
- **Chain of Responsibility exception handling** - Replaced the monolithic `ExceptionHandlingMiddleware` with three focused `IExceptionHandler` implementations under `Cinedex.WebService/ExceptionHandlers/`:
  - `ValidationExceptionHandler` — `ValidationException` → HTTP 400 with per-field error map
  - `EntityNotFoundExceptionHandler` — `EntityNotFoundException` → HTTP 404 with the exception message as `detail`
  - `DefaultExceptionHandler` — catch-all → HTTP 500; also logs the exception (was missing before)
- **Per-use-case handlers with FluentValidation** - Decomposed `IMovieService` / `MovieService` into discrete CQRS-style handler classes in `Cinedex.Application/Movies/`:
  - `CreateMovieHandler` + `CreateMovieValidator` (title max 256 chars, year range 1888–current)
  - `UpdateMovieHandler` + `UpdateMovieValidator`
  - `DeleteMovieHandler`, `GetMovieByIdHandler`, `ListMoviesHandler`
  - Each feature folder contains command/query record, handler, interface, and optional validator
- **React + TypeScript + Vite frontend** - Scaffolded `frontend/cinadex-ui` with React 19, TypeScript, Vite, Vitest, ESLint, and Prettier; includes a working `App.tsx` shell and an `App.test.tsx` smoke test
- **Cinedex-branded landing & changelog pages** - Redesigned the web service's landing and changelog pages (served from `wwwroot`) with Cinedex product branding
- **`SmokeTests`** - Backend integration test asserting the API returns HTTP 200 on `GET /api/movies`
- **`CreateMovieEndpointTests`** - Integration tests for `POST /api/movies` covering happy path and FluentValidation error scenarios (missing title, out-of-range year, etc.)
- **Mono-repo layout** - Restructured the repository for a future standalone frontend:
  - All .NET solution files (`src/`, `tests/`, `Movies.slnx`, build props, `global.json`, coverage scripts) moved under `backend/` with `git mv` to preserve history
  - `frontend/` placeholder added for the upcoming SPA (Angular or React), which will consume the backend's OpenAPI spec
  - New repository-level `README.md` landing page; the architecture guide moved to `backend/README.md`
  - `compose.yaml` build context repointed to `./backend`; CI workflow now runs `dotnet` commands from `backend/`
- **Movies CRUD API** - First real resource, implemented with [FastEndpoints](https://fast-endpoints.com/) using the REPR (Request-Endpoint-Response) pattern — one class per endpoint under `Endpoints/Movies/`:
  - `GET /api/movies`, `GET /api/movies/{id}`, `POST /api/movies`, `PUT /api/movies/{id}`, `DELETE /api/movies/{id}`
  - `MovieMappings` - single translation point between `Contracts` DTOs and the `Domain.Movie` model
- **`MovieRepository`** - EF Core implementation of `IMovieRepository` in `Cinedex.Persistence.Postgres` (uses `ExecuteUpdateAsync`/`ExecuteDeleteAsync`)
- **`AddPersistence(connectionString)`** - DI extension that now owns the `MoviesDbContext` and repository registrations
- **Docker Compose configuration** - Complete multi-container setup with PostgreSQL:
  - `compose.yaml` with web service and PostgreSQL 17 Alpine
  - Health checks for database readiness before application startup
  - Environment variables for database configuration

### Changed
- **`GetMovieByIdHandler`, `UpdateMovieHandler`, `DeleteMovieHandler`** - Now throw `EntityNotFoundException` when the entity is not found instead of returning `null` / `false`; handler interfaces updated to non-nullable return types (`Task<MovieDto>` / `Task`)
- **`GetMovieByIdEndpoint`, `UpdateMovieEndpoint`, `DeleteMovieEndpoint`** - Null / bool guards removed; not-found signal is now the handler's responsibility via exception
- **`Program.cs`** - `UseMiddleware<ExceptionHandlingMiddleware>()` replaced by `UseExceptionHandler()`; three `AddExceptionHandler<T>()` registrations (order matters — `DefaultExceptionHandler` last); also registers `AddApplication()` + `AddPersistence()`, `AddFastEndpoints()`/`UseFastEndpoints()`, and serves the app under `/api` via `UsePathBase`
- **`DependencyInjection.cs`** (Application) - Handlers registered individually; validators auto-discovered via `AddValidatorsFromAssembly`; `FluentValidation` dependencies added to `Cinedex.Application.csproj`
- **CI workflow** - Updated to build and test the frontend alongside the backend
- **Hexagonal layer layout** - Reorganized the backend folders to make the Ports & Adapters layers explicit, dependencies still inward-only (`Domain ← Application ← {Adapters, Presentation}`). All moves used `git mv` to preserve history:
  - `src/Applications/` → `src/Presentation/` (hosts `Cinedex.WebService`)
  - `src/Core/` dissolved; `Cinedex.Application` and `Cinedex.Domain` now sit directly under `src/`
  - Updated all `ProjectReference` paths, `Movies.slnx` solution folders, `Dockerfile` COPY paths, and `compose.yaml`
- **`Directory.Packages.props`** - Centralized versions for `FastEndpoints`, `FluentValidation`, and `Microsoft.Extensions.DependencyInjection.Abstractions`
- **`Cinedex.WebService.csproj`** - Added `FastEndpoints` package reference and a project reference to `Cinedex.WebService.Contracts`
- **`Cinedex.Application.csproj`** - Added `Microsoft.Extensions.DependencyInjection.Abstractions` and `FluentValidation`
- **Dockerfile** - Fixed build paths; added copying of `Directory.Build.props`, `Directory.Packages.props`, and explicit COPY steps for all layer dependencies before restore
- **Clean-architecture cleanup** - Renamed `Cinedex.Persistance.Postgres` → `Cinedex.Persistence.Postgres` (typo fix); merged `Cinedex.Application.Abstractions` into `Cinedex.Application`

### Removed
- **`IMovieService` / `MovieService`** - Replaced by per-use-case handlers; the single-service pattern did not scale as use cases multiplied
- **`ExceptionHandlingMiddleware`** - Replaced by the `IExceptionHandler` chain; the class violated SRP by owning both exception routing and response formatting for every exception type
- **Template sample endpoints** - Deleted the project-template placeholders now that Movies is the first real resource:
  - `GET /weatherforecast` endpoint and the `WeatherForecast` record, plus `WeatherForecastEndpointTests`
  - `GET /test-exception` endpoint and `ExceptionHandlingMiddlewareTests` (the endpoint existed only to exercise the exception middleware)

### Fixed
- **API documentation page blank behind the reverse proxy** - Upgraded `Scalar.AspNetCore` 1.2.50 → 2.16.13. The 1.x page embedded the OpenAPI document URL as absolute `/openapi/v1.json`, ignoring the `/movies-svc` path base — through the Nginx proxy the browser fetched the SPA fallback HTML instead of the spec, so `/movies-svc/api-docs/v1` rendered nothing. Scalar 2.x ships relative document URLs and derives the base path in the browser, so the page works both through the proxy and against Kestrel directly. Migration notes: the removed `EndpointPathPrefix` option is replaced by the endpoint-prefix argument to `MapScalarApiReference("/api-docs", ...)` (the URL is unchanged), and the docs favicon path now includes the `/movies-svc` base path
- **Postgres password no longer committed** - `compose.yaml` hardcoded the database password while every other secret already lived in the git-ignored `.env`; it now reads `${DB_PASSWORD}` from `.env` (the old value remains in git history, so rotate it). The Postgres healthcheck also probes the real `movies_rw` user instead of `postgres`
- **Reverse-proxy forwarded headers honored** - The web service now applies `X-Forwarded-Proto` / `X-Forwarded-For` from the Nginx proxy when `ForwardedHeaders:Enabled` is set (enabled in Docker Compose), so the app sees the original HTTPS scheme and real client address instead of plain HTTP and the proxy's IP
- **Reverse-proxy host/port in redirects** - The proxy now forwards the browser-facing host and port via `$http_host`, so redirects target the published address instead of the internal container port
- **Flaky integration test startup** - Test classes now share one app host and Postgres Testcontainer via the `WebApplicationCollection` xUnit collection fixture; booting multiple `WebApplicationFactory<Program>` hosts in parallel raced on shared `JsonSerializerOptions` state inside FastEndpoints/System.Text.Json
- **`Microsoft.OpenApi` pinned to 2.7.5** - The transitive 2.0.0 pulled in by `Microsoft.AspNetCore.OpenApi` has a known vulnerability (GHSA-v5pm-xwqc-g5wc)
- **Docker build** - Corrected path references to resolve NuGet restore failures

---

## [0.5.0] - Entity Framework Core with PostgreSQL

### Added
- **Entity Framework Core 10** - ORM integration using `Npgsql.EntityFrameworkCore.PostgreSQL`
- **`MoviesDbContext`** - EF Core `DbContext` in `Cinedex.Persistence.Postgres`; uses Fluent API configuration via `ApplyConfigurationsFromAssembly` to keep domain models free of EF annotations
- **`MoviesDbContextFactory`** - `IDesignTimeDbContextFactory` implementation enabling `dotnet ef` CLI tools to run without starting the full application
- **Database connection string** - `DefaultConnection` added to `appsettings.json` with a default local PostgreSQL configuration

### Changed
- **`Program.cs`** - Registers `MoviesDbContext` via `AddDbContext<MoviesDbContext>` wired to `ConnectionStrings:DefaultConnection`
- **`Directory.Packages.props`** - Centralized versions for `Microsoft.EntityFrameworkCore.Design` and `Npgsql.EntityFrameworkCore.PostgreSQL`
- **`Cinedex.Persistence.Postgres.csproj`** - Added EF Core and Npgsql package references
- **`Cinedex.WebService.csproj`** - Added `Microsoft.EntityFrameworkCore.Design` (required as the migrations startup project)

---

## [0.4.1] - Exception Handling Tests and Formatting Rules

### Added
- **ExceptionHandlingMiddlewareTests** - Comprehensive integration tests for exception handling middleware:
  - `TestException_ReturnsInternalServerError` - Verifies 500 status code on unhandled exceptions
  - `TestException_ReturnsProblemDetailsJson` - Validates RFC 7807 Problem Details response format
  - `TestException_ReturnsProblemDetailsWithRequiredFields` - Ensures all required fields in error response
  - `TestException_IncludesTraceIdInResponse` - Confirms trace ID in error extensions for debugging
- **EditorConfig ReSharper rules** - Enforce blank lines between POCO object properties:
  - `resharper_blank_lines_around_auto_property = 1` - Blank lines around auto-properties
  - `resharper_blank_lines_around_property = 1` - Blank lines around full properties
  - `resharper_blank_lines_after_block_statements = 1` - Blank lines after block statements

### Fixed
- **Build configuration** - Added `GenerateDocumentationFile=true` to `Cinedex.WebService.Contracts.csproj`
  - Enables IDE0005 (Remove unnecessary imports) rule enforcement on build

---

## [0.4.0] - Exception Handling Middleware (#4)

### Added
- **ExceptionHandlingMiddleware** - Proper error handling for unhandled exceptions:
  - Returns RFC 7807 Problem Details format
  - Includes trace ID in response extensions for request correlation
  - Proper async/await pattern for middleware execution
  - Correct content-type header (`application/problem+json`)
- **Test endpoint `/test-exception`** - Endpoint for testing exception handling behavior
- **GitHub branch protection** - Configured to prevent problematic merges:
  - Requires status checks to pass before merging
  - Enforces branches must be up-to-date before merge

### Changed
- **ExceptionHandlingMiddleware** - Improved JSON serialization:
  - Uses manual JSON serialization to preserve content-type
  - Properly serializes `extensions` object with `traceId`
  - Removed dependency on `Microsoft.AspNetCore.Mvc.ProblemDetails`
- **Program.cs** - Code organization improvements with better blank lines

---

## [0.3.1] - Code Quality Improvements

### Changed
- **Code quality enhancements** - Refactoring and standards improvements
- **Documentation file generation** - Added documentation generation for code analysis

---

## [0.3.0] - Structured Logging with Serilog (#3)

### Added
- **Serilog integration** - Structured logging with console output
- **CorrelationIdMiddleware** - Middleware for request correlation tracking
- **Request logging** - Automatic logging of HTTP requests via Serilog

### Changed
- **Logging configuration** - Replaced default logging with Serilog structured logging

---

## [0.2.1] - Integration Tests and Code Coverage

### Added
- **WeatherForecastEndpointTests** - Integration tests for WeatherForecast endpoint:
  - Test for 200 OK status
  - Test for JSON array response
  - Test for 5-day forecast data
  - Test for required fields in forecast objects
- **WebApplicationFixture** - Test fixture for integration testing setup
- **Code coverage configuration** - Local code coverage reporting setup

---

## [0.2.0] - Request Timeout Configuration (#2)

### Added
- **Kestrel timeout configuration** - Request timeout settings:
  - `RequestHeadersTimeout` - Protection against slowloris attacks
  - `KeepAliveTimeout` - Idle connection timeout management

---

## [0.1.0] - Initial Project Setup

### Added
- **GitHub Actions CI/CD** - Automated build and test workflow (build-and-test.yml)
- **.NET SDK configuration** - Global.json for SDK version management
- **Docker support** - Docker configuration and .dockerignore
- **Project structure** - Core project organization with:
  - Cinedex.WebService (main API)
  - Cinedex.Domain (domain models)
  - Cinedex.Application (business logic)
  - Cinedex.Application.Abstractions (interfaces)
  - Cinedex.Persistence.Postgres (data access)
  - Cinedex.WebService.Contracts (DTOs)
  - Integration tests project
- **WeatherForecast endpoint** - Sample endpoint implementation

---

## Version Numbering Strategy

This project follows [Semantic Versioning](https://semver.org/) with the following scheme for pre-1.0 development:

- **PATCH** (0.x.y) - Bug fixes, testing infrastructure, tooling improvements, code quality
- **MINOR** (0.x.0) - New features or significant capability additions
- **MAJOR** (1.0+) - Reserved for production-ready release with breaking changes
