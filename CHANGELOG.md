# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

---

## [0.7.0] - August 3 2026 Orchestration, Email & Token Hardening

### Added
- **Database migrations applied automatically on the Compose path (`Cinedex.DatabaseMigrator`)** - A new run-to-completion .NET Generic Host presentation project that applies the EF Core migrations for both `FilmDbContext` and `AuthDbContext` and then exits, wired into Compose as `movies.databasemigrator`. The web service and the scheduler worker gate on it with `condition: service_completed_successfully`, so neither can start against an unmigrated schema, and the migrator itself waits for Postgres and Seq to report healthy. This retires the manual step on the Compose path — two `dotnet ef database update` invocations, one per `DbContext`. It does not change a bare `dotnet run`, which still applies nothing.
  - **It stops itself rather than waiting to be stopped.** `DatabaseMigrationHostedService` applies both migrations from `StartAsync` inside a single `AsyncServiceScope`, then calls `IHostApplicationLifetime.StopApplication` in a `finally`, so the container exits on failure as well as on success instead of idling and holding every dependent service behind a `service_completed_successfully` that will never be satisfied. `Program.Main` returns 1 when the host throws and 0 otherwise, giving Compose a real exit status to gate on; the service is pinned to `restart: "no"` so a deliberate exit is never treated as a crash to retry.
  - **Configuration comes from `application.json`, not `appsettings.json`.** It is loaded explicitly from `AppContext.BaseDirectory`, with environment variables and command-line arguments re-added afterwards so they keep their usual precedence over the migrator-specific files. Telemetry goes through the same shared `AddObservability` extension as every other host, with the `Npgsql` source registered, so a slow or failing migration shows up in Seq alongside the rest of the stack rather than only in container logs. Setup notes in the [project README](backend/src/Presentation/Cinedex.DatabaseMigrator/README.md).
- **Persistence abstraction libraries (`NuGetLibraries/FoundryOceanus.Persistence.*`)** - Three packable libraries that put a unit of work, real transactions and repository resolution in front of Entity Framework, so the application layer can be written without a reference to it. Nothing in `src/` consumes them yet; this ships the libraries, their documentation and their tests, and leaves migrating the existing adapters as separate work with its own review.
  - **The split is the whole point.** `FoundryOceanus.Persistence.Abstractions` holds the ports — `IUnitOfWork`, `ITransaction`, `ISavepoint`, `IRepository`, the exception hierarchy — and has **no `PackageReference` entries at all**, which is the only way the "application code must not depend on infrastructure" rule survives contact with a deadline. `FoundryOceanus.Persistence.EntityFrameworkCore` implements them against `Microsoft.EntityFrameworkCore.Relational`, and `FoundryOceanus.Persistence.EntityFrameworkCore.Postgres` adds the Npgsql-specific half on top of `Npgsql` alone — not the EF provider package, which would have pinned a provider version for consumers that already choose their own.
  - **`IUnitOfWork` exposes five members where `DbContext` exposes around forty.** No `IQueryable`, no `DbSet`, no `ChangeTracker`, no raw-SQL escape hatch: a composable query handed to a command handler is the ORM with extra steps, and code that genuinely needs raw SQL belongs in an adapter where it can depend on the provider honestly. There is deliberately no `IRepository<TEntity>` with `GetAll`/`Add`/`Update`/`Delete` either — a generic CRUD repository is the same unconstrained surface under a different name. `IRepository` is a bare marker that constrains what `Repository<T>()` will resolve; the interfaces deriving from it are meant to be named for the domain (`IRefreshTokenRepository.RotateAsync`), not for the database.
  - **One `DbContext` per scope, shared between the unit of work and every repository it hands out** — the invariant that makes a single `SaveChangesAsync` flush all their writes and a single transaction cover all their statements. It is checked rather than trusted: repositories deriving from `EfRepository<TContext>` implement `IDbContextBound`, and resolving one bound to a different context throws instead of letting its writes commit on a second connection outside the caller's transaction, which is what an injected `IDbContextFactory` silently does today. Registering the context as `Singleton` or `Transient` is rejected at startup for the same reason.
  - **`TransactionIsolationLevel` has four values, not `System.Data.IsolationLevel`'s seven.** `ReadUncommitted`, `Snapshot` and `Chaos` are fictions on PostgreSQL — the first is silently treated as `ReadCommitted` — and offering levels the database will quietly reinterpret is how an abstraction starts lying to the people reading it. Transactions do not nest, because databases do not: `BeginTransactionAsync` throws rather than returning a handle whose rollback would do nothing, and `CreateSavepointAsync` covers partial rollback. Disposing an uncommitted transaction rolls it back; `RollbackAsync` tolerates being called on a completed transaction while `CommitAsync` does not, so a rollback in a `catch` block cannot throw over the exception that sent it there.
  - **Provider errors are classified once, in one file.** `NpgsqlExceptionTranslator` maps SQLSTATE to `DuplicateKeyException`, `ReferentialIntegrityException`, `ConcurrencyConflictException` and `TransientPersistenceException`, and claims everything it cannot name as `UnclassifiedPersistenceException` rather than letting an `NpgsqlException` reach code written not to know PostgreSQL exists. Translator order is load-bearing and `AddNpgsqlUnitOfWork` arranges it: the EF translator's catch-all would otherwise claim every `DbUpdateException` before any SQLSTATE was read. `57014` is deliberately *not* transient — it is nearly always `statement_timeout`, and retrying a too-slow query is how a slow endpoint becomes a busy one. The SQLSTATE constants come from Npgsql's own `PostgresErrorCodes`, which already has all 238; a private copy would have been a second list to keep in step with PostgreSQL for no benefit.
  - **`ExecuteInTransactionAsync` retries only on `IsTransient`,** with exponential backoff and jitter — two transactions that collided and retry after the same fixed delay just schedule the same collision again. It calls `DiscardChanges()` between attempts, because a rollback undoes the database's work but leaves the failed attempt's entities tracked against a snapshot that no longer exists. The requirement that the operation be safe to run twice is documented on the method rather than assumed.
  - **Advisory locks hang off `DbContext`, not `IUnitOfWork`,** and only in the PostgreSQL package. An advisory lock is a database mechanism, not a business concept; needing a `DbContext` to call one is what keeps it in the adapter where it belongs. All are transaction-scoped (`pg_advisory_xact_lock`) — session-scoped locks are not offered, because with connection pooling one that is not explicitly released stays held on a pooled connection and is eventually handed to an unrelated request. Text keys are hashed by `hashtextextended` **in PostgreSQL**: `string.GetHashCode()` is randomised per process on .NET Core, so hashing in .NET would give each instance of a service a different lock for the same key — invisible on one machine, and broken the moment it scales out.
  - **`IUnitOfWorkScopeFactory` gives background services a per-iteration scope** without injecting `IServiceScopeFactory`, which works but puts the container back in front of code that had stopped knowing about it.
  - Tested in a new `tests/Cinedex.Persistence.Tests`: 32 tests that need no database (SQLSTATE mapping, translator ordering, registration and lifetime validation, the retry loop against a recording fake) plus integration tests on a `postgres:17-alpine` Testcontainer for the things a fake cannot honestly stand in for — real 23505 translation, savepoint rollback, advisory-lock contention, and a genuine write-skew serialization failure at `Serializable`.

---

## [0.7.0] - August 3 2026 Orchestration, Email & Token Hardening

### Added
- **Database migrations applied automatically on the Compose path (`Cinedex.DatabaseMigrator`)** - A new run-to-completion .NET Generic Host presentation project that applies the EF Core migrations for both `FilmDbContext` and `AuthDbContext` and then exits, wired into Compose as `movies.databasemigrator`. The web service and the scheduler worker gate on it with `condition: service_completed_successfully`, so neither can start against an unmigrated schema, and the migrator itself waits for Postgres and Seq to report healthy. This retires the manual step on the Compose path — two `dotnet ef database update` invocations, one per `DbContext`. It does not change a bare `dotnet run`, which still applies nothing.
  - **It stops itself rather than waiting to be stopped.** `DatabaseMigrationHostedService` applies both migrations from `StartAsync` inside a single `AsyncServiceScope`, then calls `IHostApplicationLifetime.StopApplication` in a `finally`, so the container exits on failure as well as on success instead of idling and holding every dependent service behind a `service_completed_successfully` that will never be satisfied. `Program.Main` returns 1 when the host throws and 0 otherwise, giving Compose a real exit status to gate on; the service is pinned to `restart: "no"` so a deliberate exit is never treated as a crash to retry.
  - **Configuration comes from `application.json`, not `appsettings.json`.** It is loaded explicitly from `AppContext.BaseDirectory`, with environment variables and command-line arguments re-added afterwards so they keep their usual precedence over the migrator-specific files. Telemetry goes through the same shared `AddObservability` extension as every other host, with the `Npgsql` source registered, so a slow or failing migration shows up in Seq alongside the rest of the stack rather than only in container logs. Setup notes in the [project README](backend/src/Presentation/Cinedex.DatabaseMigrator/README.md).
- **Aspire AppHost for local orchestration (`aspire/Cinedex.AppHost`)** - A one-command inner loop that stands up Postgres and Mailpit as containers and runs the migrator, web service and scheduler worker as local processes, with the Aspire dashboard collecting their logs and traces. It sits alongside `docker compose up` rather than replacing it: `compose.yaml` is untouched and remains the prod-like path, with built images, the Nginx/HTTPS proxy, Seq and the SPA. Registered in `Cinedex.slnx` under a new `/aspire/` folder; Aspire versions are centralized in `Directory.Packages.props`, except the `Aspire.AppHost.Sdk` version, which is pinned inline because central package management governs `PackageReference` and not MSBuild project SDKs.
  - **Migrations are applied for you on this path.** The AppHost models `Cinedex.DatabaseMigrator` as a run-to-completion resource and gates the web service and the scheduler worker behind `WaitForCompletion`, the direct analogue of Compose's `condition: service_completed_successfully`. The long-standing "nothing applies migrations automatically" step — two `dotnet ef database update` invocations, one per `DbContext` — does not apply when starting the stack this way.
  - **…and can be skipped once the schema is current.** `Features:EnableDatabaseMigrationsSvc`, committed as `true` in the AppHost's `appsettings.json`, drops the migrator from the resource graph entirely when set to `false`; the web service and the scheduler worker then wait on Postgres directly rather than on a resource that will never run. It defaults to `true` when the key is missing, so a config file predating the flag still migrates instead of silently starting the services against an unmigrated schema. Developers override it per-machine through either channel, without editing a tracked file: User Secrets (`dotnet user-secrets set "Features:EnableDatabaseMigrationsSvc" "false"`), or a git-ignored `appsettings.Development.json` seeded from the committed `appsettings.Development.json.example`. User Secrets win where both set the same key, since the host adds them last; both apply only in Development, which the launch profiles set, so `dotnet run` and the Rider configuration pick them up while running the built executable bare does not. The ignore rule is scoped to the AppHost path alone — the web service's `appsettings.Development.json` stays tracked. The settings files are also given explicit `CopyToOutputDirectory` metadata, matching how the scheduler worker and migrator handle `application.json`: the Aspire SDK chains `Microsoft.NET.Sdk` rather than the Web SDK, so `appsettings.json` is only a `None` item and nothing would otherwise place it next to the executable.
  - **There is deliberately no `Cinedex.ServiceDefaults` project.** The packable `FoundryOceanus.Observability.OpenTelemetry` library already fills that role: every host calls `AddObservability`, which reads the standard `OTEL_EXPORTER_OTLP_*` variables that an AppHost injects, so logs and traces reach the dashboard with no change to any service — the scheduler worker's startup line reports `telemetry export enabled` on its own. Health endpoints already exist too. What ServiceDefaults would add beyond that is service discovery and HTTP resilience defaults, neither of which this codebase uses, so a second project would have been overlap rather than value.
  - **Run one stack or the other, not both.** The AppHost's Postgres publishes host port 5432, the same as Compose, so whichever starts second fails to bind. The data volume is still separate — `cinedex-aspire-pgdata` rather than `cinedex_postgres_data` — because a port clash is an error you see immediately, whereas a shared data directory would silently mix two databases.
  - **The Postgres password is developer-supplied, not generated.** `Parameters:postgres-password` has no committed value and no default: set it in the AppHost's User Secrets (`dotnet user-secrets set "Parameters:postgres-password" "<password>"`). The AppHost checks for it before building the resource graph and throws with that command in the message, because Aspire's own handling is silent — it leaves the unresolved parameter to the dashboard, brings up the containers that don't depend on it, and never says why Postgres is missing. Note Postgres only reads the password when the data volume is first initialized, so changing it later requires removing `cinedex-aspire-pgdata`. Mailpit runs with `MP_SMTP_AUTH_ACCEPT_ANY`, so beyond that one secret this path still needs no populated root `.env` — the SMTP credentials the web service sends exist only to satisfy its options validation.
  - **The web service is HTTP-only here**, as it is under Compose, where Kestrel serves 8080 behind the proxy. It runs through a new `aspire` launch profile added to `Cinedex.WebService`, alongside the existing `docker-compose` and `https-api-docs` ones. Serving HTTPS from the ASP.NET Core developer certificate would make the health probe fail on any machine that has not run `dotnet dev-certs https --trust`, and with no HTTPS port configured `UseHttpsRedirection` passes requests through instead of answering the probe with a 307. Browser authentication is unaffected: `Secure` cookies are accepted on `http://localhost`, which is a trustworthy origin.
  - **The dashboard link goes straight to the API docs.** The web service's URL is rewritten to `http://localhost:5187/movies-svc/api-docs/v1` and labelled `api-docs/v1`, rather than the endpoint root — which serves nothing, since everything sits behind the `/movies-svc` path base, so the default link landed on a 404.
  - **The profile is required, not a convenience.** Letting Aspire synthesize the endpoint instead works under `dotnet run`, where the orchestrator starts the child process and injects the port, but Visual Studio launches Aspire project resources itself and takes a web project's URLs from its launch profile. With no profile Kestrel falls back to 5000/5001 while the orchestrator's proxy forwards to the port it allocated, so every request hangs and the resource sits at `Running (Unhealthy)`. The two console hosts need no profile — they expose no endpoints — and get `DOTNET_ENVIRONMENT` set explicitly so they do not default to Production and skip the `*.Development.json` files that enable SQL command logging.
- **Rider run configuration for the AppHost** - `backend/.run/Aspire AppHost.run.xml` ships in the repo next to the existing `Docker Compose.run.xml`, so a fresh clone finds "Aspire AppHost" already in the Run dropdown. It is a `.NET Launch Settings Profile` configuration rather than a plain `.NET Project` one, so it runs the AppHost through the `https` profile that pins the dashboard and OTLP endpoints — the equivalent of `dotnet run --launch-profile https`. Nothing machine-specific is committed: the profile is named, not the executable path.
- **Scheduled background jobs (`Cinedex.SchedulerWorker`)** - A new .NET Generic Host presentation project for work that runs on a timer rather than on a request, added to Compose as `movies.schedulerworker`. It gates startup on `movies.databasemigrator` completing so a job can never run against an unmigrated schema, and serves no HTTP traffic.
  - **OpenTelemetry setup is now shared** — logging and tracing configuration moved into a new packable `FoundryOceanus.Observability.OpenTelemetry` library exposing one `AddObservability` extension on `IHostApplicationBuilder`, which both `HostApplicationBuilder` and `WebApplicationBuilder` implement. The web service, the migrator and the scheduler worker now all export to Seq through the same code path, each passing only its own instrumentation; the three near-identical copies are gone. Exporters are still omitted when no OTLP endpoint is configured, so a local `dotnet run` does not reach for a Seq that isn't there.
  - `Cinedex.DatabaseMigrator` and the scheduler worker moved from `Host.CreateDefaultBuilder` to `Host.CreateApplicationBuilder`, which is what exposes `IHostApplicationBuilder`.
- **Refresh-token cleanup (`RefreshTokenCleanupWorker`)** - Expired and revoked refresh-token rows accumulated indefinitely: rotation revokes its predecessor and inserts a replacement, and nothing on the request path ever deleted a row, so the table and its indexes only grew and old session metadata was retained forever. The scheduler worker now sweeps them every ten minutes in bounded batches.
  - **Two retention windows, because the two kinds of dead row are dead for different reasons.** An expired-but-never-revoked row is unreachable by every code path and is deleted a day past expiry. A revoked row is the only thing that makes replaying an already-rotated token distinguishable from presenting an unknown one, so it is kept for fourteen days past revocation — well beyond the seven-day token lifetime — preserving the trigger consumed by the family-wide reuse response. Neither predicate can match a live session's tail, which is unrevoked and unexpired.
  - **Bounded so it cannot block issuance or rotation.** Each batch is a single `ExecuteDelete` in its own implicit transaction, so row locks are held for one statement rather than across a sweep, and a run stops at `BatchSize × MaxBatchesPerRun` rows — a backlog drains over successive sweeps instead of becoming one long-running delete. Interval, batch size and both windows are validated at startup via `ValidateOnStart`.
  - A new composite index `IX_refreshTokens_revokedAtUtc_expiresAtUtc` serves both sweeps and supplies each its ordering. Note the trade: rotation updates `revokedAtUtc`, which was previously in no index, so rotations are no longer HOT updates. Registration is deliberately opt-in and separate from `AddAuthenticationAdapter` — the web service has no business sweeping the table, and the integration-test host runs real hosted services. Documented in the [Auth & Security Model](docs/auth-security-model.md).
- **SMTP email delivery** - Replaced `NoOpEmailSender` with a MailKit-based `SmtpEmailSender` requiring username/password authentication and supporting configurable TLS and HTML/plain-text bodies. Docker Compose now connects the web service to Mailpit so password-reset emails are captured end to end during development. An integration test starts a pinned Mailpit Testcontainer and verifies authenticated delivery through the real adapter.
  - **Delivery is queued, not inline** — `ForgotPasswordHandler` now hands the reset email to a new `IEmailDispatcher` port that enqueues onto a bounded in-memory `Channel`; an `EmailDeliveryWorker` background service in the email adapter drains it and calls `IEmailSender`. Awaiting SMTP on the request thread made `POST /auth/password/forgot` measurably slower for real accounts than for unknown ones — an account-enumeration timing oracle, despite both returning `202 Accepted`. The queue is capacity-bounded (the endpoint is anonymous and unthrottled), drops with a warning rather than growing without limit, and drains on shutdown so a redeploy does not swallow an in-flight reset email. The residual gap is documented in the [Auth & Security Model](docs/auth-security-model.md).
  - Successful sends no longer log the recipient address, so the log store cannot be read as a record of who requested a password reset.
  - Documented how to use the Mailpit web UI to read captured mail — triggering a message, the HTML/Text/Raw/Source tabs, following the reset link, and the REST API — in the [backend README](backend/README.md#viewing-captured-mail). The Mailpit image is now pinned to `v1.30.0` to match the version the integration test starts.
- **IDE launch profiles for the Compose stack** - The full stack can now be started from either IDE using configuration that ships with the repo. Rider's Docker Compose run configuration moved out of per-user `workspace.xml` — where it was invisible to everyone else, `.idea/` being git-ignored — into a tracked `backend/.run/Docker Compose.run.xml`, so a fresh clone finds it already in the Run dropdown. Visual Studio gets an equivalent `backend/docker-compose/docker-compose.dcproj`, registered in `Cinedex.slnx`, whose `Docker Compose` launch profile brings every service up without a debugger and opens the Scalar docs — the same thing the Rider profile does.
  - **Both drive the existing root `compose.yaml`; nothing was renamed.** Visual Studio appends the extension itself and probes `.yaml` as well as `.yml`. `DockerComposeProjectName` is pinned to `cinedex` because Visual Studio always passes `-p` explicitly: left to default it would derive the name from the project folder and create a second, empty set of volumes instead of reusing `cinedex_postgres_data`, leaving the app pointed at an empty database while the real one sat untouched.
  - The compose file now declares `name: cinedex`, so the project name — and with it every container and volume name — no longer depends on what the clone directory happens to be called.
  - `backend/docker-compose/Directory.Build.props` is deliberately empty and does not import its parent. A `.dcproj` has no target framework, and inheriting `TargetFramework=net10.0` from the solution-wide props fails `dotnet restore` for the entire solution with NU1105, breaking CI.

### Changed
- **Both packable libraries renamed off the product name (`Cinedex.*` → `FoundryOceanus.*`)** - `Cinedex.Observability.OpenTelemetry` is now `FoundryOceanus.Observability.OpenTelemetry`, and `Cinedex.WebService.Contracts` is now `FoundryOceanus.WebService.Contracts`. **The old package IDs are retired and receive no further updates** — anything consuming them from a feed must switch IDs, and the root namespaces change with them, so `using Cinedex.WebService.Contracts.Requests;` becomes `using FoundryOceanus.WebService.Contracts.Requests;`. Neither library ever held Cinedex-specific logic: one is an `AddObservability` extension over any OTLP collector, the other is plain request/response DTOs, and carrying the product name in the ID was the only thing implying otherwise. Package titles, descriptions and READMEs are de-branded to match; `PackageProjectUrl` and `RepositoryUrl` still point at this repository, which is where the source lives. No behavioral change — renames and reference updates only.
- **Refresh-token persistence consolidated behind a repository (`Cinedex.Auth.Identity.Persistence`)** - Every refresh-token database operation was written inline against `AuthDbContext`, spread across `JwtTokenService`, `IdentityService` and `RefreshTokenCleanupWorker`, so there was no single place to see what the system does to `auth."refreshTokens"` and EF query syntax sat in the middle of JWT-minting logic. Every read and write now goes through `IRefreshTokenRepository` in `Persistence.Repository`, backed by `AuthDbContext`; the persisted `RefreshToken` entity moved to `Persistence.Entities`. Behavior is unchanged; the extraction is structural.
  - `AcquireFamilyLockAsync` reads the ambient transaction and throws when there is none, turning "this only means anything inside a transaction" from a comment into a runtime invariant.
  - `RevokeRefreshTokenAsync` (logout) became a single conditional `UPDATE` filtered on `revokedAtUtc IS NULL` instead of a read followed by a tracked save. Still idempotent and one round trip shorter, and an earlier revocation's timestamp now survives a concurrent logout rather than being overwritten by the later one.
- **Branded password-reset email** - The reset email is now a designed HTML message in the Cinedex crimson palette, with an embedded logo, a call-to-action button, and the one-hour expiry stated inline, replacing the previous single-sentence body. `HtmlEmailBody` gained an `InlineImages` collection and `SmtpEmailSender` maps it to MIME linked resources, so the logo travels with the message and needs no remote fetch — a remote image would have told a web server which recipients opened a reset email. Composition stays in the application layer via a new `CinedexEmailLayout`; the SMTP adapter still only delivers. Design recorded in [the spec](docs/superpowers/specs/2026-07-26-branded-password-reset-email-design.md).

### Fixed
- **`/health/ready` now probes the database the application actually connects to** - The Postgres readiness check read `ConnectionStrings:DefaultConnection` eagerly from `builder.Configuration`, capturing whatever happened to be configured at that point in startup rather than the value the `DbContext`s are handed once the host finishes building. It now resolves the connection string lazily from the built `IServiceProvider`, the same way `AddAuthenticationPersistence` does; the eager read is kept solely as a startup guard that fails fast when the key is missing altogether. A readiness probe reporting on a different database than the one serving requests is worse than no probe at all — under `WebApplicationFactory` it captured `appsettings.json`'s `<SECRETS>` placeholder and returned 503, while on a developer machine User Secrets quietly made it green by pointing it at a local Postgres that was not the database under test.

### Security
- **Refresh-token reuse revokes the compromised family** - Presenting a known, unexpired refresh token that already has a replacement link now revokes every active token in that login family atomically before returning the same generic `401` and clearing the cookie. A PostgreSQL transaction-scoped advisory lock serializes rotations and reuse responses within the family, so a replacement inserted by a concurrent refresh cannot survive. Warning event `1001 / RefreshTokenReuseDetected` records only the number of tokens revoked — no token material, family or user identifiers, email, or username. Unknown, expired, logged-out, and already-family-revoked tokens retain the ordinary invalid-refresh policy; other login families and existing access tokens are unaffected. Documented in the [Auth & Security Model](docs/auth-security-model.md).
- **Refresh tokens now carry a family identifier** - Every persisted refresh token has a new indexed `familyId`: `POST /auth/login` mints a fresh one per session and each rotation through `POST /auth/refresh` copies the incoming token's value onto its replacement, so an entire rotation chain is reachable by one indexed lookup instead of by walking `ReplacedByTokenHash` hash by hash. The reuse response above consumes that identifier to contain a compromised session without touching the user's other login families. Because rows written before the column existed belong to no real family, the migration deletes every stored refresh token rather than inventing one — sessions that were live when it ran end and clients log in again, which also keeps the migration unable to fail against a populated database and block the Compose migrator. Documented in the [Auth & Security Model](docs/auth-security-model.md).
- **Password reset links now expire after one hour** - `AddAuthenticationAdapter` sets `DataProtectionTokenProviderOptions.TokenLifespan` explicitly, replacing Identity's one-day default and narrowing the window in which an intercepted or forwarded reset link stays usable. The lifespan applies to every token issued by Identity's `Default` provider, which today means password reset only. Documented in the [Auth & Security Model](docs/auth-security-model.md).

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
- **`Cinedex.WebService.csproj`** - Added `FastEndpoints` package reference and a project reference to `FoundryOceanus.WebService.Contracts`
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
- **Build configuration** - Added `GenerateDocumentationFile=true` to `FoundryOceanus.WebService.Contracts.csproj`
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
  - FoundryOceanus.WebService.Contracts (DTOs)
  - Integration tests project
- **WeatherForecast endpoint** - Sample endpoint implementation

---

## Version Numbering Strategy

This project follows [Semantic Versioning](https://semver.org/) with the following scheme for pre-1.0 development:

- **PATCH** (0.x.y) - Bug fixes, testing infrastructure, tooling improvements, code quality
- **MINOR** (0.x.0) - New features or significant capability additions
- **MAJOR** (1.0+) - Reserved for production-ready release with breaking changes
