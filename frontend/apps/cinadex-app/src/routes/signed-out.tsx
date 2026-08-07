import { createFileRoute } from '@tanstack/react-router';
import { SignedOutScreen } from '../features/auth/screens/SignedOutScreen';

export const Route = createFileRoute('/signed-out')({
  component: SignedOutScreen,
});
