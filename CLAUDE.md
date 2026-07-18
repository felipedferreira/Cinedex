# Cinedex

Full-stack movie catalog (IMDB-inspired portfolio app): .NET 10 backend + React 19 SPA, orchestrated with Docker Compose.

## Layout

- `backend/` — .NET solution (`Cinedex.slnx`), hexagonal architecture. Details in `backend/CLAUDE.md`.
- `frontend/cinadex-ui/` — React + TypeScript + Vite SPA (folder is spelled `cinadex`, not `cinedex`).
- `docs/` — design docs (auth & security model); feature specs under `docs/superpowers/specs/`.
- `compose.yaml` — full stack: Postgres 17, web service, UI + Nginx reverse proxy, Seq (logs/traces).

## Commands

- Backend (from `backend/`): `dotnet build`, `dotnet test`, `dotnet run --project src/Presentation/Cinedex.WebService`
- Frontend (from `frontend/cinadex-ui/`): `npm ci`, `npm run dev`, `npm run test:run`, `npm run lint`, `npm run format:check`
- Full stack (repo root): `docker compose up --build` — requires a root `.env` (`cp .env.example .env`, fill in DB/Seq values) or compose fails.

With compose up: UI/proxy at https://localhost:9000 (self-signed cert — `curl -k`), API at https://localhost:9000/movies-svc, Scalar API docs at `/movies-svc/api-docs/v1`, Seq at http://localhost:5341.

## Critical gotchas

- **Migrations are never applied automatically** — not by compose, not by `dotnet run`. A fresh database needs `dotnet ef database update` for BOTH `FilmDbContext` and `AuthDbContext`. Commands in `backend/CLAUDE.md`.
- **Edit only the root `CHANGELOG.md`.** `backend/CHANGELOG.md` is a build-managed copy (a local backend build refreshes it — commit the resulting diff). CI fails if the two files differ.
- **Naming drift**: product is "Cinedex", API base path is `/movies-svc`, but the catalog entity is `Title` (routes are `/movies-svc/titles`). Older docs may say "Movie"/"Movies" — trust `ApiConstants.cs` and the domain code.
- Backend treats all warnings as errors (StyleCop + .NET analyzers) — a style violation breaks the build.
- Backend integration tests require Docker running (Testcontainers Postgres).

## Toolchain & CI

- .NET SDK 10.0.100 (`backend/global.json`, prerelease allowed), Node 22, Docker.
- CI (`.github/workflows/build-and-test.yml`): backend job = changelog-sync check + Release build + tests; frontend job = lint + format:check + build + coverage. All checks required to merge to `main`.
- Branch prefixes: `feature/`, `bugfix/`, `chore/`, `docs/`. Version is bumped in `backend/Directory.Build.props` (Version, FileVersion, InformationalVersion together).
