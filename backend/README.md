# Movies Backend

[![Build and Test](https://github.com/felipedferreira/Movies/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/felipedferreira/Movies/actions/workflows/build-and-test.yml)

A clean architecture .NET solution for managing movies, crew members, and their roles — inspired by IMDB. Built with a focus on separation of concerns, testability, and maintainability.

All `dotnet` commands below are run from this folder (`backend/`). Docker Compose commands run from the repository root, where [compose.yaml](../compose.yaml) lives.

## 🗄️ Database

This project uses **PostgreSQL** via **Entity Framework Core 10**.

### Prerequisites

You have two options to run PostgreSQL:

#### Option 1: Docker Compose (Recommended for development)
```bash
docker-compose up
```

This starts:
- **PostgreSQL 17 Alpine** on port `5432`
- **Movies WebService** on ports `8080` (HTTP) and `8081` (HTTPS)
- Automatic database initialization
- Data persistence via Docker volume

#### Option 2: Local PostgreSQL
Ensure PostgreSQL is installed and running locally. The connection string is configured in `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=movies;Username=movies_rw;Password=P@ssw0rd!Secure#2024"
}
```

### Environment Configuration

Environment variables are configured in `compose.yaml` and automatically applied to the containers. All sensitive data and environment-specific settings are managed there.

For local development outside of Docker, you can use environment variables or configuration files as needed.

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

> **Domain models** live in `Movies.Domain`. EF entity configurations use **Fluent API** in `Movies.Persistence.Postgres`, keeping the domain layer free of any EF dependencies.

---

## 📚 Documentation

- **[Architecture Guide](README.md)** (this file) - Project structure and design patterns
- **[Changelog](../CHANGELOG.md)** - Version history and release notes
- **[Libraries](src/Libraries/Movies.WebService.Contracts/README.md)** - NuGet package documentation
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

The project includes a complete `compose.yaml` with PostgreSQL and the web service. All configuration is pre-set in the compose file.

### Getting Started

Start the services:
```bash
docker-compose up
```

Access the application:
- **API:** http://localhost:8080
- **API Documentation:** http://localhost:8080/api-docs/v1 (Scalar UI)
- **OpenAPI Spec:** http://localhost:8080/openapi/v1.json
- **PostgreSQL:** localhost:5432

### Services

| Service | Image | Port | Purpose |
|---------|-------|------|---------|
| `postgres` | postgres:17-alpine | 5432 | PostgreSQL database with persistent storage |
| `movies.webservice` | movies.webservice | 8080/8081 | ASP.NET Core web API |

### Features

- ✅ **Health Checks:** Database readiness verified before starting web service
- ✅ **Data Persistence:** PostgreSQL data persists via named volume (`postgres_data`)
- ✅ **Environment Variables:** Pre-configured in `compose.yaml`
- ✅ **Service Dependencies:** Web service automatically waits for database
- ✅ **API Documentation:** Scalar UI available at `/api-docs/v1`
- ✅ **Feature Flags:** Configurable via environment variables

### Stopping Services

```bash
docker-compose down
```

To also remove persistent data:
```bash
docker-compose down -v
```

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
├── Movies.Application/               # use cases + ports (Abstractions/)
├── Movies.Domain/                    # entities, no outward dependencies
└── Libraries/
    └── Movies.WebService.Contracts/  # shared API DTOs
```

## Project Descriptions

### 1. **Movies.Domain** (Foundation Layer)
**Purpose:** Core business logic and domain entities  
**Dependencies:** None  
**Responsibilities:**
- Domain entities — `Movie`, `CrewMember`, `Role`, etc.
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

### 3. **Movies.Persistence.Postgres** (Adapter Layer)
**Purpose:** Implements data persistence using PostgreSQL  
**Dependencies:** `Movies.Application`, `Movies.Domain`  
**Responsibilities:**
- `MoviesDbContext` — EF Core DbContext with Fluent API entity configurations
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
```bash
# Start both PostgreSQL and web service
docker-compose up

# Run in background
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
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
