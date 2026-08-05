import { defineConfig } from 'vitest/config';
import react, { reactCompilerPreset } from '@vitejs/plugin-react';
import babel from '@rolldown/plugin-babel';

// Storybook's `@storybook/react-vite` framework auto-loads this file, which is
// why the React Compiler plugins live here rather than in `.storybook/main.ts`
// — library components then compile exactly the way app components do.
export default defineConfig({
  plugins: [react(), babel({ presets: [reactCompilerPreset()] })],
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
    coverage: {
      provider: 'v8',
      // `text` for the terminal, `html` for local browsing,
      // `lcov` + `cobertura` for CI pipelines (Codecov, SonarQube,
      // GitLab, Azure DevOps, etc.), and `json` + `json-summary` for
      // the GitHub job-summary coverage report action.
      reporter: ['text', 'html', 'lcov', 'cobertura', 'json', 'json-summary'],
      reportsDirectory: './coverage',
      reportOnFailure: true,
      include: ['src/**/*.{ts,tsx}'],
      exclude: [
        'src/**/*.test.{ts,tsx}',
        'src/**/*.stories.tsx',
        'src/test/**',
        'src/index.ts',
      ],
    },
  },
});
