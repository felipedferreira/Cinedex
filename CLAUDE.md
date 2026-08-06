# Cinedex

Full-stack movie catalog (IMDB-inspired portfolio app): .NET 10 backend + React 19 SPA, orchestrated with Docker Compose.

## Layout

- `backend/` — .NET solution (`Cinedex.slnx`), hexagonal architecture. Details in `backend/CLAUDE.md`.
- `backend/aspire/Cinedex.AppHost/` — Aspire orchestration for local dev. There is deliberately **no ServiceDefaults project**; `NuGetLibraries/FoundryOceanus.Observability.OpenTelemetry` already fills that role.
- `frontend/` — npm workspace root (`apps/*` + `packages/*`); the lockfile lives here, not in the packages.
  - `frontend/apps/cinadex-app/` — React + TypeScript + Vite SPA (folder is spelled `cinadex`, not `cinedex`).
  - `frontend/apps/storybook/` — `@cinedex/storybook`, the Storybook for the component library. Its own app: it depends on `@cinedex/components` and owns the stories, so they can only use what the library actually exports.
  - `frontend/packages/components/` — `@cinedex/components`, the shared component library. Source-consumed: its `exports` point at `src/`, so there is no build step and no `dist/`.
- `docs/` — design docs (auth & security model); feature specs under `docs/superpowers/specs/`.
- `compose.yaml` — full stack: Postgres 17, web service, UI + Nginx reverse proxy, Storybook, Seq (logs/traces), Mailpit (dev mail sink).

## Commands

- Backend (from `backend/`): `dotnet build`, `dotnet test`, `dotnet run --project src/Presentation/Cinedex.WebService`
- Frontend (from `frontend/`, the workspace root): `npm ci`, `npm run dev` (app on 9000), `npm run storybook` (Storybook on 6006), `npm run build`, `npm run test:run`, `npm run coverage`, `npm run lint`, `npm run format:check`. Lint and format run once across every package; build/test fan out with `--workspaces`. Target one package with `-w cinadex-app` or `-w @cinedex/components`.
- Full stack (repo root): `docker compose up --build` — requires a root `.env` (`cp .env.example .env`, fill in DB/Seq/Mailpit values) or compose fails.
- Local dev loop (from `backend/`): `dotnet run --project aspire/Cinedex.AppHost` — Postgres + Mailpit as containers, the three .NET hosts and both frontend dev servers (the SPA's and Storybook's) as processes, migrations applied automatically, telemetry on the Aspire dashboard. The UI comes up at https://localhost:9000 with its `/movies-svc` proxy already pointed at the AppHost's web service, and Storybook at http://localhost:6006, so this path now covers the full stack. Needs Docker, Node/npm, and one User Secret (`Parameters:postgres-password`); needs no `.env`. **Cannot run at the same time as compose** — both bind Postgres on 5432 and the SPA on 9000 — though the data volumes are separate. Skip the migrator with `Features:EnableDatabaseMigrationsSvc=false`, the UI with `Features:EnableFrontendUiSvc=false`, or Storybook with `Features:EnableStorybookSvc=false` (see `backend/CLAUDE.md`).

With compose up: UI/proxy at https://localhost:9000 (self-signed cert — `curl -k`), API at https://localhost:9000/movies-svc, Scalar API docs at `/movies-svc/api-docs/v1`, Storybook at http://localhost:9001, Seq at http://localhost:5341, Mailpit (captured email) at http://localhost:8025.

## Critical gotchas

- **`dotnet run` never applies migrations.** A fresh database needs `dotnet ef database update` for BOTH `FilmDbContext` and `AuthDbContext` — commands in `backend/CLAUDE.md`. The two orchestrated paths do it for you: compose runs `movies.databasemigrator` and gates the web service and scheduler worker on `condition: service_completed_successfully`, and the Aspire AppHost runs the same `Cinedex.DatabaseMigrator` as a run-to-completion resource behind `WaitForCompletion`. So the manual step is only for a bare `dotnet run` (and for `dotnet ef migrations add`).
- **Edit only the root `CHANGELOG.md`.** `backend/CHANGELOG.md` is a build-managed copy (a local backend build refreshes it — commit the resulting diff). CI fails if the two files differ.
- **Naming drift**: product is "Cinedex", API base path is `/movies-svc`, but the catalog entity is `Title` (routes are `/movies-svc/titles`). Older docs may say "Movie"/"Movies" — trust `ApiConstants.cs` and the domain code.
- Backend treats all warnings as errors (StyleCop + .NET analyzers) — a style violation breaks the build.
- Backend integration tests require Docker running (Testcontainers Postgres).

## Toolchain & CI

- .NET SDK 10.0.100 (`backend/global.json`, prerelease allowed), Node 22, Docker.
- CI (`.github/workflows/build-and-test.yml`): backend job = changelog-sync check + Release build + tests; frontend job = lint + format:check + build + build-storybook + coverage (one coverage summary per workspace). All checks required to merge to `main`.
- Commit messages follow Conventional Commits: `type(scope): summary` (types in use: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`). Branches have no prefix convention — recent ones are short kebab-case descriptions (e.g. `asp-net-identity-auth`).
- Changelog entries accumulate under `## [Unreleased]` (Keep a Changelog format); a release turns them into a `## [x.y.z] - Title` section with a matching version bump in `backend/Directory.Build.props` (Version, FileVersion, InformationalVersion together).
