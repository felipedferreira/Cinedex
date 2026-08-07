# @cinedex/compounds

The templates: named layouts assembled from `@cinedex/atoms`. The middle of the three component tiers.

Private and **source-consumed**, exactly like `@cinedex/atoms` — `exports` point at `src/`, `npm run build` is a typecheck. Depends on `@cinedex/atoms` and nothing else.

One of four packages in the `frontend/` npm workspace — see [`../../CLAUDE.md`](../../CLAUDE.md).

## Commands (from `frontend/`, the workspace root)

```bash
npm run test -w @cinedex/compounds    # watch mode
npm run coverage -w @cinedex/compounds
```

## What belongs here

An assembly with a **named layout** and **no brand**: `AuthLayout`, `AuthCard`, `PasswordField`, `PasswordStrengthMeter`, `PasswordChecklist`, `StatPair`, `InlineActionRow`.

The test for the boundary in both directions:

- **Down to atoms** — if it does one job and has no internal arrangement, it is an atom. `PasswordInput` (an input plus a reveal toggle) is an atom; `PasswordField` (label + that input + error + strength) is a compound.
- **Up to solution** — if it names Cinedex, it does not belong here. `AuthCard` takes `brand` as a `ReactNode` rather than drawing the "C" mark and the wordmark; `@cinedex/solution`'s `Brand` supplies those. **This component knows where a brand goes, not which one.** Keep it that way — `AuthCard.test.tsx` asserts it renders an injected brand and no Cinedex text.

## Conventions

Same as [`@cinedex/atoms`](../atoms/CLAUDE.md): Tailwind only through `@cinedex/theme` tokens, `cn()` for class composition, named type steps rather than pixel values, one folder per component, barrel-only public surface, behaviour-and-a11y tests.

Import from `@cinedex/atoms`, never by relative path into `../atoms/src`.

## Notes

- `strengthFromRequirements` ships here rather than in atoms — it is the pure helper `PasswordStrengthMeter` and `PasswordChecklist` are driven by, and has no meaning without them.
- `PasswordStrengthMeter` wraps atoms' `ProgressBar`, which is Radix `Progress` — so the meter now has `role="progressbar"` and `aria-value*`, which the hand-rolled bar it replaced did not.
- `InlineActionRow` exists because three screens inlined the same six utilities for their "No account? · Create one" footer.
