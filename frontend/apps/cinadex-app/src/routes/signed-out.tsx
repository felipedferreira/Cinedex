import { createFileRoute } from '@tanstack/react-router';
import { SignedOutScreen } from '@cinedex/solution';

export const Route = createFileRoute('/signed-out')({
  component: SignedOutScreen,
});
