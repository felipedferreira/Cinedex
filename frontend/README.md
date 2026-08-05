# Cinedex Frontend

npm **workspace root** for the Cinedex frontend. The lockfile and all shared tooling config live here; the packages hold only what is specific to them.

| Package                                   | Path               | What it is                                                                                    |
| ----------------------------------------- | ------------------ | --------------------------------------------------------------------------------------------- |
| [`cinadex-ui`](apps/cinadex-ui/README.md) | `apps/cinadex-ui/` | The React 19 + Vite SPA. Its Docker image doubles as the stack's HTTPS reverse proxy (Nginx). |
| [`@cinedex/ui`](packages/ui/README.md)    | `packages/ui/`     | Shared component library + Storybook.                                                         |

```
frontend/
├── package.json          # workspaces: apps/*, packages/*
├── package-lock.json     # the only lockfile
├── eslint.config.js  .prettierrc.json  .prettierignore  .gitignore  .dockerignore
├── apps/cinadex-ui/
└── packages/ui/
```

## 🚀 Getting Started

Prerequisites: [Node.js](https://nodejs.org/) 22 and npm.

```bash
npm ci
npm run dev          # app        → https://localhost:9000
npm run storybook    # Storybook  → http://localhost:6006
```

The app's dev server uses a local HTTPS certificate and proxies `/movies-svc` to the backend's HTTPS dev profile at `https://localhost:7201`. Override with `VITE_API_PROXY_TARGET`.

## 📜 Scripts

All run from this directory.

| Script                    | Description                                          |
| ------------------------- | ---------------------------------------------------- |
| `npm run dev`             | Start the app's Vite dev server with HMR             |
| `npm run storybook`       | Start Storybook for `@cinedex/ui` on port 6006       |
| `npm run build`           | Type-check and build every package                   |
| `npm run build-storybook` | Build the static Storybook (also run in CI)          |
| `npm run test:run`        | Run every test suite once (CI-friendly)              |
| `npm run coverage`        | Run tests and write a `coverage/` report per package |
| `npm run lint`            | Lint every package with ESLint                       |
| `npm run format:check`    | Check formatting without writing (CI-friendly)       |

Scope any of them to one package with `-w cinadex-ui` or `-w @cinedex/ui` — for example `npm run test -w @cinedex/ui` for watch mode.

## 🧩 How the packages fit together

`@cinedex/ui` is **source-consumed**: its `exports` point at `src/`, not at a built `dist/`.

```jsonc
"exports": {
  ".":             { "types": "./src/index.ts", "default": "./src/index.ts" },
  "./tokens.css":  "./src/styles/tokens.css",
  "./base.css":    "./src/styles/base.css"
}
```

That buys three things and costs one:

- No build step for the library, so nothing to sequence in Docker or CI.
- HMR crosses the package boundary — editing a component refreshes the running app.
- Storybook, Vitest and the app all compile the exact same source.
- The cost: `tsc -b` in the app also typechecks library source under the app's compiler flags, so the two tsconfigs should stay in step.

Design tokens live with the library, not the app. `apps/cinadex-ui/src/main.tsx` imports `@cinedex/ui/tokens.css` and `@cinedex/ui/base.css` before its own `index.css`, and Storybook's preview loads the same pair — so a component looks the same in a story as it does in the app.

## 🎨 Linting & Formatting

One ESLint flat config at this level covers every package. It uses `projectService: true`, which resolves the nearest `tsconfig.json` per file, so type-aware rules work across the workspace without per-package configs.

```bash
npm run lint          # report problems
npm run lint:fix      # report + auto-fix
npm run format        # rewrite files to match Prettier
npm run format:check  # verify formatting (used in CI)
```

## 🐳 Docker

The app's image builds from **this** directory, because the lockfile is here:

```bash
docker build -f apps/cinadex-ui/Dockerfile -t cinadex-ui .
```

`compose.yaml` at the repo root does the same via `context: ./frontend` and `dockerfile: apps/cinadex-ui/Dockerfile`.
