# Cinedex Documentation

Design documentation that describes *why* the system is shaped the way it is. It lives in the
repository (rather than the GitHub wiki) so it is versioned with the code, reviewed in the same
pull request as the change it describes, and impossible to update out of band.

Operational instructions — how to build, run, test, and migrate — stay in the READMEs.

## Contents

| Document | What it covers |
|---|---|
| [Auth Execution Plan](superpowers/plans/2026-07-29-auth-execution-plan.md) | 65-issue roadmap organized in 7 waves (dependency depth) and 6 lanes (staffing themes). Includes the interactive swimlane board, execution order algorithm, and critical path corrections. Start with Wave 0: 21 unblocked issues ready to ship today. |
| [Auth & Security Model](auth-security-model.md) | JWT access tokens, rotating refresh tokens, the `auth` schema, Identity behind ports, and the known gaps. |

## Elsewhere

| Document | What it covers |
|---|---|
| [Root README](../README.md) | Repository layout, Docker Compose quick start. |
| [Backend README](../backend/README.md) | Architecture guide, migrations, health checks, observability, coverage. |
| [Frontend README](../frontend/cinadex-ui/README.md) | `cinadex-ui` stack, scripts, linting, testing. |
| [Contracts README](../backend/NuGetLibraries/Cinedex.WebService.Contracts/README.md) | Shared request/response DTOs. |
| [CONTRIBUTING](../CONTRIBUTING.md) | Workflow, code standards, PR checklist. |
| [CHANGELOG](../CHANGELOG.md) | Version history. |

## Planned

Not yet written. Listed so the gaps are visible rather than forgotten:

- **Architecture Decision Records** (`adr/`) — hexagonal layering, Identity behind ports, the
  separate `auth` schema, FastEndpoints/REPR over MVC, EF Fluent API to keep the domain
  framework-free, Seq via OTLP, Guid v7 keys.
- **API conventions** — the `/movies-svc` base path, RFC 7807 problem details, the correlation-id
  header, and the status-code contract. (The endpoint *reference* is generated: see the Scalar UI
  at `/movies-svc/api-docs/v1`.)
- **Frontend ↔ backend contract** — CORS or reverse proxy, where the access token is stored,
  refresh-on-401 retry semantics.
- **Domain glossary** — `Title` vs. "Movie". Two naming questions are already decided and
  just need writing up: the `movies-svc` base path / `movies.webservice` image / `movies`
  database naming is **intentional legacy and stays** (renaming the base path would be a
  breaking API change touching the auth cookie path, the reverse proxy, and every client),
  and the frontend's `cinadex-ui` spelling is **deliberate**, not a typo.
