import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    projects: [
      'apps/cinedex-app/vite.config.ts',
      'packages/atoms/vite.config.ts',
      'packages/compounds/vite.config.ts',
      'packages/solution/vite.config.ts',
    ],
  },
});
