# @cinedex/components

Shared component library for Cinedex. Private and **source-consumed**: `exports` in `package.json` point straight at `src/`, so there is no build step, no `dist/`, and no build ordering for consumers to worry about. `npm run build` here is `tsc -b` — a typecheck, nothing more.

**Storybook is not in this package.** It is its own app at [`apps/storybook`](../../apps/storybook/CLAUDE.md), which depends on `@cinedex/components` and owns the stories. This package therefore has no Storybook dependency at all — its only job is components.

## Commands (from `frontend/`, the workspace root)

```bash
npm run test -w @cinedex/components    # watch mode
npm run coverage -w @cinedex/components
```

Lint and format run once from the workspace root and cover this package.

## Layout

```
src/
├── index.ts              # barrel — every public export
├── styles/tokens.css     # design tokens (exported as @cinedex/components/tokens.css)
├── styles/base.css       # base element styling (@cinedex/components/base.css)
├── utils/cx.ts           # tiny className joiner; no clsx dependency
└── <Component>/          # Component.tsx + .module.css + .test.tsx
```

## Conventions

- **One folder per component**, holding the component, its CSS Module and its test. Export it from `src/index.ts` — that barrel is the only surface consumers and stories can reach, so anything not exported there is effectively private. Its story goes in `apps/storybook`.
- **Styling is CSS Modules only**, and component CSS resolves colour/spacing/radii **through the tokens and nothing else** — never a hard-coded hex. Native CSS nesting is used throughout.
- **Theming goes through `light-dark()`.** Tokens declare both values at once and the used `color-scheme` picks one, so `color-scheme: light dark` follows the OS while setting `dark`/`light` on the root forces a theme. That is the hook the Storybook app's theme toolbar uses — there is no theme class and no duplicated dark block.
- **React 19 ref-as-prop**, not `forwardRef`. `ComponentProps<'button'>` already includes `ref`.
- Components spread unknown props onto the underlying element and merge a caller's `className` with their own.
- Tests assert behaviour and accessibility (roles, label association, `aria-*`). Where a test must assert a CSS Module class, match the readable prefix — classes are hashed as `_<name>_<hash>`.

## Notes

- `vite.config.ts` here is **Vitest config only**. It still carries the React Compiler Babel preset so components are tested the way they are built; `apps/storybook` has its own copy for the same reason.
- A new component is not visible to Storybook until it is exported from `src/index.ts` — the stories import from `@cinedex/components`, not by relative path.
