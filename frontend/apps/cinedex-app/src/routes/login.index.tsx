import { createFileRoute } from '@tanstack/react-router';
import { SignInScreen } from '@cinedex/solution';

export const Route = createFileRoute('/login/')({
  component: RouteComponent,
});

function RouteComponent() {
  const { state } = Route.useSearch();
  return <SignInScreen locked={state === 'locked'} />;
}
