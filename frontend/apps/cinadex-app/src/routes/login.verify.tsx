import { createFileRoute } from '@tanstack/react-router';
import { TwoFactorScreen } from '@cinedex/solution';

export const Route = createFileRoute('/login/verify')({
  component: TwoFactorScreen,
});
