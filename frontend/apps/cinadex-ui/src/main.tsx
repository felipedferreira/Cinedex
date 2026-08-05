import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
// Design tokens and base element styling come from the shared component
// library, and must load before the app's own rules so those can override.
import '@cinedex/ui/tokens.css';
import '@cinedex/ui/base.css';
import './index.css';
import App from './App.tsx';

const rootElement = document.getElementById('root');
if (!rootElement) {
  throw new Error('Root element #root not found');
}

createRoot(rootElement).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
