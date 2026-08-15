import { useEffect, useState } from 'react';
import {
  createRootRoute,
  Link,
  Outlet,
  useRouterState,
} from '@tanstack/react-router';
import {
  ScreenTransition,
  SolutionProvider,
  useCaptureOutgoing,
  variantForEdge,
  type SolutionLinkProps,
} from '@cinedex/solution';
import { Toaster } from 'sonner';

/**
 * Adapts `@cinedex/solution`'s router-agnostic link contract to TanStack Router.
 *
 * This function is the entire coupling between the screen library and the
 * router, and the cast is where their two type systems meet: `@cinedex/solution`
 * deals in plain path strings so it can stay router-free and storyable, while
 * TanStack narrows `to` to the union of generated route paths. The paths the
 * screens use are real routes — `login-routing.test.tsx` is what keeps that
 * honest.
 *
 * It is also where the auth transition freezes the outgoing screen. Every in-app
 * navigation goes through this one component, and a click handler runs before
 * React re-renders — which is the only moment the outgoing DOM still exists to
 * be cloned. A capture that turns out not to precede a screen change costs
 * nothing; `ScreenTransition` drops it.
 */
function RouterLink({ to, search, ...rest }: SolutionLinkProps) {
  const capture = useCaptureOutgoing();

  return (
    <Link
      to={to as never}
      search={search as never}
      onClick={() => {
        capture();
      }}
      {...rest}
    />
  );
}

/**
 * Plays the rack-focus transition on every routed screen change.
 *
 * The key is pathname **plus** search string, because the lockout state is
 * `/login?state=locked` — a search-param change that renders a materially
 * different screen from the same route.
 *
 * Every `popstate` is treated as backward. That is right for the Back button and
 * wrong for Forward, which is a deliberate trade: a true history delta needs
 * TanStack's internal history index, which is not public API, and forward-button
 * use inside an auth flow is vanishingly rare.
 */
function RouterScreenTransition() {
  const location = useRouterState({
    select: (state) => state.location.pathname + state.location.searchStr,
  });

  const [pendingBack, setPendingBack] = useState(false);

  /**
   * The edge is derived during render rather than tracked in a ref, because the
   * variant has to be right on the very render that changes the key:
   * `ScreenTransition` reads it in a layout effect, and a parent's layout effect
   * runs *after* its child's, so there is no later pass to correct it in.
   *
   * Adjusting state during render is React's own sanctioned shape for this. A
   * ref would be the obvious alternative and is worse on both counts — writing
   * one during render is unsafe under the React Compiler, and consuming the
   * back-flag in an effect is what `react-hooks/set-state-in-effect` exists to
   * stop.
   */
  const [edge, setEdge] = useState<{
    from: string | null;
    to: string;
    wentBack: boolean;
  }>({ from: null, to: location, wentBack: false });

  if (edge.to !== location) {
    setEdge({ from: edge.to, to: location, wentBack: pendingBack });
    if (pendingBack) {
      setPendingBack(false);
    }
  }

  useEffect(() => {
    // `popstate` fires before the router commits the new location, so this lands
    // in time for the render that reads it.
    const onPopState = () => {
      setPendingBack(true);
    };

    window.addEventListener('popstate', onPopState);
    return () => {
      window.removeEventListener('popstate', onPopState);
    };
  }, []);

  return (
    <ScreenTransition
      transitionKey={location}
      variant={variantForEdge(edge.from, edge.to, edge.wentBack)}
    >
      <Outlet />
    </ScreenTransition>
  );
}

export const Route = createRootRoute({
  component: () => (
    <SolutionProvider linkComponent={RouterLink}>
      <RouterScreenTransition />
      <Toaster />
    </SolutionProvider>
  ),
});
