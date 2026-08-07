# @cinedex/storybook

The Storybook for Cinedex's three component tiers — [`@cinedex/atoms`](../../packages/atoms/CLAUDE.md), [`@cinedex/compounds`](../../packages/compounds/CLAUDE.md) and [`@cinedex/solution`](../../packages/solution/CLAUDE.md) — as its own workspace app. It depends on them the same way the SPA does, through the package exports, so the stories can only use what each `src/index.ts` actually exports. A forgotten export breaks this build rather than going unnoticed.

One of three apps in the `frontend/` npm workspace — see [`../../CLAUDE.md`](../../CLAUDE.md).

## Commands (from `frontend/`, the workspace root)

```bash
npm run storybook        # http://localhost:9001
npm run build-storybook  # static output to apps/storybook/storybook-static/ (also run in CI)
npm run build            # tsc -b here; typechecks the stories
```

Both root scripts delegate to this package with `-w @cinedex/storybook`.

## Layout

```
.storybook/main.ts        # stories glob, addons, staticDirs
.storybook/preview.tsx    # theme CSS, SolutionProvider, a11y params, theme toolbar
src/atoms/*.stories.tsx      # grouped by tier — this is also the sidebar grouping
src/compounds/*.stories.tsx
src/solution/*.stories.tsx
vite.config.ts            # Tailwind + React + React Compiler; Storybook auto-loads it
Dockerfile  nginx.conf    # static bundle on Nginx, port 9001 in compose
```

## Conventions

- **Import from the packages, never by relative path into them.** That is the whole point of the split; `import { Button } from '@cinedex/atoms'`.
- Stories are CSF3 with `satisfies Meta<typeof X>` and `tags: ['autodocs']`.
- **Title by tier** — `Atoms/…`, `Compounds/…`, `Solution/…` — and put the file in the matching `src/` subfolder.
- Global styles come from one import in `preview.tsx`: `@cinedex/theme/tailwind.css`, which pulls in the tokens and base styling itself. Same single import the SPA's `main.tsx` uses.

## Notes

- **`vite.config.ts` must keep `@tailwindcss/vite`.** Every component in all three libraries is Tailwind-styled; without the plugin, `preview.tsx`'s theme import is inert and every story renders with correct markup and **no styling at all**, silently. This is the app-side half of the trap described in [`packages/theme/CLAUDE.md`](../../packages/theme/CLAUDE.md).
- **`preview.tsx` wraps stories in `<SolutionProvider>` with no `linkComponent`**, so `@cinedex/solution`'s screens fall back to plain anchors. A full screen therefore renders here with no router and no mock — do not add a router to make a story work; that would defeat the boundary the package exists to keep.
- **The Aspire AppHost (`backend/aspire/Cinedex.AppHost`) runs this package's `storybook` script as a resource**, so `dotnet run --project aspire/Cinedex.AppHost` brings it up on http://localhost:9001 alongside the rest of the stack. `Features:EnableStorybookSvc: false` there omits it. Its `AppHostConstants.StorybookAppDirectory` points at **this** directory rather than the workspace root, because the root script delegates through `-w @cinedex/storybook` and would swallow the `--port` Aspire appends. The script name is passed explicitly — `AddViteApp` defaults to a `dev` script, which this package deliberately does not have.
- **The theme toolbar works by setting `color-scheme` on the root**, because the theme's tokens resolve through `light-dark()`. There is no theme class and no duplicated dark block.
- **The decorator renders `<SolutionProvider key={scheme}>` on purpose — do not drop the key.** Chrome does not re-resolve `light-dark()` for every property on an existing element when `color-scheme` changes at runtime: a form control's `border-color` in particular keeps the old theme's value, while `color` on the same element updates. Remounting on theme change sidesteps it. This is a runtime-toggle quirk only; a page that loads under a theme resolves everything correctly, so the SPA is unaffected.
- `staticDirs` points at `../../cinadex-app/public` so the `/icons.svg#id` sprite resolves in stories.
- `tsconfig.node.json` covers `vite.config.ts` and `.storybook/**`, and needs `vite/client` in `types` for `preview.tsx`'s side-effect CSS imports plus `DOM` for its `document` access.
- **`addon-a11y`'s `test: 'error'` does not fail `build-storybook`** — that needs the Vitest addon, which this package does not install. It only escalates the accessibility panel's reporting.
- The image builds from the **`frontend/` context**, not this directory. `frontend/.dockerignore` must never ignore `**/.storybook`, or the build loses its config.
