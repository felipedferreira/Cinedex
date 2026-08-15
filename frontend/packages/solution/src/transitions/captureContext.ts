import { createContext, useCallback, useContext } from 'react';

/**
 * The channel `ScreenTransition` uses to let whatever triggers a screen change
 * freeze the outgoing one first.
 *
 * This lives beside the component rather than inside it for the same reason
 * `link/linkContext.ts` does: a file that exports both a component and a hook
 * trips `react-refresh/only-export-components`, and splitting is the honest fix
 * rather than an inline suppression.
 */
export const CaptureContext = createContext<(() => void) | null>(null);

/**
 * Returns a function that freezes the current screen for the transition that is
 * about to start. Call it immediately before whatever changes the key — a router
 * navigation, a `setState` in a submit handler.
 *
 * The timing is the whole point. React commits DOM changes before layout effects
 * run, so by the time any effect could react to a key change the outgoing screen
 * is already gone; the event that *causes* the change is the last moment its DOM
 * still exists to be cloned.
 *
 * Outside a `ScreenTransition` this is a no-op, so screens using it still render
 * bare in Storybook and in tests with no provider — the same shape as
 * `useLinkComponent`.
 */
export function useCaptureOutgoing(): () => void {
  const capture = useContext(CaptureContext);

  return useCallback(() => {
    capture?.();
  }, [capture]);
}
