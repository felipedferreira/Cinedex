# Frontend

The standalone SPA for the Movies project. It lives here as an independent application and consumes the backend's OpenAPI spec (`http://localhost:8080/openapi/v1.json`).

## 📁 Layout

```
frontend/
└── cinadex-ui/   # React + TypeScript + Vite single-page application
```

## 🧰 Tech Stack

- **[React 19](https://react.dev/)** with the [React Compiler](https://react.dev/learn/react-compiler)
- **[TypeScript](https://www.typescriptlang.org/)**
- **[Vite](https://vite.dev/)** for dev server, HMR, and builds
- **[Vitest](https://vitest.dev/)** with [Testing Library](https://testing-library.com/) for unit/component tests
- **[ESLint](https://eslint.org/)** with type-aware rules (`typescript-eslint` strict + stylistic, React Hooks)
- **[Prettier](https://prettier.io/)** for consistent code formatting

## 🚀 Getting Started

Prerequisites: [Node.js](https://nodejs.org/) (LTS recommended) and npm.

```bash
cd cinadex-ui
npm install     # install dependencies
npm run dev     # start the dev server with HMR
```

The dev server runs on http://localhost:9000 (configured in [`cinadex-ui/vite.config.ts`](cinadex-ui/vite.config.ts)).

## 📜 Scripts

Run these from the `cinadex-ui/` directory:

| Script                 | Description                                     |
| ---------------------- | ----------------------------------------------- |
| `npm run dev`          | Start the Vite dev server with HMR              |
| `npm run build`        | Type-check and build for production to `dist/`  |
| `npm run preview`      | Preview the production build locally            |
| `npm run lint`         | Lint the project with ESLint                    |
| `npm run lint:fix`     | Lint and auto-fix fixable problems              |
| `npm run format`       | Format all files with Prettier                  |
| `npm run format:check` | Check formatting without writing (CI-friendly)  |
| `npm run test`         | Run the test suite in watch mode (Vitest)       |
| `npm run test:run`     | Run the test suite once (CI-friendly)           |
| `npm run test:ui`      | Run tests in the interactive Vitest UI          |
| `npm run coverage`     | Run tests once and generate a coverage report   |

## 🎨 Linting & Formatting

[ESLint](https://eslint.org/) handles code quality and [Prettier](https://prettier.io/) handles formatting; the two are kept from overlapping via [`eslint-config-prettier`](https://github.com/prettier/eslint-config-prettier).

- ESLint uses **type-aware** rules (`typescript-eslint` `strictTypeChecked` + `stylisticTypeChecked`), so it reads the TypeScript project to catch type-level issues. Config: [`cinadex-ui/eslint.config.js`](cinadex-ui/eslint.config.js).
- Prettier settings live in [`cinadex-ui/.prettierrc.json`](cinadex-ui/.prettierrc.json) (single quotes, no semicolons).

```bash
cd cinadex-ui
npm run lint          # report problems
npm run lint:fix      # report + auto-fix
npm run format        # rewrite files to match Prettier
npm run format:check  # verify formatting (used in CI)
```

CI runs `lint`, `format:check`, `build`, and `test:run` for the frontend, so all four must pass before a change can merge.

## 🧪 Testing

Tests are written with [Vitest](https://vitest.dev/) and [Testing Library](https://testing-library.com/), running in a [jsdom](https://github.com/jsdom/jsdom) environment.

- Configuration lives in the `test` block of [`cinadex-ui/vite.config.ts`](cinadex-ui/vite.config.ts).
- Global setup (jest-dom matchers and DOM cleanup) is in `cinadex-ui/src/test/setup.ts`.
- Test files live next to the code they cover and are named `*.test.ts` / `*.test.tsx`.

```bash
cd cinadex-ui
npm run test        # watch mode during development
npm run test:run    # single run, e.g. in CI
```

### Interactive UI

For a richer development experience, [`@vitest/ui`](https://vitest.dev/guide/ui.html) opens a browser dashboard to explore tests, results, and module graphs:

```bash
cd cinadex-ui
npm run test:ui
```

### Coverage

Coverage is collected with the [V8 provider](https://vitest.dev/guide/coverage.html) and written to `cinadex-ui/coverage/` (git-ignored):

```bash
cd cinadex-ui
npm run coverage
```

The following reporters are configured in [`cinadex-ui/vite.config.ts`](cinadex-ui/vite.config.ts) so the output works both locally and in CI pipelines:

| Reporter    | Output                          | Use                                              |
| ----------- | ------------------------------- | ------------------------------------------------ |
| `text`      | terminal                        | quick summary while developing                   |
| `html`      | `coverage/index.html`           | browsable local report                           |
| `lcov`      | `coverage/lcov.info`            | Codecov, Coveralls, SonarQube, etc.              |
| `cobertura` | `coverage/cobertura-coverage.xml` | GitLab CI, Azure DevOps, Jenkins coverage gates |

## 🔌 Backend

The SPA consumes the backend API. With the backend running (see the [root README](../README.md)), the API is available at:

- **API:** http://localhost:8080
- **OpenAPI Spec:** http://localhost:8080/openapi/v1.json
