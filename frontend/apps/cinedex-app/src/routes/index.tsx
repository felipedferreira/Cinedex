import { createFileRoute } from '@tanstack/react-router';
import { HomeScreen } from '@cinedex/scenes';

export const Route = createFileRoute('/')({
  component: HomeScreen,
});
