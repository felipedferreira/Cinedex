import js from '@eslint/js';
import globals from 'globals';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import tseslint from 'typescript-eslint';
import prettier from 'eslint-config-prettier';
import { defineConfig, globalIgnores } from 'eslint/config';

export default defineConfig([
  globalIgnores([
    '**/dist',
    '**/coverage',
    '**/storybook-static',
    '**/node_modules',
  ]),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.strictTypeChecked,
      tseslint.configs.stylisticTypeChecked,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      globals: globals.browser,
      parserOptions: {
        // `projectService` resolves the nearest tsconfig per file, so this one
        // config covers every workspace package from the frontend root.
        projectService: true,
        tsconfigRootDir: import.meta.dirname,
      },
    },
  },
  {
    // Barrel files and Storybook stories legitimately export things that are
    // not components, which `react-refresh/only-export-components` flags.
    files: ['packages/*/src/index.ts', '**/*.stories.tsx'],
    rules: {
      'react-refresh/only-export-components': 'off',
    },
  },
  {
    // TanStack Router's file-based routes export a `Route` config object
    // alongside their component, which is the framework's own convention
    // (see https://tanstack.com/router) and not something to restructure
    // around.
    files: ['apps/cinadex-app/src/routes/**'],
    rules: {
      'react-refresh/only-export-components': 'off',
    },
  },
  {
    files: ['**/*.test.{ts,tsx}', '**/test/**'],
    languageOptions: {
      globals: globals.vitest,
    },
  },
  prettier,
]);
