# frontend

npm **workspace root** for the Cinedex frontend. The lockfile lives here — there is no lockfile inside the packages.

| Package       | Path               | What it is                                                                                                                     |
| ------------- | ------------------ | ------------------------------------------------------------------------------------------------------------------------------ |
| `cinadex-ui`  | `apps/cinadex-ui/` | The React 19 + Vite SPA. Its Docker image doubles as the stack's HTTPS reverse proxy (Nginx).                                  |
| `@cinedex/ui` | `packages/ui/`     | Shared component library + Storybook. **Source-consumed** — `exports` point at `src/`, so it has no build step and no `dist/`. |

## Commands (all from `frontend/`)

```bash
npm ci
npm run dev              # app on https://localhost:9000 (basic-ssl, strictPort)
npm run storybook        # Storybook on http://localhost:6006
npm run build            # every package (--workspaces --if-present)
npm run build-storybook  # static Storybook, also run in CI
npm run test:run         # every package
npm run coverage         # what CI runs — one coverage/ dir per package
npm run lint             # eslint . — one pass across all packages
npm run format:check     # prettier — run `npm run format` before pushing
```

Target one package with `-w cinadex-ui` or `-w @cinedex/ui`, e.g. `npm run test -w @cinedex/ui` for watch mode.

## Notes

- **Shared config is hoisted here**, not per-package: `eslint.config.js`, `.prettierrc.json`, `.prettierignore`, `.gitignore`, `.dockerignore`. ESLint uses `projectService: true`, which resolves the nearest `tsconfig` per file, so the single root config covers both packages.
- **The app depends on `@cinedex/ui` as source.** Vite compiles its TSX directly and HMR crosses the package boundary; `tsc -b` in the app therefore also typechecks library source under the app's compiler flags. Keep `apps/cinadex-ui/tsconfig.app.json` and `packages/ui/tsconfig.lib.json` in step.
- **Docker builds from this directory**, not from the app folder — the lockfile is here. `compose.yaml` sets `context: ./frontend` with `dockerfile: apps/cinadex-ui/Dockerfile`.
- Folder is spelled `cinadex-ui`; the product is "Cinedex". The library package is scoped `@cinedex/ui` (correct spelling).
