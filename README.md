# Movies Project

[![Build and Test](https://github.com/felipedferreira/Cinedex/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/felipedferreira/Cinedex/actions/workflows/build-and-test.yml)

A full-stack portfolio application for managing movies, crew members, and their roles — inspired by IMDB.

## 📁 Repository Layout

```
Movies/
├── backend/      # .NET solution (Web API, application core, persistence, tests)
├── frontend/     # Standalone SPA consuming the backend's OpenAPI spec
└── compose.yaml  # Orchestrates PostgreSQL, the web service, the frontend, and Seq
```

- **[Backend](backend/README.md)** — clean architecture .NET solution: architecture guide, build/test/migration instructions
- **[Frontend](frontend/cinadex-ui/README.md)** — standalone React + TypeScript + Vite SPA (`cinadex-ui`)
- **[Design docs](docs/README.md)** — why the system is shaped this way (auth & security model, planned ADRs)
- **[Changelog](CHANGELOG.md)** — version history and release notes

## 🚀 Quick Start

Create the root `.env` file (needed for the Seq observability stack — see the
[backend README](backend/README.md#environment-configuration)), then run everything with
Docker Compose from the repository root:

```bash
cp .env.example .env       # one-time; fill in the database and Seq values
docker compose up --build
```

> **Migrations are not applied automatically.** The database starts empty; run `dotnet ef database
> update` for both `FilmDbContext` and `AuthDbContext` before using the API. See
> [Migrations](backend/README.md#migrations).

Access the application:
- **UI:** http://localhost:9000
- **API:** http://localhost:8080
- **API Documentation:** http://127.0.0.1:8080/movies-svc/api-docs/v1 (Scalar UI)
- **OpenAPI Spec:** http://127.0.0.1:8080/movies-svc/openapi/v1.json
- **Seq (logs & traces):** http://localhost:5341
- **PostgreSQL:** localhost:5432

For local development without Docker, and for first-run Seq setup, see the
[backend README](backend/README.md).
