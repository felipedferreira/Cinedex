# cinadex-ui

React 19 + TypeScript + Vite SPA (React Compiler enabled via Babel preset). Vitest + Testing Library for tests; ESLint + Prettier enforced in CI. The production Docker image doubles as the stack's HTTPS reverse proxy (Nginx).

## Commands (from `frontend/cinadex-ui/`)

```bash
npm ci
npm run dev          # https://localhost:9000 (basic-ssl, strictPort; proxies /movies-svc → https://localhost:7201)
npm run test:run     # single Vitest pass (plain `npm test` runs watch mode)
npm run lint         # eslint (`npm run lint:fix` to autofix)
npm run format:check # prettier (`npm run format` to write)
npm run build        # tsc -b && vite build
npm run coverage     # what CI runs
```

CI requires lint, format:check, build, and coverage to all pass — run `npm run format` before pushing to avoid format:check failures.

## Notes

- The dev-server API proxy target is overridable via `VITE_API_PROXY_TARGET` (defaults to the local `dotnet run` backend at `https://localhost:7201`).
- `nginx.conf` serves the SPA and proxies `/movies-svc/` to the `movies.webservice` container — a backend base-path change must be mirrored there and in `vite.config.ts`.
- Folder name is spelled `cinadex-ui`; the product is "Cinedex".
