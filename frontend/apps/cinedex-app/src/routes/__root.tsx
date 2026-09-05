import type { FC } from 'react';
import { createRootRoute, Link, Outlet } from '@tanstack/react-router';
import { SceneProvider, type SceneLinkProps } from '@cinedex/scenes';
import { Toaster } from 'sonner';

/**
 * Adapts `@cinedex/scenes`'s router-agnostic link contract to TanStack Router.
 *
 * This component is the entire coupling between the screen library and the
 * router, and the cast is where their two type systems meet: `@cinedex/scenes`
 * deals in plain path strings so it can stay router-free and storyable, while
 * TanStack narrows `to` to the union of generated route paths. The paths the
 * screens use are real routes — `login-routing.test.tsx` is what keeps that
 * honest.
 */
const RouterLink: FC<SceneLinkProps> = ({ to, search, ...rest }) => {
  return <Link to={to as never} search={search as never} {...rest} />;
};

export const Route = createRootRoute({
  component: () => (
    <SceneProvider linkComponent={RouterLink}>
      <Outlet />
      <Toaster />
    </SceneProvider>
  ),
});
