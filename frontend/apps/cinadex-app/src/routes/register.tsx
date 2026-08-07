import { createFileRoute } from '@tanstack/react-router';
import { CreateAccountScreen } from '@cinedex/solution';

export const Route = createFileRoute('/register')({
  component: CreateAccountScreen,
});
