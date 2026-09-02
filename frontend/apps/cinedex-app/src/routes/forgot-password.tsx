import type { FC } from 'react';
import { createFileRoute } from '@tanstack/react-router';
import { ForgotPasswordScreen } from '@cinedex/solution';
import { toast } from 'sonner';

const RouteComponent: FC = () => {
  return (
    <ForgotPasswordScreen
      onSubmit={() => {
        window.setTimeout(() => {
          toast.success('Password reset requested.');
        }, 2_000);
      }}
    />
  );
};

export const Route = createFileRoute('/forgot-password')({
  component: RouteComponent,
});
