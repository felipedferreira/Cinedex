# Cinedex

[![Build and Test](https://github.com/felipedferreira/Cinedex/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/felipedferreira/Cinedex/actions/workflows/build-and-test.yml)

A full-stack portfolio application for cataloging movie titles and their genres — inspired by IMDB — with JWT-based authentication in front of a members-only catalog.

## 📁 Repository Layout

```
Cinedex/
├── backend/      # .NET solution (Web API, application core, persistence, tests)
│   └── aspire/   # Aspire AppHost — the local dev loop (see below)
├── frontend/     # Standalone SPA consuming the backend's OpenAPI spec
├── docs/         # Design docs (auth & security model, planned ADRs)
└── compose.yaml  # Orchestrates PostgreSQL, the web service, the frontend, Seq, and Mailpit
```

- **[Getting Started](docs/getting-started.md)** — new here? Clone-to-running-app in 5 minutes via Docker Compose
- **[Backend](backend/README.md)** — hexagonal (ports & adapters) .NET solution: architecture guide, build/test/migration instructions
- **[Frontend](frontend/README.md)** — npm workspace: the React + TypeScript + Vite SPA (`cinadex-ui`) and the shared component library (`@cinedex/ui`) with its Storybook
- **[Design docs](docs/README.md)** — why the system is shaped this way (auth & security model, planned ADRs)
- **[Changelog](CHANGELOG.md)** — version history and release notes

## 🚀 Quick Start

```bash
git clone https://github.com/felipedferreira/Cinedex.git
cd Cinedex
cp .env.example .env       # fill in the database, Seq, and Mailpit values
docker compose up --build
```

Then open **https://localhost:9000** (self-signed cert — trust it or use `curl -k`).

There's one manual step before logs show up in Seq — full walkthrough, access points, and
troubleshooting in **[docs/getting-started.md](docs/getting-started.md)**.

### 🧪 Or: the Aspire dev loop

For iterating on the backend, the Aspire AppHost is faster — it runs the .NET services as local
processes instead of rebuilding images, and it applies the database migrations for you:

```bash
cd backend/aspire/Cinedex.AppHost
dotnet user-secrets set "Parameters:postgres-password" "<choose a password>"   # once
dotnet run
```

It needs Docker and that one secret, but no `.env`. The password is only applied when the database
volume is first created — to change it later, `docker volume rm cinedex-aspire-pgdata` first. PostgreSQL and Mailpit come up as containers; the migrator, web
service and scheduler worker run as processes, with their logs and traces on the Aspire dashboard
(the console prints a login URL). In JetBrains Rider, the equivalent **Aspire AppHost** entry is
already in the Run dropdown.

Once your schema is current, you can skip the migration step on subsequent runs — from
`backend/aspire/Cinedex.AppHost`, either `dotnet user-secrets set "Features:EnableDatabaseMigrationsSvc" "false"`
or copy `appsettings.Development.json.example` to `appsettings.Development.json` (git-ignored). Both
are per-developer and neither touches a tracked file. Turn it back on for a run after pulling a new
migration or against an empty database.

This complements Compose rather than replacing it — `docker compose up` is still the prod-like path,
with built images, the Nginx/HTTPS proxy, Seq and the SPA. **Run one or the other**: both publish
PostgreSQL on 5432, so the second to start fails to bind. Their data volumes are separate, so neither
can corrupt the other's database. The AppHost does not serve the SPA, so use Compose or `npm run dev`
for frontend work.
