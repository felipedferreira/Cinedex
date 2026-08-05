# @cinedex/ui

Shared component library for Cinedex, plus its Storybook. Private and **source-consumed**: `exports` in `package.json` point straight at `src/`, so there is no build step, no `dist/`, and no build ordering for consumers to worry about. `npm run build` here is `tsc -b` — a typecheck, nothing more.

## Commands (from `frontend/`, the workspace root)

```bash
npm run storybook              # http://localhost:6006
npm run build-storybook        # static output to packages/ui/storybook-static/
npm run test -w @cinedex/ui    # watch mode
npm run coverage -w @cinedex/ui
```

Lint and format run once from the workspace root and cover this package.

## Layout

```
src/
├── index.ts              # barrel — every public export
├── styles/tokens.css     # design tokens (exported as @cinedex/ui/tokens.css)
├── styles/base.css       # base element styling (@cinedex/ui/base.css)
├── utils/cx.ts           # tiny className joiner; no clsx dependency
└── <Component>/          # Component.tsx + .module.css + .test.tsx + .stories.tsx
```

## Conventions

- **One folder per component**, holding the component, its CSS Module, its test and its stories. Export it from `src/index.ts`.
- **Styling is CSS Modules only**, and component CSS resolves colour/spacing/radii **through the tokens and nothing else** — never a hard-coded hex. Native CSS nesting is used throughout.
- **Theming goes through `light-dark()`.** Tokens declare both values at once and the used `color-scheme` picks one, so `color-scheme: light dark` follows the OS while setting `dark`/`light` on the root forces a theme. That is the hook Storybook's theme toolbar uses — there is no theme class and no duplicated dark block.
- **React 19 ref-as-prop**, not `forwardRef`. `ComponentProps<'button'>` already includes `ref`.
- Components spread unknown props onto the underlying element and merge a caller's `className` with their own.
- Tests assert behaviour and accessibility (roles, label association, `aria-*`). Where a test must assert a CSS Module class, match the readable prefix — classes are hashed as `_<name>_<hash>`.

## Notes

- `.storybook/main.ts` sets `staticDirs` to the app's `public/` so the `/icons.svg#id` sprite resolves in stories.
- `.storybook/*` and `vite.config.ts` are covered by `tsconfig.node.json`, which needs `vite/client` in `types` for the side-effect CSS imports in `preview.tsx`.
- Storybook auto-loads this package's `vite.config.ts`, which is why the React Compiler plugins live there — library components compile exactly the way app components do.
