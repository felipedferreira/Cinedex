import { createRootRoute, Link, Outlet } from '@tanstack/react-router';
import { SolutionProvider, type SolutionLinkProps } from '@cinedex/solution';

/**
 * Adapts `@cinedex/solution`'s router-agnostic link contract to TanStack Router.
 *
 * This function is the entire coupling between the screen library and the
 * router, and the cast is where their two type systems meet: `@cinedex/solution`
 * deals in plain path strings so it can stay router-free and storyable, while
 * TanStack narrows `to` to the union of generated route paths. The paths the
 * screens use are real routes — `login-routing.test.tsx` is what keeps that
 * honest.
 */
function RouterLink({ to, search, ...rest }: SolutionLinkProps) {
  return <Link to={to as never} search={search as never} {...rest} />;
}

export const Route = createRootRoute({
  component: () => (
    <SolutionProvider linkComponent={RouterLink}>
      <Outlet />
    </SolutionProvider>
  ),
});
