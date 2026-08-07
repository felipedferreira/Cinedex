# @cinedex/solution

Cinedex's own screens, assembled from `@cinedex/compounds`. The top of the three component tiers, and the only one that knows the product exists.

Private and **source-consumed** like the other two. Depends on `@cinedex/compounds` and `@cinedex/atoms`.

One of four packages in the `frontend/` npm workspace — see [`../../CLAUDE.md`](../../CLAUDE.md).

## Commands (from `frontend/`, the workspace root)

```bash
npm run test -w @cinedex/solution    # watch mode
npm run coverage -w @cinedex/solution
```

## The two hard rules

**1. Presentational only. No router import, no `fetch`.**

The screens _do_ know Cinedex's route paths — `/login`, `/register`, `/forgot-password` are Cinedex facts and belong here. What they do not know is how to navigate. Only the link component is injected:

```tsx
// cinadex-app/src/routes/__root.tsx
<SolutionProvider linkComponent={RouterLink}>
```

`SolutionLink` is the single component that reads `LinkContext`; everything else goes through it or through `AuthLink`. With no provider the default is a plain `<a>`, which is what Storybook and the tests get — **a full screen renders with no router and no mock.** That is the whole reason these live in a package rather than in the app.

Submit handlers arrive as optional props (`onSubmit`, `onResend`, …) defaulting to no-ops. Wiring them to `/movies-svc/auth/*` is the app's job — "Lane E: Frontend Runtime" in the auth execution plan.

**2. This is the only tier allowed to draw the brand.** `Brand` is the "C" mark and the wordmark; `CinedexAuthCard` (internal, not exported) pre-fills `AuthCard`'s `brand` slot so no screen repeats it. Swap `Brand` and every screen rebrands.

## Layout

```
src/
├── index.ts
├── Brand/Brand.tsx              # the mark + wordmark — a fragment, since AuthCard's row is a flex parent
├── link/
│   ├── linkTypes.ts             # SolutionLinkProps / SolutionLinkComponent
│   ├── AnchorLink.tsx           # the default: maps `to` → `href`
│   ├── linkContext.ts           # LinkContext + useLinkComponent
│   ├── SolutionLink.tsx         # THE only reader of LinkContext
│   ├── SolutionProvider.tsx
│   └── AuthLink.tsx             # AuthLink (navigates) + AuthActionLink (a real <button>)
└── screens/                     # one file per screen + CinedexAuthCard + formatCountdown
```

## Notes

- **`HomeScreen` is the app's index** — a directory of every screen, and the only way to reach three of them by clicking. The two-factor step, the signed-out panel and the locked-out sign-in all need backend support that does not exist, so nothing in the app links to them. Adding a screen means adding a row to its `SCREENS` list.
- **`to` is pathname-only; query state goes in `search`.** Router link components match `to` against their own route table, so `to="/login?state=locked"` would not resolve — `SolutionLinkProps` carries `search?: Record<string, string>` instead, which `AnchorLink` serialises back onto the href and a router link passes to its own search prop. The locked-out sign-in is the one entry that needs it.
- **`SolutionLink.tsx` carries a file-level `eslint-disable react-hooks/static-components`.** A component read from context is stable by construction — `SolutionProvider`'s prop and the `AnchorLink` default are both module-level — but the rule cannot tell a constant context value from one built inline. The file exists to hold that one exemption instead of repeating it at every call site. Do not add a second reader of `LinkContext`.
- **`TwoFactorScreen` and `SignedOutScreen` are presentational on purpose** — the backend has no MFA and no session-listing/revoke-all endpoint (`docs/auth-security-model.md`, "Known gaps"). `SignInScreen`'s `locked` state is likewise unreachable through normal use; the app exposes it at `/login?state=locked`.
- Screen tests are plain `render()` — no memory router, no `renderAuthScreen` helper. The app keeps `login-routing.test.tsx`, which mounts the real route tree and is what verifies the paths these screens hardcode are real routes.
