---
sidebar_position: 4
---

# Frontend & Component Library

`frontend/` is an npm workspace holding seven packages: the SPA, its Storybook, this docs site, and
four library packages — a design system plus three component tiers.

| Package              | Path                  | What it is                                                                          |
| -------------------- | --------------------- | ----------------------------------------------------------------------------------- |
| `cinedex-app`        | `apps/cinedex-app/`   | The React 19 + Vite SPA, served by Nginx behind Compose's Caddy HTTPS/API edge.     |
| `@cinedex/storybook` | `apps/storybook/`     | Storybook for all three component tiers — served on port 9001.                      |
| `@cinedex/docs-site` | `apps/docs-site/`     | This site.                                                                          |
| `@cinedex/theme`     | `packages/theme/`     | The design system — tokens, base element styling, the Tailwind theme. **No React.** |
| `@cinedex/atoms`     | `packages/atoms/`     | Primitives — Radix-backed, Tailwind-styled, one job each.                           |
| `@cinedex/compounds` | `packages/compounds/` | Templates — brand-agnostic assemblies of atoms.                                     |
| `@cinedex/solution`  | `packages/solution/`  | Cinedex own screens. Presentational: no router, no data fetching.                   |

## The SPA

React 19 with the [React Compiler](https://react.dev/learn/react-compiler), TypeScript, and Vite for
the dev server, HMR, and builds. Tests run on Vitest + Testing Library.

The dev server runs at **https://localhost:9000** with a local HTTPS certificate, and proxies
`/movies-svc` to the backend's HTTPS dev profile — so auth cookies use the same secure, same-origin
shape locally as they do under Docker Compose. In both local modes, browser code calls the API with
relative paths such as `/movies-svc/auth/login`.

## Three component tiers

Components are split by how fast they change, and each tier is bounded by what it is allowed to know:

- **`@cinedex/atoms`** — one job, no internal arrangement: `Button`, `Input`, `Checkbox`,
  `PasswordInput`, `OtpInput`. Built on Radix primitives wherever real interaction semantics are
  involved, styled with Tailwind, variants expressed with [cva](https://cva.style/).
- **`@cinedex/compounds`** — a named layout assembled from atoms, **with no brand in it**:
  `AuthCard`, `PasswordField`, `StatPair`.
- **`@cinedex/solution`** — Cinedex-specific: the auth screens, the copy, the `Brand`. The only tier
  that names the product.

The clearest illustration is `AuthCard`. It takes `brand` as a prop and never draws the wordmark;
`@cinedex/solution`’s `Brand` supplies it. Compounds know _where_ a brand goes; solution knows
_which_. The same idea covers navigation: the screens know the route paths, but not how to navigate
them, so the host injects a link component — which is why a full sign-in screen renders in Storybook
with no router and no mock.

All four library packages are **source-consumed** — their `exports` point straight at `src/`, not a
built `dist/`:

```jsonc
"exports": {
  ".": { "types": "./src/index.ts", "default": "./src/index.ts" }
}
```

That means no build step, HMR that crosses package boundaries (editing an atom refreshes the running
app), and Storybook, Vitest, and the SPA all compiling the exact same source.

Every component below has a live, interactive story — with the theme toolbar and the accessibility
panel — in [the Storybook workbench](#the-storybook-workbench). If the stack is already running,
it's at **[http://localhost:9001](http://localhost:9001)**; otherwise `npm run storybook` from
`frontend/`.

A selection of the atoms:

| Component       | What it does                                                                                                                                                           |
| --------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Button`        | Action primitive — four variants across three sizes. Defaults to `type="button"` so it never submits a form by accident; `asChild` renders a link with button styling. |
| `TextField`     | Form-input primitive — label + input + optional error, wired together with a generated id so `htmlFor`, `aria-describedby`, and `aria-invalid` always agree.           |
| `Checkbox`      | Radix checkbox — a `<button role="checkbox">` rather than an `<input>`, named via `aria-labelledby`.                                                                   |
| `PasswordInput` | Masked input with an in-field reveal toggle.                                                                                                                           |
| `OtpInput`      | One box per digit, behaving like a single input: keyboard navigation, paste, backspace.                                                                                |

### Theming

Every component resolves colour, type, spacing, and radii through `@cinedex/theme`’s design tokens —
never a hard-coded value, and never a raw pixel size where a named type step exists. Colours are declared once with
[`light-dark()`](https://developer.mozilla.org/en-US/docs/Web/CSS/color_value/light-dark) rather
than duplicated under a `prefers-color-scheme` media query:

```css
:root {
  color-scheme: light dark;
  --accent: light-dark(#6d41a9, #bc98f9);
}
```

The used `color-scheme` picks a side — `light dark` follows the operating system by default.
Setting `color-scheme: dark` (or `light`) on the root forces a theme instead; that single line is
the entire mechanism behind Storybook's theme toolbar, and it also makes native form controls
render with matching chrome.

## The Storybook workbench

`@cinedex/storybook` is its own app, not part of any library — it depends on all three tiers and
imports every story through their public exports (`import { Button } from '@cinedex/atoms'`), never
by relative path into a package's source. That keeps those public surfaces honest: a component
missing from a barrel export fails the Storybook build rather than going unnoticed. Stories are
grouped **Atoms**, **Compounds** and **Solution**, so the sidebar mirrors the tiers.

```bash
npm run storybook    # from frontend/ → http://localhost:9001
```

## Everything together

```bash
npm ci
npm run start        # SPA        → https://localhost:9000
npm run storybook    # Storybook  → http://localhost:9001
npm run docs-site    # this site  → http://localhost:9004
```

ESLint (type-aware, `typescript-eslint` strict + stylistic) and Prettier are configured once at the
workspace root and cover every package; CI runs `lint`, `format:check`, `build`, and `coverage` for
the frontend on every change. The workspace build includes Storybook.
