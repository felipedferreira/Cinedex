import { createFileRoute } from '@tanstack/react-router';
import { SignInScreen } from '../features/auth/screens/SignInScreen';

interface LoginSearch {
  /** `locked` is a review-only toggle for the lockout state — nothing in this
   *  pass can trigger it for real, since the backend has no way to report a
   *  lockout distinctly from any other failed sign-in. Wire it from the real
   *  API error once that lands. */
  state?: 'locked';
}

export const Route = createFileRoute('/login')({
  validateSearch: (search: Record<string, unknown>): LoginSearch => ({
    state: search.state === 'locked' ? 'locked' : undefined,
  }),
  component: RouteComponent,
});

function RouteComponent() {
  const { state } = Route.useSearch();
  return <SignInScreen locked={state === 'locked'} />;
}
