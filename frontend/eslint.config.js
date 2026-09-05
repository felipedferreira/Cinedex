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
  {
    // Components are arrow functions typed with `FC`, never `function`
    // declarations. Enforced with AST selectors rather than
    // `react/function-component-definition`, because `eslint-plugin-react`
    // still caps its peer range at ESLint 9 and this repo is on 10 — and with
    // selectors rather than `func-style`, because that rule cannot tell a
    // component from a helper and would take `cn`, `resolveBrandSize` and the
    // GSAP timeline builders with it. The capitalised-name convention is the
    // only signal available here, which is why both selectors key off it.
    files: ['**/*.{ts,tsx}'],
    rules: {
      'no-restricted-syntax': [
        'error',
        {
          selector: 'FunctionDeclaration[id.name=/^[A-Z]/]',
          message:
            'React components must be arrow functions: `const Foo: FC<FooProps> = (props) => { … }`.',
        },
        {
          selector:
            'VariableDeclarator[id.name=/^[A-Z]/]:not([id.typeAnnotation]) > ArrowFunctionExpression',
          message:
            'Annotate the component with `FC<Props>` (or `FC<PropsWithChildren<Props>>` when it takes children).',
        },
        // The next three keep component APIs narrow. They are ratchets: all
        // three match zero occurrences today, so they cost nothing now and stop
        // the shapes appearing later. They live here rather than in a local
        // plugin because they are pure syntax — a plugin would be three new
        // files and a test harness that `test:run` could not reach, since
        // `frontend/` is not a workspace.
        {
          selector:
            'TSPropertySignature > TSTypeAnnotation > TSTypeReference[typeName.name=/^(ComponentType|ElementType)$/]',
          message:
            'Passing a component *type* makes the library instantiate a caller-supplied component. Prefer a `ReactNode` slot (the caller builds the element) or Radix `asChild`. The one legitimate port is `SolutionLinkComponent`, which carries a documented eslint-disable.',
        },
        {
          selector: 'TSPropertySignature[key.name=/^render[A-Z]/]',
          message:
            'No render props in the component tiers. If the parent owns the arrangement, take a `ReactNode` slot; a function is only warranted when laziness or a re-invocation key is load-bearing.',
        },
        {
          selector:
            'TSInterfaceDeclaration[id.name=/Props$/] TSPropertySignature[key.name="children"]',
          message:
            'Declare children with `FC<PropsWithChildren<Props>>` rather than a `children` member on the Props interface.',
        },
      ],
    },
  },
  prettier,
]);
