import { defineConfig } from 'vitest/config';
import react, { reactCompilerPreset } from '@vitejs/plugin-react';
import babel from '@rolldown/plugin-babel';
import tailwindcss from '@tailwindcss/vite';
import { tanstackRouter } from '@tanstack/router-plugin/vite';

const apiProxyTarget =
  process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5186';

// Direct development defaults to Vite's conventional 5173 port. Aspire supplies PORT=9000 for its
// separate full-stack workflow; Vite does not otherwise read PORT on its own.
const devServerPort = Number(process.env.PORT ?? 5_173);

// Whether the dev server pops a browser tab on start. VITE_OPEN_BROWSER wins when it is set; with it
// unset the answer is `true`, so a bare `npm run start` is unchanged. The Aspire AppHost sets it to
// "false" because its dashboard already links here, and a tab per resource on every run is noise.
// Only the listed literals turn it off — an unrecognised value stays on rather than silently
// suppressing the tab.
const openBrowserSetting = process.env.VITE_OPEN_BROWSER;
const openBrowser =
  openBrowserSetting === undefined
    ? true
    : !/^(false|0|off|no)$/i.test(openBrowserSetting);

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    tanstackRouter({ target: 'react', autoCodeSplitting: true }),
    tailwindcss(),
    react(),
    babel({ presets: [reactCompilerPreset()] }),
  ],
  server: {
    port: devServerPort,
    strictPort: true,
    open: openBrowser,
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
