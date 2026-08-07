import { createFileRoute } from '@tanstack/react-router';
import { TwoFactorScreen } from '../features/auth/screens/TwoFactorScreen';

export const Route = createFileRoute('/login/verify')({
  component: TwoFactorScreen,
});
