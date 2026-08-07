import { createFileRoute } from '@tanstack/react-router';
import { ResetPasswordScreen } from '@cinedex/solution';

interface ResetPasswordSearch {
  email?: string;
  token?: string;
}

export const Route = createFileRoute('/reset-password')({
  validateSearch: (search: Record<string, unknown>): ResetPasswordSearch => ({
    email: typeof search.email === 'string' ? search.email : undefined,
    token: typeof search.token === 'string' ? search.token : undefined,
  }),
  component: RouteComponent,
});

function RouteComponent() {
  const { email } = Route.useSearch();
  return <ResetPasswordScreen email={email} />;
}
