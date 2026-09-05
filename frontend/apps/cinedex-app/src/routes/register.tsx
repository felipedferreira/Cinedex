import type { FC } from 'react';
import { createFileRoute } from '@tanstack/react-router';
import { CreateAccountScreen } from '@cinedex/scenes';
import { toast } from 'sonner';

const RouteComponent: FC = () => {
  return (
    <CreateAccountScreen
      onSubmit={() => {
        window.setTimeout(() => {
          toast.success('Account created.');
        }, 2_000);
      }}
    />
  );
};

export const Route = createFileRoute('/register')({
  component: RouteComponent,
});
