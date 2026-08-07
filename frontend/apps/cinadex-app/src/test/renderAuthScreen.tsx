import type { ReactElement } from 'react';
import { render } from '@testing-library/react';
import {
  createMemoryHistory,
  createRoute,
  createRootRoute,
  createRouter,
  RouterProvider,
} from '@tanstack/react-router';

const AUTH_PATHS = [
  '/login',
  '/login/verify',
  '/register',
  '/forgot-password',
  '/reset-password',
  '/signed-out',
];

/**
 * Renders a screen component standalone, with just enough router context for
 * its `Link`/`AuthLink` targets to resolve — the real route files (which own
 * search-param parsing) aren't involved.
 */
export async function renderAuthScreen(screen: ReactElement, path: string) {
  const rootRoute = createRootRoute();
  const screenRoute = createRoute({
    getParentRoute: () => rootRoute,
    path,
    component: () => screen,
  });
  const otherRoutes = AUTH_PATHS.filter((candidate) => candidate !== path).map(
    (candidate) =>
      createRoute({
        getParentRoute: () => rootRoute,
        path: candidate,
        component: () => null,
      }),
  );

  const router = createRouter({
    routeTree: rootRoute.addChildren([screenRoute, ...otherRoutes]),
    history: createMemoryHistory({ initialEntries: [path] }),
  });

  // The router matches the initial location asynchronously; loading it first
  // means the screen is already mounted by the time `render` returns, so
  // callers can use synchronous `getBy*` queries instead of `findBy*`.
  await router.load();

  return render(<RouterProvider router={router} />);
}
