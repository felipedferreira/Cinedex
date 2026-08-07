---
sidebar_position: 1
---

# Overview

Cinedex is a full-stack portfolio application for cataloging movie titles and their genres —
inspired by IMDB — with JWT-based authentication in front of a members-only catalog.

## Repository layout

```
Cinedex/
├── backend/      # .NET solution (Web API, application core, persistence, tests)
│   └── aspire/   # Aspire AppHost — the local dev loop
├── frontend/     # npm workspace — the SPA, the component library, its Storybook, and this docs site
├── docs/         # Design docs (auth & security model, planned ADRs)
└── compose.yaml  # Orchestrates PostgreSQL, the web service, the SPA, Storybook, Seq, and Mailpit
```

## What's in this section

- **[Movie Catalog & API](./movie-catalog.md)** — genres, titles, and the request/response contracts.
- **[Architecture](./architecture.md)** — the hexagonal (ports & adapters) layering and how the six
  projects fit together.
- **[Frontend & Component Library](./frontend.md)** — the React SPA, the shared `@cinedex/components`
  design system, and its Storybook workbench.
- **[Observability & Local Dev Workflow](./observability-and-dev-workflow.md)** — structured
  logging/tracing via Seq, health checks, the dev mail sink, and the two ways to run the stack
  locally (Docker Compose or the Aspire dev loop).

For how authentication and authorization work, see the [Security](../security/overview.md) section.

## Two ways to run it

**Docker Compose** — the prod-like path, built images behind an HTTPS reverse proxy:

```bash
git clone https://github.com/felipedferreira/Cinedex.git
cd Cinedex
cp .env.example .env       # fill in the database, Seq, and Mailpit values
docker compose up --build
```

Then open **https://localhost:9000** (self-signed cert — trust it, or use `curl -k`).

**The Aspire dev loop** — faster for day-to-day work: runs the .NET services and the frontend dev
servers as local processes instead of rebuilding images, and applies database migrations for you.

```bash
cd backend/aspire/Cinedex.AppHost
dotnet user-secrets set "Parameters:postgres-password" "<choose a password>"   # once
dotnet run
```

Full detail, including how to switch individual resources off, is in
[Observability & Local Dev Workflow](./observability-and-dev-workflow.md).
