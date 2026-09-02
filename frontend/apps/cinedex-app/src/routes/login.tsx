import type { FC } from 'react';
import { createFileRoute, Outlet } from '@tanstack/react-router';

interface LoginSearch {
  /** `locked` is a review-only toggle for the lockout state — nothing in this
   *  pass can trigger it for real, since the backend has no way to report a
   *  lockout distinctly from any other failed sign-in. Wire it from the real
   *  API error once that lands. */
  state?: 'locked';
}

const RouteComponent: FC = () => {
  return <Outlet />;
};

/** Layout only. The sign-in form lives in `login.index.tsx` so that
 *  `/login/verify` has somewhere to render — a parent that renders a screen
 *  instead of an `<Outlet />` swallows its children. */
export const Route = createFileRoute('/login')({
  validateSearch: (search: Record<string, unknown>): LoginSearch => ({
    state: search.state === 'locked' ? 'locked' : undefined,
  }),
  component: RouteComponent,
});
