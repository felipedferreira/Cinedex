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

## Prop APIs

Three rules, because a template's whole value is its prop surface and this package has already lost that argument once.

- **A `ReactNode` slot is right only when the parent owns the arrangement AND the content cannot be `children`.** `Field`'s `labelExtra` cannot be `children` — `children` is the form control; `InlineActionRow`'s `action` cannot be — `children` is the prompt. Both are components whose entire job is a two-position row, which is what earns the second named hole. A component with **one** hole uses `children`. `AuthCard.brand` is the third case: it is a slot because it crosses a package boundary, not because of arrangement.
- **Type a node prop by what actually goes in it.** `ReactNode` when a caller genuinely needs inline markup; `string` otherwise. The worked pair is in this repo: `AuthCard`'s `description` is `string` because all seven callers pass text, while `apps/docs-site`'s `FeatureItem.description` is correctly `ReactNode` because its values really do carry `<Link>` and `<code>`. `ReactNode` is `any` wearing a hat — it buys a hole nobody fills and costs the compiler's help. Note this is about phrasing vs block content, not about `<p>`: both of those render inside a `<p>`.
- **A story may only demo a prop a screen already passes.** This is the one that actually bites. `AuthCard.kickerTone` was a four-member union and a class map reachable from an `argTypes` inline-radio and one story and **nothing else** — the story enumerated a complete matrix and the component grew to satisfy it. It is gone, and so are the three that carried the same fingerprint one tier down: `Button`'s `ghost` variant and `sm` size, and `Alert`'s `success` tone. Each was reachable only from an `argTypes` matrix and a story built to fill it. `scripts/check-speculative-props.mjs` now fails CI on the pattern.

`eslint.config.js`'s `no-restricted-syntax` block holds three ratchets for the neighbouring shapes — a `ComponentType`/`ElementType`-typed prop, a `render*` prop, and a `children` member on a `*Props` interface. All three match nothing today; they exist to keep it that way.

## Conventions

Same as [`@cinedex/atoms`](../atoms/CLAUDE.md): Tailwind only through `@cinedex/theme` tokens, `cn()` for class composition, named type steps rather than pixel values, one folder per component, barrel-only public surface, behaviour-and-a11y tests.

Import from `@cinedex/atoms`, never by relative path into `../atoms/src`.

## Notes

- `strengthFromRequirements` ships here rather than in atoms — it is the pure helper `PasswordStrengthMeter` and `PasswordChecklist` are driven by, and has no meaning without them.
- `PasswordStrengthMeter` wraps atoms' `ProgressBar`, which is Radix `Progress` — so the meter now has `role="progressbar"` and `aria-value*`, which the hand-rolled bar it replaced did not.
- `InlineActionRow` exists because three screens inlined the same six utilities for their "No account? · Create one" footer.
