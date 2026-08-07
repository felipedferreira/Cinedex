---
sidebar_position: 4
---

# Frontend & Component Library

`frontend/` is an npm workspace holding four packages: the SPA, its Storybook, the shared component
library, and this docs site.

| Package               | Path                   | What it is                                                                                             |
| --------------------- | ---------------------- | ------------------------------------------------------------------------------------------------------ |
| `cinadex-app`         | `apps/cinadex-app/`    | The React 19 + Vite SPA. Its Docker image doubles as the stack's HTTPS reverse proxy (Nginx).          |
| `@cinedex/storybook`  | `apps/storybook/`      | Storybook for the component library — served on port 9001.                                             |
| `@cinedex/components` | `packages/components/` | The shared design system — components, design tokens, base styles. No Storybook dependency of its own. |
| `@cinedex/docs-site`  | `apps/docs-site/`      | This site.                                                                                             |

## The SPA

React 19 with the [React Compiler](https://react.dev/learn/react-compiler), TypeScript, and Vite for
the dev server, HMR, and builds. Tests run on Vitest + Testing Library.

The dev server runs at **https://localhost:9000** with a local HTTPS certificate, and proxies
`/movies-svc` to the backend's HTTPS dev profile — so auth cookies use the same secure, same-origin
shape locally as they do under Docker Compose. In both local modes, browser code calls the API with
relative paths such as `/movies-svc/auth/login`.

## The component library

`@cinedex/components` is **source-consumed** — its `exports` point straight at `src/`, not a built
`dist/`:

```jsonc
"exports": {
  ".":             { "types": "./src/index.ts", "default": "./src/index.ts" },
  "./tokens.css":  "./src/styles/tokens.css",
  "./base.css":    "./src/styles/base.css"
}
```

That means no build step for the library, HMR that crosses the package boundary (editing a
component refreshes the running app), and Storybook, Vitest, and the SPA all compiling the exact
same source.

Every component below has a live, interactive story — with the theme toolbar and the accessibility
panel — in [the Storybook workbench](#the-storybook-workbench). If the stack is already running,
it's at **[http://localhost:9001](http://localhost:9001)**; otherwise `npm run storybook` from
`frontend/`.

Three primitives ship today:

| Component   | What it does                                                                                                                                                 |
| ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `Box`       | Layout primitive — a flex container with `direction`, `padding`, `gap`, `align`, `justify` drawn from the spacing tokens.                                    |
| `Button`    | Action primitive — `variant` (`primary` \| `ghost`) and `size` (`sm` \| `md`). Defaults to `type="button"` so it never submits a form by accident.           |
| `TextField` | Form-input primitive — label + input + optional error, wired together with a generated id so `htmlFor`, `aria-describedby`, and `aria-invalid` always agree. |

### Theming

Every component resolves colour, spacing, and radii through design tokens — never a hard-coded
value. Colours are declared once with
[`light-dark()`](https://developer.mozilla.org/en-US/docs/Web/CSS/color_value/light-dark) rather
than duplicated under a `prefers-color-scheme` media query:

```css
:root {
  color-scheme: light dark;
  --accent: light-dark(#aa3bff, #c084fc);
}
```

The used `color-scheme` picks a side — `light dark` follows the operating system by default.
Setting `color-scheme: dark` (or `light`) on the root forces a theme instead; that single line is
the entire mechanism behind Storybook's theme toolbar, and it also makes native form controls
render with matching chrome.

## The Storybook workbench

`@cinedex/storybook` is its own app, not part of the library — it depends on
`@cinedex/components` and imports every story through the package's public exports
(`import { Box, Button } from '@cinedex/components'`), never by relative path into the library's
source. That keeps the library's public surface honest: a component missing from the barrel export
fails the Storybook build rather than going unnoticed.

```bash
npm run storybook    # from frontend/ → http://localhost:9001
```

## Everything together

```bash
npm ci
npm run dev          # SPA        → https://localhost:9000
npm run storybook    # Storybook  → http://localhost:9001
npm run docs-site    # this site  → http://localhost:9004
```

ESLint (type-aware, `typescript-eslint` strict + stylistic) and Prettier are configured once at the
workspace root and cover every package; CI runs `lint`, `format:check`, `build`, `build-storybook`,
and `coverage` for the frontend on every change.
