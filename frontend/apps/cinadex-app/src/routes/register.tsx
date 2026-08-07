import { createFileRoute } from '@tanstack/react-router';
import { CreateAccountScreen } from '../features/auth/screens/CreateAccountScreen';

export const Route = createFileRoute('/register')({
  component: CreateAccountScreen,
});
