# Cinedex

[![Build and Test](https://github.com/felipedferreira/Cinedex/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/felipedferreira/Cinedex/actions/workflows/build-and-test.yml)

A full-stack portfolio application for cataloging movie titles and their genres — inspired by IMDB — with JWT-based authentication in front of a members-only catalog.

## 📁 Repository Layout

```
Cinedex/
├── backend/      # .NET solution (Web API, application core, persistence, tests)
├── frontend/     # Standalone SPA consuming the backend's OpenAPI spec
├── docs/         # Design docs (auth & security model, planned ADRs)
└── compose.yaml  # Orchestrates PostgreSQL, the web service, the frontend, Seq, and Mailpit
```

- **[Getting Started](docs/getting-started.md)** — new here? Clone-to-running-app in 5 minutes via Docker Compose
- **[Backend](backend/README.md)** — hexagonal (ports & adapters) .NET solution: architecture guide, build/test/migration instructions
- **[Frontend](frontend/cinadex-ui/README.md)** — standalone React + TypeScript + Vite SPA (`cinadex-ui`)
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
