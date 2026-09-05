import { defineConfig } from 'vite';
import react, { reactCompilerPreset } from '@vitejs/plugin-react';
import babel from '@rolldown/plugin-babel';
import tailwindcss from '@tailwindcss/vite';

// Storybook's `@storybook/react-vite` framework auto-loads this file. Vite applies these plugins to
// the linked `@cinedex/frames`, `@cinedex/shots` and `@cinedex/scenes` workspace source as well
// as to the stories, so library components compile here exactly the way they do in the SPA — same
// React Compiler output, not an approximation of it.
//
// `tailwindcss()` is not optional. Every component in all three libraries is Tailwind-styled, and
// without the plugin `preview.tsx`'s `@cinedex/theme/tailwind.css` import is inert: the stories
// render with correct markup and no styling at all, silently.
export default defineConfig({
  plugins: [
    tailwindcss(),
    react(),
    babel({ presets: [reactCompilerPreset()] }),
  ],
});
