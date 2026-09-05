# @cinedex/theme

The Cinedex design system: **three stylesheets and no JavaScript.** Everything visual in `@cinedex/frames`, `@cinedex/shots` and `@cinedex/scenes` resolves through this package, so a rebrand touches one file.

Part of the [`frontend/` workspace](../../README.md).

## Using it

One import gets the whole system:

```ts
import '@cinedex/theme/tailwind.css';
```

`tailwind.css` `@import`s the other two itself, in the only order that works. They stay exported for a consumer that genuinely wants one alone — the docs site takes `--accent` for its Infima primary — but an app or Storybook wants the single entry.

```jsonc
"exports": {
  "./tokens.css":   "./src/tokens.css",
  "./base.css":     "./src/base.css",
  "./tailwind.css": "./src/tailwind.css"
}
```

## What's in it

**`tokens.css`** — colour, status ramps, shadow, type, tracking, spacing, radii. Colours are declared once with `light-dark()` rather than duplicated under a `prefers-color-scheme` block:

```css
:root {
  color-scheme: light dark;
  --accent: light-dark(#6d41a9, #bc98f9);
}
```

The used `color-scheme` picks a side, so the default follows the OS while a host can force a theme by setting `color-scheme` on the root. That is the whole mechanism behind Storybook's theme toolbar, and it gives native form controls matching chrome for free.

**Type steps are named for their role, not their size** — `--type-label`, `--type-caption`, `--type-note`, `--type-body`, `--type-title`, `--type-footnote`, `--type-brand`, plus `--track-label` and `--track-eyebrow`. A component asks for `text-label` and carries no pixel value.

**`base.css`** — token-driven styling for bare HTML, so unstyled markup already looks right.

**`tailwind.css`** — `@import 'tailwindcss'`, the `@theme inline` bridge, and the `@source` registrations. `@theme inline` is what keeps one variable per token: with `inline`, Tailwind writes the _value_ into the utility (`background: var(--bg)`) instead of emitting its own `--color-bg`, so `--color-bg: var(--bg)` is a bridge and not a circular definition.

## ⚠️ Three ways to break this silently

None of these produce an error. All three produce a page that is subtly or completely wrong with a green build.

**1. A new library package needs an `@source` line.** Tailwind never scans `node_modules`, and npm workspaces symlink `node_modules/@cinedex/frames` → `packages/frames`. Without registration, a class used _only_ inside a library generates no CSS at all:

```css
@source "../../frames/src";
@source "../../shots/src";
@source "../../scenes/src";
```

(`@source` paths resolve relative to the CSS file that declares them — verified empirically — which is why they can live here and serve every consumer instead of being duplicated into each app.)

**2. `base.css` must arrive `layer(base)`, after `tailwindcss`.** Tailwind v4 puts everything it emits into cascade layers, and **unlayered CSS outranks every layer** regardless of source order or specificity. Imported bare, `base.css`'s `h1 { font-size: 56px }` beats `text-title` on the same element — which is exactly what happened, and why every auth card rendered its heading at the landing page's size. Inside `layer(base)` it lands after preflight (so its typography still reaches bare markup) but below `layer(utilities)` (so a component's own classes win).

**3. A new `--text-*`/`--tracking-*` step needs registering in `cn()`.** `tailwind-merge` classifies an unrecognised `text-*` class as a _colour_, so `text-label text-accent` would be treated as two conflicting colours and one silently dropped. [`packages/frames/src/utils/cn.ts`](../frames/src/utils/cn.ts) extends the `font-size` and `tracking` class groups; that list and the `@theme inline` block must stay in step.

## Notes

- `tailwindcss` is a real dependency here, not a devDependency — `tailwind.css`'s `@import 'tailwindcss'` needs it at consumer build time.
- No `build`, `test` or `coverage` script, so the workspace-wide `--if-present` runs skip this package, and CI has no coverage step for it. There is no JavaScript to cover.
- `rounded-xs` is deliberately unmapped; components wanting a hairline radius use Tailwind's own 2px default.
