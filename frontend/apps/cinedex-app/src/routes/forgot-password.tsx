import { createFileRoute } from '@tanstack/react-router';
import { ForgotPasswordScreen } from '@cinedex/solution';
import { toast } from 'sonner';

export const Route = createFileRoute('/forgot-password')({
  component: RouteComponent,
});

function RouteComponent() {
  return (
    <ForgotPasswordScreen
      onSubmit={() => {
        window.setTimeout(() => {
          toast.success('Password reset requested.');
        }, 2_000);
      }}
    />
  );
}
