import { defineConfig } from 'vite';
import react, { reactCompilerPreset } from '@vitejs/plugin-react';
import babel from '@rolldown/plugin-babel';

// Storybook's `@storybook/react-vite` framework auto-loads this file. Vite applies these plugins to
// the linked `@cinedex/components` workspace source as well as to the stories, so library components
// compile here exactly the way they do in the SPA — same React Compiler output, not an
// approximation of it.
export default defineConfig({
  plugins: [react(), babel({ presets: [reactCompilerPreset()] })],
});
