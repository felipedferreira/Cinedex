import type { TransitionVariant } from './rackFocus';

/**
 * Resolves which rack-focus variant an edge in the auth flow runs, from the
 * design's `MAP · 2A` table.
 *
 * `from` and `to` are full locations — pathname plus search — because two of the
 * design's edges are not pathname changes at all: the lockout is
 * `/login?state=locked`, and `ForgotPasswordScreen`'s request/sent step never
 * leaves `/forgot-password`. Callers key on whatever string identifies the
 * screen, so an in-screen step passes something like `/forgot-password#sent`.
 *
 * Precedence is deliberate and tested:
 *
 *  1. **Cold load wins outright** — there is no outgoing screen to animate, so
 *     no other variant can apply.
 *  2. **Lockout outranks the history override** — arriving at a lockout via the
 *     Back button is still a lockout. The direction is the less important fact.
 *  3. **History backward outranks the map** — the Back button must read as
 *     backward even on an edge the map calls forward.
 */

/** Destinations that always recede, whichever way you reached them. */
const RECEDING = ['/signed-out'];

/**
 * Destinations that take a beat before moving. `/account-ready` has no route in
 * the app yet — it is here because the design specifies it and the Storybook
 * rail exercises it. Adding the route later needs no change to this file.
 */
const HOLDING = ['/account-ready'];

function pathOf(location: string): string {
  const [path] = location.split('?');
  return path;
}

function isLockout(location: string): boolean {
  return location.includes('state=locked');
}

export function variantForEdge(
  from: string | null,
  to: string,
  wentBack: boolean,
): TransitionVariant {
  if (from === null) {
    return 'coldLoad';
  }

  if (isLockout(to)) {
    return 'lockout';
  }

  if (wentBack || RECEDING.includes(pathOf(to))) {
    return 'back';
  }

  if (HOLDING.includes(pathOf(to))) {
    return 'accountReady';
  }

  return 'forward';
}
