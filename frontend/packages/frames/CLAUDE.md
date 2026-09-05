# @cinedex/frames

The primitives: Radix-backed, Tailwind-styled, one job each. The bottom of the three component tiers — `@cinedex/shots` assembles these, `@cinedex/scenes` assembles those.

Private and **source-consumed**: `exports` in `package.json` point straight at `src/`, so there is no build step, no `dist/`, and no build ordering for consumers to worry about. `npm run build` here is `tsc -b` — a typecheck, nothing more.

**Storybook is not in this package.** It is its own app at [`apps/storybook`](../../apps/storybook/CLAUDE.md), which depends on all three tiers and owns the stories.

## Commands (from `frontend/`, the workspace root)

```bash
npm run test -w @cinedex/frames    # watch mode
npm run coverage -w @cinedex/frames
```

Lint and format run once from the workspace root and cover this package.

## Layout

```
src/
├── index.ts                # barrel — every public export
├── utils/cn.ts             # clsx + tailwind-merge, with the theme's custom class groups
├── Field/fieldContext.ts   # the id/aria wiring Input and PasswordInput pick up
└── <Component>/            # Component.tsx + <component>Variants.ts + .test.tsx
```

## Conventions

- **One folder per component**, holding the component, its cva variant map and its test. Export it from `src/index.ts` — that barrel is the only surface consumers and stories can reach.
- **Styling is Tailwind only**, resolved through `@cinedex/theme`'s tokens — never a hard-coded hex, never a raw pixel value where a type step exists (`text-label`, not `text-[10px]`). There are no CSS Modules and no `.css` files in this package.
- **Variants are cva, in their own file.** `react-refresh/only-export-components` fires on a module exporting both a component and a non-component, so `buttonVariants` lives in `Button/buttonVariants.ts`, not beside `Button`. Export the map as well as the component — `buttonVariants({ variant: 'outline' })` is how a caller styles a non-button.
- **Always compose classes with `cn()`, never string concatenation.** That is what makes a caller's `className` reliably beat the component's own; `cn('rounded-md', 'rounded-lg')` is `rounded-lg`, where `+ ' '` would leave both and let stylesheet order decide.
- **Radix for anything with real interaction semantics** — `Checkbox`, `Label`, `Progress`, `Separator`, `Slot`, `VisuallyHidden`. **Stable primitives only**: `OtpInput` and `PasswordInput` are hand-rolled because Radix's equivalents are still `unstable_`-prefixed previews.
- **React 19 ref-as-prop**, not `forwardRef`. `ComponentProps<'button'>` already includes `ref`.
- Components spread unknown props onto the underlying element and merge a caller's `className` with their own.
- Tests assert behaviour and accessibility (roles, label association, `aria-*`), not class strings — except where the class _is_ the behaviour under test (`cn()` conflict resolution, the invalid-input variant).

## Notes

- **`Field` hands its generated id to whatever control sits inside it**, through `FieldContext`. `Input` and `PasswordInput` call `useFieldControl()` and pick up `id`, `aria-invalid` and `aria-describedby`, so the three can never disagree. Both still render standalone outside a `Field`.
- **`Button` covers three looks and two sizes in one component** — `primary` (the landing page's tinted style) and `solid`/`outline` (the auth flow's ink-fill CTA), at `md`/`block`. `size="block"` sets `display: block`, which `cn()` resolves against the base `inline-flex`. A `ghost` variant and an `sm` size were cut once it was clear no screen reached for either — both existed only in the Storybook grid that demoed them. Add one back with its first real call site, not ahead of one.
- **`Button`'s `asChild`** (Radix `Slot`) renders the caller's element with the button's styling — how a router `<Link>` becomes a CTA without an `<a>` inside a `<button>`.
- **`Checkbox` is a `<button role="checkbox">`, not an `<input>`.** Its accessible name comes from `aria-labelledby`, not the `<label>` alone: a `<label for>` does associate with a button, but a button's accessible name is computed from its contents first, and this one has none. The API is Radix's — `onCheckedChange`, not `onChange`.
- **jsdom has no `ResizeObserver`**, and Radix needs one via `useSize` whenever a control participates in a form (`Checkbox` mirrors its size onto the hidden input it submits with). `src/test/setup.ts` stubs it; every package with tests carries the same stub.
- `vite.config.ts` here is **Vitest config only**. It still carries the React Compiler Babel preset so components are tested the way they are built.
- A new component is not visible to Storybook until it is exported from `src/index.ts` — the stories import from `@cinedex/frames`, not by relative path.
