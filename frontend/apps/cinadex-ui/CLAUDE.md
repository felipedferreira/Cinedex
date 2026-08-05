# cinadex-ui

React 19 + TypeScript + Vite SPA (React Compiler enabled via Babel preset). Vitest + Testing Library for tests. The production Docker image doubles as the stack's HTTPS reverse proxy (Nginx).

One of two packages in the `frontend/` npm workspace — see [`../../CLAUDE.md`](../../CLAUDE.md). It consumes [`@cinedex/ui`](../../packages/ui/CLAUDE.md) for shared components, design tokens and base styling.

## Commands (from `frontend/`, the workspace root)

```bash
npm ci
npm run dev          # https://localhost:9000 (basic-ssl, strictPort; proxies /movies-svc → https://localhost:7201)
npm run build        # tsc -b && vite build
npm run test:run     # single Vitest pass across the workspace
npm run coverage     # what CI runs
npm run lint         # eslint (`npm run lint:fix` to autofix)
npm run format:check # prettier (`npm run format` to write)
```

Scope to this package with `-w cinadex-ui` (e.g. `npm run test -w cinadex-ui` for watch mode). Lint and format are workspace-wide only.

CI requires lint, format:check, build, build-storybook, and coverage to all pass — run `npm run format` before pushing to avoid format:check failures.

## Notes

- The dev-server API proxy target is overridable via `VITE_API_PROXY_TARGET` (defaults to the local `dotnet run` backend at `https://localhost:7201`); the port is overridable via `PORT` (defaults to 9000). Both are read from `process.env` in `vite.config.ts`, i.e. from the shell — a `.env` file will **not** work for them.
- The Aspire AppHost (`backend/aspire/Cinedex.AppHost`) runs this package's `npm run dev` as a resource — pinning the port to 9000 (as `--port` and `PORT`) and setting `VITE_API_PROXY_TARGET` to the web service it started — so `dotnet run --project aspire/Cinedex.AppHost` brings the SPA up at https://localhost:9000 already wired to the API. `Features:EnableFrontendUiSvc: false` there omits it. Its `AppHostConstants.FrontendAppDirectory` points at **this** directory, not the workspace root, because that is what makes `npm run dev` resolve to plain `vite` so Aspire's `--port` reaches it.
- `nginx.conf` serves the SPA and proxies `/movies-svc/` to the `movies.webservice` container — a backend base-path change must be mirrored there and in `vite.config.ts`.
- **`main.tsx` imports `@cinedex/ui/tokens.css` and `@cinedex/ui/base.css` before `./index.css`.** Design tokens and base element styling belong to the library; only app-specific layout lives in `src/index.css`.
- The `Dockerfile` here builds from the **`frontend/` context**, not this directory — the workspace lockfile lives one level up.
- Folder name is spelled `cinadex-ui`; the product is "Cinedex".
