# Cinedex Frontend

npm **workspace root** for the Cinedex frontend. The lockfile and all shared tooling config live here; the packages hold only what is specific to them.

| Package                                                | Path                   | What it is                                                                                             |
| ------------------------------------------------------ | ---------------------- | ------------------------------------------------------------------------------------------------------ |
| [`cinadex-app`](apps/cinadex-app/README.md)            | `apps/cinadex-app/`    | The React 19 + Vite SPA. Its Docker image doubles as the stack's HTTPS reverse proxy (Nginx).          |
| [`@cinedex/storybook`](apps/storybook/README.md)       | `apps/storybook/`      | Storybook for the component library. Depends on `@cinedex/components`; served on port 9001 in Compose. |
| [`@cinedex/components`](packages/components/README.md) | `packages/components/` | Shared component library — components, design tokens, base styles. No Storybook dependency.            |

```
frontend/
├── package.json          # workspaces: apps/*, packages/*
├── package-lock.json     # the only lockfile
├── eslint.config.js  .prettierrc.json  .prettierignore  .gitignore  .dockerignore
├── apps/cinadex-app/
├── apps/storybook/
└── packages/components/
```

Both apps consume `@cinedex/components`; nothing depends on an app.

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

| Script                    | Description                                            |
| ------------------------- | ------------------------------------------------------ |
| `npm run dev`             | Start the app's Vite dev server with HMR               |
| `npm run storybook`       | Start Storybook for `@cinedex/components` on port 6006 |
| `npm run build`           | Type-check and build every package                     |
| `npm run build-storybook` | Build the static Storybook (also run in CI)            |
| `npm run test:run`        | Run every test suite once (CI-friendly)                |
| `npm run coverage`        | Run tests and write a `coverage/` report per package   |
| `npm run lint`            | Lint every package with ESLint                         |
| `npm run format:check`    | Check formatting without writing (CI-friendly)         |

Scope any of them to one package with `-w cinadex-app`, `-w @cinedex/storybook` or `-w @cinedex/components` — for example `npm run test -w @cinedex/components` for watch mode.

## 🧩 How the packages fit together

`@cinedex/components` is **source-consumed**: its `exports` point at `src/`, not at a built `dist/`.

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
- Storybook, Vitest and the SPA all compile the exact same source.
- The cost: `tsc -b` in the app also typechecks library source under the app's compiler flags, so the two tsconfigs should stay in step.

Design tokens live with the library, not the app. `apps/cinadex-app/src/main.tsx` imports `@cinedex/components/tokens.css` and `@cinedex/components/base.css` before its own `index.css`, and the Storybook app's preview loads the same pair through the same export entries — so a component looks the same in a story as it does in the app.

The Storybook app is a consumer like any other: its stories `import { Box, Button } from '@cinedex/components'` rather than reaching into `packages/components/src`. That keeps `packages/components/src/index.ts` honest — a component missing from the barrel fails `build-storybook`.

## 🎨 Linting & Formatting

One ESLint flat config at this level covers every package. It uses `projectService: true`, which resolves the nearest `tsconfig.json` per file, so type-aware rules work across the workspace without per-package configs.

```bash
npm run lint          # report problems
npm run lint:fix      # report + auto-fix
npm run format        # rewrite files to match Prettier
npm run format:check  # verify formatting (used in CI)
```

## 🐳 Docker

Both images build from **this** directory, because the lockfile is here:

```bash
docker build -f apps/cinadex-app/Dockerfile -t cinadex-app .
```

```bash
docker build -f apps/storybook/Dockerfile -t cinedex-storybook .
```

`compose.yaml` at the repo root does the same via `context: ./frontend` and an explicit `dockerfile:` per service — the SPA on 9000 (HTTPS, self-signed) and Storybook on 9001 (plain HTTP; it proxies nothing).

Two things these Dockerfiles depend on, both easy to break:

- **Every workspace manifest is `COPY`d before `npm ci`**, even though each install is scoped with `--workspace`. `npm ci` validates the lockfile against the entire workspace, so a missing `package.json` fails the build — adding a package means adding a `COPY` line to both files.
- **`.dockerignore` must not ignore `**/.storybook`.** The Storybook image runs `build-storybook` inside the container and needs that config in the context.
