# cinadex-app

React 19 + TypeScript + Vite SPA (React Compiler enabled via Babel preset). Vitest + Testing Library for tests. Its Docker image serves the static bundle over internal HTTP with Nginx; Compose's Caddy edge owns HTTPS and API routing.

One of seven packages in the `frontend/` npm workspace — see [`../../CLAUDE.md`](../../CLAUDE.md). It consumes [`@cinedex/solution`](../../packages/solution/CLAUDE.md) for its auth screens, [`@cinedex/atoms`](../../packages/atoms/CLAUDE.md) for the odd primitive, and [`@cinedex/theme`](../../packages/theme/CLAUDE.md) for the design system.

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

Scope to this package with `-w cinadex-app` (e.g. `npm run test -w cinadex-app` for watch mode). Lint and format are workspace-wide only.

CI requires lint, format:check, build, build-storybook, and coverage to all pass — run `npm run format` before pushing to avoid format:check failures.

## Notes

- The dev-server API proxy target is overridable via `VITE_API_PROXY_TARGET` (defaults to the local `dotnet run` backend at `https://localhost:7201`); the port is overridable via `PORT` (defaults to 9000). Both are read from `process.env` in `vite.config.ts`, i.e. from the shell — a `.env` file will **not** work for them.
- The Aspire AppHost (`backend/aspire/Cinedex.AppHost`) runs this package's `npm run dev` as a resource — pinning the port to 9000 (as `--port` and `PORT`) and setting `VITE_API_PROXY_TARGET` to the web service it started — so `dotnet run --project aspire/Cinedex.AppHost` brings the SPA up at https://localhost:9000 already wired to the API. `Features:EnableFrontendUiSvc: false` there omits it. Its `AppHostConstants.FrontendAppDirectory` points at **this** directory, not the workspace root, because that is what makes `npm run dev` resolve to plain `vite` so Aspire's `--port` reaches it.
- `nginx.conf` only serves the SPA and its history fallback on internal port 8080. The root `../../../Caddyfile` owns the Compose `/movies-svc` route to `movies.webservice`; a backend base-path change must be mirrored there and in `vite.config.ts`.
- **`main.tsx` imports `@cinedex/theme/tailwind.css` and nothing else.** That one import pulls in the tokens, the base element styling and Tailwind, in the cascade-layer order they have to be in — see [`packages/theme/CLAUDE.md`](../../packages/theme/CLAUDE.md). This app has **no stylesheet of its own**: every screen it renders comes from `@cinedex/solution` and is styled through the theme's utilities. An app-specific rule would go in a new `src/index.css` imported after that line.
- **This app is routes and nothing else.** `src/` holds `routes/`, the generated `routeTree.gen.ts`, `main.tsx` and one test. There is no `App.tsx` — the Vite scaffold landing page (hero image, counter, Vite/React links) was replaced by `@cinedex/solution`'s `HomeScreen`, which indexes every screen in the flow.
- **`routes/__root.tsx` holds `RouterLink`, the entire coupling between the screen library and the router.** `@cinedex/solution` navigates through an injected link component so it can stay router-free and storyable; this app adapts TanStack Router's `Link` to that contract, with one cast where the two type systems meet. `login-routing.test.tsx` mounts the real route tree and is what verifies the paths the screens hardcode are real routes.
- The `Dockerfile` here builds from the **`frontend/` context**, not this directory — the workspace lockfile lives one level up.
- Folder name is spelled `cinadex-app`; the product is "Cinedex".
