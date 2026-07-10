import { defineConfig } from 'vitest/config';
import react, { reactCompilerPreset } from '@vitejs/plugin-react';
import babel from '@rolldown/plugin-babel';
import basicSsl from '@vitejs/plugin-basic-ssl';

const apiProxyTarget =
  process.env.VITE_API_PROXY_TARGET ?? 'https://localhost:7201';

// https://vite.dev/config/
export default defineConfig({
  plugins: [basicSsl(), react(), babel({ presets: [reactCompilerPreset()] })],
  server: {
    port: 9_000,
    strictPort: true,
    open: true,
    proxy: {
      '/movies-svc': {
        target: apiProxyTarget,
        changeOrigin: true,
        secure: false,
      },
    },
  },
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
        'src/test/**',
        'src/main.tsx',
        'src/vite-env.d.ts',
      ],
    },
  },
});
