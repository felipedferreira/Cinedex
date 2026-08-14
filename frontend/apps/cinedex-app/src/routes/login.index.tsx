import { createFileRoute } from '@tanstack/react-router';
import { SignInScreen } from '@cinedex/solution';
import { toast } from 'sonner';

export const Route = createFileRoute('/login/')({
  component: RouteComponent,
});

function RouteComponent() {
  const { state } = Route.useSearch();
  return (
    <SignInScreen
      locked={state === 'locked'}
      onSubmit={() => {
        window.setTimeout(() => {
          toast.success('Signed in.');
        }, 2_000);
      }}
    />
  );
}
