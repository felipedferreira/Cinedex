import { createFileRoute } from '@tanstack/react-router';
import { ForgotPasswordScreen } from '@cinedex/solution';

export const Route = createFileRoute('/forgot-password')({
  component: ForgotPasswordScreen,
});
