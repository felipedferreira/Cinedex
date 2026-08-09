import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import {
  createMemoryHistory,
  createRouter,
  RouterProvider,
} from '@tanstack/react-router';
import { routeTree } from './routeTree.gen';

/** Mounts the real route tree at `path`, so these assertions exercise the
 *  actual parent/child wiring rather than a component in isolation. */
function renderAt(path: string) {
  const router = createRouter({
    routeTree,
    history: createMemoryHistory({ initialEntries: [path] }),
  });

  render(<RouterProvider router={router} />);
}

describe('login routing', () => {
  it('renders the sign-in form at /login', async () => {
    renderAt('/login');

    expect(
      await screen.findByRole('heading', { name: 'Sign in' }),
    ).toBeInTheDocument();
  });

  // Regression: `login.tsx` is the parent route of `login.verify.tsx`. While it
  // rendered SignInScreen directly instead of an <Outlet />, /login/verify
  // silently showed the sign-in form instead of the two-factor screen.
  it('renders the two-factor screen at /login/verify', async () => {
    renderAt('/login/verify');

    expect(
      await screen.findByRole('heading', { name: 'Two-factor code' }),
    ).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Sign in' })).toBeNull();
  });
});
