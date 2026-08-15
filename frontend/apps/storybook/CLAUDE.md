# @cinedex/storybook

The Storybook for Cinedex's three component tiers — [`@cinedex/atoms`](../../packages/atoms/CLAUDE.md), [`@cinedex/compounds`](../../packages/compounds/CLAUDE.md) and [`@cinedex/solution`](../../packages/solution/CLAUDE.md) — as its own workspace app. It depends on them the same way the SPA does, through the package exports, so the stories can only use what each `src/index.ts` actually exports. A forgotten export breaks this build rather than going unnoticed.

One of three apps in the `frontend/` npm workspace — see [`../../CLAUDE.md`](../../CLAUDE.md).

## Commands (from `frontend/`, the workspace root)

```bash
npm run storybook        # http://localhost:9001
npm run build            # typecheck + static output to apps/storybook/storybook-static/
```

Both root scripts delegate to this package with `-w @cinedex/storybook`.

## Layout

```
.storybook/main.ts          # stories glob, addons, staticDirs
.storybook/manager.ts       # the manager UI's own theme — brand lockup, not story content
.storybook/manager-head.html # <head> injection for the manager UI — favicon and the social card
.storybook/preview.tsx      # theme CSS, SolutionProvider, a11y params, theme toolbar
.storybook/public/brand.svg # the static Cinedex lockup manager.ts's brandImage points at
src/atoms/*.stories.tsx      # grouped by tier — this is also the sidebar grouping
src/compounds/*.stories.tsx
src/solution/*.stories.tsx
vite.config.ts            # Tailwind + React + React Compiler; Storybook auto-loads it
Dockerfile  nginx.conf    # static bundle on Nginx, port 9001 in compose
```

## Conventions

- **Import from the packages, never by relative path into them.** That is the whole point of the split; `import { Button } from '@cinedex/atoms'`.
- Stories are CSF3 with `satisfies Meta<typeof X>` and `tags: ['autodocs']`.
- **One component per file, named after it, with `component` set** — `Card.stories.tsx` holds `Atoms/Card` and nothing else. A meta without `component` gets no props table and no controls for anything in it, which is what made the old catch-all `Atoms/Primitives` and `Compounds/Assemblies` files worth splitting. Two components exported from one source folder still get a file each (`PasswordStrengthMeter`, `PasswordChecklist`).
- **Title by tier** — `Atoms/…`, `Compounds/…`, `Solution/…` — and put the file in the matching `src/` subfolder.
- Global styles come from one import in `preview.tsx`: `@cinedex/theme/tailwind.css`, which pulls in the tokens and base styling itself. Same single import the SPA's `main.tsx` uses.

## Notes

- **`vite.config.ts` must keep `@tailwindcss/vite`.** Every component in all three libraries is Tailwind-styled; without the plugin, `preview.tsx`'s theme import is inert and every story renders with correct markup and **no styling at all**, silently. This is the app-side half of the trap described in [`packages/theme/CLAUDE.md`](../../packages/theme/CLAUDE.md).
- **`preview.tsx` wraps stories in `<SolutionProvider>` with no `linkComponent`**, so `@cinedex/solution`'s screens fall back to plain anchors. A full screen therefore renders here with no router and no mock — do not add a router to make a story work; that would defeat the boundary the package exists to keep.
- **The Aspire AppHost (`backend/aspire/Cinedex.AppHost`) runs this package's `storybook` script as a resource**, so `dotnet run --project aspire/Cinedex.AppHost` brings it up on http://localhost:9001 alongside the rest of the stack. `Features:EnableStorybookSvc: false` there omits it. Its `AppHostConstants.StorybookAppDirectory` points at **this** directory rather than the workspace root, because the root script delegates through `-w @cinedex/storybook` and would swallow the `--port` Aspire appends. The script name is passed explicitly — `AddViteApp` defaults to a `dev` script, which this package deliberately does not have.
- **The theme toolbar works by setting `color-scheme` on the root**, because the theme's tokens resolve through `light-dark()`. There is no theme class and no duplicated dark block.
- **The decorator renders `<SolutionProvider key={scheme}>` on purpose — do not drop the key.** Chrome does not re-resolve `light-dark()` for every property on an existing element when `color-scheme` changes at runtime: a form control's `border-color` in particular keeps the old theme's value, while `color` on the same element updates. Remounting on theme change sidesteps it. This is a runtime-toggle quirk only; a page that loads under a theme resolves everything correctly, so the SPA is unaffected.
- `staticDirs` points at `../../cinedex-app/public` so the `/icons.svg#id` sprite resolves in stories, and at `./public` (this app's own) for `manager.ts`'s brand asset — both are copied into `storybook-static/` on build.
- **The manager UI's own branding (the sidebar header) is a separate concern from `Solution/Brand`'s stories.** `manager.ts` sets `brandImage`/`brandTitle` via `storybook/theming`'s `create()` — that's the _static_ lockup (`./public/brand.svg`, the same file `@cinedex/solution`'s animated components were built from), because Storybook's manager only accepts a static image for `brandImage`, never a React component; it is a wholly separate bundle from the preview iframe stories render into. **`base: 'dark'` is load-bearing, not a style choice** — the lockup's wordmark is drawn in a light colour for a dark ground, so on Storybook's default light manager theme it would render at near-zero contrast against the light sidebar bar. `manager.ts` changes need a dev-server restart; unlike `preview.tsx`, this does not hot-reload.
- **The social card lives in `manager-head.html`, and its URLs are absolute and hardcoded on purpose.** Sharing a link to this Storybook renders `assets/og-cinedex-card.jpg` — the product brand card, served by the same `staticDirs` line as the favicon, out of `cinedex-app/public/`. Three constraints shape it. **Storybook 10 removed the `managerHead` config hook**, so there is nowhere in `main.ts` to build these tags — grep `node_modules` for it before reaching for one; this file is the only injection point, and the favicon is the standing proof it is honoured. Being static HTML, it cannot read the environment, so the URLs name where Storybook publicly lives (`https://cinedex.online/storybook/`, `STORYBOOK_BASE_URL`'s production value) rather than the building host — the same canonical-origin choice as [`@cinedex/docs-site`](../docs-site/CLAUDE.md)'s `url`, and a crawler rejects a relative `og:image` anyway. And `og:title` is spelled out because Storybook names its own tab **"storybook - Storybook"**, which is what a crawler would otherwise put on the card. Verify a change against `storybook-static/index.html` after a build — never the dev server. A bespoke design-system card is a drop-in: change the two image URLs, the alt text, and `og:image:type` if it is not a JPEG.
- `tsconfig.node.json` covers `vite.config.ts` and `.storybook/**`, and needs `vite/client` in `types` for `preview.tsx`'s side-effect CSS imports plus `DOM` for its `document` access.
- **`addon-a11y`'s `test: 'error'` does not fail the Storybook build** — that needs the Vitest addon, which this package does not install. It only escalates the accessibility panel's reporting.
- The image builds from the **`frontend/` context**, not this directory. `frontend/.dockerignore` must never ignore `**/.storybook`, or the build loses its config.
