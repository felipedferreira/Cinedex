import { createFileRoute } from '@tanstack/react-router';
import { HomeScreen } from '@cinedex/solution';

export const Route = createFileRoute('/')({
  component: HomeScreen,
});
