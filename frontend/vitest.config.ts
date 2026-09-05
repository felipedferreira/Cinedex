import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    projects: [
      'apps/cinedex-app/vite.config.ts',
      'packages/frames/vite.config.ts',
      'packages/shots/vite.config.ts',
      'packages/scenes/vite.config.ts',
    ],
  },
});
