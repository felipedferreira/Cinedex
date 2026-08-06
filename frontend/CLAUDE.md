# frontend

npm **workspace root** for the Cinedex frontend. The lockfile lives here — there is no lockfile inside the packages.

| Package               | Path                   | What it is                                                                                                             |
| --------------------- | ---------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| `cinadex-app`         | `apps/cinadex-app/`    | The React 19 + Vite SPA. Its Docker image doubles as the stack's HTTPS reverse proxy (Nginx).                          |
| `@cinedex/storybook`  | `apps/storybook/`      | Storybook for the component library. Depends on `@cinedex/components` and owns the stories. Served on 9001 in compose. |
| `@cinedex/components` | `packages/components/` | Shared component library. **Source-consumed** — `exports` point at `src/`, so it has no build step and no `dist/`.     |

## Commands (all from `frontend/`)

```bash
npm ci
npm run dev              # app on https://localhost:9000 (basic-ssl, strictPort)
npm run storybook        # Storybook on http://localhost:9001
npm run build            # every package (--workspaces --if-present)
npm run build-storybook  # static Storybook, also run in CI
npm run test:run         # every package
npm run coverage         # what CI runs — one coverage/ dir per package
npm run lint             # eslint . — one pass across all packages
npm run format:check     # prettier — run `npm run format` before pushing
```

Target one package with `-w cinadex-app`, `-w @cinedex/components` or `-w @cinedex/storybook`, e.g. `npm run test -w @cinedex/components` for watch mode.

## Notes

- **Shared config is hoisted here**, not per-package: `eslint.config.js`, `.prettierrc.json`, `.prettierignore`, `.gitignore`, `.dockerignore`. ESLint uses `projectService: true`, which resolves the nearest `tsconfig` per file, so the single root config covers every package.
- **Both apps depend on `@cinedex/components` as source.** Vite compiles its TSX directly and HMR crosses the package boundary; `tsc -b` in a consumer therefore also typechecks library source under that consumer's compiler flags. Keep the `tsconfig` files in step.
- **The library exports nothing but its barrel.** `packages/components/src/index.ts` is the whole public surface — the Storybook app imports through it, so a component missing from the barrel fails `build-storybook` rather than going unnoticed.
- **Docker builds from this directory**, not from an app folder — the lockfile is here. Both images set `context: ./frontend` with an explicit `dockerfile:`. Every workspace manifest must be `COPY`d before `npm ci`, which validates the lockfile against the whole workspace even when scoped with `--workspace`.
- **Never add `**/.storybook` to `.dockerignore`** — `apps/storybook` builds inside its image and needs that config.
- **Install scripts are default-deny.** `.npmrc` sets `strict-allow-scripts=true`, so any dependency carrying a `preinstall`/`install`/`postinstall` script that is not listed in the root `package.json`'s `allowScripts` map makes `npm ci` **fail** rather than warn. npm's default is warn-and-run, which is the exact vector CVE-2025-54313 used against `eslint-config-prettier`. Both `esbuild` and `fsevents` are set to `false` — neither script is needed, since esbuild's binary ships via `optionalDependencies` and `fsevents` is macOS-only. A new script surfacing means a dependency changed shape: review it, then `npm approve-scripts <pkg>` (pinned to the version by default) or `npm deny-scripts <pkg>`. Never reach for `--dangerously-allow-all-scripts`.
- **`.npmrc` must be `COPY`d before `npm ci` in both Dockerfiles**, or images install with the guard off. It holds config only — never put a token in it, since it is committed and copied into build contexts.
- The SPA is spelled `cinadex-app` — an "a", not an "e". The product is "Cinedex" and the scoped packages (`@cinedex/components`, `@cinedex/storybook`) use that correct spelling. The mismatch is deliberate and long-standing; see [`docs/README.md`](../docs/README.md).
