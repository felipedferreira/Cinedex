# Movies Project

[![Build and Test](https://github.com/felipedferreira/Movies/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/felipedferreira/Movies/actions/workflows/build-and-test.yml)

A full-stack portfolio application for managing movies, crew members, and their roles — inspired by IMDB.

## 📁 Repository Layout

```
Movies/
├── backend/      # .NET solution (Web API, application core, persistence, tests)
├── frontend/     # Standalone SPA consuming the backend's OpenAPI spec
└── compose.yaml  # Orchestrates PostgreSQL and the web service
```

- **[Backend](backend/README.md)** — clean architecture .NET solution: architecture guide, build/test/migration instructions
- **[Frontend](frontend/README.md)** — standalone React + TypeScript + Vite SPA (`cinadex-ui`)
- **[Changelog](CHANGELOG.md)** — version history and release notes

## 🚀 Quick Start

Run everything with Docker Compose from the repository root:

```bash
docker compose up
```

Access the application:
- **API:** http://localhost:8080
- **API Documentation:** http://localhost:8080/api-docs/v1 (Scalar UI)
- **OpenAPI Spec:** http://localhost:8080/openapi/v1.json
- **PostgreSQL:** localhost:5432

For local development without Docker, see the [backend README](backend/README.md).
