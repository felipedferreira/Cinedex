# @cinedex/compounds

The templates: **named layouts assembled from [`@cinedex/atoms`](../atoms/README.md), with no brand in them.** The middle of Cinedex's three component tiers.

Part of the [`frontend/` workspace](../../README.md). Its stories live in [`@cinedex/storybook`](../../apps/storybook/README.md).

## Commands (from `frontend/`)

```bash
npm run test -w @cinedex/compounds       # watch mode
npm run coverage -w @cinedex/compounds
```

## What's in it

`AuthLayout` · `AuthCard` · `PasswordField` · `PasswordStrengthMeter` · `PasswordChecklist` · `StatPair` · `InlineActionRow` · `strengthFromRequirements`

## Where the boundary falls

**Down to atoms** — if it does one job and has no internal arrangement, it is an atom. `PasswordInput` (an input plus a reveal toggle) is an atom; `PasswordField` (label + that input + error + strength) is a compound.

**Up to solution** — if it names Cinedex, it does not belong here. `AuthCard` takes `brand` as a `ReactNode`:

```tsx
<AuthCard brand={<Brand />} eyebrow="CIN · Auth" kicker="Session · Sign in" title="Sign in">
```

It knows _where_ a brand goes; `@cinedex/solution`'s `Brand` knows _which_. `AuthCard.test.tsx` asserts it renders an injected brand and no Cinedex text — keep it that way. The `Compounds/AuthCard` stories show the same card unbranded and wearing a made-up "Acme" mark, which is the boundary made visible.

## Conventions

Same as [`@cinedex/atoms`](../atoms/README.md): Tailwind only through `@cinedex/theme` tokens, `cn()` for class composition, named type steps rather than pixel values, one folder per component, barrel-only public surface, behaviour-and-accessibility tests.

Import from `@cinedex/atoms`, never by relative path into `../atoms/src`.

## Notes

- `strengthFromRequirements` ships here rather than in atoms — it is the pure helper the meter and checklist are driven by, and has no meaning without them.
- `PasswordStrengthMeter` wraps atoms' `ProgressBar` (Radix `Progress`), so it now exposes `role="progressbar"` and `aria-value*`. The hand-rolled bar it replaced announced nothing at all.
- `InlineActionRow` exists because three screens inlined the same six utilities for their "No account? · Create one" footer.
- Source-consumed: `exports` point at `src/`, no build step, no `dist/`. `npm run build` is `tsc -b`.
