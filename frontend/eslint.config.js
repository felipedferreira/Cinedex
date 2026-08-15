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
        projectService: {
          // ...with one exception: `vitest.config.ts` sits *at* this root, and
          // the root is the one directory with no tsconfig of its own — every
          // other config file is covered by its package's `tsconfig.node.json`.
          // Without this it is the only file the service cannot resolve a
          // project for, and type-aware linting fails on it outright
          // ("was not found by the project service"), taking `npm run lint`
          // with it. `allowDefaultProject` lints it against inferred compiler
          // options instead. Listed file by file on purpose: the service
          // refuses any entry that some tsconfig already covers, and the list
          // is capped, so it stays a short exception rather than a catch-all.
          allowDefaultProject: ['vitest.config.ts'],
        },
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
    files: ['apps/cinedex-app/src/routes/**'],
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
