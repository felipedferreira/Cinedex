# Movies Backend

[![Build and Test](https://github.com/felipedferreira/Movies/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/felipedferreira/Movies/actions/workflows/build-and-test.yml)

A clean architecture .NET solution for managing movies, their genres, crew members, and roles — inspired by IMDB. Built with a focus on separation of concerns, testability, and maintainability.

All `dotnet` commands below are run from this folder (`backend/`). Docker Compose commands run from the repository root, where [compose.yaml](../compose.yaml) lives.

## 🏷️ Genres

Genres are their own entity (`Id`, `Name`, `Description`) stored in the `genres` table, and
movies link to genres through a many-to-many relationship backed by a `movie_genres`
junction table. A genre's navigation is one-directional — a movie knows its genres, but a
genre does not hold a back-reference to movies.

- **CRUD endpoints** under `/movies-svc/genres` (`GET`, `GET /{id}`, `POST`, `PUT /{id}`, `DELETE /{id}`).
- **Movies reference genres by id** — `CreateMoviesRequest`/`UpdateMoviesRequest` carry a `GenreIds` collection, and movie responses include the linked genres.
- **Seeded data** — the database ships with 17 common genres (Action, Comedy, Drama, …) so movies can be tagged immediately.

See the [contracts README](NuGetLibraries/Movies.WebService.Contracts/README.md) for the request/response DTOs.

## 🗄️ Database

This project uses **PostgreSQL** via **Entity Framework Core 10**.

### Prerequisites

You have two options to run PostgreSQL:

#### Option 1: Docker Compose (Recommended for development)
```bash
docker compose up            # from the repository root, where compose.yaml lives
```

This starts:
- **PostgreSQL 17 Alpine** on port `5432`
- **Movies WebService** on ports `8080` (HTTP) and `8081` (HTTPS)
- Automatic database initialization
- Data persistence via Docker volume

#### Option 2: Local PostgreSQL
Ensure PostgreSQL is installed and running locally. The connection string is **not**
committed — for local runs (`Development` environment) it is supplied via
[.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) so the
password stays out of git. Set it once from the web service project directory:
```bash
# from backend/src/Presentation/Movies.WebService
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "<YOUR_LOCAL_CONNECTION_STRING>"
```

Verify the secret was stored correctly (same directory):
```bash
# from backend/src/Presentation/Movies.WebService
dotnet user-secrets list
```

### Environment Configuration

Most environment variables are baked into `compose.yaml` and applied to the containers
automatically. **Secrets are the exception** — the database connection string and the Seq
observability secrets are read from a git-ignored `.env` file at the repository root, which
`compose.yaml` interpolates via `${...}`. A `docker compose up` without this file will fail.

Create it once by copying the template and filling in values:

```bash
cp .env.example .env          # from the repository root
```

| Variable | Purpose |
|----------|---------|
| `DB_CONNECTION_STRING` | Full Postgres connection string for the web service container (host is the `postgres` service name). |
| `SEQ_ADMIN_PASSWORD` | First-login password for the Seq UI `admin` user. Seq prompts you to choose the permanent UI password on first login. |
| `SEQ_API_KEY` | Ingestion API-key token the web service sends to Seq over OTLP (`X-Seq-ApiKey`). |

For local development outside of Docker, the connection string is supplied via .NET User
Secrets (see [Option 2](#option-2-local-postgresql) above); other settings can use
environment variables or configuration files as needed.

### Migrations

Run from this folder, specifying the persistence project and the WebService as the startup project:

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> \
  --project src/Adapters/Movies.Persistence.Postgres \
  --startup-project src/Presentation/Movies.WebService

# Apply migrations to the database
dotnet ef database update \
  --project src/Adapters/Movies.Persistence.Postgres \
  --startup-project src/Presentation/Movies.WebService
```

By default these commands resolve `ConnectionStrings:DefaultConnection` from the startup
project's configuration (User Secrets in `Development`; see [Option 2](#option-2-local-postgresql)).
If that isn't set — or you want to target a specific database such as the Docker Postgres
container exposed on `localhost:5432` — pass the connection string explicitly with `--connection`:

```bash
dotnet ef database update \
  --project src/Adapters/Movies.Persistence.Postgres \
  --startup-project src/Presentation/Movies.WebService \
  --connection "<YOUR_CONNECTION_STRING>"
# e.g. "Host=127.0.0.1;Port=5432;Database=movies;Username=movies_rw;Password=<DB_PASSWORD>"
```

> **Domain models** live in `Movies.Domain`. EF entity configurations use **Fluent API** in `Movies.Persistence.Postgres`, keeping the domain layer free of any EF dependencies.

---

## 📚 Documentation

- **[Architecture Guide](README.md)** (this file) - Project structure and design patterns
- **[Changelog](../CHANGELOG.md)** - Version history and release notes
- **[NuGetLibraries](NuGetLibraries/Movies.WebService.Contracts/README.md)** - NuGet package documentation
  - Movies.WebService.Contracts - API contracts and DTOs

## 🚀 Quick Start

```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Run the web service
dotnet run --project src/Presentation/Movies.WebService

# Generate coverage report
.\coverage.ps1 -Open
```

## 🐳 Docker Compose

The project includes a complete `compose.yaml` with PostgreSQL, the web service, the frontend,
and a [Seq](https://datalust.co/seq) instance for logs and traces.

### Getting Started

1. Create the root `.env` file (one-time — see [Environment Configuration](#environment-configuration)):
   ```bash
   cp .env.example .env       # then fill in the database and Seq values
   ```
2. Start the services from the repository root:
   ```bash
   docker compose up --build
   ```

Access the application:
- **API:** http://localhost:8080
- **API Documentation:** http://127.0.0.1:8080/movies-svc/api-docs/v1 (Scalar UI)
- **OpenAPI Spec:** http://127.0.0.1:8080/movies-svc/openapi/v1.json
- **Seq (logs & traces):** http://localhost:5341 — first login is `admin` with `SEQ_ADMIN_PASSWORD`; after the required password change, use the password you chose
- **PostgreSQL:** localhost:5432

### Services

| Service | Image | Port | Purpose |
|---------|-------|------|---------|
| `postgres` | postgres:17-alpine | 5432 | PostgreSQL database with persistent storage |
| `movies.webservice` | movies.webservice | 8080/8081 | ASP.NET Core web API |
| `cinadex-ui` | cinadex-ui | 9000 | React SPA frontend (Nginx) |
| `seq` | datalust/seq | 5341 | Structured logs + distributed traces (OpenTelemetry/OTLP) |

### Features

- ✅ **Health Checks:** Postgres and Seq must report healthy before the web service starts, which in turn must be healthy before the UI starts; the web service also exposes its own liveness/readiness endpoints (see [Health Checks](#-health-checks))
- ✅ **Data Persistence:** PostgreSQL and Seq data persist via named volumes (`postgres_data`, `seq_data`)
- ✅ **Observability:** Logs and traces shipped to Seq via OpenTelemetry (see [below](#-observability-seq))
- ✅ **Service Dependencies:** Web service automatically waits for its dependencies
- ✅ **API Documentation:** Scalar UI available at `/movies-svc/api-docs/v1`
- ✅ **Feature Flags:** Configurable via environment variables

### Stopping Services

```bash
docker compose down
```

To also remove persistent data (PostgreSQL **and** Seq volumes):
```bash
docker compose down -v
```

## 🩺 Health Checks

The web service exposes two health endpoints, following the standard liveness/readiness split.
Both live under the `/movies-svc` base path and return a minimal JSON body (`{ "status": ..., "checks": [...] }`)
along with an HTTP status code (`200` healthy, `503` unhealthy). The payload intentionally omits
exception detail so the endpoints don't leak internal information.

| Endpoint | Purpose | Dependencies checked |
|----------|---------|----------------------|
| `GET /movies-svc/health/live` | **Liveness** — confirms the process is up and serving requests. | None |
| `GET /movies-svc/health/ready` | **Readiness** — confirms the service can handle traffic. | PostgreSQL (checks tagged `ready`) |

```bash
# Liveness
curl -s http://localhost:8080/movies-svc/health/live
# {"status":"Healthy","checks":[]}

# Readiness (includes the Postgres connectivity check)
curl -s http://localhost:8080/movies-svc/health/ready
# {"status":"Healthy","checks":[{"name":"postgres","status":"Healthy"}]}
```

The Compose container healthcheck polls `/health/live` (see `compose.yaml`); `cinadex-ui` waits for
the web service to report healthy before starting.

## 📈 Observability (Seq)

The web service emits **structured logs** and **distributed traces** through OpenTelemetry,
exporting both over OTLP to the `seq` container. Inside the Compose network the app targets
`http://seq/ingest/otlp` (configured via the `OTEL_EXPORTER_OTLP_*` environment variables on
`movies.webservice`); from your machine the Seq UI is at **http://localhost:5341**.

Traces cover incoming HTTP requests (ASP.NET Core), outbound `HttpClient` calls, and PostgreSQL
queries (the `Npgsql` activity source). Every request's `CorrelationId` is attached to its log
events and to the trace as a `correlation_id` tag, so you can pivot between logs and traces for
the same request.

### First-run setup

Seq refuses to start without an admin credential and won't auto-provision an ingestion key, so
two `.env` values need preparing once.

1. **Choose the first-login admin password.** Put it in `SEQ_ADMIN_PASSWORD`. Compose passes it
   to Seq as `SEQ_FIRSTRUN_ADMINPASSWORD`, which Seq only reads when the `seq_data` volume is
   empty. On first login, Seq will require you to choose the permanent UI password. After that,
   the saved password lives in the `seq_data` volume, and changing `.env` will not update it.

2. **Choose an ingestion token** and set it as `SEQ_API_KEY` (any sufficiently random string).

3. **Start the stack** (`docker compose up --build`), then register the API key in Seq so its
   token matches `SEQ_API_KEY`. The web service sends this token on every OTLP request via the
   `X-Seq-ApiKey` header, so the token stored in Seq **must equal** the `SEQ_API_KEY` in your
   `.env` — don't let Seq auto-generate one. Use either the CLI or the UI:

   **Option A — CLI (`seqcli`):**
   ```bash
   docker run --rm --network movies_default datalust/seqcli apikey create \
     -t "Movies WebService" --token "<your-SEQ_API_KEY>" --permissions "Ingest" \
     -s http://seq --connect-username admin --connect-password "<your-password>"
   ```

   **Option B — Seq UI** (http://localhost:5341 → **Settings → API Keys**):
   1. Click **ADD API KEY**.
   2. **Title:** anything descriptive, e.g. `Movies WebService`.
   3. **Token:** type your `SEQ_API_KEY` value here instead of generating a random one, so it
      matches `.env`.
   4. **Permissions:** ensure **Ingest** is selected (all the web service needs to write events).
   5. Save.

   > Requiring authentication for ingestion is optional — by default Seq accepts all events, so
   > logs flow even without a key. Registering the key with the matching token still ensures
   > ingestion is attributed to it and keeps working if you later enable
   > *Require authentication for HTTP/S ingestion*.

   Restart the web service afterwards so it picks up the key: `docker compose up -d movies.webservice`.

If you already started Seq with the wrong first-run settings or forgot the saved UI password,
reset only the Seq volume and start it again:

```bash
docker compose down
docker volume rm movies_seq_data
docker compose up -d seq
```

This deletes local Seq logs, API keys, and settings, but leaves the PostgreSQL volume alone.

> **Note:** On Docker Desktop / Windows the Seq port is published on IPv4 loopback
> (`127.0.0.1:5341`) on purpose — a dual-stack bind makes `localhost` resolve to IPv6 first,
> which Docker Desktop fails to relay. Use `http://localhost:5341` or `http://127.0.0.1:5341`.

## Architecture Overview

The solution is organized into layers that enforce separation of concerns and dependency direction. Dependencies flow inward—outer layers depend on inner layers, never the reverse.

```
┌─────────────────────────────────────────────────┐
│         Movies.WebService (Presentation)        │
│              (Web API / Entry Point)            │
└──────────────────┬──────────────────────────────┘
                   │
       ┌───────────┴────────────────┐
       │                            │
┌──────▼───────────────┐  ┌─────────▼────────────────────────┐
│  Movies.Application  │◄─┤  Movies.Persistence.Postgres     │
│  (Use Cases + Ports) │  │     (Persistence Adapter)        │
└──────┬───────────────┘  └─────────┬────────────────────────┘
       │                            │
       └─────────────┬──────────────┘
                     │
           ┌─────────▼──────────┐
           │   Movies.Domain    │
           │  (Business Logic)  │
           └────────────────────┘
```

### Solution Layout

Projects are grouped on disk by Hexagonal layer. Layers that can have multiple
projects (Presentation, Adapters) keep a grouping folder; the single Application
and Domain projects sit directly under `src/`.

```
backend/src/
├── Presentation/
│   └── Movies.WebService/            # driving adapter (HTTP entry point)
├── Adapters/
│   └── Movies.Persistence.Postgres/  # driven adapter (implements ports)
├── Application/                      # use cases + ports (Abstractions/)
├── Domain/                           # entities, no outward dependencies
└── Libraries/
    └── Movies.WebService.Contracts/  # shared API DTOs
```

## Project Descriptions

### 1. **Movies.Domain** (Foundation Layer)
**Purpose:** Core business logic and domain entities  
**Dependencies:** None  
**Responsibilities:**
- Domain entities — `Movie`, `Genre`, `CrewMember`, `Role`, etc.
- Business rules and invariants
- No external dependencies (no EF, no web frameworks)

### 2. **Movies.Application** (Use Cases Layer)
**Purpose:** Implements application use cases and defines the ports they depend on  
**Dependencies:** `Movies.Domain`  
**Responsibilities:**
- Use case implementations and application services
- Repository and service interfaces (ports), grouped under `Abstractions/`
- Orchestrates domain logic with persistence
- Coordinates between domain and external systems

**Handler conventions:**
- Application handlers expose asynchronous use cases through `HandleAsync(...)`.
- Create handlers assign the new domain id and return that `Guid` so presentation can build the `Location` header.
- Update and delete handlers return `Task`; clients retrieve current resource state through the relevant query endpoint.
- Query handlers return application DTOs for presentation mapping.
- Repository create ports persist supplied domain models and return `Task` rather than echoing the saved entity.

### 3. **Movies.Persistence.Postgres** (Adapter Layer)
**Purpose:** Implements data persistence using PostgreSQL  
**Dependencies:** `Movies.Application`, `Movies.Domain`  
**Responsibilities:**
- `FilmDbContext` — EF Core DbContext with Fluent API entity configurations
- Concrete repository implementations
- Database migrations and schema management
- Adapts PostgreSQL to the repository ports defined in `Movies.Application`
- *Note: Listed under `Adapters/` to reflect that it's an interchangeable persistence adapter*

### 4. **Movies.WebService** (Presentation/Entry Point Layer)
**Purpose:** Web API and HTTP request handling  
**Dependencies:** `Movies.Application`, `Movies.Persistence.Postgres`  
**Responsibilities:**
- ASP.NET Core web API endpoints
- HTTP request/response handling
- Dependency injection and service configuration
- Wires together application logic and persistence implementations
- Docker containerization

## Dependency Rules

The architecture enforces these dependency directions:

| From | To | Allowed? |
|------|-----|----------|
| Domain | Anything | ❌ No (Domain has no outward dependencies) |
| Application | Domain | ✅ Yes |
| Adapters (Persistence) | Application, Domain | ✅ Yes |
| WebService | Application, Adapters | ✅ Yes |
| WebService | Domain | ✅ Yes (transitively) |

## How It Works Together

1. **WebService** is the entry point—it handles HTTP requests and delegates to **Application**
2. **Application** implements business logic by orchestrating **Domain** entities
3. **Application** calls repository methods defined by its own ports (interfaces under `Abstractions/`)
4. **Persistence.Postgres** implements those ports, translating repository calls to database operations
5. **Domain** contains the pure business rules that drive everything

## Building and Running

### Build the entire solution:
```bash
dotnet build
```

### Run the web service locally:
```bash
# Make sure PostgreSQL is running (locally or via Docker)
dotnet run --project src/Presentation/Movies.WebService
```

The service will be available at:
- HTTP: http://localhost:5000
- HTTPS: http://localhost:5001
- API Docs: http://localhost:5000/api-docs

### Run with Docker Compose:

Run from the repository root. Requires the root `.env` file — see the
[🐳 Docker Compose](#-docker-compose) section above for full details.

```bash
# Start the full stack (PostgreSQL, web service, frontend, Seq)
docker compose up --build

# Run in background
docker compose up -d

# View logs
docker compose logs -f

# Stop services
docker compose down
```

## Testing & Code Coverage

### Running the tests
Run the full test suite (all test projects in the solution):
```bash
dotnet test
```

### Generating a coverage report
Coverage is **opt-in** — a plain `dotnet test` reports pass/fail only. To produce a
browsable HTML coverage report, use the `coverage.ps1` script in this folder:

```bash
# Windows
.\coverage.ps1

# Ubuntu / macOS (requires PowerShell 7+: `pwsh`)
pwsh ./coverage.ps1
```

Add the `-Open` switch to launch the report in your browser when it finishes
(`.\coverage.ps1 -Open`).

The script:
1. Clears stale results from previous runs.
2. Runs every test project with cross-platform coverage collection (`--collect:"XPlat Code Coverage"`).
3. Merges all results into one HTML report, excluding auto-generated OpenAPI code so the
   percentage reflects hand-written code.
4. Prints a text summary and writes the full report to `CoverageReport/index.html`.

> If tests fail, the report is **not** generated.

### Prerequisites
| Tool | Notes |
|------|-------|
| .NET SDK 10 | Required to build and test |
| PowerShell 7+ (`pwsh`) | Only needed on Ubuntu/macOS; Windows can use built-in PowerShell |
| `dotnet-reportgenerator-globaltool` | Install once: `dotnet tool install -g dotnet-reportgenerator-globaltool` |

> On Ubuntu/macOS, ensure `~/.dotnet/tools` is on your `PATH` so `reportgenerator` is found.

### Reading the report
Coverage measures the union of all assemblies referenced by the test projects. As tests are
added for the domain and application layers, those assemblies appear in the report
automatically. New test projects are picked up with no script changes, provided they are
added to the solution and reference the `coverlet.collector` package.

> Both `TestResults/` and `CoverageReport/` are git-ignored build artifacts.

## Design Benefits

- **Testability:** Business logic in Domain and Application can be tested without databases
- **Maintainability:** Changes to infrastructure (e.g., switching databases) only affect Adapters
- **Flexibility:** Easy to add new adapters (REST API, gRPC, message queues) without changing core logic
- **Clarity:** Clear layer responsibilities and data flow
