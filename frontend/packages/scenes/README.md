# @cinedex/scenes

Cinedex's own screens, assembled from [`@cinedex/shots`](../shots/README.md). The top of the three component tiers — the dramatic content of _this_ film — and **the only one that knows the product exists**.

Part of the [`frontend/` workspace](../../README.md). Its stories live in [`@cinedex/storybook`](../../apps/storybook/README.md).

## Commands (from `frontend/`)

```bash
npm run test -w @cinedex/scenes       # watch mode
npm run coverage -w @cinedex/scenes
```

## What's in it

`HomeScreen`, the app's index, plus seven auth states across six screens — `SignInScreen` (with its locked-out variant), `TwoFactorScreen`, `CreateAccountScreen`, `ForgotPasswordScreen`, `ResetPasswordScreen`, `SignedOutScreen` — and `Brand`, `SceneProvider` and the link components.

**`HomeScreen` earns its place**: three of those states cannot be reached by clicking through the app, because the backend support that would trigger them does not exist. Without an index, reviewing the two-factor step, the signed-out panel or the locked-out sign-in means typing URLs.

## Presentational only: no router, no `fetch`

The screens _do_ know Cinedex's route paths — `/login`, `/register`, `/forgot-password` are Cinedex facts and belong here. What they do not know is how to navigate. Only the link component is injected:

```tsx
// cinedex-app/src/routes/__root.tsx
<SceneProvider linkComponent={RouterLink}>
  <Outlet />
</SceneProvider>
```

With no provider, links fall back to plain anchors — which is what Storybook and the tests get. **A full sign-in screen renders with no router and no mock.** That is the whole reason these live in a package rather than in the app; screen tests are plain `render()` calls.

`to` is pathname-only. Query state travels in `search`, because a router link matches `to` against its own route table and `to="/login?state=locked"` would not resolve:

```tsx
<SceneLink to="/login" search={{ state: 'locked' }}>
```

`AnchorLink` serialises that back onto the href; a router link forwards it to its own search prop.

Submit handlers arrive as optional props (`onSubmit`, `onResend`, …) defaulting to no-ops, so a story renders with zero wiring:

```tsx
<SignInScreen onSubmit={({ email, password, keepSignedIn }) => …} />
```

Wiring those to `/movies-svc/auth/*` is the app's job.

## The only tier that draws the brand

`Brand` is the "C" mark and the wordmark. `CinedexAuthCard` (internal, not exported) pre-fills `AuthCard`'s `brand` slot so no screen repeats it. Swap `Brand` and every screen rebrands.

## Notes

- **`SceneLink` is the single reader of `LinkContext`**, and carries a file-level `eslint-disable react-hooks/static-components`. A component read from context is stable by construction — `SceneProvider`'s prop and the `AnchorLink` default are both module-level — but the rule cannot tell a constant context value from one built inline. That file exists to hold the one exemption instead of repeating it at every call site. Don't add a second reader.
- **`TwoFactorScreen` and `SignedOutScreen` are presentational on purpose** — the backend has no MFA and no session-listing/revoke-all endpoint yet (see [`docs/auth-security-model.md`](../../../docs/auth-security-model.md), "Known gaps"). `SignInScreen`'s `locked` state is likewise unreachable through normal use; the app exposes it at `/login?state=locked` for review.
- The app keeps `login-routing.test.tsx`, which mounts the real route tree — that is what verifies the paths these screens hardcode are real routes.
- Source-consumed: `exports` point at `src/`, no build step, no `dist/`. `npm run build` is `tsc -b`.
