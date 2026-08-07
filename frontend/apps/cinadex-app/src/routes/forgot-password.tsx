import { createFileRoute } from '@tanstack/react-router';
import { ForgotPasswordScreen } from '../features/auth/screens/ForgotPasswordScreen';

export const Route = createFileRoute('/forgot-password')({
  component: ForgotPasswordScreen,
});
