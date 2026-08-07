# @cinedex/theme

The Cinedex design system: tokens, base element styling, and the Tailwind theme. **No React, no JavaScript** — three stylesheets and a `package.json`. Everything visual in `@cinedex/atoms`, `@cinedex/compounds` and `@cinedex/solution` resolves through this package, so a rebrand touches one file.

One of four packages in the `frontend/` npm workspace — see [`../../CLAUDE.md`](../../CLAUDE.md).

## Layout

```
src/
├── tokens.css     # every token (@cinedex/theme/tokens.css)
├── base.css       # base element styling (@cinedex/theme/base.css)
└── tailwind.css   # THE entry point (@cinedex/theme/tailwind.css)
```

## Conventions

- **Consumers import `tailwind.css` and nothing else.** It `@import`s the other two itself, in the only order that works. `tokens.css` and `base.css` stay exported for a consumer that genuinely wants one alone (the docs site takes `--accent` for its Infima primary), but an app or Storybook wants the single entry.
- **Raw tokens are unprefixed** (`--accent`, `--bg`, `--radius-md`) **except the type and tracking scales**, which are `--type-*` and `--track-*`. That is deliberate: `--text` is already the body colour, and `--text-*`/`--tracking-*` are Tailwind's own theme namespaces, so distinct raw names keep the `@theme inline` bridge readable.
- **Type steps are named for their role, not their size** — `--type-label`, `--type-caption`, `--type-note`, `--type-body`, `--type-title`, `--type-footnote`, `--type-brand`. A component asks for `text-label` and never carries a pixel value. Adding a step means a line in `tokens.css`, a line in the `@theme inline` block, **and** a line in `packages/atoms/src/utils/cn.ts` (see below).
- **Theming goes through `light-dark()`.** Tokens declare both values at once and the used `color-scheme` picks one, so `color-scheme: light dark` follows the OS while setting `dark`/`light` on the root forces a theme. That is the hook Storybook's theme toolbar uses — there is no theme class and no duplicated dark block.

## Three things that fail silently

- **`@source` is load-bearing.** Tailwind never scans `node_modules`, and npm workspaces symlink `node_modules/@cinedex/atoms` → `packages/atoms`. Without the `@source` lines in `tailwind.css`, a class used _only_ inside a library package generates no CSS — no error, no warning, just an unstyled component. **A new library package needs a line there.** (Verified empirically: `@source` paths resolve relative to the CSS file that declares them, which is why they can live here and serve every consumer.)
- **`base.css` must arrive `layer(base)`, after `tailwindcss`.** Tailwind v4 puts everything it emits into cascade layers, and **unlayered CSS outranks every layer** regardless of source order or specificity. Imported bare, `base.css`'s `h1 { font-size: 56px }` beat `text-title` on the same element and every auth card rendered its heading at the landing page's size. Inside `layer(base)` it lands after preflight (so its typography still reaches bare markup) but below `layer(utilities)` (so a component's own classes win).
- **New `--text-*`/`--tracking-*` steps need registering in `cn()`.** `tailwind-merge` classifies an unrecognised `text-*` class as a _colour_, so `text-label text-accent` would be treated as two conflicting colours and one silently dropped. `packages/atoms/src/utils/cn.ts` extends the `font-size` and `tracking` class groups with these steps; the two lists must stay in step.

## Notes

- `tailwindcss` is a real dependency here, not a devDependency — `tailwind.css`'s `@import 'tailwindcss'` needs it at consumer build time.
- There is no `build`, `test` or `coverage` script, so the workspace-wide `--if-present` runs skip this package. CI has no coverage step for it either — there is no JavaScript to cover.
- `rounded-xs` is deliberately not mapped in `@theme inline`; components wanting a hairline radius use Tailwind's own 2px default.
