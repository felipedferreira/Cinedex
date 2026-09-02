import type { FC } from 'react';
import { createFileRoute } from '@tanstack/react-router';
import { TwoFactorScreen } from '@cinedex/solution';
import { toast } from 'sonner';

const RouteComponent: FC = () => {
  return (
    <TwoFactorScreen
      codeLength={4}
      onSubmit={({ code }) => {
        window.setTimeout(() => {
          if (code === '1234') {
            toast.success('Verification successful.');
            return;
          }

          toast.error('Invalid verification code.');
        }, 2_000);
      }}
    />
  );
};

export const Route = createFileRoute('/login/verify')({
  component: RouteComponent,
});
