import { createFileRoute } from '@tanstack/react-router';
import { CreateAccountScreen } from '@cinedex/solution';
import { toast } from 'sonner';

export const Route = createFileRoute('/register')({
  component: RouteComponent,
});

function RouteComponent() {
  return (
    <CreateAccountScreen
      onSubmit={() => {
        window.setTimeout(() => {
          toast.success('Account created.');
        }, 2_000);
      }}
    />
  );
}
