import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { createRouter, RouterProvider } from '@tanstack/react-router';
// The whole design system in one import: @cinedex/theme's `tailwind.css` pulls in
// the tokens and the base element styling itself, in the cascade-layer order they
// have to be in. This app has no stylesheet of its own — every screen it renders
// comes from @cinedex/scenes and is styled through the theme's utilities. An
// app-specific rule would go in a new `./index.css` imported after this line.
import '@cinedex/theme/tailwind.css';
import { routeTree } from './routeTree.gen';

const router = createRouter({ routeTree });

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}

const rootElement = document.getElementById('root');
if (!rootElement) {
  throw new Error('Root element #root not found');
}

createRoot(rootElement).render(
  <StrictMode>
    <RouterProvider router={router} />
  </StrictMode>,
);
