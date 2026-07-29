# Cinedex Database Migrator

`Cinedex.DatabaseMigrator` is a one-shot console application that applies all pending Entity
Framework Core migrations required by Cinedex. It is intended to run before
`Cinedex.WebService`, either from Docker Compose or as a migration step in a deployment pipeline.

## Responsibilities

The migrator updates both EF Core contexts that share the Cinedex PostgreSQL database:

| Context | Schema | Migration assembly | Purpose |
|---------|--------|--------------------|---------|
| `FilmDbContext` | `catalog` | `Cinedex.Persistence.Postgres` | Titles and genres |
| `AuthDbContext` | `auth` | `Cinedex.Auth.Identity` | Identity users, roles and refresh tokens |

Migrations run sequentially in the order shown above. EF Core records applied migrations in each
context's migration history, so rerunning the application safely applies only pending migrations.

This project applies existing migrations. New migration files are still created with `dotnet ef`;
see the main [migration documentation](../../../README.md#migrations) for those commands.

## Execution Model

`Program.Main(string[] args)` builds the Microsoft Generic Host and registers
`DatabaseMigrationHostedService` as an `IHostedService`.

When the host starts, the service:

1. Creates a dependency-injection scope.
2. Applies pending `FilmDbContext` migrations.
3. Applies pending `AuthDbContext` migrations.
4. Requests application shutdown.

The process returns exit code `0` when all migrations succeed and `1` when startup or migration
execution fails. This makes the executable suitable for CI/CD gates and one-shot containers.

## Configuration

The required setting is:

```text
ConnectionStrings:DefaultConnection
```

Migrator-specific configuration is loaded in this order, with later sources overriding earlier
ones:

1. `application.json`
2. `application.{DOTNET_ENVIRONMENT}.json`
3. .NET User Secrets when `DOTNET_ENVIRONMENT=Development`
4. Environment variables
5. Command-line arguments

Environment variables use the standard .NET double-underscore syntax:

```text
ConnectionStrings__DefaultConnection
```

`application.json` contains only a placeholder. Never commit a real database password.

### User Secrets

The migrator shares the WebService user-secrets store, so the local connection string only needs
to be configured once:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" `
  "Host=localhost;Database=movies;Username=movies_rw;Password=<PASSWORD>"
```

User Secrets are loaded only in the `Development` environment.

## Running Locally

Run these commands from this project directory:

```powershell
$env:DOTNET_ENVIRONMENT = "Development"
dotnet run
```

Arguments passed after `--` participate in configuration and have the highest precedence:

```powershell
dotnet run -- --ConnectionStrings:DefaultConnection="Host=localhost;Database=movies;Username=movies_rw;Password=<PASSWORD>"
```

The target PostgreSQL server must already be running and reachable.

## Docker Compose

The repository's `compose.yaml` defines `movies.databasemigrator` as a one-shot service:

- It waits for PostgreSQL to become healthy.
- Compose supplies `DB_CONNECTION_STRING` as `ConnectionStrings__DefaultConnection`.
- It exits after both contexts are migrated.
- `movies.webservice` starts only when the migrator exits successfully.

Run the complete stack from the repository root:

```powershell
docker compose up --build
```

Run only PostgreSQL and the migrator:

```powershell
docker compose up --build movies.databasemigrator
```

## Pipeline Usage

A deployment pipeline should:

1. Build or pull the `movies.databasemigrator` image.
2. Supply `ConnectionStrings__DefaultConnection` through a secret-backed environment variable.
3. Run exactly one migrator instance for the target database.
4. Continue deployment only when the container exits with code `0`.

Do not bake credentials into the image or configuration files.

## Troubleshooting

### PostgreSQL container exits before migration

The official PostgreSQL image requires `DB_PASSWORD` when initializing an empty volume. In the
root `.env`, `DB_PASSWORD` must match the password inside `DB_CONNECTION_STRING`.

### Migrator cannot connect

For Docker Compose, the connection-string host must be `postgres`, which is the Compose service
name. For a local process connecting through the published port, use `localhost`.

### Web service does not start

Compose uses `condition: service_completed_successfully`. Inspect the migration logs:

```powershell
docker compose logs movies.databasemigrator
```

The web service remains stopped until the migration container exits with code `0`.
