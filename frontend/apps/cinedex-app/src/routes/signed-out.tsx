import { createFileRoute } from '@tanstack/react-router';
import { SignedOutScreen } from '@cinedex/scenes';

export const Route = createFileRoute('/signed-out')({
  component: SignedOutScreen,
});
