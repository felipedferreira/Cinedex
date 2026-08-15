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
// cinedex-app/src/routes/__root.tsx
<SolutionProvider linkComponent={RouterLink}>
```

`SolutionLink` is the single component that reads `LinkContext`; everything else goes through it or through `AuthLink`. With no provider the default is a plain `<a>`, which is what Storybook and the tests get — **a full screen renders with no router and no mock.** That is the whole reason these live in a package rather than in the app.

Submit handlers arrive as optional props (`onSubmit`, `onResend`, …) defaulting to no-ops. Wiring them to `/movies-svc/auth/*` is the app's job — "Lane E: Frontend Runtime" in the auth execution plan.

**2. This is the only tier allowed to draw the brand.** `Brand` is the mark (a camera iris forming a "C", inline SVG built from `Brand/mark.ts`'s geometry) plus the text wordmark; `CinedexAuthCard` (internal, not exported) pre-fills `AuthCard`'s `brand` slot so no screen repeats it. Swap `Brand` and every screen rebrands.

Two more exports, `BrandApertureAnimation` and `BrandFocusRingsAnimation`, play the mark's two 1.2s intro sequences once on mount and settle into the exact same static state `Brand` renders — built from the same `MarkBody`, so all three stay pixel-identical at rest. `HomeScreen` is the only current consumer of either, passing `brand={<BrandApertureAnimation />}` to `CinedexAuthCard` (which otherwise defaults to plain `Brand`) since it's the app's one landing moment; `BrandFocusRingsAnimation` ships fully built and exported as the alternate sequence, reviewable in Storybook's `Solution/Brand` stories. The mark's colour is intentionally independent of `@cinedex/theme`'s `--accent` — it's an achromatic material study (metal on dark, flat ink on light, via `light-dark()` gradient stops), not a brand-colour study, so a theme rebrand and a mark rebrand are two separate changes.

## Layout

```
src/
├── index.ts
├── Brand/
│   ├── Brand.tsx                       # the static mark + wordmark — a fragment, since AuthCard's row is a flex parent
│   ├── BrandApertureAnimation.tsx      # Brand, plus the "lens aperture" intro
│   ├── BrandFocusRingsAnimation.tsx    # Brand, plus the "focus rings" intro
│   ├── MarkBody.tsx                    # the shared <svg> all three render — single source of truth for the geometry
│   ├── MarkDefs.tsx                    # the shared gradients/clip path, keyed per instance via useId()
│   ├── Wordmark.tsx                    # the text half of the lockup
│   ├── mark.ts                         # path data and colour constants MarkBody/MarkDefs build from
│   ├── brandSize.ts                    # the XS–XL scale and its resolver
│   ├── timelines.ts                    # both sequences as GSAP timelines, built paused
│   └── useMarkTimeline.ts              # plays one on mount, honours prefers-reduced-motion
├── link/
│   ├── linkTypes.ts             # SolutionLinkProps / SolutionLinkComponent
│   ├── AnchorLink.tsx           # the default: maps `to` → `href`
│   ├── linkContext.ts           # LinkContext + useLinkComponent
│   ├── SolutionLink.tsx         # THE only reader of LinkContext
│   ├── SolutionProvider.tsx
│   └── AuthLink.tsx             # AuthLink (navigates) + AuthActionLink (a real <button>)
├── transitions/
│   ├── cubicBezier.ts           # solves the design's exact CSS beziers into a GSAP ease
│   ├── rackFocus.ts             # the five variants + buildRackFocusTimeline, built paused
│   ├── authEdges.ts             # (from, to, wentBack) → variant — the flow's edge map
│   ├── captureContext.ts        # CaptureContext + useCaptureOutgoing
│   └── ScreenTransition.tsx     # the clone host
└── screens/                     # one file per screen + CinedexAuthCard + formatCountdown
```

## Notes

- **`HomeScreen` is the app's index** — a directory of every screen, and the only way to reach three of them by clicking. The two-factor step, the signed-out panel and the locked-out sign-in all need backend support that does not exist, so nothing in the app links to them. Adding a screen means adding a row to its `SCREENS` list.
- **`to` is pathname-only; query state goes in `search`.** Router link components match `to` against their own route table, so `to="/login?state=locked"` would not resolve — `SolutionLinkProps` carries `search?: Record<string, string>` instead, which `AnchorLink` serialises back onto the href and a router link passes to its own search prop. The locked-out sign-in is the one entry that needs it.
- **`SolutionLink.tsx` carries a file-level `eslint-disable react-hooks/static-components`.** A component read from context is stable by construction — `SolutionProvider`'s prop and the `AnchorLink` default are both module-level — but the rule cannot tell a constant context value from one built inline. The file exists to hold that one exemption instead of repeating it at every call site. Do not add a second reader of `LinkContext`.
- **`TwoFactorScreen` and `SignedOutScreen` are presentational on purpose** — the backend has no MFA and no session-listing/revoke-all endpoint (`docs/auth-security-model.md`, "Known gaps"). `SignInScreen`'s `locked` state is likewise unreachable through normal use; the app exposes it at `/login?state=locked`.
- Screen tests are plain `render()` — no memory router, no `renderAuthScreen` helper. The app keeps `login-routing.test.tsx`, which mounts the real route tree and is what verifies the paths these screens hardcode are real routes.
- **`Brand/` is the first inline-`<svg>`-in-JSX in this repo** — `atoms` and `compounds` have no icons yet and no `vite-plugin-svgr`. Nothing else needed adding for it: no build step, no new Vite plugin, consistent with every tier being source-consumed.
- **All animation in this package is GSAP, and every sequence is a pure builder returning a `paused` timeline.** `Brand/timelines.ts` and `transitions/rackFocus.ts` both follow it, with a thin hook or component owning only the React lifecycle around the timeline. The shape is not decorative: a paused timeline can be driven to any instant with `progress(p)` and flushes its writes synchronously, with no ticker and no rAF — which is the only reason a 1.2s logo intro and an 820ms screen transition are unit-testable at all. Keep new sequences in that shape; anything that animates inside a component is unassertable.
- **jsdom has no `matchMedia`, and `test/setup.ts`'s stub defaults `matches` to `true`** — so **every test in this package takes the reduced-motion path unless it says otherwise**. That is the right default (nothing waits on an animation it does not assert about) and a live trap: a full-motion test that forgets to override the stub passes for the wrong reason. `ScreenTransition.test.tsx` has a `useFullMotion()` helper for the cases that need the real thing. `apps/cinedex-app` carries the same stub for the same reason, since `__root.tsx` now renders `ScreenTransition`.
- **`transitions/` animates through CSS custom properties, never `style.filter` / `style.transform`.** jsdom's `CSSStyleDeclaration` implements only a subset of real properties and can silently drop `filter`, which would leave the screen correct and the tests asserting nothing. The panes read all three variables from `PANE_STYLE`; the timeline only moves the variables. Every tween is a `fromTo`, because jsdom returns an empty string for an unset custom property and a plain `to()` parses that as `0` and animates from the wrong place.
- **The outgoing screen is a DOM clone, captured imperatively.** A React subtree kept alive re-renders against the _new_ state — a router `Outlet` or a step-switching screen would cross-fade a screen with itself. And the clone has to be taken before React commits, so `useCaptureOutgoing` is called by the event that causes the change (`RouterLink`'s click, `ForgotPasswordScreen`'s submit), not by an effect. With no provider it is a no-op, so screens stay storyable — same shape as `useLinkComponent`. Hosts nest, capture at every level, and only the level whose key actually changed mounts its snapshot.
- **`progress()`-driven tests cannot catch a timeline that never plays.** That is not hypothetical: under `StrictMode` the transition effect was torn down and re-invoked, early-returned on the key it had already recorded, and left every screen frozen at opacity 0 behind an 11px blur — a blank app with a green suite. The effect is re-entrant now (its cleanup restores what it changed), and `ScreenTransition.test.tsx`'s `playback` block asserts the timeline actually settles, including under a double-invoked effect. Keep those tests.
