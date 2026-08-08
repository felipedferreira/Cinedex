import { useEffect, useState } from 'react';

/**
 * `true` once `delayMs` has elapsed since mount — used to fade the wordmark
 * in partway through a logo animation without wiring it into the imperative
 * `requestAnimationFrame` loop `useMarkAnimation` runs for the mark itself.
 * A plain CSS opacity transition is precise enough for text; the exact frame
 * sync that loop gives the mark's own attributes isn't worth the coupling.
 *
 * Reveals immediately for a reduced-motion viewer (via the lazy initializer,
 * so that case never needs an effect + `setState` at all — this app has no
 * server render, so reading `matchMedia` before the first paint is safe).
 */
export function useDelayedReveal(delayMs: number): boolean {
  const [revealed, setRevealed] = useState(
    () => window.matchMedia('(prefers-reduced-motion: reduce)').matches,
  );

  useEffect(() => {
    if (revealed) {
      return;
    }
    const id = window.setTimeout(() => {
      setRevealed(true);
    }, delayMs);
    return () => {
      window.clearTimeout(id);
    };
  }, [delayMs, revealed]);

  return revealed;
}
