# @cinedex/storybook

The Storybook for [`@cinedex/components`](../../packages/components/CLAUDE.md), as its own workspace app. It depends on the library the same way the SPA does — through the package exports — so the stories can only use what `packages/components/src/index.ts` actually exports. A forgotten export breaks this build rather than going unnoticed.

One of two apps in the `frontend/` npm workspace — see [`../../CLAUDE.md`](../../CLAUDE.md).

## Commands (from `frontend/`, the workspace root)

```bash
npm run storybook        # http://localhost:6006
npm run build-storybook  # static output to apps/storybook/storybook-static/ (also run in CI)
npm run build            # tsc -b here; typechecks the stories
```

Both root scripts delegate to this package with `-w @cinedex/storybook`.

## Layout

```
.storybook/main.ts       # stories glob, addons, staticDirs
.storybook/preview.tsx   # global styles, a11y params, theme toolbar
src/<Name>.stories.tsx   # one file per component, flat — not mirrored per folder
vite.config.ts           # React + React Compiler; Storybook auto-loads it
Dockerfile  nginx.conf   # static bundle on Nginx, port 9001 in compose
```

## Conventions

- **Import from `@cinedex/components`, never by relative path into the library.** That is the whole point of the split; `import { Box, Button } from '@cinedex/components'`.
- Stories are CSF3 with `satisfies Meta<typeof X>` and `tags: ['autodocs']`.
- Global styles come from the library's export entries (`@cinedex/components/tokens.css`, then `base.css`) in `preview.tsx`, in that order — the same pair, in the same order, that the SPA's `main.tsx` imports.

## Notes

- **The Aspire AppHost (`backend/aspire/Cinedex.AppHost`) runs this package's `storybook` script as a resource**, so `dotnet run --project aspire/Cinedex.AppHost` brings it up on http://localhost:6006 alongside the rest of the stack. `Features:EnableStorybookSvc: false` there omits it. Its `AppHostConstants.StorybookAppDirectory` points at **this** directory rather than the workspace root, because the root script delegates through `-w @cinedex/storybook` and would swallow the `--port` Aspire appends. The script name is passed explicitly — `AddViteApp` defaults to a `dev` script, which this package deliberately does not have.
- **The theme toolbar works by setting `color-scheme` on the root**, because the library's tokens resolve through `light-dark()`. There is no theme class and no duplicated dark block — see the library's CLAUDE.md.
- **The decorator renders `<Story key={scheme} />` on purpose — do not drop the key.** Chrome does not re-resolve `light-dark()` for every property on an existing element when `color-scheme` changes at runtime: a form control's `border-color` in particular keeps the old theme's value, while `color` on the same element updates. Remounting on theme change sidesteps it. This is a runtime-toggle quirk only; a page that loads under a theme resolves everything correctly, so the SPA is unaffected.
- `staticDirs` points at `../../cinadex-app/public` so the `/icons.svg#id` sprite resolves in stories.
- `vite.config.ts` carries the React Compiler Babel preset. Vite applies it to the linked `@cinedex/components` source too, so components compile here exactly as they do in the SPA.
- `tsconfig.node.json` covers `vite.config.ts` and `.storybook/**`, and needs `vite/client` in `types` for `preview.tsx`'s side-effect CSS imports plus `DOM` for its `document` access.
- **`addon-a11y`'s `test: 'error'` does not fail `build-storybook`** — that needs the Vitest addon, which this package does not install. It only escalates the accessibility panel's reporting.
- The image builds from the **`frontend/` context**, not this directory. `frontend/.dockerignore` must never ignore `**/.storybook`, or the build loses its config.
