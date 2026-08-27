# Cinedex Web Service

The Cinedex REST API—the catalog of titles, cast and crew, plus authentication. Serves
`/movies-svc` and its Scalar API reference.

`Cinedex.WebService` is the ASP.NET Core host at the Presentation edge of the backend's hexagonal
architecture. It owns HTTP concerns only—routing, model binding, authentication, problem details
and the OpenAPI document—and delegates every decision to the Application layer.

## Endpoints

All routes hang off the `/movies-svc` base path, defined once in
[`Constants/ApiConstants.cs`](Constants/ApiConstants.cs). The product is "Cinedex" but the catalog
entity is `Title`, so the catalog routes read `/movies-svc/titles`.

| Group | Routes | Notes |
|-------|--------|-------|
| Titles | `titles`, `titles/{id:guid}` | The movie catalog |
| Genres | `genres`, `genres/{id:guid}` | |
| Auth | `auth/register`, `auth/login`, `auth/refresh`, `auth/logout`, `auth/sessions/all` | Session routes nest under `auth` so the refresh cookie's `Path=/movies-svc/auth` reaches them |

The Scalar API reference is served at `/movies-svc/api-docs/v1`.

## Execution Model

`Program.Main` builds a `WebApplication`, registers observability through
`FoundryOceanus.Observability.OpenTelemetry`, then composes the host from two extension methods:

1. `ConfigureWebServer()` — Kestrel, forwarded headers and the web-host level concerns.
2. `AddPresentationServices()` — endpoint, auth, validation and OpenAPI registrations.
3. `ConfigureRequestPipeline()` — middleware order, exception handlers and endpoint mapping.

Tracing instruments ASP.NET Core, `HttpClient` and the `Npgsql` `ActivitySource`, so EF Core
database spans appear alongside request spans in Seq and on the Aspire dashboard.

## Configuration

The required setting is:

```text
ConnectionStrings:DefaultConnection
```

Environment variables use the standard .NET double-underscore syntax:

```text
ConnectionStrings__DefaultConnection
```

`appsettings.json` contains only a placeholder. Never commit a real database password.

## Migrations

This service does **not** apply migrations—`dotnet run` never does. A fresh database needs
[`Cinedex.DatabaseMigrator`](../Cinedex.DatabaseMigrator/README.md) run to completion first. Both
orchestrated paths handle that: Compose gates this service on
`condition: service_completed_successfully`, and the Aspire AppHost runs the migrator behind
`WaitForCompletion`.

## Related

- [Backend overview](../../../README.md)
- [Database migrator](../Cinedex.DatabaseMigrator/README.md)
- [Scheduler worker](../Cinedex.SchedulerWorker/README.md)
