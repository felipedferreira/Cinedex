import { defineConfig } from 'vitest/config';
import react, { reactCompilerPreset } from '@vitejs/plugin-react';
import basicSsl from '@vitejs/plugin-basic-ssl';
import babel from '@rolldown/plugin-babel';

const apiProxyTarget =
  process.env.VITE_API_PROXY_TARGET ?? 'https://localhost:7201';

// PORT is what the Aspire AppHost sets for the endpoint it publishes for this dev server (it also
// passes `--port` on the command line, which wins over this either way). Vite does not read PORT on
// its own. The 9000 fallback keeps a bare `npm run dev` — and the URL the compose stack serves the
// SPA on — unchanged.
const devServerPort = Number(process.env.PORT ?? 9_000);

// Whether the dev server pops a browser tab on start. VITE_OPEN_BROWSER wins when it is set; with it
// unset the answer is `true`, so a bare `npm run dev` is unchanged. The Aspire AppHost sets it to
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
    basicSsl({ name: 'Cinedex local development' }),
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
